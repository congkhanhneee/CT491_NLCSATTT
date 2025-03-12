namespace WindowsFormsApp2
{
    partial class Form4
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
            this.panelMain = new System.Windows.Forms.Panel();
            this.btnLoadActionChart = new System.Windows.Forms.Button();
            this.btnLoadFileChart = new System.Windows.Forms.Button();
            this.cbUserFilter = new System.Windows.Forms.ComboBox();
            this.formsPlot1 = new ScottPlot.WinForms.FormsPlot();
            this.panelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.btnLoadActionChart);
            this.panelMain.Controls.Add(this.btnLoadFileChart);
            this.panelMain.Controls.Add(this.cbUserFilter);
            this.panelMain.Controls.Add(this.formsPlot1);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(1084, 527);
            this.panelMain.TabIndex = 0;
            // 
            // btnLoadActionChart
            // 
            this.btnLoadActionChart.Location = new System.Drawing.Point(331, 481);
            this.btnLoadActionChart.Name = "btnLoadActionChart";
            this.btnLoadActionChart.Size = new System.Drawing.Size(227, 34);
            this.btnLoadActionChart.TabIndex = 3;
            this.btnLoadActionChart.Text = "Loại hành động";
            this.btnLoadActionChart.UseVisualStyleBackColor = true;
            this.btnLoadActionChart.Click += new System.EventHandler(this.btnLoadActionChart_Click);
            // 
            // btnLoadFileChart
            // 
            this.btnLoadFileChart.Location = new System.Drawing.Point(12, 480);
            this.btnLoadFileChart.Name = "btnLoadFileChart";
            this.btnLoadFileChart.Size = new System.Drawing.Size(256, 35);
            this.btnLoadFileChart.TabIndex = 2;
            this.btnLoadFileChart.Text = "Loại file";
            this.btnLoadFileChart.UseVisualStyleBackColor = true;
            this.btnLoadFileChart.Click += new System.EventHandler(this.btnLoadFileChart_Click);
            // 
            // cbUserFilter
            // 
            this.cbUserFilter.FormattingEnabled = true;
            this.cbUserFilter.Location = new System.Drawing.Point(12, 446);
            this.cbUserFilter.Name = "cbUserFilter";
            this.cbUserFilter.Size = new System.Drawing.Size(323, 28);
            this.cbUserFilter.TabIndex = 1;
            // 
            // formsPlot1
            // 
            this.formsPlot1.DisplayScale = 0F;
            this.formsPlot1.Location = new System.Drawing.Point(0, 0);
            this.formsPlot1.Name = "formsPlot1";
            this.formsPlot1.Size = new System.Drawing.Size(1081, 440);
            this.formsPlot1.TabIndex = 0;
            this.formsPlot1.Load += new System.EventHandler(this.Form4_Load);
            // 
            // Form4
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 527);
            this.Controls.Add(this.panelMain);
            this.Name = "Form4";
            this.Text = "Biểu đồ";
            this.Load += new System.EventHandler(this.Form4_Load);
            this.panelMain.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelMain;
        private ScottPlot.WinForms.FormsPlot formsPlot1;
        private System.Windows.Forms.Button btnLoadFileChart;
        private System.Windows.Forms.ComboBox cbUserFilter;
        private System.Windows.Forms.Button btnLoadActionChart;
    }
}