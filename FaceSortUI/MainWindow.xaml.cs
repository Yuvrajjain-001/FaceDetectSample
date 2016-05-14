using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace FaceSortUI
{
    /// <summary>
    /// Top level window Contains the mainWindow canvas
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private BackgroundCanvas _mainCanvas;
        private Canvas _background;
        private MainWindowLayout _mainWindowLayout;
        private DockPanel _mainDocPanel;
        private Border _statusBox;
        private Border _debugBox;
        private System.Windows.Controls.Button _photoButton;
        private System.Windows.Controls.Button _layoutUpdateButton;
        private TextBlock _statusTextBox;
        private TextBlock _errorTextBox;
        private OptionDialog _optionsDialog;

        private static string defaultConfigPath;
        private static string defaultConfigDirectory = "\\Microsoft\\FaceSort\\";
        private static string defaultConfigFile = "config.cfg";
        private static Brush _buttonAlternateColor = Brushes.Green;
        private static Brush _buttonDefaultColor = Brushes.Wheat;
        private static String _photoDisabledText = "Hide";
        private static String _photoEnabledText = "Show";


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pathname">pathhname to load</param>
        public MainWindow(string pathname)
        {
            InitializeComponent();

            _mainWindowLayout = new MainWindowLayout(this);
            //Deserialize the Options dialog
            defaultConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + defaultConfigDirectory;
            if (true != Deserialize(defaultConfigPath + defaultConfigFile, false))
            {
                _optionsDialog = new OptionDialog();
            }

            _background = new Canvas();
            _mainCanvas = new BackgroundCanvas(this, _background);

            _background.Children.Add(_mainCanvas);

            Content = _mainWindowLayout;
            SetMemberVars();
            SetBackgroundImage();
            _mainCanvas.Background = Brushes.Transparent;
            SizeChanged += SizeChangedHandler;


            if (null != pathname && pathname.Length > 0)
            {
                _mainCanvas.AddPhotoCollection(pathname, _optionsDialog.MaximumImages);
                //_mainCanvas.AddImageCollection(pathname, _optionsDialog.MaximumImages);
                //CreateGallery(pathname);
            }

            SetPanelVisibility();
        }

        /// <summary>
        /// Top level manager to deserialize a file. Serialization
        /// Serialization can contains (a) Options and optionally
        /// the UI display
        /// </summary>
        /// <param name="serializedFile">File name to deserialize</param>
        /// <param name="doMainCanvas">When true deserialize the UI. False desrerialize only options</param>
        /// <returns></returns>
        public bool Deserialize(string serializedFile, bool doMainCanvas)
        {
            bool returnValue = false;

            if (File.Exists(serializedFile))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(serializedFile, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        _optionsDialog = (OptionDialog)formatter.Deserialize(stream);

                        if (true == doMainCanvas)
                        {
                            _background.Children.Clear();
                            BackgroundCanvas oldCanvas = _mainCanvas;
                            _mainCanvas = (BackgroundCanvas)formatter.Deserialize(stream);
                            _mainCanvas.Deserialize(this, _background);
                            _mainCanvas.RebuildTree(_mainCanvas);
                            _background.Children.Add(_mainCanvas);
                            _mainCanvas.Background = Brushes.Transparent;
                            ResetCanvasSizes();
                            _mainCanvas.DistanceCalculationDelegate = oldCanvas.DistanceCalculationDelegate;
                            _mainCanvas.runMdsDelegate = oldCanvas.runMdsDelegate;
                            _mainCanvas.runMdsUpdate = oldCanvas.runMdsUpdate;
                            oldCanvas = null;
                            returnValue = true;
                        }
                        else
                        {
                            returnValue = true;
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorText = "Error reading " + serializedFile + " " + e.Message;
                    }
                }
            }

            return returnValue;
        }

        /// <summary>
        /// Top level for serializing to a file
        /// </summary>
        /// <param name="serializedFile">File name to Sererialize</param>
        /// <param name="doMainCanvas">If true then include serialization of the whole UI
        /// false only serialize the options dialog</param>
        public void Serialize(string serializedFile, bool  doMainCanvas)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (Stream stream = File.Create(serializedFile))
            {
                formatter.Serialize(stream, _optionsDialog);
                if (true == doMainCanvas)
                {
                    formatter.Serialize(stream, _mainCanvas);
                }
            }
        }

        /// <summary>
        /// Add a collection of faces. A collection can either be a directory
        /// path (all images in directory are added) or a suite file - a text file
        /// containing a list of image file pathnames
        /// </summary>
        /// <param name="pathNames">Either a directory path or suite file</param>
        public void AddFaceCollection(string [] pathNames)
        {

            foreach (string pathname in pathNames)
            {
                _mainCanvas.AddImageCollection(pathname, _optionsDialog.MaximumImages);
            }
        }

        /// <summary>
        /// Add a collection of photos. A collection can either be a directory
        /// path (all photos in directory are added) or a suite file - a text file
        /// containing a list of image file pathnames
        /// </summary>
        /// <param name="pathNames">Either a directory path or suite file</param>
        public void AddPhotoCollection(string[] pathNames)
        {

            foreach (string pathname in pathNames)
            {
                _mainCanvas.AddPhotoCollection(pathname, _optionsDialog.MaximumImages);
            }
        }

        /// <summary>
        /// Temp hack method that reads eye locations from file
        /// </summary>
        /// <param name="pathNames"></param>
        public void AddPhotoCollectionEyes(string[] pathNames)
        {

            foreach (string pathname in pathNames)
            {
                _mainCanvas.AddPhotoCollectionEyes(pathname, _optionsDialog.MaximumImages);
            }
        }
        
        /// <summary>
        /// Add a list of face images
        /// </summary>
        /// <param name="imageNames">Image file pathnames</param>
        public void AddFaceImages(string[] imageNames)
        {

            _mainCanvas.AddImages(imageNames, _optionsDialog.MaximumImages);
        }
        /// <summary>
        /// Show the options dialog
        /// </summary>
        public void ShowOptions()
        {
            if (System.Windows.Forms.DialogResult.OK == _optionsDialog.ShowDialog())
            {
                SetPanelVisibility();
                if (null != _mainCanvas)
                {
                    _mainCanvas.SyncDisplayOptions();
                }

                SaveOptionDialog(defaultConfigPath, defaultConfigFile);
            }
        }
        /// <summary>
        /// Return the current OptionDialog
        /// </summary>
        public OptionDialog OptionDialog
        {
            get
            {
                return _optionsDialog;
            }
        }

        
        /// <summary>
        /// Set or get the text string in the status box
        /// </summary>
        public string StatusText
        {
            get
            {
                if (null != _statusTextBox)
                {
                    return _statusTextBox.Text;
                }
                return "";
            }
            set
            {
                if (null != _statusTextBox)
                {
                    _statusTextBox.Text = "Status: " + value;
                }
            }
        }
        /// <summary>
        /// Set or get the text string in the Error box
        /// </summary>
        public string ErrorText
        {
            get
            {
                if (null != _errorTextBox)
                {
                    return _errorTextBox.Text;
                }
                return "";
            }
            set
            {
                if (null != _errorTextBox)
                {
                    _errorTextBox.Text = "Last Error: " + value;
                }
            }
        }
        /// <summary>
        /// Set text in the Debug panel's List box header
        /// </summary>
        public string DebugPanelListBoxHeader
        {
            set
            {
                _mainWindowLayout.DebugText = value;
            }
        }
        /// <summary>
        /// Get the listview in the debug panel
        /// </summary>
        public System.Windows.Controls.ListView DebugPanelListView
        {
            get
            {
                return _mainWindowLayout.DebugListView;
            }
        }

        /// <summary>
        /// Set or get the visibility of the status panel. 
        /// Panel is visible if true
        /// </summary>
        public bool IsStatusVisible
        {
            set
            {
                _statusBox.Height = (value == true) ? 25 : 0;
            }
        }
        /// <summary>
        /// Set or get the visibility of the debug panel
        /// Panel is visible if true
        /// </summary>
        public bool IsDebugVisible
        {
            get
            {
                return (_debugBox.ActualWidth > 0);
            }
            set
            {
                _debugBox.Width = (value == true) ? 250 : 0;
            }
        }

        /// <summary>
        /// Get/Set state of the photo visble button. The default state is false
        /// </summary>
        public bool IsPhotoVisible
        {
            get
            {
                if (_photoButton.Background == _buttonDefaultColor)
                {
                    return false;

                }
                else
                {
                    return true;
                }
            }

            set
            {
                if (value == true)
                {
                    _photoButton.Background = _buttonAlternateColor;
                    _photoButton.Content = _photoEnabledText;
                }
                else
                {
                    _photoButton.Background = _buttonDefaultColor;
                    _photoButton.Content = _photoDisabledText;
                }
            }
        }

        /// <summary>
        /// Get/Set state of the update Button. The default state is false
        /// </summary>
        public bool IsUpdateLayoutEnabled
        {
            get
            {
                if (_layoutUpdateButton.Background == _buttonDefaultColor)
                {
                    return false;

                }
                else
                {
                    return true;
                }
            }

            set
            {
                if (value == true)
                {
                    // Update is enables so change text to enable stopping
                    _layoutUpdateButton.Background = _buttonAlternateColor;
                    _layoutUpdateButton.Content = "Stop";
                }
                else
                {
                    // Update is stopped so allow it to be restrated
                    _layoutUpdateButton.Background = _buttonDefaultColor;
                    _layoutUpdateButton.Content = "Update";
                }
            }
        }

        /// <summary>
        /// Returns the main Canvas container for FaceSort
        /// The main canvas is the root of the FaceSort UI
        /// </summary>
        public BackgroundCanvas MainCanvas
        {
            get
            {
                return _mainCanvas;
            }
        }
        private void SetPanelVisibility()
        {
            IsStatusVisible = _optionsDialog.IsStatusVisible;
            IsDebugVisible = _optionsDialog.IsDebugVisible;

        }


        private void SetMemberVars()
        {
            foreach (UIElement el in _mainWindowLayout.Children)
            {
                Type t = el.GetType();
                if (t == typeof(DockPanel))
                {
                    _mainDocPanel = (DockPanel)el;
                }
            }

            DockPanel.SetDock(_mainCanvas, Dock.Left);

            Object ob = _mainDocPanel.FindName("PhotoDisplayButton");
            if (null != ob && ob is System.Windows.Controls.Button)
            {
                _photoButton = ob as System.Windows.Controls.Button;
            }

            ob = _mainDocPanel.FindName("LayoutUpdateButton");
            if (null != ob && ob is System.Windows.Controls.Button)
            {
                _layoutUpdateButton = ob as System.Windows.Controls.Button;
            }

            ob = _mainDocPanel.FindName("MainDebugBorder");
            if (null != ob && ob is Border)
            {
                _debugBox = ob as Border;
            }

            ob = _mainDocPanel.FindName("MainStatusBorder");
            if (null != ob && ob is Border)
            {
                _statusBox = ob as Border;
                //_statusTextBox = (TextBlock)_statusBox.Child;
            }
            ob = _mainDocPanel.FindName("StatusBlock");
            if (null != ob && ob is TextBlock)
            {
                _statusTextBox = ob as TextBlock;
            }
            ob = _mainDocPanel.FindName("LastError");
            if (null != ob && ob is TextBlock)
            {
                _errorTextBox = ob as TextBlock;
            }
            _mainDocPanel.Children.Add(_background);
            
        }

        private void ResetCanvasSizes()
        {
            _mainDocPanel.Width = ActualWidth;
            _mainDocPanel.Height = ActualHeight - 25;
            _background.Height = _mainDocPanel.Height - _statusBox.Height;
            _background.Width = _mainDocPanel.Width - _debugBox.ActualWidth;
            _mainCanvas.Height = _mainDocPanel.Height - _statusBox.Height - 30;
            _mainCanvas.Width = _mainDocPanel.Width - _debugBox.ActualWidth; ;
        }
        private void SizeChangedHandler(Object sender, SizeChangedEventArgs e)
        {
            ResetCanvasSizes();
        }
        /// <summary>
        /// Load the background bitmap from resource and assign as background to the main canvas
        /// </summary>
        private void SetBackgroundImage()
        {
            System.Drawing.Bitmap background = Properties.Resources.MainBackground;
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, background.Width, background.Height);
            System.Drawing.Imaging.BitmapData bData = background.LockBits(
                rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int byteCount = background.Width * background.Height * 4;
            byte[] pixs = new byte[byteCount];
            System.Runtime.InteropServices.Marshal.Copy(bData.Scan0, pixs, 0, byteCount);
            background.UnlockBits(bData);

            BitmapSource bitmapSource = BitmapSource.Create(
                background.Width,
                background.Height,
                background.HorizontalResolution,
                background.VerticalResolution,
                PixelFormats.Bgr32,
                null,
                pixs,
                background.Width * 4);

            _mainDocPanel.Background = new ImageBrush(bitmapSource);
        }

        private void SaveOptionDialog(String path, string fileName)
        {
            if (false == Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Serialize(path + fileName, false);
        }

    }
}