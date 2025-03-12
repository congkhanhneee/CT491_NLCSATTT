using MongoDB.Bson;
using MongoDB.Driver;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form4 : Form
    {
        private readonly IMongoCollection<BsonDocument> identityCollection;
        private readonly IMongoCollection<BsonDocument> detailFileCollection;
        private readonly IMongoCollection<BsonDocument> actionCollection;
        private bool isDatabaseConnected = false;

        public Form4()
        {
            InitializeComponent();

            try
            {
                var client = new MongoClient("mongodb://localhost:27017");
                var database = client.GetDatabase("FileManagement");

                identityCollection = database.GetCollection<BsonDocument>("identity");
                detailFileCollection = database.GetCollection<BsonDocument>("detail_file");
                actionCollection = database.GetCollection<BsonDocument>("action");

                isDatabaseConnected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể kết nối CSDL: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            if (!isDatabaseConnected) return;
            LoadUsers();
        }

        private void LoadUsers()
        {
            if (!isDatabaseConnected) return;

            var users = detailFileCollection.Distinct<string>("user", new BsonDocument()).ToList();
            cbUserFilter.Items.Clear();
            cbUserFilter.Items.Add("Tất cả");
            cbUserFilter.Items.AddRange(users.ToArray());
            cbUserFilter.SelectedIndex = 0;
        }

        private void btnLoadFileChart_Click(object sender, EventArgs e)
        {
            DrawFileTypeChart();
        }

        private void btnLoadActionChart_Click(object sender, EventArgs e)
        {
            DrawActionChart();
        }

        private void DrawFileTypeChart()
        {
            string directoryPath = @"D:\TAILIEU\CT211_ANM";

            if (!Directory.Exists(directoryPath))
            {
                MessageBox.Show("Thư mục không tồn tại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                                .Select(f => Path.GetExtension(f).ToLower())
                                .GroupBy(ext => ext)
                                .ToDictionary(g => g.Key, g => g.Count());

            if (files.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để hiển thị!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Chuẩn bị dữ liệu
            double[] values = files.Values.Select(v => (double)v).ToArray();
            string[] labels = files.Keys.ToArray();
            double[] positions = Enumerable.Range(0, values.Length).Select(x => x * 0.5).ToArray(); // Giảm khoảng cách giữa các vị trí

            // Xóa biểu đồ cũ
            formsPlot1.Plot.Clear();

            // Thêm dữ liệu vào biểu đồ cột
            var bars = new List<ScottPlot.Bar>();
            for (int i = 0; i < values.Length; i++)
            {
                bars.Add(new ScottPlot.Bar
                {
                    Position = positions[i],      // Vị trí của cột trên trục X
                    Value = values[i],            // Chiều cao của cột
                    FillColor = ScottPlot.Colors.Gray, // Màu của cột
                    Size = 0.4                    // Giảm độ rộng để tránh chồng lấn
                });
            }
            formsPlot1.Plot.Add.Bars(bars);

            // Cập nhật trục X với nhãn và giới hạn để loại bỏ khoảng trống
            formsPlot1.Plot.Axes.Bottom.SetTicks(positions, labels);
            formsPlot1.Plot.Axes.SetLimitsX(positions.Min() - 0.2, positions.Max() + 0.2); // Giới hạn trục X sát với dữ liệu

            // Đặt giới hạn trục Y (bắt đầu từ 0)
            formsPlot1.Plot.Axes.SetLimitsY(0, values.Max() * 1.1);

            // Đặt tiêu đề và nhãn
            formsPlot1.Plot.Title("Phân bố loại file");
            formsPlot1.Plot.XLabel("Loại file");
            formsPlot1.Plot.YLabel("Số lượng");

            // Cập nhật biểu đồ
            formsPlot1.Refresh();
        }




        private void DrawActionChart()
        {
            if (!isDatabaseConnected) return;

            string selectedUser = cbUserFilter.SelectedItem.ToString();
            FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Empty; // No filter if "All" is selected

            if (selectedUser != "Tất cả")
            {
                filter = Builders<BsonDocument>.Filter.Eq("user", selectedUser);
            }

            // Get action counts from the database
            var actions = detailFileCollection.Find(filter).ToList()
                .Where(doc => doc.Contains("id_action"))  // Kiểm tra tồn tại
                .GroupBy(doc => doc["id_action"].IsInt32 ? doc["id_action"].AsInt32 : int.Parse(doc["id_action"].AsString))
                .ToDictionary(g => g.Key, g => g.Count());


            // Get action names from the `action` collection
            var actionNames = actionCollection.Find(new BsonDocument()).ToList()
                .ToDictionary(doc => doc["_id"].AsInt32, doc => doc["name"].AsString);

            if (actions.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu hành động!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Prepare data for the bar char
            double[] values = actions.Values.Select(v => (double)v).ToArray();
            string[] labels = actions.Keys.Select(id => actionNames.ContainsKey(id) ? actionNames[id] : $"Hành động {id}").ToArray();
            double[] positions = Enumerable.Range(0, values.Length).Select(i => (double)i).ToArray();

            // Clear the old plot
            formsPlot1.Plot.Clear();

            // Create and add bars
            var bars = new List<ScottPlot.Bar>();
            for (int i = 0; i < values.Length; i++)
            {
                bars.Add(new ScottPlot.Bar
                {
                    Position = positions[i],
                    Value = values[i],
                    FillColor = ScottPlot.Colors.Gray, // Correct for 5.x
                    Size = 0.6 // Correct for 5.x
                });
            }
            formsPlot1.Plot.Add.Bars(bars);

            // Set X-axis ticks
            formsPlot1.Plot.Axes.Bottom.SetTicks(positions, labels); // Correct for 5.x

            // Set Y-axis limits (start from 0)
            formsPlot1.Plot.Axes.SetLimitsY(0, values.Max() * 1.1); // Correct for 5.x

            // Add titles and labels
            formsPlot1.Plot.Title("Biểu đồ hành động");
            formsPlot1.Plot.YLabel("Số lần thực hiện");

            // Refresh the plot
            formsPlot1.Refresh();
        }

    }
}
