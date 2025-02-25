using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WindowsFormsApp2
{
    public partial class Form2 : Form
    {
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<BsonDocument> detailFileCollection;
        private readonly IMongoCollection<BsonDocument> fileCollection;
        private readonly IMongoCollection<BsonDocument> actionCollection;
        private readonly IMongoCollection<BsonDocument> userCollection;

        private DateTime startTime;
        private DateTime endTime;

        public Form2(DateTime start, DateTime end)
        {
            InitializeComponent();
            startTime = start;
            endTime = end;

            // Kết nối MongoDB
            var client = new MongoClient("mongodb://localhost:27017");
            database = client.GetDatabase("FileManagement");
            detailFileCollection = database.GetCollection<BsonDocument>("detail_file");
            fileCollection = database.GetCollection<BsonDocument>("file");
            actionCollection = database.GetCollection<BsonDocument>("action");
            userCollection = database.GetCollection<BsonDocument>("user");
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            LoadStatistics();
            LoadFileList();
        }

        private void LoadStatistics()
        {
            try
            {
                // Chuyển đổi thời gian sang định dạng chuỗi
                var filter = Builders<BsonDocument>.Filter.Gte("datetime", startTime.ToString("yyyy-MM-dd HH:mm:ss")) &
                             Builders<BsonDocument>.Filter.Lte("datetime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

                var detailFiles = detailFileCollection.Find(filter).ToList();

                int totalActions = detailFiles.Count;

                // Lấy danh sách hành động từ actionCollection
                var actionDocs = actionCollection.Find(new BsonDocument()).ToList();
                var actionDict = actionDocs.ToDictionary(a => a["_id"].ToString(), a => a["name"].ToString());

                int createdFiles = detailFiles.Count(d => actionDict.TryGetValue(d["id_action"].ToString(), out string action) && action == "Tạo");
                int editedFiles = detailFiles.Count(d => actionDict.TryGetValue(d["id_action"].ToString(), out string action) && action == "Sửa");
                int deletedFiles = detailFiles.Count(d => actionDict.TryGetValue(d["id_action"].ToString(), out string action) && action == "Xóa");
                int renamedFiles = detailFiles.Count(d => actionDict.TryGetValue(d["id_action"].ToString(), out string action) && action.StartsWith("Đổi tên"));
                int openedFiles = detailFiles.Count(d => actionDict.TryGetValue(d["id_action"].ToString(), out string action) && action == "Mở");

                lblTotalActions.Text = $"Tổng số hành động: {totalActions}";
                lblCreatedFiles.Text = $"Số file được tạo: {createdFiles}";
                lblOpenedFiles.Text = $"Số file được mở: {openedFiles}";
                lblEditedFiles.Text = $"Số file được sửa: {editedFiles}";
                lblDeletedFiles.Text = $"Số file bị xóa: {deletedFiles}";
                lblRenamedFiles.Text = $"Số file bị đổi tên: {renamedFiles}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải thống kê: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFileList()
        {
            try
            {
                // Chuyển đổi startTime và endTime sang định dạng chuỗi khớp với MongoDB
                var filter = Builders<BsonDocument>.Filter.Gte("datetime", startTime.ToString("yyyy-MM-dd HH:mm:ss")) &
                             Builders<BsonDocument>.Filter.Lte("datetime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));

                // Lấy danh sách id_file duy nhất từ detailFileCollection
                var fileIds = detailFileCollection.Find(filter)
                                  .Project(Builders<BsonDocument>.Projection.Include("id_file"))
                                  .ToList()
                                  .Select(d => d["id_file"].AsObjectId)
                                  .Distinct()
                                  .ToList();

                if (!fileIds.Any())
                {
                    MessageBox.Show("Không có tệp nào trong khoảng thời gian đã chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                cboFiles.Items.Clear();

                foreach (var fileId in fileIds)
                {
                    try
                    {
                        var fileDoc = fileCollection.Find(Builders<BsonDocument>.Filter.Eq("_id", fileId)).FirstOrDefault();
                        if (fileDoc != null && fileDoc.Contains("name"))
                        {
                            cboFiles.Items.Add(fileDoc["name"].ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi xử lý fileId {fileId}: {ex.Message}");
                        continue;
                    }
                }

                if (cboFiles.Items.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy tên tệp nào trong collection file!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


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

        private void LoadFileHistory(string fileName)
        {
            try
            {
                var fileFilter = Builders<BsonDocument>.Filter.Eq("name", fileName);
                var fileDoc = fileCollection.Find(fileFilter).FirstOrDefault();

                if (fileDoc == null)
                {
                    MessageBox.Show("Không tìm thấy tệp!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var detailFilter = Builders<BsonDocument>.Filter.Eq("id_file", fileDoc["_id"].AsObjectId);
                var fileHistory = detailFileCollection.Find(detailFilter)
                                  .Sort(Builders<BsonDocument>.Sort.Descending("datetime"))
                                  .ToList();

                DataTable dt = new DataTable();
                dt.Columns.Add("DATE_ACTION");
                dt.Columns.Add("CURRENT_NAME");
                dt.Columns.Add("OLD_NAME");
                dt.Columns.Add("OLD_PATH");
                dt.Columns.Add("NEW_PATH");
                dt.Columns.Add("NAME_ACTION");
                dt.Columns.Add("NAME_USER");

                foreach (var detail in fileHistory)
                {
                    // Lấy id_action (chuyển về số)
                    string actionName = "Không xác định";
                    if (detail.Contains("id_action"))
                    {
                        int idAction;
                        if (int.TryParse(detail["id_action"].ToString(), out idAction)) // Chuyển chuỗi thành số
                        {
                            var actionFilter = Builders<BsonDocument>.Filter.Eq("_id", idAction);
                            var actionDoc = actionCollection.Find(actionFilter).FirstOrDefault();
                            if (actionDoc != null)
                            {
                                actionName = actionDoc["name"].ToString();
                            }
                        }
                    }

                    // Lấy id_user (chuyển về số)
                    string userName = "Không xác định";
                    if (detail.Contains("id_user"))
                    {
                        int idUser;
                        if (int.TryParse(detail["id_user"].ToString(), out idUser)) // Chuyển chuỗi thành số
                        {
                            var userFilter = Builders<BsonDocument>.Filter.Eq("_id", idUser);
                            var userDoc = userCollection.Find(userFilter).FirstOrDefault();
                            if (userDoc != null)
                            {
                                userName = userDoc["name"].ToString();
                            }
                        }
                    }

                    dt.Rows.Add(
                        detail["datetime"].ToString(),
                        fileDoc["name"].ToString(),
                        detail.Contains("oldname") ? detail["oldname"].ToString() : "Không xác định",
                        detail.Contains("old_path") ? detail["old_path"].ToString() : "Không xác định",
                        detail.Contains("new_path") ? detail["new_path"].ToString() : "Không xác định",
                        actionName,
                        userName
                    );
                }

                dgvFileHistory.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải lịch sử tệp: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
