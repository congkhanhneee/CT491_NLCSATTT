using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using MySql.Data.MySqlClient;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices.ComTypes;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private FileSystemWatcher watcher;
        private string connectionString = "server=localhost;database=file_manager;user=root;password=;";

        // Import từ kernel32.dll và psapi.dll
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
            FileEventHandler(newPath, $"Đổi tên từ {Path.GetFileName(oldPath)}");
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
        private void FileEventHandler(string filePath, string action, string oldPath=null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                string processName = GetProcessUsingFile(filePath);
                string oldName = GetOldName(oldPath);
                string fileExtension = Path.GetExtension(filePath); // Lấy phần mở rộng (VD: ".txt")

                int? idIdentity = null;

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Truy vấn để lấy id_identity nếu phần mở rộng tồn tại trong bảng identity
                    string identityQuery = "SELECT id_identity FROM identity WHERE id_identity = @extension";
                    using (MySqlCommand cmd = new MySqlCommand(identityQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@extension", fileExtension);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            idIdentity = Convert.ToInt32(result);
                        }
                    }

                    // Chèn dữ liệu vào file_changes, bao gồm id_identity nếu có
                    string insertQuery = "INSERT INTO file (name_file, id_identity) " +
                                         "VALUES (@name_file, @id_identity)";
                    string fileName = Path.GetFileName(filePath);

                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@name_file", fileName);
                        cmd.Parameters.AddWithValue("@id_identity",fileExtension);
                        cmd.ExecuteNonQuery();
                    }

                    // Lấy ID của file vừa chèn vào bảng file (để chèn vào bảng DETAIL_FILE)
                    string selectFileIdQuery = "SELECT LAST_INSERT_ID()";
                    int fileId = Convert.ToInt32(new MySqlCommand(selectFileIdQuery, conn).ExecuteScalar());

                    // Chèn vào bảng DETAIL_FILE
                    string insertDetailFileQuery = "INSERT INTO DETAIL_FILE (date_action, id_file, id_action, id_user, old_path, size, new_path, old_name, procces_name) " +
                                                   "VALUES (NOW(), @id_file, @id_action,1,@old_path, @size, @new_path, @old_name, @proccess_name)";

                    using (MySqlCommand cmd = new MySqlCommand(insertDetailFileQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id_file", fileId);   // ID file từ bảng FILE
                        cmd.Parameters.AddWithValue("@id_action", action);  // ID hành động (Cần ánh xạ hành động -> ID)
                        cmd.Parameters.AddWithValue("@old_path", oldPath ?? (object)DBNull.Value); // Đường dẫn cũ (nếu có)
                        cmd.Parameters.AddWithValue("@size", GetFileSize(filePath)); // Kích thước file (cần lấy trước đó)
                        cmd.Parameters.AddWithValue("@new_path", filePath); // Đường dẫn mới
                        cmd.Parameters.AddWithValue("@old_name", oldName ?? (object)DBNull.Value); // Tên cũ (nếu có)
                        cmd.Parameters.AddWithValue("@process_name", processName); // Tên tiến trình
                        cmd.ExecuteNonQuery();
                    }
                }

                // Cập nhật giao diện
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
                MessageBox.Show("Có thay đổi xảy ra!", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                string query = "SELECT df.id_file, f.name_file, df.date_action, df.old_name " +
                                 "FROM DETAIL_FILE df " +
                                 "JOIN file f ON df.id_file = f.id_file " +
                                "WHERE df.date_action BETWEEN @start AND @end " +
                                "ORDER BY df.date_action DESC";

                DataTable dt = new DataTable();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", dtpStart.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@end", dtpEnd.Value.ToString("yyyy-MM-dd HH:mm:ss"));

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }

                dataGridView1.DataSource = dt;
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
