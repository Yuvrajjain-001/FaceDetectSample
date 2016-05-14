using System;

namespace FaceSortUI
{
    partial class OptionDialog
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxFaceDetector = new System.Windows.Forms.TextBox();
            this.buttonFaceDetector = new System.Windows.Forms.Button();
            this.checkBoxShowStatusBar = new System.Windows.Forms.CheckBox();
            this.checkBoxShowDebugPanel = new System.Windows.Forms.CheckBox();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.numericUpDownMaxImages = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxShowEyes = new System.Windows.Forms.CheckBox();
            this.checkBoxNormalizedFace = new System.Windows.Forms.CheckBox();
            this.numericUpDownPhotoWidth = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.numericUpDownAnim = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.numericUpDownDetectY = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownDetectX = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.numericUpDownFaceDetThresh = new System.Windows.Forms.NumericUpDown();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.buttonEyeDetectPath = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxEyeDetectPath = new System.Windows.Forms.TextBox();
            this.textBoxBackEndConfigFile = new System.Windows.Forms.TextBox();
            this.buttonBackEndConfigFile = new System.Windows.Forms.Button();
            this.labelBackConfigFile = new System.Windows.Forms.Label();
            this.checkBoxCompactGroups = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxImages)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPhotoWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAnim)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDetectY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDetectX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFaceDetThresh)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "FaceDetector";
            // 
            // textBoxFaceDetector
            // 
            this.textBoxFaceDetector.Location = new System.Drawing.Point(82, 18);
            this.textBoxFaceDetector.Name = "textBoxFaceDetector";
            this.textBoxFaceDetector.Size = new System.Drawing.Size(164, 20);
            this.textBoxFaceDetector.TabIndex = 1;
            this.textBoxFaceDetector.Text = "%ProgramFiles%\\LiveLabs\\faceSort\\classifier.txt";
            // 
            // buttonFaceDetector
            // 
            this.buttonFaceDetector.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonFaceDetector.Location = new System.Drawing.Point(259, 15);
            this.buttonFaceDetector.Name = "buttonFaceDetector";
            this.buttonFaceDetector.Size = new System.Drawing.Size(50, 23);
            this.buttonFaceDetector.TabIndex = 2;
            this.buttonFaceDetector.Text = "Browse ...";
            this.buttonFaceDetector.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonFaceDetector.UseVisualStyleBackColor = true;
            this.buttonFaceDetector.Click += new System.EventHandler(this.buttonFaceDetector_Click);
            // 
            // checkBoxShowStatusBar
            // 
            this.checkBoxShowStatusBar.AutoSize = true;
            this.checkBoxShowStatusBar.Checked = true;
            this.checkBoxShowStatusBar.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxShowStatusBar.Location = new System.Drawing.Point(6, 19);
            this.checkBoxShowStatusBar.Name = "checkBoxShowStatusBar";
            this.checkBoxShowStatusBar.Size = new System.Drawing.Size(105, 17);
            this.checkBoxShowStatusBar.TabIndex = 16;
            this.checkBoxShowStatusBar.Text = "Show Status Bar";
            this.checkBoxShowStatusBar.UseVisualStyleBackColor = true;
            // 
            // checkBoxShowDebugPanel
            // 
            this.checkBoxShowDebugPanel.AutoSize = true;
            this.checkBoxShowDebugPanel.Location = new System.Drawing.Point(6, 47);
            this.checkBoxShowDebugPanel.Name = "checkBoxShowDebugPanel";
            this.checkBoxShowDebugPanel.Size = new System.Drawing.Size(118, 17);
            this.checkBoxShowDebugPanel.TabIndex = 17;
            this.checkBoxShowDebugPanel.Text = "Show Debug Panel";
            this.checkBoxShowDebugPanel.UseVisualStyleBackColor = true;
            // 
            // buttonOk
            // 
            this.buttonOk.Location = new System.Drawing.Point(228, 350);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(42, 23);
            this.buttonOk.TabIndex = 18;
            this.buttonOk.Text = "Ok";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(285, 350);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(54, 23);
            this.buttonCancel.TabIndex = 19;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 17);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 13);
            this.label6.TabIndex = 20;
            this.label6.Text = "Max Images";
            // 
            // numericUpDownMaxImages
            // 
            this.numericUpDownMaxImages.Increment = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDownMaxImages.Location = new System.Drawing.Point(80, 17);
            this.numericUpDownMaxImages.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownMaxImages.Name = "numericUpDownMaxImages";
            this.numericUpDownMaxImages.Size = new System.Drawing.Size(44, 20);
            this.numericUpDownMaxImages.TabIndex = 21;
            this.numericUpDownMaxImages.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxCompactGroups);
            this.groupBox1.Controls.Add(this.checkBoxShowEyes);
            this.groupBox1.Controls.Add(this.checkBoxNormalizedFace);
            this.groupBox1.Controls.Add(this.numericUpDownPhotoWidth);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.checkBoxShowDebugPanel);
            this.groupBox1.Controls.Add(this.checkBoxShowStatusBar);
            this.groupBox1.Location = new System.Drawing.Point(24, 247);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(315, 97);
            this.groupBox1.TabIndex = 22;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Display settings";
            // 
            // checkBoxShowEyes
            // 
            this.checkBoxShowEyes.AutoSize = true;
            this.checkBoxShowEyes.Location = new System.Drawing.Point(210, 43);
            this.checkBoxShowEyes.Name = "checkBoxShowEyes";
            this.checkBoxShowEyes.Size = new System.Drawing.Size(76, 17);
            this.checkBoxShowEyes.TabIndex = 21;
            this.checkBoxShowEyes.Text = "ShowEyes";
            this.checkBoxShowEyes.UseVisualStyleBackColor = true;
            // 
            // checkBoxNormalizedFace
            // 
            this.checkBoxNormalizedFace.AutoSize = true;
            this.checkBoxNormalizedFace.Checked = true;
            this.checkBoxNormalizedFace.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxNormalizedFace.Location = new System.Drawing.Point(210, 19);
            this.checkBoxNormalizedFace.Name = "checkBoxNormalizedFace";
            this.checkBoxNormalizedFace.Size = new System.Drawing.Size(105, 17);
            this.checkBoxNormalizedFace.TabIndex = 20;
            this.checkBoxNormalizedFace.Text = "Normalized Face";
            this.checkBoxNormalizedFace.UseVisualStyleBackColor = true;
            // 
            // numericUpDownPhotoWidth
            // 
            this.numericUpDownPhotoWidth.DecimalPlaces = 2;
            this.numericUpDownPhotoWidth.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numericUpDownPhotoWidth.Location = new System.Drawing.Point(82, 69);
            this.numericUpDownPhotoWidth.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDownPhotoWidth.Name = "numericUpDownPhotoWidth";
            this.numericUpDownPhotoWidth.Size = new System.Drawing.Size(49, 20);
            this.numericUpDownPhotoWidth.TabIndex = 19;
            this.numericUpDownPhotoWidth.Value = new decimal(new int[] {
            25,
            0,
            0,
            131072});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(2, 71);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 13);
            this.label5.TabIndex = 18;
            this.label5.Text = "PhotoWidth";
            // 
            // numericUpDownAnim
            // 
            this.numericUpDownAnim.DecimalPlaces = 1;
            this.numericUpDownAnim.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownAnim.Location = new System.Drawing.Point(80, 38);
            this.numericUpDownAnim.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownAnim.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            -2147418112});
            this.numericUpDownAnim.Name = "numericUpDownAnim";
            this.numericUpDownAnim.Size = new System.Drawing.Size(44, 20);
            this.numericUpDownAnim.TabIndex = 24;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 40);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(73, 13);
            this.label7.TabIndex = 23;
            this.label7.Text = "Anim Duration";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.numericUpDownDetectY);
            this.groupBox2.Controls.Add(this.numericUpDownDetectX);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.numericUpDownFaceDetThresh);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.numericUpDownAnim);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.numericUpDownMaxImages);
            this.groupBox2.Location = new System.Drawing.Point(24, 138);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(309, 90);
            this.groupBox2.TabIndex = 25;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Processing Options";
            // 
            // numericUpDownDetectY
            // 
            this.numericUpDownDetectY.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDownDetectY.Location = new System.Drawing.Point(264, 15);
            this.numericUpDownDetectY.Maximum = new decimal(new int[] {
            9000,
            0,
            0,
            0});
            this.numericUpDownDetectY.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownDetectY.Name = "numericUpDownDetectY";
            this.numericUpDownDetectY.Size = new System.Drawing.Size(45, 20);
            this.numericUpDownDetectY.TabIndex = 29;
            this.numericUpDownDetectY.Value = new decimal(new int[] {
            480,
            0,
            0,
            0});
            // 
            // numericUpDownDetectX
            // 
            this.numericUpDownDetectX.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDownDetectX.Location = new System.Drawing.Point(211, 15);
            this.numericUpDownDetectX.Maximum = new decimal(new int[] {
            9000,
            0,
            0,
            0});
            this.numericUpDownDetectX.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownDetectX.Name = "numericUpDownDetectX";
            this.numericUpDownDetectX.Size = new System.Drawing.Size(47, 20);
            this.numericUpDownDetectX.TabIndex = 28;
            this.numericUpDownDetectX.Value = new decimal(new int[] {
            640,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(143, 17);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(62, 13);
            this.label3.TabIndex = 27;
            this.label3.Text = "Detect Size";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(-1, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(75, 13);
            this.label4.TabIndex = 26;
            this.label4.Text = "Detect Thresh";
            // 
            // numericUpDownFaceDetThresh
            // 
            this.numericUpDownFaceDetThresh.DecimalPlaces = 1;
            this.numericUpDownFaceDetThresh.Location = new System.Drawing.Point(80, 59);
            this.numericUpDownFaceDetThresh.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDownFaceDetThresh.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            -2147483648});
            this.numericUpDownFaceDetThresh.Name = "numericUpDownFaceDetThresh";
            this.numericUpDownFaceDetThresh.Size = new System.Drawing.Size(44, 20);
            this.numericUpDownFaceDetThresh.TabIndex = 25;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.buttonEyeDetectPath);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.textBoxEyeDetectPath);
            this.groupBox3.Controls.Add(this.textBoxBackEndConfigFile);
            this.groupBox3.Controls.Add(this.buttonBackEndConfigFile);
            this.groupBox3.Controls.Add(this.labelBackConfigFile);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.textBoxFaceDetector);
            this.groupBox3.Controls.Add(this.buttonFaceDetector);
            this.groupBox3.Location = new System.Drawing.Point(24, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(315, 110);
            this.groupBox3.TabIndex = 26;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Config Paths";
            // 
            // buttonEyeDetectPath
            // 
            this.buttonEyeDetectPath.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonEyeDetectPath.Location = new System.Drawing.Point(259, 76);
            this.buttonEyeDetectPath.Name = "buttonEyeDetectPath";
            this.buttonEyeDetectPath.Size = new System.Drawing.Size(50, 21);
            this.buttonEyeDetectPath.TabIndex = 14;
            this.buttonEyeDetectPath.Text = "Browse ...";
            this.buttonEyeDetectPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonEyeDetectPath.UseVisualStyleBackColor = true;
            this.buttonEyeDetectPath.Click += new System.EventHandler(this.buttonEyeDetectPath_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Eye Detect";
            // 
            // textBoxEyeDetectPath
            // 
            this.textBoxEyeDetectPath.Location = new System.Drawing.Point(82, 77);
            this.textBoxEyeDetectPath.Name = "textBoxEyeDetectPath";
            this.textBoxEyeDetectPath.Size = new System.Drawing.Size(164, 20);
            this.textBoxEyeDetectPath.TabIndex = 12;
            this.textBoxEyeDetectPath.Text = "DEFAULT";
            // 
            // textBoxBackEndConfigFile
            // 
            this.textBoxBackEndConfigFile.Location = new System.Drawing.Point(82, 51);
            this.textBoxBackEndConfigFile.Name = "textBoxBackEndConfigFile";
            this.textBoxBackEndConfigFile.Size = new System.Drawing.Size(164, 20);
            this.textBoxBackEndConfigFile.TabIndex = 10;
            this.textBoxBackEndConfigFile.Text = "%ProgramFiles%\\LiveLabs\\faceSort\\ConfigBoost.txt";
            // 
            // buttonBackEndConfigFile
            // 
            this.buttonBackEndConfigFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonBackEndConfigFile.Location = new System.Drawing.Point(259, 51);
            this.buttonBackEndConfigFile.Name = "buttonBackEndConfigFile";
            this.buttonBackEndConfigFile.Size = new System.Drawing.Size(50, 23);
            this.buttonBackEndConfigFile.TabIndex = 11;
            this.buttonBackEndConfigFile.Text = "Browse ...";
            this.buttonBackEndConfigFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonBackEndConfigFile.UseVisualStyleBackColor = true;
            this.buttonBackEndConfigFile.Click += new System.EventHandler(this.buttonBackEndConfigFile_Click);
            // 
            // labelBackConfigFile
            // 
            this.labelBackConfigFile.AutoSize = true;
            this.labelBackConfigFile.Location = new System.Drawing.Point(11, 51);
            this.labelBackConfigFile.Name = "labelBackConfigFile";
            this.labelBackConfigFile.Size = new System.Drawing.Size(54, 13);
            this.labelBackConfigFile.TabIndex = 9;
            this.labelBackConfigFile.Text = "BE Config";
            // 
            // checkBoxCompactGroups
            // 
            this.checkBoxCompactGroups.AutoSize = true;
            this.checkBoxCompactGroups.Checked = true;
            this.checkBoxCompactGroups.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCompactGroups.Location = new System.Drawing.Point(211, 67);
            this.checkBoxCompactGroups.Name = "checkBoxCompactGroups";
            this.checkBoxCompactGroups.Size = new System.Drawing.Size(105, 17);
            this.checkBoxCompactGroups.TabIndex = 22;
            this.checkBoxCompactGroups.Text = "Compact Groups";
            this.checkBoxCompactGroups.UseVisualStyleBackColor = true;
            // 
            // OptionDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 398);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Name = "OptionDialog";
            this.Text = "Face Sort Options";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownMaxImages)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPhotoWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAnim)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDetectY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDetectX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownFaceDetThresh)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxFaceDetector;
        private System.Windows.Forms.Button buttonFaceDetector;
        private System.Windows.Forms.CheckBox checkBoxShowStatusBar;
        private System.Windows.Forms.CheckBox checkBoxShowDebugPanel;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numericUpDownMaxImages;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.NumericUpDown numericUpDownAnim;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.NumericUpDown numericUpDownFaceDetThresh;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericUpDownPhotoWidth;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox checkBoxNormalizedFace;
        private System.Windows.Forms.Button buttonBackEndConfigFile;
        private System.Windows.Forms.Label labelBackConfigFile;
        private System.Windows.Forms.CheckBox checkBoxShowEyes;
        private System.Windows.Forms.TextBox textBoxBackEndConfigFile;
        private System.Windows.Forms.Button buttonEyeDetectPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxEyeDetectPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numericUpDownDetectX;
        private System.Windows.Forms.NumericUpDown numericUpDownDetectY;
        private System.Windows.Forms.CheckBox checkBoxCompactGroups;
    }
}