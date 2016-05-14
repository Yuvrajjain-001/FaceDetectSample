using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DetectionManagedLib;

namespace DetectView
{
	public partial class DetectView : Form
	{
		const float _eMinThresh = -10.0f,
			_eMaxThresh = 10.0f,
			_eStepSize = 0.1f;

		const String strDetectorPath = @".\classifier.txt";

		FaceDetector _faceDetect = null;

		LabeledImageCollection _ImgCol = null;

		bool _fImgDirty = true,
			_fDetectionDirty = true;

		public DetectView ()
		{
			InitializeComponent ();

			checkBoxLabel.Checked = true;
			checkBoxDetect.Checked = true;

			radioButtonMergedRect.Checked = true;
			radioButtonMergedRect.Enabled = checkBoxDetect.Checked;
			radioButtonRawRectangles.Enabled = checkBoxDetect.Checked;
			checkBoxPrune.Checked = true;

			_faceDetect = new FaceDetector (strDetectorPath, checkBoxPrune.Checked, _eMinThresh);

			if (	_faceDetect.DefaultThreshold < _eMinThresh ||
					_faceDetect.DefaultThreshold > _eMaxThresh
				)
			{
				throw new Exception("Cannot continue: Loaded classifier has a final threshold that is out of the expected range");
			}

			labelMinThreshold.Text = "Min = " + _eMinThresh.ToString ("F4");
			labelMaxThreshold.Text = "Max = " + _eMaxThresh.ToString ("F4");

			trackBarThreshold.SetRange ((int)(_eMinThresh / _eStepSize), (int)(_eMaxThresh / _eStepSize));
			trackBarThreshold.Value = (int)(_faceDetect.DefaultThreshold / _eStepSize);

			labelThreshold.Text = "Threshold = " + (trackBarThreshold.Value * _eStepSize).ToString ("F4");
		}

		private void buttonLoad_Click (object sender, EventArgs e)
		{
			try
			{
				OpenFileDialog Dlg = new OpenFileDialog ();

				Dlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
				Dlg.FilterIndex = 1;
				Dlg.CheckFileExists = true;
				Dlg.RestoreDirectory = false;

				if (Dlg.ShowDialog () == DialogResult.OK)
				{
					listBoxImg.Items.Clear ();

					Cursor = Cursors.WaitCursor;

					_ImgCol = new LabeledImageCollection (Dlg.FileName);

					foreach (LabeledImg img in _ImgCol.ImgList)
					{
						listBoxImg.Items.Add (img);
					}

					Text = Dlg.FileName;

					Cursor = Cursors.Default;
				}
			}
			catch (Exception ex)
			{
				Cursor = Cursors.Default;

				if (ex.InnerException != null)
				{
					MessageBox.Show (ex.InnerException.Message, ex.Message);
				}
				else
				{
					MessageBox.Show (ex.Message);
				}
			}
		}

		private void ImgListBoxDrawItem (object sender, DrawItemEventArgs e)
		{
			if (e != null && _ImgCol != null && e.Index >= 0 && e.Index < _ImgCol.ImgList.Count)
			{
				LabeledImg labeledImg = _ImgCol.ImgList[e.Index];

				Bitmap img = new Bitmap(labeledImg.FileName);

				Rectangle	rectDraw = e.Bounds;

				// offset
				rectDraw.Inflate (-1, 0);

				// draw a boundary if selected
				if ((e.State & DrawItemState.Selected) != 0)
				{
					e.Graphics.DrawRectangle (new Pen (Color.Blue), rectDraw);
				}
				else
				{
					e.Graphics.DrawRectangle (new Pen (Color.White), rectDraw);
				}

				// offset
				rectDraw.Offset (5, 5);
				rectDraw.Inflate (-10, -10);

				Rectangle rect = rectDraw;

				if ((img.Width * rectDraw.Height) > (img.Height * rectDraw.Width))
				{
					rect.Width = rectDraw.Width;
					rect.Height = ((img.Height * rectDraw.Width) / img.Width);
				}
				else
				{
					rect.Height = rectDraw.Height;
					rect.Width = ((img.Width * rectDraw.Height) / img.Height);
				}

				rect.X += (rectDraw.Width - rect.Width) / 2;
				rect.Y += (rectDraw.Height - rect.Height) / 2;

				e.Graphics.DrawImage (img, rect);

				e.Graphics.DrawString ((e.Index + 1).ToString (),
					new Font ("Arial", 14),
					new SolidBrush (Color.Maroon),
					new PointF (rectDraw.X + 5, rectDraw.Y + 5));
			}
		}

