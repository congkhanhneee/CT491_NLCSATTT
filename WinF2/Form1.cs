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
using System.Management;
using Microsoft.Win32;

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

        private string GetCurrentUser()
        {
            return Environment.UserName; // Lấy tên user hiện tại
        }

        private void FileEventHandler(string filePath, string action, string oldPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                string oldName = oldPath != null ? GetOldName(oldPath) : null;
                string fileExtension = Path.GetExtension(filePath);
                string idAction = null;
                string idIdentity = null;
                string idOldFile = null;

                string currentUser = GetCurrentUser(); // Lấy user hiện tại
                string idUser = null;

                // Kiểm tra xem user đã tồn tại trong DB chưa
                var userFilter = Builders<BsonDocument>.Filter.Eq("name", currentUser);
                var userDocument = userCollection.Find(userFilter).FirstOrDefault();

                if (userDocument != null)
                {
                    idUser = userDocument["_id"].ToString(); // Nếu user tồn tại, lấy ID user
                }
                else
                {
                    // Nếu user chưa tồn tại, thêm mới vào bảng user
                    var newUser = new BsonDocument
                    {
                        { "name", currentUser },
                        { "role", "default" } // Gán vai trò mặc định, có thể thay đổi theo yêu cầu
                    };

                    userCollection.InsertOne(newUser);
                    idUser = newUser["_id"].ToString();
                    string userLogFile = $"{DateTime.Now:dd_MM_yyyy}_user.txt";
                    string userLogEntry = newUser.ToJson();
                    WriteToFile(userLogFile, userLogEntry);

                }

                // Kiểm tra phần mở rộng file trong bảng identity
                var identityFilter = Builders<BsonDocument>.Filter.Eq("_id", fileExtension);
                var identityDocument = identityCollection.Find(identityFilter).FirstOrDefault();
                if (identityDocument != null)
                {
                    idIdentity = identityDocument["_id"].ToString();
                }
                else
                {
                    MessageBox.Show($"Phần mở rộng '{fileExtension}' không tồn tại trong bảng identity.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string dateStr = DateTime.Now.ToString("dd_MM_yyyy");
                string fileLog = $"{dateStr}_file.txt";
                string detailFileLog = $"{dateStr}_detailFile.txt";

                if (idIdentity != null)
                {
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

                    // Ghi nhận thông tin file vào log
                    var fileDocument = new BsonDocument
                    {
                        { "name", Path.GetFileName(filePath) },
                        { "id_identity", idIdentity }
                    };

                    // Kiểm tra kết nối MongoDB trước khi ghi vào database
                    bool isDatabaseConnected = TestDatabaseConnection();

                    if (isDatabaseConnected)
                    {
                        fileCollection.InsertOne(fileDocument);
                    }

                    ObjectId fileId = fileDocument["_id"].AsObjectId;

                    var actionFilter = Builders<BsonDocument>.Filter.Eq("name", action);
                    var actionDocument = actionCollection.Find(actionFilter).FirstOrDefault();
                    if (actionDocument != null)
                    {
                        idAction = actionDocument["_id"].ToString();
                    }

                    var detailFileDocument = new BsonDocument
                    {
                        { "datetime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                        { "id_file", fileId },
                        { "id_action", idAction },
                        { "id_user", idUser },
                        { "oldname", oldName ?? "Không xác định" },
                        { "old_path", oldPath ?? "Không xác định" },
                        { "size", GetFileSize(filePath) },
                        { "new_path", filePath },
                        { "user", currentUser } // Ghi user thực hiện thao tác
                    };

                    if (idOldFile != null)
                    {
                        detailFileDocument.Add("id_old_file", idOldFile);
                    }

                    if (isDatabaseConnected)
                    {
                        detailFileCollection.InsertOne(detailFileDocument);
                    }

                    // Ghi vào file TXT
                    WriteToFile(fileLog, fileDocument.ToJson());
                    WriteToFile(detailFileLog, detailFileDocument.ToJson());

                    Invoke(new Action(() => listBox1.Items.Insert(0, $"{DateTime.Now}: {action} - {filePath} - User: {currentUser}")));
                    Invoke(new Action(LoadData));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi ghi vào DB: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool TestDatabaseConnection()
        {
            try
            {
                var client = new MongoClient("mongodb://localhost:27017");
                var database = client.GetDatabase("FileManagement");
                database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait();
                return true;
            }
            catch
            {
                return false;
            }
        }




        public static List<string> GetOpenFiles(string directoryPath)
        {
            List<string> result = new List<string>();

            try
            {
                string query = "SELECT ProcessId, Name, CommandLine FROM Win32_Process";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    foreach (ManagementObject process in searcher.Get())
                    {
                        string processName = process["Name"]?.ToString();
                        string commandLine = process["CommandLine"]?.ToString();

                        if (!string.IsNullOrEmpty(commandLine))
                        {
                            foreach (string filePath in Directory.GetFiles(directoryPath))
                            {
                                if (commandLine.IndexOf(filePath, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    result.Add($"{processName}");
                                   // result.Add($"{processName} đang mở: {filePath}");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy danh sách file mở: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return result;
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

                //MessageBox.Show($"Tải thành công {resultList.Count} bản ghi!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void btn_General_Directory_Click(object sender, EventArgs e)
        {
            Form3 detail_General_Directory = new Form3();
            detail_General_Directory.Show();
        }

        private void btnChart_Click(object sender, EventArgs e)
        {
            Form4 detail_Chart = new Form4();
            detail_Chart.Show();
        }
    }
}