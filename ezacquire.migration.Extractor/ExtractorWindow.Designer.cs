namespace ezacquire.migration.Extractor
{
    partial class ExtractorWindow
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.listBoxRecord = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnExceMigration = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblTime = new System.Windows.Forms.Label();
            this.lblTimeTitle = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.timer3 = new System.Windows.Forms.Timer(this.components);
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxRecord
            // 
            this.listBoxRecord.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxRecord.FormattingEnabled = true;
            this.listBoxRecord.ItemHeight = 15;
            this.listBoxRecord.Location = new System.Drawing.Point(0, 78);
            this.listBoxRecord.Name = "listBoxRecord";
            this.listBoxRecord.Size = new System.Drawing.Size(686, 470);
            this.listBoxRecord.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnExceMigration);
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Controls.Add(this.btnClose);
            this.panel1.Controls.Add(this.progressBar1);
            this.panel1.Controls.Add(this.lblTime);
            this.panel1.Controls.Add(this.lblTimeTitle);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(686, 78);
            this.panel1.TabIndex = 4;
            // 
            // btnExceMigration
            // 
            this.btnExceMigration.AutoSize = true;
            this.btnExceMigration.BackColor = System.Drawing.Color.WhiteSmoke;
            this.btnExceMigration.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExceMigration.Location = new System.Drawing.Point(577, 9);
            this.btnExceMigration.Margin = new System.Windows.Forms.Padding(13, 0, 0, 0);
            this.btnExceMigration.Name = "btnExceMigration";
            this.btnExceMigration.Size = new System.Drawing.Size(100, 34);
            this.btnExceMigration.TabIndex = 21;
            this.btnExceMigration.TabStop = false;
            this.btnExceMigration.Text = "取得資料";
            this.btnExceMigration.UseVisualStyleBackColor = false;
            this.btnExceMigration.Click += new System.EventHandler(this.btnExceMigration_Click);
            // 
            // btnStart
            // 
            this.btnStart.AutoSize = true;
            this.btnStart.BackColor = System.Drawing.Color.WhiteSmoke;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Location = new System.Drawing.Point(13, 9);
            this.btnStart.Margin = new System.Windows.Forms.Padding(13, 0, 0, 0);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(100, 34);
            this.btnStart.TabIndex = 19;
            this.btnStart.TabStop = false;
            this.btnStart.Text = "開始";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnClose
            // 
            this.btnClose.AutoSize = true;
            this.btnClose.BackColor = System.Drawing.Color.WhiteSmoke;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Location = new System.Drawing.Point(122, 9);
            this.btnClose.Margin = new System.Windows.Forms.Padding(0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 34);
            this.btnClose.TabIndex = 20;
            this.btnClose.TabStop = false;
            this.btnClose.Text = "關閉";
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.BackColor = System.Drawing.Color.Silver;
            this.progressBar1.Location = new System.Drawing.Point(226, 13);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(4);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(344, 30);
            this.progressBar1.TabIndex = 18;
            // 
            // lblTime
            // 
            this.lblTime.AutoSize = true;
            this.lblTime.Location = new System.Drawing.Point(194, 55);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(171, 15);
            this.lblTime.TabIndex = 5;
            this.lblTime.Text = "yyyy-mm-dd ~ yyyy-mm-dd";
            // 
            // lblTimeTitle
            // 
            this.lblTimeTitle.AutoSize = true;
            this.lblTimeTitle.Location = new System.Drawing.Point(12, 55);
            this.lblTimeTitle.Name = "lblTimeTitle";
            this.lblTimeTitle.Size = new System.Drawing.Size(176, 15);
            this.lblTimeTitle.TabIndex = 4;
            this.lblTimeTitle.Text = "目前取得資料的時間範圍:";
            // 
            // timer1
            // 
            this.timer1.Interval = 10000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timer2
            // 
            this.timer2.Interval = 30000;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // timer3
            // 
            this.timer3.Interval = 60000;
            this.timer3.Tick += new System.EventHandler(this.timer3_Tick);
            // 
            // ExtractorWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(686, 548);
            this.Controls.Add(this.listBoxRecord);
            this.Controls.Add(this.panel1);
            this.Name = "ExtractorWindow";
            this.Text = "影像取出程式";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxRecord;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.Label lblTimeTitle;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Button btnExceMigration;
        private System.Windows.Forms.Timer timer3;
    }
}

