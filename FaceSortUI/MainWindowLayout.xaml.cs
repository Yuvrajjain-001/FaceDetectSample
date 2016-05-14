using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.ComponentModel;

namespace FaceSortUI
{
    /// <summary>
    /// Interaction logic for Grid.xaml
    /// </summary>

    public partial class MainWindowLayout : System.Windows.Controls.Canvas
    {
        private MainWindow _mainWindow;
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private Face _lastDebugFaceSelected;
        private System.Windows.Media.Effects.OuterGlowBitmapEffect _debugSelectedEffect;

        public MainWindowLayout(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            _lastDebugFaceSelected = null;
            _debugSelectedEffect = new System.Windows.Media.Effects.OuterGlowBitmapEffect();
            _debugSelectedEffect.GlowSize = 12;
            _debugSelectedEffect.GlowColor = Colors.Salmon;

        }
        /// <summary>
        /// Set the  text in the debug panel
        /// </summary>
        public string DebugText
        {
            set
            {
                DebugListBoxHead.Text= value;
            }
        }

        private void OnLoadCollectionClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckPathExists = true;
            fileDialog.Multiselect = true;
            fileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;

            if (DialogResult.OK  == fileDialog.ShowDialog())
            {
                string [] retFiles = fileDialog.FileNames;
                if (retFiles.Length > 0)
                {
                    _mainWindow.AddFaceCollection(retFiles);
                }
            }

        }
        void OnLoadPhotoCollectionClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckPathExists = true;
            fileDialog.Multiselect = true;
            fileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;

            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                string[] retFiles = fileDialog.FileNames;
                if (retFiles.Length > 0)
                {
                    _mainWindow.AddPhotoCollection(retFiles);
                }
            }

        }
        void OnLoadPhotoCollectionLabelledClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckPathExists = true;
            fileDialog.Multiselect = false;
            fileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;

            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                string[] retFiles = fileDialog.FileNames;
                if (retFiles.Length > 0)
                {
                    _mainWindow.AddPhotoCollectionEyes(retFiles);
                }
            }

        }

        void OnLoadPhotoDirectoryClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fileDialog = new FolderBrowserDialog();

            fileDialog.ShowNewFolderButton = false;
            fileDialog.Description = "Load photos by Directory";

            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                if (fileDialog.SelectedPath.Length > 0)
                {
                    string[] retFiles = new string[1];
                    retFiles[0] = fileDialog.SelectedPath;

                    _mainWindow.AddPhotoCollection(retFiles);
                }
            }

        }


        void OnLoadImagesClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckFileExists = true;
            fileDialog.Multiselect = true;
            fileDialog.Filter = "jpg files (*.jpg)|*.jpg|png files (*.png)|*.png|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;

            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                string[] retFiles = fileDialog.FileNames;
                if (retFiles.Length > 0)
                {
                    _mainWindow.AddFaceImages(retFiles);
                }
            }

        }

        void DistanceButtonClick(Object sender, RoutedEventArgs e)
        {
            _mainWindow.MainCanvas.DoComputeDistances();
        }

        void InitialButtonClick(Object sender, RoutedEventArgs e)
        {
            _mainWindow.MainCanvas.DoInitialLayout();
        }

        void UpdateButtonClick(Object sender, RoutedEventArgs e)
        {
            //_mainWindow.MainCanvas.DoUpdateLayout();
            
            if (_mainWindow.IsUpdateLayoutEnabled == true)
            {
                _mainWindow.IsUpdateLayoutEnabled = false;
            }
            else
            {
                _mainWindow.IsUpdateLayoutEnabled = true;
            }

            _mainWindow.MainCanvas.RunUpdate(_mainWindow.IsUpdateLayoutEnabled);
        }

        void PhotoToggleClick(Object sender, RoutedEventArgs e)
        {
            if (_mainWindow.IsPhotoVisible == true)
            {
                _mainWindow.IsPhotoVisible = false;
            }
            else
            {
                _mainWindow.IsPhotoVisible = true;
            }
        }

        void OnSaveAs(Object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();

            fileDialog.CheckPathExists = false;
            fileDialog.CheckFileExists = false;
            fileDialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;

            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                _mainWindow.Serialize(fileDialog.FileName, true);
            }
        }

        void OnLoadGallery(Object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.CheckPathExists = true;
            fileDialog.Multiselect = false;
            fileDialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            fileDialog.FilterIndex = 1;

            if (DialogResult.OK == fileDialog.ShowDialog())
            {
                _mainWindow.Deserialize(fileDialog.FileName, true);
            }
        }

        void OnFileOptionClick(object sender, RoutedEventArgs e)
        {
            _mainWindow.ShowOptions();
        }
        void OnClearClick(object sender, RoutedEventArgs e)
        {
            _mainWindow.MainCanvas.Clear();
        }


        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked =
                  e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;
                    Sort(header, direction);

                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }


                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(DebugListView.Items);


            if (null != view)
            {
                view.SortDescriptions.Clear();

                SortDescription sd = new SortDescription(sortBy, direction);
                view.SortDescriptions.Add(sd);
                view.Refresh();
            }
        }

        private void GridViewSelectionChangedHandler(Object Sender, SelectionChangedEventArgs e)
        {
            FaceDistance faceDistance = ((Sender as System.Windows.Controls.ListView).SelectedItem as FaceDistance);

            if (null != faceDistance)
            {
                if (null != _lastDebugFaceSelected)
                {
                    _lastDebugFaceSelected.BitmapEffect = null;
                }

                Face face = faceDistance.Face;
                face.BitmapEffect = _debugSelectedEffect;
                _lastDebugFaceSelected = face;
            }
        }
    }
}