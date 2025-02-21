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
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

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

            watcher.Created += (s, e) => FileEventHandler(e.FullPath, "tạo");
            watcher.Changed += (s, e) => FileEventHandler(e.FullPath, "sửa");
            watcher.Deleted += (s, e) => FileEventHandler(e.FullPath, "xóa");
            watcher.Renamed += (s, e) => FileRenamedHandler(e.OldFullPath, e.FullPath);

            listBox1.Items.Add("Đang theo dõi: " + folderPath);
        }

        private void FileRenamedHandler(string oldPath, string newPath)
        {
            FileEventHandler(newPath, $"đổi tên");
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
        private void FileEventHandler(string filePath, string action, string oldPath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                string processName = GetProcessUsingFile(filePath);
                string oldName = GetOldName(oldPath);
                string fileExtension = Path.GetExtension(filePath);
                int idAction = -1;

                string idIdentity = null;

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
                            idIdentity = result.ToString(); // Chuyển thành string
                        }
                    }

                    // Kiểm tra giá trị trước khi chèn
                    MessageBox.Show($"File Extension: {fileExtension}\nID Identity: {idIdentity}");

                    if (idIdentity != null) 
                    {
                        string insertQuery = "INSERT INTO file (name_file, id_identity) VALUES (@name_file, @id_identity)";
                        string fileName = Path.GetFileName(filePath);

                        using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@name_file", fileName);
                            cmd.Parameters.AddWithValue("@id_identity", idIdentity); // Dùng idIdentity từ DB
                            cmd.ExecuteNonQuery();
                        }

                        // Lấy ID của file vừa chèn
                        string selectFileIdQuery = "SELECT LAST_INSERT_ID()";
                        int fileId = Convert.ToInt32(new MySqlCommand(selectFileIdQuery, conn).ExecuteScalar());

                        // Lấy id_action từ bảng action
                        
                        using (MySqlCommand cmd = new MySqlCommand("SELECT ID_ACTION FROM action WHERE name_action = @action", conn))
                        {
                            cmd.Parameters.AddWithValue("@action", action);
                            object result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                idAction = Convert.ToInt32(result);
                            }
                            else
                            {
                                Console.WriteLine("Không tìm thấy ID_ACTION cho action: " + action);
                            }
                        }

                        if (idAction == -1)
                        {
                            MessageBox.Show($"Không tìm thấy ID_ACTION cho hành động: {action}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Chèn vào bảng DETAIL_FILE
                        string insertDetailFileQuery = "INSERT INTO DETAIL_FILE (date_action, id_file, id_action, id_user, old_path, size, new_path, old_name, process_name) " +
                                                       "VALUES (NOW(), @id_file, @id_action, 1, @old_path, @size, @new_path, @old_name, @process_name)";

                        using (MySqlCommand cmd = new MySqlCommand(insertDetailFileQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@id_file", fileId);
                            cmd.Parameters.AddWithValue("@id_action", idAction);
                            cmd.Parameters.AddWithValue("@old_path", oldPath ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@size", GetFileSize(filePath));
                            cmd.Parameters.AddWithValue("@new_path", filePath);
                            cmd.Parameters.AddWithValue("@old_name", oldName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@process_name", processName);
                            cmd.ExecuteNonQuery();
                        }

                    }
                    else
                    {
                        MessageBox.Show($"Phần mở rộng '{fileExtension}' không tồn tại trong bảng identity.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                string query = "SELECT df.id_file, f.name_file, df.date_action, df.new_path " +
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
