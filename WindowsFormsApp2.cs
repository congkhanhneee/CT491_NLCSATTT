using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
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

        private void FileEventHandler(string filePath, string action)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return;

                string query = "INSERT INTO file_changes (file_path, action, timestamp) VALUES (@file_path, @action, NOW())";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@file_path", filePath);
                        cmd.Parameters.AddWithValue("@action", action);
                        cmd.ExecuteNonQuery();
                    }
                }

                Invoke(new Action(() => listBox1.Items.Insert(0, $"{DateTime.Now}: {action} - {filePath}")));
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
                string query = "SELECT id, file_path, action, timestamp FROM file_changes ORDER BY timestamp DESC";
                DataTable dt = new DataTable();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        adapter.Fill(dt);
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
                string query = "SELECT id, file_path, action, timestamp FROM file_changes WHERE timestamp BETWEEN @start AND @end ORDER BY timestamp DESC";
                DataTable dt = new DataTable();

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", dtpStart.Value);
                        cmd.Parameters.AddWithValue("@end", dtpEnd.Value);

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
    }
}
