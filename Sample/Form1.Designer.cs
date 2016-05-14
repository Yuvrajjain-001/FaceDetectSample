namespace Microsoft.LiveLabs
{
    partial class FaceDetectSample
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        //private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && (components != null))
        //    {
        //        components.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBoxFiles = new System.Windows.Forms.ListBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checkBoxEyeDetect = new System.Windows.Forms.CheckBox();
            this.numericUpDownFaceDetectThreshold = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxFaceDetectTime = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxEyeDetect = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SaveAllButton = new System.Windows.Forms.Button();
            this.detectorFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFaceDetectThreshold)).BeginInit();
            this.SuspendLayout();
            // 
            // listBoxFiles
            // 
            this.listBoxFiles.FormattingEnabled = true;
            this.listBoxFiles.HorizontalScrollbar = true;
            this.listBoxFiles.Location = new System.Drawing.Point(13, 64);
            this.listBoxFiles.Name = "listBoxFiles";
            this.listBoxFiles.Size = new System.Drawing.Size(151, 446);
            this.listBoxFiles.TabIndex = 0;
            this.listBoxFiles.SelectedIndexChanged += new System.EventHandler(this.listBoxFiles_SelectedIndexChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(220, 64);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(618, 460);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(907, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.detectorFileToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // checkBoxEyeDetect
            // 
            this.checkBoxEyeDetect.AutoSize = true;
            this.checkBoxEyeDetect.Location = new System.Drawing.Point(220, 27);
            this.checkBoxEyeDetect.Name = "checkBoxEyeDetect";
            this.checkBoxEyeDetect.Size = new System.Drawing.Size(79, 17);
            this.checkBoxEyeDetect.TabIndex = 5;
            this.checkBoxEyeDetect.Text = "Eye Detect";
            this.checkBoxEyeDetect.UseVisualStyleBackColor = true;
            this.checkBoxEyeDetect.CheckStateChanged += new System.EventHandler(this.checkBoxEyeDetect_CheckStateChanged);
            // 
            // numericUpDownFaceDetectThreshold
            // 
            this.numericUpDownFaceDetectThreshold.DecimalPlaces = 1;
            this.numericUpDownFaceDetectThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownFaceDetectThreshold.Location = new System.Drawing.Point(306, 27);
            this.numericUpDownFaceDetectThreshold.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownFaceDetectThreshold.Name = "numericUpDownFaceDetectThreshold";
            this.numericUpDownFaceDetectThreshold.Size = new System.Drawing.Size(51, 20);
            this.numericUpDownFaceDetectThreshold.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(363, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Detector Threshold";
            // 
            // textBoxFaceDetectTime
            // 
            this.textBoxFaceDetectTime.Location = new System.Drawing.Point(486, 24);
            this.textBoxFaceDetectTime.Name = "textBoxFaceDetectTime";
            this.textBoxFaceDetectTime.ReadOnly = true;
            this.textBoxFaceDetectTime.Size = new System.Drawing.Size(47, 20);
            this.textBoxFaceDetectTime.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(539, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(128, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "mSec  -Face Detect Time";
            // 
            // textBoxEyeDetect
            // 
            this.textBoxEyeDetect.Location = new System.Drawing.Point(700, 24);
            this.textBoxEyeDetect.Name = "textBoxEyeDetect";
            this.textBoxEyeDetect.ReadOnly = true;
            this.textBoxEyeDetect.Size = new System.Drawing.Size(45, 20);
            this.textBoxEyeDetect.TabIndex = 10;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(752, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(116, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "mSec Eye Detect Time";
            // 
            // SaveAllButton
            // 
            this.SaveAllButton.Location = new System.Drawing.Point(12, 24);
            this.SaveAllButton.Name = "SaveAllButton";
            this.SaveAllButton.Size = new System.Drawing.Size(75, 23);
            this.SaveAllButton.TabIndex = 12;
            this.SaveAllButton.Text = "Save All";
            this.SaveAllButton.UseVisualStyleBackColor = true;
            this.SaveAllButton.Click += new System.EventHandler(this.SaveAllButton_Click);
            // 
            // detectorFileToolStripMenuItem
            // 
            this.detectorFileToolStripMenuItem.Name = "detectorFileToolStripMenuItem";
            this.detectorFileToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.detectorFileToolStripMenuItem.Text = "DetectorFile";
            this.detectorFileToolStripMenuItem.Click += new System.EventHandler(this.detectorFileToolStripMenuItem_Click);
            // 
            // FaceDetectSample
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(907, 560);
            this.Controls.Add(this.SaveAllButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxEyeDetect);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxFaceDetectTime);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDownFaceDetectThreshold);
            this.Controls.Add(this.checkBoxEyeDetect);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.listBoxFiles);
            this.Controls.Add(this.menuStrip1);
            this.Name = "FaceDetectSample";
            this.Text = "FaceDetectSample";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFaceDetectThreshold)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxFiles;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.CheckBox checkBoxEyeDetect;
        private System.Windows.Forms.NumericUpDown numericUpDownFaceDetectThreshold;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxFaceDetectTime;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxEyeDetect;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button SaveAllButton;
        private System.Windows.Forms.ToolStripMenuItem detectorFileToolStripMenuItem;
    }
}

