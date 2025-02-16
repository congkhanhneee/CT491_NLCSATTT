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

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private FileSystemWatcher watcher;
        private string connectionString = "server=localhost;database=file_tracking;user=root;password=;";

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

        private void FileEventHandler(string filePath, string action)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                string processName = GetProcessUsingFile(filePath);

                string query = "INSERT INTO file_changes (file_path, action, timestamp, process_name) VALUES (@file_path, @action, NOW(), @app_name)";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@file_path", filePath);
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@app_name", processName);
                        cmd.ExecuteNonQuery();
                    }
                }

                Invoke(new Action(() => listBox1.Items.Insert(0, $"{DateTime.Now}: {action} - {filePath} - {processName}")));
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
                string query = "SELECT id, file_path, action, timestamp, process_name FROM file_changes " +
                               "WHERE timestamp BETWEEN STR_TO_DATE(@start, '%Y-%m-%d %H:%i:%s') " +
                               "AND STR_TO_DATE(@end, '%Y-%m-%d %H:%i:%s') ORDER BY timestamp DESC";

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
    }
}
