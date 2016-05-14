using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using Microsoft.LiveLabs;

namespace FaceSortUI
{
    [Serializable()]
    public partial class OptionDialog : Form, ISerializable
    {
        private string[] _supportedImageTypes;
        private System.Windows.Point _normalizedLeftEyePos = new System.Windows.Point(25, 30);
        private System.Windows.Point _normalizedRightEyePos = new System.Windows.Point(55, 30);

        public OptionDialog()
        {
            InitializeComponent();
        }
        protected OptionDialog(SerializationInfo info, StreamingContext context)
        {
            InitializeComponent();
            try
            {
                MaximumImages = info.GetInt32("UpDown");
                AnimationDuration = info.GetDouble("AnimDuration");
                FaceDetectorThreshold = (float)info.GetDouble("FaceDetectThresh");
                IsStatusVisible = info.GetBoolean("StatusVisible");
                IsDebugVisible = info.GetBoolean("DebugVisible");
                FaceDetectorDataPath = info.GetString("FaceDetectorPath");
                EyeDetectPathName = info.GetString("EyeDetectPathName");
                BackEndConfigFile = info.GetString("BackEndConfigFile");
                PhotoWidth = info.GetDouble("PhotoWidth");
                FaceDisplayBitmap = (Face.DisplayBitmapEnum)info.GetInt32("FaceDisplayMode");
                DoGroupRedisplay = info.GetBoolean("DoGroupRedisplay");
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                // benign probably an out of date file
            }
            catch (Exception)
            {
                // Unexpected
            }
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("UpDown", (int)MaximumImages);
            info.AddValue("AnimDuration", (double)AnimationDuration);
            info.AddValue("FaceDetectThresh", (float)FaceDetectorThreshold);
            info.AddValue("StatusVisible", (bool)IsStatusVisible);
            info.AddValue("DebugVisible", (bool)IsDebugVisible);
            info.AddValue("FaceDetectorPath", FaceDetectorDataPathNoExpand);
            info.AddValue("BackEndConfigFile", BackEndConfigFileNoExpand);
            info.AddValue("EyeDetectPathName", EyeDetectPathNameNoExpand);
            info.AddValue("PhotoWidth", PhotoWidth);
            info.AddValue("FaceDisplayMode", (Int32)(FaceDisplayBitmap));
            info.AddValue("FaceDetectTargetWidth", (Int32)(FaceDetectTargetWidth));
            info.AddValue("FaceDetectTargetHeight", (Int32)(FaceDetectTargetHeight));
            info.AddValue("DoGroupRedisplay", (bool)(DoGroupRedisplay));
        }

        /// <summary>
        /// Returns the maximum number of images that should be loaded
        /// </summary>
        public int MaximumImages
        {
            get
            {
                return (int)numericUpDownMaxImages.Value;
            }
            set
            {
                numericUpDownMaxImages.Value = value;
            }
        }

        /// <summary>
        /// Returns animation duration
        /// </summary>
        public double AnimationDuration
        {
            get
            {
                return (double)numericUpDownAnim.Value;
            }
            set
            {
                numericUpDownAnim.Value = (decimal)value;
            }
        }
        /// <summary>
        /// Returns animation duration
        /// </summary>
        public float FaceDetectorThreshold
        {
            get
            {
                return (float)numericUpDownFaceDetThresh.Value;
            }
            set
            {
                numericUpDownFaceDetThresh.Value = (decimal)value;
            }
        }

        /// <summary>
        /// Returns if the Status bar Check box is checked
        /// </summary>
        public bool IsStatusVisible
        {
            get
            {
                return checkBoxShowStatusBar.Checked;
            }
            set
            {
                checkBoxShowStatusBar.Checked = value;
            }
        }
        /// <summary>
        /// Returns if the Debug checkBox is checked
        /// </summary>
        public bool IsDebugVisible
        {
            get
            {
                return checkBoxShowDebugPanel.Checked;
            }
            set
            {
                checkBoxShowDebugPanel.Checked = value;
            }
        }

        public string FaceDetectorDataPathNoExpand
        {
            get
            {
                return textBoxFaceDetector.Text;
            }
        }

        public string FaceDetectorDataPath
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(textBoxFaceDetector.Text);
            }

