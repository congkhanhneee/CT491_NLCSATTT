namespace WindowsFormsApp2
{
    partial class Form3
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lb_Sum_Directory = new System.Windows.Forms.Label();
            this.mySqlCommand1 = new MySql.Data.MySqlClient.MySqlCommand();
            this.dt_File = new System.Windows.Forms.DataGridView();
            this.cbTypeFile = new System.Windows.Forms.ComboBox();
            this.cbExtensionFile = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lb_Sum_File = new System.Windows.Forms.Label();
            this.btn_Click_Directory = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lb_Sum_Directory
            // 
            this.lb_Sum_Directory.AutoSize = true;
            this.lb_Sum_Directory.Location = new System.Drawing.Point(12, 40);
            this.lb_Sum_Directory.Name = "lb_Sum_Directory";
            this.lb_Sum_Directory.Size = new System.Drawing.Size(195, 20);
            this.lb_Sum_Directory.TabIndex = 0;
            this.lb_Sum_Directory.Text = "Tổng số tệp trong thư mục";
            // 
            // mySqlCommand1
            // 
            this.mySqlCommand1.CacheAge = 0;
            this.mySqlCommand1.Connection = null;
            this.mySqlCommand1.EnableCaching = false;
            this.mySqlCommand1.Transaction = null;
            // 
            // dt_File
            // 
            this.dt_File.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dt_File.Location = new System.Drawing.Point(54, 211);
            this.dt_File.Name = "dt_File";
            this.dt_File.RowHeadersWidth = 62;
            this.dt_File.RowTemplate.Height = 28;
            this.dt_File.Size = new System.Drawing.Size(689, 192);
            this.dt_File.TabIndex = 1;
            // 
            // cbTypeFile
            // 
            this.cbTypeFile.FormattingEnabled = true;
            this.cbTypeFile.Location = new System.Drawing.Point(179, 92);
            this.cbTypeFile.Name = "cbTypeFile";
            this.cbTypeFile.Size = new System.Drawing.Size(121, 28);
            this.cbTypeFile.TabIndex = 2;
            // 
            // cbExtensionFile
            // 
            this.cbExtensionFile.FormattingEnabled = true;
            this.cbExtensionFile.Location = new System.Drawing.Point(494, 92);
            this.cbExtensionFile.Name = "cbExtensionFile";
            this.cbExtensionFile.Size = new System.Drawing.Size(121, 28);
            this.cbExtensionFile.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(346, 95);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Chọn đuôi tệp";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(27, 100);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(112, 20);
            this.label3.TabIndex = 5;
            this.label3.Text = "Chọn Loại Tệp";
            // 
            // lb_Sum_File
            // 
            this.lb_Sum_File.AutoSize = true;
            this.lb_Sum_File.Location = new System.Drawing.Point(207, 157);
            this.lb_Sum_File.Name = "lb_Sum_File";
            this.lb_Sum_File.Size = new System.Drawing.Size(93, 20);
            this.lb_Sum_File.TabIndex = 6;
            this.lb_Sum_File.Text = "Tổng số tệp";
            // 
            // btn_Click_Directory
            // 
            this.btn_Click_Directory.Location = new System.Drawing.Point(684, 89);
            this.btn_Click_Directory.Name = "btn_Click_Directory";
            this.btn_Click_Directory.Size = new System.Drawing.Size(73, 31);
            this.btn_Click_Directory.TabIndex = 7;
            this.btn_Click_Directory.Text = "Go";
            this.btn_Click_Directory.UseVisualStyleBackColor = true;
            this.btn_Click_Directory.Click += new System.EventHandler(this.btn_Click_Directory_Click);
            // 
            // Form3
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btn_Click_Directory);
            this.Controls.Add(this.lb_Sum_File);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbExtensionFile);
            this.Controls.Add(this.cbTypeFile);
            this.Controls.Add(this.dt_File);
            this.Controls.Add(this.lb_Sum_Directory);
            this.Name = "Form3";
            this.Text = "Chi Tiết Thư mục";
            this.Load += new System.EventHandler(this.Form3_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dt_File)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lb_Sum_Directory;
        private MySql.Data.MySqlClient.MySqlCommand mySqlCommand1;
        private System.Windows.Forms.DataGridView dt_File;
        private System.Windows.Forms.ComboBox cbTypeFile;
        private System.Windows.Forms.ComboBox cbExtensionFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lb_Sum_File;
        private System.Windows.Forms.Button btn_Click_Directory;

    }
}