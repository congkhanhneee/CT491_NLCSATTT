using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp2
{
    public partial class Form2 : Form
    {
        private string connectionString = "server=localhost;database=file_tracking;user=root;password=;";
        private DateTime startTime;
        private DateTime endTime;

        public Form2(DateTime start, DateTime end)
        {
            InitializeComponent();
            startTime = start;
            endTime = end;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            LoadStatistics();
            LoadFileList();
        }

        // Hiển thị thống kê
        private void LoadStatistics()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT 
                            COUNT(*) AS total_actions,
                            SUM(CASE WHEN action = 'Tạo' THEN 1 ELSE 0 END) AS created_files,
                            SUM(CASE WHEN action = 'Mở' THEN 1 ELSE 0 END) AS opened_files,
                            SUM(CASE WHEN action = 'Sửa' THEN 1 ELSE 0 END) AS edited_files,
                            SUM(CASE WHEN action = 'Xóa' THEN 1 ELSE 0 END) AS deleted_files,
                            SUM(CASE WHEN action LIKE 'Đổi tên%' THEN 1 ELSE 0 END) AS renamed_files
                        FROM file_changes
                        WHERE timestamp BETWEEN @start AND @end";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@end", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                lblTotalActions.Text = $"Tổng số hành động: {reader.GetInt32("total_actions")}";
                                lblCreatedFiles.Text = $"Số file được tạo: {reader.GetInt32("created_files")}";
                                lblOpenedFiles.Text = $"Số file được mở: {reader.GetInt32("opened_files")}";
                                lblEditedFiles.Text = $"Số file được sửa: {reader.GetInt32("edited_files")}";
                                lblDeletedFiles.Text = $"Số file bị xóa: {reader.GetInt32("deleted_files")}";
                                lblRenamedFiles.Text = $"Số file bị đổi tên: {reader.GetInt32("renamed_files")}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải thống kê: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Nạp danh sách tệp vào ComboBox
        private void LoadFileList()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT DISTINCT file_path FROM file_changes
                        WHERE timestamp BETWEEN @start AND @end";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@end", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            cboFiles.Items.Clear();
                            while (reader.Read())
                            {
                                cboFiles.Items.Add(reader.GetString("file_path"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Khi chọn tệp và nhấn "Chi tiết tệp"
        private void btnDetails_File_Click(object sender, EventArgs e)
        {
            if (cboFiles.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một tệp!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedFile = cboFiles.SelectedItem.ToString();
            LoadFileHistory(selectedFile);
        }

        // Hiển thị lịch sử của tệp đã chọn
        private void LoadFileHistory(string fileName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                    WITH RECURSIVE file_history AS (
                        SELECT 
                            id, 
                            file_path, 
                            SUBSTRING_INDEX(file_path, '\\', -1) AS file_name,
                            action,
                            process_name,
                            timestamp,
                        CASE 
                            WHEN action LIKE 'Đổi tên từ %' THEN SUBSTRING(action, 12) 
                            ELSE NULL 
                        END AS old_name
                    FROM file_changes
                    WHERE file_path LIKE CONCAT('%', @fileName)

                    UNION ALL

                    SELECT 
                        fc.id, 
                        fc.file_path, 
                        SUBSTRING_INDEX(fc.file_path, '\\', -1) AS file_name,
                        fc.action,
                        fc.process_name,
                        fc.timestamp,
                        CASE 
                            WHEN fc.action LIKE 'Đổi tên từ %' THEN SUBSTRING(fc.action, 12) 
                            ELSE NULL 
                        END AS old_name
                    FROM file_changes fc
                    INNER JOIN file_history fh ON fh.old_name = SUBSTRING_INDEX(fc.file_path, '\\', -1)
                )
                SELECT * FROM file_history ORDER BY timestamp DESC;";


                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@fileName", fileName);

                        DataTable dt = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.Fill(dt);
                        }

                        dgvFileHistory.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải lịch sử tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close(); // Đóng Form2
        }
    }
}