            set
            {
                textBoxFaceDetector.Text = value;
            }
        }


        public bool DoGroupRedisplay
        {
            get
            {
                return checkBoxCompactGroups.Checked;
            }
            set
            {
                checkBoxCompactGroups.Checked = value;
            }
        }
        public Face.DisplayBitmapEnum FaceDisplayBitmap
        {
            get
            {
                Face.DisplayBitmapEnum ret = Face.DisplayBitmapEnum.Primary;
                if (checkBoxNormalizedFace.Checked == false)
                {
                    ret = Face.DisplayBitmapEnum.Alternate1;
                }
                return ret;
            }
            set
            {
                switch (value)
                {
                    case Face.DisplayBitmapEnum.Alternate1:
                        checkBoxNormalizedFace.Checked  = false;
                        break;

                    case Face.DisplayBitmapEnum.Primary:
                        checkBoxNormalizedFace.Checked  = true;
                        break;

                    default:
                        checkBoxNormalizedFace.Checked  = true;
                        break;

                }
            }
        }
        public string BackEndConfigFileNoExpand
        {
            get
            {
                return textBoxBackEndConfigFile.Text;
            }
        }

        public string BackEndConfigFile
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(textBoxBackEndConfigFile.Text);
            }

            set
            {
                textBoxBackEndConfigFile.Text = value;
            }
        }


        public string EyeDetectPathNameNoExpand
        {
            get
            {
                return textBoxEyeDetectPath.Text;
            }
        }

        public string EyeDetectPathName
        {
            get
            {
                return Environment.ExpandEnvironmentVariables(textBoxEyeDetectPath.Text);
            }

            set
            {
                textBoxEyeDetectPath.Text = value;
            }
        }

        public EyeDetect.AlgorithmEnum EyeDetectAlgo
        {
            get
            {
                if (EyeDetectPathName.Length <= 0)
                {
                    return EyeDetect.AlgorithmEnum.MSRA;
                }
                return EyeDetect.AlgorithmEnum.NN;
            }
        }
        public bool IsShowEyes
        {
            get
            {
                return checkBoxShowEyes.Checked;
            }
            set
            {
                checkBoxShowEyes.Checked = value;
            }
        }


        public double PhotoWidth
        {
            get
            {
                return (double)numericUpDownPhotoWidth.Value;
            }
            set
            {
                numericUpDownPhotoWidth.Value = (decimal)value;
            }
        }

        public int FaceDetectTargetWidth
        {
            get
            {
                return (int)numericUpDownDetectX.Value;
            }
            set
            {
                numericUpDownDetectX.Value = value;
            }
        }

        public int FaceDetectTargetHeight
        {
            get
            {
                return (int)numericUpDownDetectY.Value;
            }
            set
            {
                numericUpDownDetectY.Value = value;
            }
        }

        public int BorderWidth
        {
            get
            {
                return 3;
            }
        }

        public double DefaultMaxDistance
        {
            get
            {
                return 100000.0;
            }
        }
        public double EmphasisLineThickness
        {
            get
            {
                return 5.0;
            }
        }
        public double DefaultLineThickness
        {
            get
            {
                return 3.0;
            }
        }


        public int DefaultDPI
        {
            get
            {
                return 96;
            }
        }

        public int FaceDisplayWidth
        {
            get
            {
                return 80;
            }
        }

        public int GridFaceSpace
        {
            get
            {
                return 10;
            }
        }

        public string[] SupportedImageTypes
        {
            get
            {
                if (null == _supportedImageTypes)
                {
                    _supportedImageTypes = new string[] { "jpg", "jpeg", "png", "pgm", "bmp" };
                }
                return _supportedImageTypes;
            }
        }

        public System.Windows.Point NormalizeLeftEyeLocation
        {
            get
            {
                return _normalizedLeftEyePos;
            }

        }

        public System.Windows.Point NormalizeRightEyeLocation
        {
            get
            {
                return _normalizedRightEyePos;
            }

        }

        public double EyeDiameter
        {
            get
            {
                return 0.05;
            }
        }
        private void buttonOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void buttonFaceDetector_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            string prevPath = fileDialog.InitialDirectory;

            fileDialog.CheckPathExists = true;
            fileDialog.Multiselect = false;
            fileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;
            fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(FaceDetectorDataPath);


            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                FaceDetectorDataPath = fileDialog.FileName;
            }

        }

        private void buttonBackEndConfigFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckPathExists = true;
            fileDialog.Multiselect = false;
            fileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;
            fileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(BackEndConfigFile);

            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                BackEndConfigFile = fileDialog.FileName;
            }
        }

        private void buttonEyeDetectPath_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckPathExists = true;
            fileDialog.Multiselect = false;
            fileDialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;

            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                EyeDetectPathName = fileDialog.FileName;
            }
        }

        private void textBoxBackEndConfigFile_TextChanged(object sender, EventArgs e)
        {

        }

    }
}