		private void ImgListBoxMeasureItem (object sender, MeasureItemEventArgs e)
		{
			e.ItemWidth = listBoxImg.ClientRectangle.Width;
			e.ItemHeight = listBoxImg.ClientRectangle.Height / 10;
		}

		private void OnResize (object sender, EventArgs e)
		{
			// change the draw mode to trigger a measure item event
			listBoxImg.DrawMode = DrawMode.OwnerDrawFixed;
			listBoxImg.DrawMode = DrawMode.OwnerDrawVariable;

			listBoxImg.Invalidate ();
		}

		DetectionResult DetectFaces (LabeledImg labeledImg)
		{
			DetectionResult detRes = null;

			if (checkBoxDetect.Checked)
			{
				try
				{
					labelStatus.Text = "Detecting...";
					Application.DoEvents ();

					Cursor = Cursors.WaitCursor;

					detRes = labeledImg.GetDetectionResult (_faceDetect);

					if (detRes != null)
					{
						labelStatus.Text = "Detection Time: " + detRes.tmDetection.Ticks / 10000 + " msec";
					}
					else
					{
						labelStatus.Text = "Failed!";
					}

					Cursor = Cursors.Default;
				}
				catch (Exception ex)
				{
					Cursor = Cursors.Default;

					MessageBox.Show (ex.Message);

					return null;
				}
			}

			return detRes;
		}

		void RenderImage (Graphics graf)
		{
			if (	_ImgCol == null ||
					listBoxImg.SelectedIndex < 0 ||
					listBoxImg.SelectedIndex >= _ImgCol.ImgList.Count
				)
			{
				_fImgDirty =
				_fDetectionDirty = false;

				return;
			}

			LabeledImg labeledImg = _ImgCol.ImgList[listBoxImg.SelectedIndex];

			// render the image
			if (_fImgDirty)
			{
				labelImgName.Text = labeledImg.FileName;

				Rectangle	rect = labelImageBorder.Bounds;
				rect.Width = labelHeader.Left - 1 - labelImageBorder.Bounds.Left;
				rect.Height = labelStatus.Top - 1 - labelImageBorder.Bounds.Top;

				graf.FillRectangle (new SolidBrush(BackColor), rect);

				labeledImg.Render (graf, labelImageBorder.Bounds);

				if (checkBoxLabel.Checked)
				{
					labeledImg.RenderAnnotations (graf, labelImageBorder.Bounds);
				}

				_fImgDirty = false;
			}

			if (_fDetectionDirty)
			{
				if (checkBoxDetect.Checked)
				{
					DetectionResult detRes = DetectFaces (labeledImg);

					if (detRes != null)
					{
						float eThreshold = trackBarThreshold.Value * _eStepSize;

						if (radioButtonRawRectangles.Checked)
						{
							detRes.RenderRawRect (eThreshold, graf, labelImageBorder.Bounds);
						}

						if (radioButtonMergedRect.Checked)
						{
							detRes.RenderMergedRect (eThreshold, graf, labelImageBorder.Bounds);
						}
					}

					if (listBoxDetectedRect.Items.Count > 0)
					{
						ScoredRect scoredRect = (ScoredRect)listBoxDetectedRect.SelectedItem;

						if (scoredRect != null)
						{
							labelRectInfo.Text = "Rect #" + listBoxDetectedRect.SelectedIndex + "\r\n" +
										scoredRect.ToString () + "\r\n" +
										"Score: " + scoredRect.Score;

							scoredRect.Render (graf, labelImageBorder.Bounds, true);

							labeledImg.Render (graf, scoredRect.Bounds, labelFaceBorder.Bounds);
							labeledImg.Render (graf, scoredRect.Bounds, labelFaceBorderBig.Bounds);
						}
					}
				}
		
				_fDetectionDirty = false;
			}
		}

		
		private void listBoxImg_SelectedIndexChanged (object sender, EventArgs e)
		{
			Graphics graf = CreateGraphics ();

			graf.FillRectangle (new SolidBrush(BackColor), labelImageBorder.Bounds);

			// render the image and label info
			_fImgDirty = true;
			RenderImage (graf);

			// show the detected rectangles
			ShowDetectedRect ();

			if (listBoxDetectedRect.Items.Count > 0)
			{
				listBoxDetectedRect.SelectedIndex = 0;
			}
		}

