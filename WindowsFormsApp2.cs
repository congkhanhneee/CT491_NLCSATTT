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
                dtpStart.Value = DateTime.Now.Date; // Mặc định từ đầu ngày hiện tại
                dtpEnd.Value = DateTime.Now;        // Đến thời điểm hiện tại
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
                string folderPath = @"D:\TAILIEU\CT182_UML";
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

                listBox1.Items.Add("🔍 Đang theo dõi: " + folderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi khởi tạo FileSystemWatcher: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                                string[] openFiles = GetOpenFilesByProcess(processId);
                                if (openFiles.Contains(filePath))
                                {
                                    return processName;
                                }
                            }
                        }
                    }
                    catch { } // Bỏ qua lỗi quyền truy cập
                }
            }
            return "Không xác định";
        }

        private string[] GetOpenFilesByProcess(int processId)
        {
            List<string> files = new List<string>();
            try
            {
                using (Process proc = Process.GetProcessById(processId))
                {
                    if (!proc.HasExited && proc.MainModule != null)
                    {
                        files.Add(proc.MainModule.FileName);
                    }
                }
            }
            catch { } // Bỏ qua lỗi truy cập
            return files.ToArray();
        }

        private void FileEventHandler(string filePath, string action)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                string appName = GetProcessUsingFile(filePath); // Lấy ứng dụng mở tệp

                string query = "INSERT INTO file_changes (file_path, action, timestamp, APP_NAME) VALUES (@file_path, @action, NOW(), @app_name)";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@file_path", filePath);
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.Parameters.AddWithValue("@app_name", appName);
                        cmd.ExecuteNonQuery();
                    }
                }

                Invoke(new Action(() => listBox1.Items.Insert(0, $"{DateTime.Now}: {action} - {filePath} - {appName}")));
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
                string query = "SELECT id, file_path, action, timestamp, APP_NAME FROM file_changes " +
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
                        string startTime = dtpStart.Value.ToString("yyyy-MM-dd HH:mm:ss");
                        string endTime = dtpEnd.Value.ToString("yyyy-MM-dd HH:mm:ss");

                        // Hiển thị giá trị thời gian để kiểm tra
                        MessageBox.Show($"Lọc từ: {startTime} đến {endTime}", "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        cmd.Parameters.AddWithValue("@start", startTime);
                        cmd.Parameters.AddWithValue("@end", endTime);

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }

                // Hiển thị dữ liệu lên DataGridView
                dataGridView1.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu theo khoảng thời gian: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btnLoadData_Click_1(object sender, EventArgs e)
        {

        }
    }
}
