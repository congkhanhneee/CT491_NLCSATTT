using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Runtime.InteropServices;
using System.Collections;


namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private FileSystemWatcher watcher;
        private string connectionString = "server=localhost;database=file_tracking;user=root;password=;";

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
            try
            {
                string folderPath = @"D:\TAILIEU\Test"; //Thư mục ghi nhận
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
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi khởi tạo FileSystemWatcher: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        [DllImport("ntdll.dll")]
        public static extern int NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, out int ReturnLength);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        private List<string> GetOpenFilesByProcess(int processId)
        {
            List<string> openFiles = new List<string>();

            try
            {
                Process process = Process.GetProcessById(processId);
                if (process != null)
                {
                    string processName = process.ProcessName;
                    openFiles.Add(process.MainModule.FileName);
                }
            }
            catch { }

            return openFiles;
        }

        private void FileRenamedHandler(string oldPath, string newPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(newPath))
                {
                    FileEventHandler(newPath, $"Đổi tên từ {Path.GetFileName(oldPath)}");
                }
                else
                {
                    FileEventHandler(oldPath, "Xóa");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xử lý đổi tên: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetProcessUsingFile(string filePath)
        {
            string query = "SELECT ProcessId, Name FROM Win32_Process";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject process in searcher.Get())
                {
                    try
                    {
                        int processId = Convert.ToInt32(process["ProcessId"]);
                        using (Process proc = Process.GetProcessById(processId))
                        {
                            if (!proc.HasExited && proc.MainModule != null)
                            {
                                string processName = proc.ProcessName;
                                List<string> openFiles = GetOpenFilesByProcess(processId);
                                if (openFiles.Contains(filePath))
                                {
                                    return processName;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            return "Không xác định";
        }


        private void FileEventHandler(string filePath, string action)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                string processName = "Không xác định";

                foreach (Process process in Process.GetProcesses())
                {
                    List<string> openFiles = GetOpenFilesByProcess(process.Id);
                    if (openFiles.Contains(filePath))
                    {
                        processName = process.ProcessName;
                        break;
                    }
                }

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



        private void LoadData()
        {
            try
            {
                MessageBox.Show("LoadData() được gọi", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
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




        private void btnLoadData_Click(object sender, EventArgs e)
        {
            try
            {
                string startTime = dtpStart.Value.ToString("yyyy-MM-dd HH:mm:ss");
                string endTime = dtpEnd.Value.ToString("yyyy-MM-dd HH:mm:ss");
                MessageBox.Show($"Lọc dữ liệu từ: {startTime} đến {endTime}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                string query = "SELECT id, file_path, action, timestamp FROM file_changes " +
                               "WHERE timestamp BETWEEN STR_TO_DATE(@start, '%Y-%m-%d %H:%i:%s') " +
                               "AND STR_TO_DATE(@end, '%Y-%m-%d %H:%i:%s') " +
                               "ORDER BY timestamp DESC";

                DataTable dt = new DataTable();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        //string startTime = dtpStart.Value.ToString("yyyy-MM-dd HH:mm:ss");
                        //string endTime = dtpEnd.Value.ToString("yyyy-MM-dd HH:mm:ss");
                        MessageBox.Show($"Lọc từ: {startTime} đến {endTime}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        cmd.Parameters.AddWithValue("@start", startTime);
                        cmd.Parameters.AddWithValue("@end", endTime);

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
                MessageBox.Show("Lỗi khi tải dữ liệu theo khoảng thời gian: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btnLoadData_Click_1(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}
