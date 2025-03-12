namespace WindowsFormsApp2
{
    partial class Form2
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblTotalActions;
        private System.Windows.Forms.Label lblCreatedFiles;
        private System.Windows.Forms.Label lblOpenedFiles;
        private System.Windows.Forms.Label lblEditedFiles;
        private System.Windows.Forms.Label lblDeletedFiles;
        private System.Windows.Forms.Label lblRenamedFiles;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTotalActions = new System.Windows.Forms.Label();
            this.lblCreatedFiles = new System.Windows.Forms.Label();
            this.lblOpenedFiles = new System.Windows.Forms.Label();
            this.lblEditedFiles = new System.Windows.Forms.Label();
            this.lblDeletedFiles = new System.Windows.Forms.Label();
            this.lblRenamedFiles = new System.Windows.Forms.Label();
            this.btnExit = new System.Windows.Forms.Button();
            this.cboFiles = new System.Windows.Forms.ComboBox();
            this.dgvFileHistory = new System.Windows.Forms.DataGridView();
            this.btnDetails_File = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFileHistory)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTotalActions
            // 
            this.lblTotalActions.AutoSize = true;
            this.lblTotalActions.Location = new System.Drawing.Point(20, 30);
            this.lblTotalActions.Name = "lblTotalActions";
            this.lblTotalActions.Size = new System.Drawing.Size(0, 20);
            this.lblTotalActions.TabIndex = 0;
            // 
            // lblCreatedFiles
            // 
            this.lblCreatedFiles.AutoSize = true;
            this.lblCreatedFiles.Location = new System.Drawing.Point(20, 60);
            this.lblCreatedFiles.Name = "lblCreatedFiles";
            this.lblCreatedFiles.Size = new System.Drawing.Size(0, 20);
            this.lblCreatedFiles.TabIndex = 1;
            // 
            // lblOpenedFiles
            // 
            this.lblOpenedFiles.AutoSize = true;
            this.lblOpenedFiles.Location = new System.Drawing.Point(20, 90);
            this.lblOpenedFiles.Name = "lblOpenedFiles";
            this.lblOpenedFiles.Size = new System.Drawing.Size(0, 20);
            this.lblOpenedFiles.TabIndex = 2;
            // 
            // lblEditedFiles
            // 
            this.lblEditedFiles.AutoSize = true;
            this.lblEditedFiles.Location = new System.Drawing.Point(20, 120);
            this.lblEditedFiles.Name = "lblEditedFiles";
            this.lblEditedFiles.Size = new System.Drawing.Size(0, 20);
            this.lblEditedFiles.TabIndex = 3;
            // 
            // lblDeletedFiles
            // 
            this.lblDeletedFiles.AutoSize = true;
            this.lblDeletedFiles.Location = new System.Drawing.Point(20, 150);
            this.lblDeletedFiles.Name = "lblDeletedFiles";
            this.lblDeletedFiles.Size = new System.Drawing.Size(0, 20);
            this.lblDeletedFiles.TabIndex = 4;
            // 
            // lblRenamedFiles
            // 
            this.lblRenamedFiles.AutoSize = true;
            this.lblRenamedFiles.Location = new System.Drawing.Point(20, 180);
            this.lblRenamedFiles.Name = "lblRenamedFiles";
            this.lblRenamedFiles.Size = new System.Drawing.Size(0, 20);
            this.lblRenamedFiles.TabIndex = 5;
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(355, 456);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(93, 34);
            this.btnExit.TabIndex = 6;
            this.btnExit.Text = "Đóng";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // cboFiles
            // 
            this.cboFiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFiles.FormattingEnabled = true;
            this.cboFiles.Location = new System.Drawing.Point(250, 210);
            this.cboFiles.Name = "cboFiles";
            this.cboFiles.Size = new System.Drawing.Size(300, 28);
            this.cboFiles.TabIndex = 7;
            // 
            // dgvFileHistory
            // 
            this.dgvFileHistory.AllowUserToAddRows = false;
            this.dgvFileHistory.AllowUserToDeleteRows = false;
            this.dgvFileHistory.BackgroundColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.dgvFileHistory.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvFileHistory.GridColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dgvFileHistory.Location = new System.Drawing.Point(24, 250);
            this.dgvFileHistory.Name = "dgvFileHistory";
            this.dgvFileHistory.ReadOnly = true;
            this.dgvFileHistory.RowHeadersWidth = 62;
            this.dgvFileHistory.RowTemplate.Height = 28;
            this.dgvFileHistory.Size = new System.Drawing.Size(760, 200);
            this.dgvFileHistory.TabIndex = 9;
            // 
            // btnDetails_File
            // 
            this.btnDetails_File.Location = new System.Drawing.Point(556, 209);
            this.btnDetails_File.Name = "btnDetails_File";
            this.btnDetails_File.Size = new System.Drawing.Size(97, 28);
            this.btnDetails_File.TabIndex = 10;
            this.btnDetails_File.Text = "Chi tiết tệp";
            this.btnDetails_File.UseVisualStyleBackColor = true;
            this.btnDetails_File.Click += new System.EventHandler(this.btnDetails_File_Click);
            // 
            // Form2
            // 
            this.ClientSize = new System.Drawing.Size(812, 493);
            this.Controls.Add(this.btnDetails_File);
            this.Controls.Add(this.dgvFileHistory);
            this.Controls.Add(this.cboFiles);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.lblTotalActions);
            this.Controls.Add(this.lblCreatedFiles);
            this.Controls.Add(this.lblOpenedFiles);
            this.Controls.Add(this.lblEditedFiles);
            this.Controls.Add(this.lblDeletedFiles);
            this.Controls.Add(this.lblRenamedFiles);
            this.Name = "Form2";
            this.Text = "Chi tiết thống kê";
            this.Load += new System.EventHandler(this.Form2_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFileHistory)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.ComboBox cboFiles;
        private System.Windows.Forms.DataGridView dgvFileHistory;
        private System.Windows.Forms.Button btnDetails_File;
    }
}