		private void OnPaint (object sender, PaintEventArgs e)
		{
			_fImgDirty =
			_fDetectionDirty = true;

			RenderImage (e.Graphics);
		}

		private void checkBoxLabel_CheckedChanged (object sender, EventArgs e)
		{
			_fImgDirty =
			_fDetectionDirty = true;

			RenderImage(CreateGraphics());
		}

		private void checkBoxDetect_CheckedChanged (object sender, EventArgs e)
		{
			radioButtonMergedRect.Enabled = checkBoxDetect.Checked;
			radioButtonRawRectangles.Enabled = checkBoxDetect.Checked;

			_fImgDirty =
			_fDetectionDirty = true;

			RenderImage (CreateGraphics ());
		}

		private void radioButtonMergedRect_CheckedChanged (object sender, EventArgs e)
		{
			ShowDetectedRect ();

			if (listBoxDetectedRect.Items.Count > 0)
			{
				listBoxDetectedRect.SelectedIndex = 0;
			}
			else
			{
				_fImgDirty = true;
				RenderImage (CreateGraphics());
			}
		}

		private void radioButtonRawRectangles_CheckedChanged (object sender, EventArgs e)
		{
			ShowDetectedRect ();

			if (listBoxDetectedRect.Items.Count > 0)
			{
				listBoxDetectedRect.SelectedIndex = 0;
			}
			else
			{
				_fImgDirty = true;
				RenderImage (CreateGraphics ());
			}
		}

		void ShowDetectedRect ()
		{
			labelStatus.Text = "";
			labelDetectedRect.Text = "";
			labelRectInfo.Text = "";
			listBoxDetectedRect.Items.Clear ();

			if (	_ImgCol != null &&
					listBoxImg.SelectedIndex >= 0 &&
					listBoxImg.SelectedIndex < _ImgCol.ImgList.Count &&
					checkBoxDetect.Checked
				)
			{
				
				LabeledImg labeledImg = _ImgCol.ImgList[listBoxImg.SelectedIndex];

				if (labeledImg != null)
				{
					List<ScoredRect> ascoredRect = null;
					DetectionResult detRes = DetectFaces (labeledImg);

					if (detRes != null)
					{
						float eThreshold = trackBarThreshold.Value * _eStepSize;

						if (radioButtonMergedRect.Checked)
						{
							ascoredRect = detRes.GetMergedRectList(eThreshold);
						}
						else
						{
							ascoredRect = detRes.GetRawRectList (eThreshold);
						}
					}

					if (ascoredRect != null)
					{
						listBoxDetectedRect.SuspendLayout ();

						listBoxDetectedRect.Items.Clear ();

						labelDetectedRect.Text = ascoredRect.Count + " rectangle(s)";

						foreach (ScoredRect rect in ascoredRect)
						{
							listBoxDetectedRect.Items.Add (rect);
						}

						listBoxDetectedRect.ResumeLayout ();
					}
				}
			}
		}

		private void listBoxDetectedRect_SelectedIndexChanged (object sender, EventArgs e)
		{
			if (	_ImgCol != null &&
					listBoxImg.SelectedIndex >= 0 &&
					listBoxImg.SelectedIndex < _ImgCol.ImgList.Count
				)
			{
				_fImgDirty =
				_fDetectionDirty = true;

				RenderImage (CreateGraphics ());
			}
		}

		private void checkBoxPrune_CheckedChanged (object sender, EventArgs e)
		{
			if (_faceDetect != null)
			{
				_faceDetect.SetPrune (checkBoxPrune.Checked);
			}

			// recompute the detected rectangles
			if (listBoxDetectedRect.Items.Count > 0)
			{
				listBoxDetectedRect.SelectedIndex = 0;
			}
			else
			{
				_fImgDirty = true;
				RenderImage (CreateGraphics ());
			}
		}

		private void trackBarThreshold_Scroll (object sender, EventArgs e)
		{
			labelThreshold.Text = "Threshold = " + (trackBarThreshold.Value * _eStepSize).ToString ("F4");

			// recompute the detected rectangles
			ShowDetectedRect ();

			if (listBoxDetectedRect.Items.Count > 0)
			{
				listBoxDetectedRect.SelectedIndex = 0;
			}
			else
			{
				_fImgDirty = true;
				RenderImage (CreateGraphics ());
			}
		}

		private void buttonDefaultThreshold_Click (object sender, EventArgs e)
		{
			trackBarThreshold.Value = (int)(_faceDetect.DefaultThreshold / _eStepSize);

			trackBarThreshold_Scroll (this, null);
		}
	}
}