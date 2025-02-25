using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private FileSystemWatcher watcher;
        private IMongoCollection<BsonDocument> actionCollection;
        private IMongoCollection<BsonDocument> userCollection;
        private IMongoCollection<BsonDocument> roleCollection;
        private IMongoCollection<BsonDocument> identityCollection;
        private IMongoCollection<BsonDocument> fileTypeCollection;
        private IMongoCollection<BsonDocument> fileCollection;
        private IMongoCollection<BsonDocument> detailFileCollection;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("psapi.dll")]
        public static extern bool EnumProcesses([Out] int[] processIds, int arraySizeBytes, out int bytesCopied);

        [DllImport("psapi.dll")]
        public static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[] moduleHandles, int arraySizeBytes, out int bytesCopied);

        [DllImport("psapi.dll", CharSet = CharSet.Auto)]
        public static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpFilename, int nSize);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                dtpStart.Value = DateTime.Now.Date;
                dtpEnd.Value = DateTime.Now;
                InitWatcher();
               // LoadData();
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // Kết nối MongoDB
                var client = new MongoClient("mongodb://localhost:27017");
                var database = client.GetDatabase("FileManagement");
                actionCollection = database.GetCollection<BsonDocument>("action");
                userCollection = database.GetCollection<BsonDocument>("user");
                roleCollection = database.GetCollection<BsonDocument>("role");
                identityCollection = database.GetCollection<BsonDocument>("identity");
                fileTypeCollection = database.GetCollection<BsonDocument>("file_type");
                fileCollection = database.GetCollection<BsonDocument>("file");
                detailFileCollection = database.GetCollection<BsonDocument>("detail_file");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi khởi động: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitWatcher()
        {
            string folderPath = @"D:\TAILIEU\Test"; // Thư mục giám sát
            if (!Directory.Exists(folderPath))
            {
                MessageBox.Show("Thư mục không tồn tại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            watcher = new FileSystemWatcher(folderPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) => FileEventHandler(e.FullPath, "Tạo");
            watcher.Changed += (s, e) => FileEventHandler(e.FullPath, "Sửa");
            watcher.Deleted += (s, e) => FileEventHandler(e.FullPath, "Xóa");
            watcher.Renamed += (s, e) => FileRenamedHandler(e.OldFullPath, e.FullPath);

            listBox1.Items.Add("Đang theo dõi: " + folderPath);
        }

        private void FileRenamedHandler(string oldPath, string newPath)
        {
            FileEventHandler(newPath, $"Đổi tên", oldPath);
        }

        private string GetOldName(string oldPath)
        {
            return Path.GetFileName(oldPath);
        }

        long GetFileSize(string filePath)
        {
            if (File.Exists(filePath))
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Length; // Kích thước tính bằng byte
            }
            return 0; // Trả về 0 nếu file không tồn tại
        }

        private void WriteToFile(string fileName, string content)
        {
            string directoryPath = @"D:\TAILIEU\CT491_NLCSATTT\Logs";
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string filePath = Path.Combine(directoryPath, fileName);
            using (StreamWriter writer = new StreamWriter(filePath, true, Encoding.UTF8))
            {
                writer.WriteLine(content);
            }
        }


        private void FileEventHandler(string filePath, string action, string oldPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                string processName = GetProcessUsingFile(filePath);
                string oldName = oldPath != null ? GetOldName(oldPath) : null;
                string fileExtension = Path.GetExtension(filePath);
                string idAction = null;
                string idIdentity = null;
                string idOldFile = null;

                // Tìm id_identity dựa trên phần mở rộng
                var identityFilter = Builders<BsonDocument>.Filter.Eq("_id", fileExtension);
                var identityDocument = identityCollection.Find(identityFilter).FirstOrDefault();
                if (identityDocument != null)
                {
                    idIdentity = identityDocument["_id"].ToString();
                }

                string dateStr = DateTime.Now.ToString("dd_MM_yyyy");
                string fileLog = $"{dateStr}_file.txt";
                string detailFileLog = $"{dateStr}_detailFile.txt";

                if (idIdentity != null)
                {
                    // Tìm id_old_file nếu không phải hành động "Tạo"
                    if (action != "Tạo")
                    {
                        string searchName = oldPath != null ? Path.GetFileName(oldPath) : Path.GetFileName(filePath);
                        var oldFileFilter = Builders<BsonDocument>.Filter.Eq("name", searchName);
                        var oldFileDocument = fileCollection.Find(oldFileFilter).FirstOrDefault();
                        if (oldFileDocument != null)
                        {
                            idOldFile = oldFileDocument["_id"].ToString();
                        }
                    }

                    // Tạo bản ghi mới trong fileCollection
                    var fileDocument = new BsonDocument
                    {
                        { "name", Path.GetFileName(filePath) },
                        { "id_identity", idIdentity }
                    };
                    fileCollection.InsertOne(fileDocument);
                    ObjectId fileId = fileDocument["_id"].AsObjectId;

                    // Tìm id_action
                    var actionFilter = Builders<BsonDocument>.Filter.Eq("name", action);
                    var actionDocument = actionCollection.Find(actionFilter).FirstOrDefault();
                    if (actionDocument != null)
                    {
                        idAction = actionDocument["_id"].ToString();
                    }

                    // Tìm id_user
                    var userFilter = Builders<BsonDocument>.Filter.Eq("name", "default_user");
                    var userDocument = userCollection.Find(userFilter).FirstOrDefault();
                    string idUser = userDocument?["_id"].AsString ?? "1";

                    // Tạo bản ghi trong detail_file
                    var detailFileDocument = new BsonDocument
                    {
                        { "datetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "id_file", fileId },
                        { "id_action", idAction },
                        { "id_user", idUser },
                        { "oldname", oldName ?? "Không xác định" },
                        { "old_path", oldPath ?? "Không xác định" },
                        { "size", GetFileSize(filePath) },
                        { "new_path", filePath }
                    };

                    // Thêm id_old_file nếu có (cho các hành động không phải "Tạo")
                    if (idOldFile != null)
                    {
                        detailFileDocument.Add("id_old_file", idOldFile);
                    }

                    detailFileCollection.InsertOne(detailFileDocument);

                    // Ghi dữ liệu vào file txt
                    WriteToFile(fileLog, fileDocument.ToJson());
                    WriteToFile(detailFileLog, detailFileDocument.ToJson());
                }
                else
                {
                    MessageBox.Show($"Phần mở rộng '{fileExtension}' không tồn tại trong bảng identity.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Invoke(new Action(() => listBox1.Items.Insert(0, $"{DateTime.Now}: {action} - {filePath} - {processName} - ID: {idIdentity}")));
                Invoke(new Action(LoadData));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi ghi vào DB: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private string GetProcessUsingFile(string filePath)
        {
            int[] processIds = new int[1024];
            if (!EnumProcesses(processIds, processIds.Length * sizeof(int), out int bytesCopied))
                return "Không xác định";

            int numProcesses = bytesCopied / sizeof(int);
            foreach (int pid in processIds.Take(numProcesses))
            {
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
                if (hProcess == IntPtr.Zero)
                    continue;

                try
                {
                    IntPtr[] moduleHandles = new IntPtr[1024];
                    if (EnumProcessModules(hProcess, moduleHandles, moduleHandles.Length * IntPtr.Size, out int bytesNeeded))
                    {
                        int numModules = bytesNeeded / IntPtr.Size;
                        for (int i = 0; i < numModules; i++)
                        {
                            StringBuilder moduleName = new StringBuilder(1024);
                            if (GetModuleFileNameEx(hProcess, moduleHandles[i], moduleName, moduleName.Capacity) > 0)
                            {
                                if (moduleName.ToString().Equals(filePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    Process process = Process.GetProcessById(pid);
                                    return process.ProcessName;
                                }
                            }
                        }
                    }
                }
                catch
                {

                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }

            return "Không xác định";
        }

        private void LoadData()
        {
            try
            {
                DateTime start = dtpStart.Value;
                DateTime end = dtpEnd.Value;

                // Chuyển đổi sang định dạng chuỗi giống MongoDB
                var filter = Builders<BsonDocument>.Filter.Gte("datetime", start.ToString("yyyy-MM-dd HH:mm:ss")) &
                             Builders<BsonDocument>.Filter.Lte("datetime", end.ToString("yyyy-MM-dd HH:mm:ss"));

                var detailFiles = detailFileCollection.Find(filter).ToList();

                if (detailFiles.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu trong MongoDB trong khoảng thời gian đã chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var files = fileCollection.Find(new BsonDocument()).ToList();

                var result = from df in detailFiles
                             join f in files on df["id_file"].AsObjectId equals f["_id"].AsObjectId
                             select new
                             {
                                 ID_File = df["id_file"].ToString(),
                                 File_Name = f["name"].ToString(),
                                 Date_Action = df["datetime"].ToString(),
                                 Old_Path = df.Contains("old_path") ? df["old_path"].ToString() : "Không xác định",
                                 New_Path = df.Contains("new_path") ? df["new_path"].ToString() : "Không xác định",
                                 Old_Name = df.Contains("oldname") ? df["oldname"].ToString() : "Không có",
                                 Size = df.Contains("size") ? df["size"].ToString() : "Không có",
                                 Action = df.Contains("id_action") ? df["id_action"].ToString() : "Không có"
                             };

                var resultList = result.ToList();

                if (resultList.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu sau khi xử lý! Kiểm tra dữ liệu trong bảng file.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                dataGridView1.DataSource = null;
                dataGridView1.DataSource = resultList;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView1.Refresh();

                MessageBox.Show($"Tải thành công {resultList.Count} bản ghi!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        private void btnLoadData_Click_1(object sender, EventArgs e)
        {
            LoadData();
        }

        private void btnDetails_Click(object sender, EventArgs e)
        {
            DateTime start = dtpStart.Value;
            DateTime end = dtpEnd.Value;

            Form2 detailsForm = new Form2(start, end);
            detailsForm.Show();
        }
    }
}
