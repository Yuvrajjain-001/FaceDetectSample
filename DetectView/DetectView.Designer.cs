namespace DetectView
{
	partial class DetectView
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent ()
		{
			this.listBoxImg = new System.Windows.Forms.ListBox ();
			this.label1 = new System.Windows.Forms.Label ();
			this.buttonLoad = new System.Windows.Forms.Button ();
			this.labelImgName = new System.Windows.Forms.Label ();
			this.labelImageBorder = new System.Windows.Forms.Label ();
			this.groupBox1 = new System.Windows.Forms.GroupBox ();
			this.checkBoxPrune = new System.Windows.Forms.CheckBox ();
			this.radioButtonRawRectangles = new System.Windows.Forms.RadioButton ();
			this.radioButtonMergedRect = new System.Windows.Forms.RadioButton ();
			this.checkBoxDetect = new System.Windows.Forms.CheckBox ();
			this.checkBoxLabel = new System.Windows.Forms.CheckBox ();
			this.labelStatus = new System.Windows.Forms.Label ();
			this.labelHeader = new System.Windows.Forms.Label ();
			this.labelFaceBorder = new System.Windows.Forms.Label ();
			this.labelRectInfo = new System.Windows.Forms.Label ();
			this.labelFaceBorderBig = new System.Windows.Forms.Label ();
			this.buttonDefaultThreshold = new System.Windows.Forms.Button ();
			this.labelMaxThreshold = new System.Windows.Forms.Label ();
			this.labelMinThreshold = new System.Windows.Forms.Label ();
			this.labelThreshold = new System.Windows.Forms.Label ();
			this.trackBarThreshold = new System.Windows.Forms.TrackBar ();
			this.labelDetectedRect = new System.Windows.Forms.Label ();
			this.listBoxDetectedRect = new System.Windows.Forms.ListBox ();
			this.groupBox1.SuspendLayout ();
			((System.ComponentModel.ISupportInitialize)(this.trackBarThreshold)).BeginInit ();
			this.SuspendLayout ();
			// 
			// listBoxImg
			// 
			this.listBoxImg.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)));
			this.listBoxImg.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
			this.listBoxImg.FormattingEnabled = true;
			this.listBoxImg.Location = new System.Drawing.Point (12, 23);
			this.listBoxImg.Name = "listBoxImg";
			this.listBoxImg.Size = new System.Drawing.Size (143, 810);
			this.listBoxImg.TabIndex = 0;
			this.listBoxImg.DrawItem += new System.Windows.Forms.DrawItemEventHandler (this.ImgListBoxDrawItem);
			this.listBoxImg.SelectedIndexChanged += new System.EventHandler (this.listBoxImg_SelectedIndexChanged);
			this.listBoxImg.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler (this.ImgListBoxMeasureItem);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point (12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size (85, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Image Collection";
			// 
			// buttonLoad
			// 
			this.buttonLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonLoad.Location = new System.Drawing.Point (12, 839);
			this.buttonLoad.Name = "buttonLoad";
			this.buttonLoad.Size = new System.Drawing.Size (143, 28);
			this.buttonLoad.TabIndex = 3;
			this.buttonLoad.Text = "&Load...";
			this.buttonLoad.UseVisualStyleBackColor = true;
			this.buttonLoad.Click += new System.EventHandler (this.buttonLoad_Click);
			// 
			// labelImgName
			// 
			this.labelImgName.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelImgName.Font = new System.Drawing.Font ("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelImgName.Location = new System.Drawing.Point (161, 23);
			this.labelImgName.Name = "labelImgName";
			this.labelImgName.Size = new System.Drawing.Size (697, 36);
			this.labelImgName.TabIndex = 4;
			this.labelImgName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// labelImageBorder
			// 
			this.labelImageBorder.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelImageBorder.Location = new System.Drawing.Point (161, 63);
			this.labelImageBorder.Name = "labelImageBorder";
			this.labelImageBorder.Size = new System.Drawing.Size (697, 804);
			this.labelImageBorder.TabIndex = 5;
			this.labelImageBorder.Visible = false;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add (this.checkBoxPrune);
			this.groupBox1.Controls.Add (this.radioButtonRawRectangles);
			this.groupBox1.Controls.Add (this.radioButtonMergedRect);
			this.groupBox1.Controls.Add (this.checkBoxDetect);
			this.groupBox1.Controls.Add (this.checkBoxLabel);
			this.groupBox1.Location = new System.Drawing.Point (1048, 23);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size (265, 101);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Settings";
			// 
			// checkBoxPrune
			// 
			this.checkBoxPrune.AutoSize = true;
			this.checkBoxPrune.Location = new System.Drawing.Point (16, 53);
			this.checkBoxPrune.Name = "checkBoxPrune";
			this.checkBoxPrune.Size = new System.Drawing.Size (54, 17);
			this.checkBoxPrune.TabIndex = 5;
			this.checkBoxPrune.Text = "&Prune";
			this.checkBoxPrune.UseVisualStyleBackColor = true;
			this.checkBoxPrune.CheckedChanged += new System.EventHandler (this.checkBoxPrune_CheckedChanged);
			// 
			// radioButtonRawRectangles
			// 
			this.radioButtonRawRectangles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonRawRectangles.AutoSize = true;
			this.radioButtonRawRectangles.Location = new System.Drawing.Point (146, 65);
			this.radioButtonRawRectangles.Name = "radioButtonRawRectangles";
			this.radioButtonRawRectangles.Size = new System.Drawing.Size (47, 17);
			this.radioButtonRawRectangles.TabIndex = 4;
			this.radioButtonRawRectangles.TabStop = true;
			this.radioButtonRawRectangles.Text = "&Raw";
			this.radioButtonRawRectangles.UseVisualStyleBackColor = true;
			this.radioButtonRawRectangles.CheckedChanged += new System.EventHandler (this.radioButtonRawRectangles_CheckedChanged);
			// 
			// radioButtonMergedRect
			// 
			this.radioButtonMergedRect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioButtonMergedRect.AutoSize = true;
			this.radioButtonMergedRect.Location = new System.Drawing.Point (146, 42);
			this.radioButtonMergedRect.Name = "radioButtonMergedRect";
			this.radioButtonMergedRect.Size = new System.Drawing.Size (61, 17);
			this.radioButtonMergedRect.TabIndex = 3;
			this.radioButtonMergedRect.TabStop = true;
			this.radioButtonMergedRect.Text = "&Merged";
			this.radioButtonMergedRect.UseVisualStyleBackColor = true;
			this.radioButtonMergedRect.CheckedChanged += new System.EventHandler (this.radioButtonMergedRect_CheckedChanged);
			// 
			// checkBoxDetect
			// 
			this.checkBoxDetect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxDetect.AutoSize = true;
			this.checkBoxDetect.Location = new System.Drawing.Point (136, 19);
			this.checkBoxDetect.Name = "checkBoxDetect";
			this.checkBoxDetect.Size = new System.Drawing.Size (123, 17);
			this.checkBoxDetect.TabIndex = 1;
			this.checkBoxDetect.Text = "Show &Detection Info";
			this.checkBoxDetect.UseVisualStyleBackColor = true;
			this.checkBoxDetect.CheckedChanged += new System.EventHandler (this.checkBoxDetect_CheckedChanged);
			// 
			// checkBoxLabel
			// 
			this.checkBoxLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.checkBoxLabel.AutoSize = true;
			this.checkBoxLabel.Location = new System.Drawing.Point (15, 19);
			this.checkBoxLabel.Name = "checkBoxLabel";
			this.checkBoxLabel.Size = new System.Drawing.Size (103, 17);
			this.checkBoxLabel.TabIndex = 0;
			this.checkBoxLabel.Text = "Show &Label Info";
			this.checkBoxLabel.UseVisualStyleBackColor = true;
			this.checkBoxLabel.CheckedChanged += new System.EventHandler (this.checkBoxLabel_CheckedChanged);
			// 
			// labelStatus
			// 
			this.labelStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelStatus.Location = new System.Drawing.Point (-1, 883);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size (1324, 31);
			this.labelStatus.TabIndex = 7;
			this.labelStatus.Text = "label2";
			// 
			// labelHeader
			// 
			this.labelHeader.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelHeader.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelHeader.Font = new System.Drawing.Font ("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelHeader.Location = new System.Drawing.Point (864, 130);
			this.labelHeader.Name = "labelHeader";
			this.labelHeader.Size = new System.Drawing.Size (449, 22);
			this.labelHeader.TabIndex = 11;
			this.labelHeader.Text = "Detection Info";
			// 
			// labelFaceBorder
			// 
			this.labelFaceBorder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelFaceBorder.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelFaceBorder.Location = new System.Drawing.Point (864, 163);
			this.labelFaceBorder.Name = "labelFaceBorder";
			this.labelFaceBorder.Size = new System.Drawing.Size (48, 48);
			this.labelFaceBorder.TabIndex = 12;
			this.labelFaceBorder.Visible = false;
			// 
			// labelRectInfo
			// 
			this.labelRectInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.labelRectInfo.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelRectInfo.Location = new System.Drawing.Point (1112, 163);
			this.labelRectInfo.Name = "labelRectInfo";
			this.labelRectInfo.Size = new System.Drawing.Size (201, 466);
			this.labelRectInfo.TabIndex = 13;
			// 
			// labelFaceBorderBig
			// 
			this.labelFaceBorderBig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelFaceBorderBig.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelFaceBorderBig.Location = new System.Drawing.Point (918, 163);
			this.labelFaceBorderBig.Name = "labelFaceBorderBig";
			this.labelFaceBorderBig.Size = new System.Drawing.Size (188, 190);
			this.labelFaceBorderBig.TabIndex = 16;
			this.labelFaceBorderBig.Visible = false;
			// 
			// buttonDefaultThreshold
			// 
			this.buttonDefaultThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonDefaultThreshold.Location = new System.Drawing.Point (864, 770);
			this.buttonDefaultThreshold.Name = "buttonDefaultThreshold";
			this.buttonDefaultThreshold.Size = new System.Drawing.Size (127, 31);
			this.buttonDefaultThreshold.TabIndex = 26;
			this.buttonDefaultThreshold.Text = "De&fault!";
			this.buttonDefaultThreshold.UseVisualStyleBackColor = true;
			this.buttonDefaultThreshold.Click += new System.EventHandler (this.buttonDefaultThreshold_Click);
			// 
			// labelMaxThreshold
			// 
			this.labelMaxThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.labelMaxThreshold.Location = new System.Drawing.Point (878, 654);
			this.labelMaxThreshold.Name = "labelMaxThreshold";
			this.labelMaxThreshold.Size = new System.Drawing.Size (113, 24);
			this.labelMaxThreshold.TabIndex = 25;
			this.labelMaxThreshold.Text = "Max =";
			this.labelMaxThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// labelMinThreshold
			// 
			this.labelMinThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.labelMinThreshold.Location = new System.Drawing.Point (869, 841);
			this.labelMinThreshold.Name = "labelMinThreshold";
			this.labelMinThreshold.Size = new System.Drawing.Size (122, 24);
			this.labelMinThreshold.TabIndex = 24;
			this.labelMinThreshold.Text = "Min = ";
			this.labelMinThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// labelThreshold
			// 
			this.labelThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.labelThreshold.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelThreshold.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.labelThreshold.Font = new System.Drawing.Font ("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.labelThreshold.Location = new System.Drawing.Point (864, 735);
			this.labelThreshold.Name = "labelThreshold";
			this.labelThreshold.Size = new System.Drawing.Size (127, 32);
			this.labelThreshold.TabIndex = 23;
			this.labelThreshold.Text = "Threshold =";
			this.labelThreshold.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// trackBarThreshold
			// 
			this.trackBarThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBarThreshold.Location = new System.Drawing.Point (997, 640);
			this.trackBarThreshold.Name = "trackBarThreshold";
			this.trackBarThreshold.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.trackBarThreshold.Size = new System.Drawing.Size (45, 240);
			this.trackBarThreshold.TabIndex = 22;
			this.trackBarThreshold.Scroll += new System.EventHandler (this.trackBarThreshold_Scroll);
			// 
			// labelDetectedRect
			// 
			this.labelDetectedRect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.labelDetectedRect.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.labelDetectedRect.Location = new System.Drawing.Point (1048, 649);
			this.labelDetectedRect.Name = "labelDetectedRect";
			this.labelDetectedRect.Size = new System.Drawing.Size (265, 29);
			this.labelDetectedRect.TabIndex = 21;
			// 
			// listBoxDetectedRect
			// 
			this.listBoxDetectedRect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.listBoxDetectedRect.FormattingEnabled = true;
			this.listBoxDetectedRect.Location = new System.Drawing.Point (1048, 681);
			this.listBoxDetectedRect.Name = "listBoxDetectedRect";
			this.listBoxDetectedRect.Size = new System.Drawing.Size (265, 186);
			this.listBoxDetectedRect.TabIndex = 20;
			this.listBoxDetectedRect.SelectedIndexChanged += new System.EventHandler (this.listBoxDetectedRect_SelectedIndexChanged);
			// 
			// DetectView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size (1325, 914);
			this.Controls.Add (this.buttonDefaultThreshold);
			this.Controls.Add (this.labelMaxThreshold);
			this.Controls.Add (this.labelMinThreshold);
			this.Controls.Add (this.labelThreshold);
			this.Controls.Add (this.trackBarThreshold);
			this.Controls.Add (this.labelDetectedRect);
			this.Controls.Add (this.listBoxDetectedRect);
			this.Controls.Add (this.labelFaceBorderBig);
			this.Controls.Add (this.labelRectInfo);
			this.Controls.Add (this.labelFaceBorder);
			this.Controls.Add (this.labelHeader);
			this.Controls.Add (this.labelStatus);
			this.Controls.Add (this.groupBox1);
			this.Controls.Add (this.labelImageBorder);
			this.Controls.Add (this.labelImgName);
			this.Controls.Add (this.buttonLoad);
			this.Controls.Add (this.label1);
			this.Controls.Add (this.listBoxImg);
			this.Name = "DetectView";
			this.Text = "Detailed Detection Info";
			this.Paint += new System.Windows.Forms.PaintEventHandler (this.OnPaint);
			this.Resize += new System.EventHandler (this.OnResize);
			this.groupBox1.ResumeLayout (false);
			this.groupBox1.PerformLayout ();
			((System.ComponentModel.ISupportInitialize)(this.trackBarThreshold)).EndInit ();
			this.ResumeLayout (false);
			this.PerformLayout ();

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxImg;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonLoad;
		private System.Windows.Forms.Label labelImgName;
		private System.Windows.Forms.Label labelImageBorder;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton radioButtonMergedRect;
		private System.Windows.Forms.RadioButton radioButtonRawRectangles;
		private System.Windows.Forms.CheckBox checkBoxDetect;
		private System.Windows.Forms.CheckBox checkBoxLabel;
		private System.Windows.Forms.Label labelStatus;
		private System.Windows.Forms.Label labelHeader;
		private System.Windows.Forms.Label labelFaceBorder;
		private System.Windows.Forms.Label labelRectInfo;
		private System.Windows.Forms.CheckBox checkBoxPrune;
		private System.Windows.Forms.Label labelFaceBorderBig;
		private System.Windows.Forms.Button buttonDefaultThreshold;
		private System.Windows.Forms.Label labelMaxThreshold;
		private System.Windows.Forms.Label labelMinThreshold;
		private System.Windows.Forms.Label labelThreshold;
		private System.Windows.Forms.TrackBar trackBarThreshold;
		private System.Windows.Forms.Label labelDetectedRect;
		private System.Windows.Forms.ListBox listBoxDetectedRect;
	}
}

