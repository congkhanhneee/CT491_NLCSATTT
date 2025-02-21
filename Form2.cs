using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp2
{
    public partial class Form2 : Form
    {
        private string connectionString = "server=localhost;database=file_manager;user=root;password=;";
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
                                SUM(CASE WHEN A.NAME_ACTION = 'Tạo' THEN 1 ELSE 0 END) AS created_files,
                                SUM(CASE WHEN A.NAME_ACTION = 'Mở' THEN 1 ELSE 0 END) AS opened_files,
                                SUM(CASE WHEN A.NAME_ACTION = 'Sửa' THEN 1 ELSE 0 END) AS edited_files,
                                SUM(CASE WHEN A.NAME_ACTION = 'Xóa' THEN 1 ELSE 0 END) AS deleted_files,
                                SUM(CASE WHEN A.NAME_ACTION LIKE 'Đổi tên%' THEN 1 ELSE 0 END) AS renamed_files
                        FROM DETAIL_FILE D
                        JOIN ACTION A ON D.ID_ACTION = A.ID_ACTION
                        WHERE D.DATE_ACTION BETWEEN @start AND @end;";
;

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
                        SELECT DISTINCT f.name_file FROM file f JOIN DETAIL_FILE df
                        ON f.id_file=df.id_file
                        WHERE df.date_action BETWEEN @start AND @end";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@end", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            cboFiles.Items.Clear();
                            while (reader.Read())
                            {
                                cboFiles.Items.Add(reader.GetString("name_file"));
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
                    SELECT 
                        df.DATE_ACTION,
                        f.NAME_FILE AS CURRENT_NAME,
                        df.OLD_NAME,
                        df.OLD_PATH,
                        df.NEW_PATH,
                        a.NAME_ACTION,
                        u.NAME_USER
                    FROM DETAIL_FILE df
                    JOIN FILE f ON df.ID_FILE = f.ID_FILE
                    JOIN ACTION a ON df.ID_ACTION = a.ID_ACTION
                    JOIN USER u ON df.ID_USER = u.ID_USER
                    WHERE f.NAME_FILE = @file_name
                    ORDER BY df.DATE_ACTION DESC;";


                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@file_name", fileName);

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
