using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Runtime.Serialization;
using System.IO;

namespace FaceSortUI
{
    /// <summary>
    /// Probably the most import class in the UI
    /// The main canvas (also called backgroundCanvas)  hosts all the key objects in FaceSort
    /// such as:
    /// <list>Faces</list>
    /// <list>Photos</list>
    /// <list>Groups</list>
    /// </summary>

    [Serializable()]
    public class BackgroundCanvas : System.Windows.Controls.Canvas, FaceSortUI.IFaceContainer, FaceSortUI.IDisplayableElement, ISerializable
    {
        public delegate void BackendDelegate(DataExchange dataExchange);

        #region classVariables
        /// <summary>
        /// Mouse selection modes. Controls what happens when the mouse moves
        /// //
        /// None - Nothing is selected - ignore mousemoves
        /// GroupCreateCenter - Selecting faces to form group from center outwards.  Run Face selection for group creation
        /// GroupCreateCorner - Selecting faces staring from a corner. Run Face Selection ofor group Creation
        /// Pan - panning over the mainCanvas
        /// ElementDrag - Dragging one free face around the canvas
        /// ExtendElementSelect - Process of doing extended selection - ignore mouse moves
        /// ExtendElementDrag - Extended selection and dragging
        /// GroupSelect - Selected a group
        /// GroupMenu - Displaying a group context menu
        /// </summary>
        public enum SelectState { 
            None, 
            GroupCreateCenter, 
            GroupCreateCorner,
            Pan,
            Element, 
            ExtendElementSelect, 
            ExtendElementDrag, 
            GroupSelect, 
            GroupMenu
        };

        /// The 4key groups are maintained in in these lists

        private List<Face> _allFaceList;
        private List<Group> _groupList;
        private List<Photo> _photoList;
        private List<SortGroup> _sortList;

        private int _nextID;     // Next ID assigned to object created;
        private double _currentScale;

        /// These are transient lists used to temporarily hold objects during group
        /// construction

        private List<Face> _extendSelectedFaceList;
        private List<Face> _createGroupFaceList;        // List of faces selected by a group creation event

        /// Internal state of the main canvas

        private ScaleTransform _scaleTransform;
        private ScaleTransform _unitScaleTransform;
        private ScaleTransform _inverseTransform;
        private TranslateTransform _translateTransform;

        private MainWindow _mainWindow;
        private Canvas _backgroundCanvas;
        private System.Windows.Media.Effects.OuterGlowBitmapEffect _glowEffect;
        private Rectangle _selectRectangle;
        private Point _mouseDownLocation;
        private SelectState _selectState;
        private System.Windows.Threading.DispatcherTimer _menuTimer;
        private CreateGroupMenu _createGroupMenu;       // Menu to create a group 
        private Group _groupToDestroy;
        private bool _doDestroyLastRemovedGroup;

        private SerializationInfo _deserializationInfo ;
        private StreamingContext _deserializationContext;

        private BackgroundWorker _backgroundWorker;
        private bool _doBackground;


        /// <summary>
        /// Delegate for backend to compute distances between faces
        /// </summary>
        public BackendDelegate DistanceCalculationDelegate;
        /// <summary>
        /// Backend delegate (Hook) for running initial MDS layout
        /// </summary>
        public BackendDelegate runMdsDelegate;
        /// <summary>
        /// Backend delegate (Hook) for doing an MDS update
        /// </summary>
        public BackendDelegate runMdsUpdate;

        /// <summary>
        /// Special parent id to indicate an element has no parent
        /// </summary>
        public static int NOPARENT = -1;
        private static double animateAccelerationRatio = 0.2;    // Acceleration ratio for animation
        private static double animateDecelerationRatio = 0.2;    // Deceleration ratio for animation

        private static double _scaleIncrement = 0.9F;       // Multiplicative factor to adjust scaling on mousewheel turns
        private static double _minZoomScale = 0.001;        // Absolute Minimum scale value for zooming
        private static double _minRelativeMove = 0.01;     // Minimum distance that cursor moves before calling it a move


    #endregion classVariables

        #region publicMethods

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="mainWindow">Top level window container for canvas</param>
        /// <param name="background">The background canvas with image</param>
        public BackgroundCanvas(MainWindow mainWindow, Canvas background)
        {
            _nextID = 0;
            _currentScale = 1.0;
            _allFaceList = new List<Face>();
            _groupList = new List<Group>();
            _sortList = new List<SortGroup>();
            _photoList = new List<Photo>();

            Initialize(mainWindow, background);
        }
        /// <summary>
        /// Initialize internal data structures of the main canvas. Usually
        /// called as part of constructing the object
        /// </summary>
        /// <param name="mainWindow">Top level window container for canvas</param>
        /// <param name="background">The background canvas with image</param>
        public void Initialize(MainWindow mainWindow, Canvas background)
        {
            int childLoc = 0;

            _mainWindow = mainWindow;
            _backgroundCanvas = background;

            _createGroupFaceList = new List<Face>();
            _extendSelectedFaceList = new List<Face>();


            _glowEffect = new System.Windows.Media.Effects.OuterGlowBitmapEffect();
            _glowEffect.GlowSize = 12;

            _selectRectangle = new Rectangle();
            _selectRectangle.Stroke = Brushes.Blue;
            _selectRectangle.Visibility = Visibility.Hidden;
            _selectState = SelectState.None;
            _menuTimer = new System.Windows.Threading.DispatcherTimer();
            _menuTimer.Interval = TimeSpan.FromSeconds(0.5);
            _unitScaleTransform = new ScaleTransform(1.0, 1.0);
            _scaleTransform = new ScaleTransform(1.0, 1.0);
            _inverseTransform = new ScaleTransform(1.0, 1.0);
            _translateTransform = new TranslateTransform();
            TransformGroup tg = new TransformGroup();
            tg.Children.Add(_translateTransform);
            tg.Children.Add(_scaleTransform);
            RenderTransform = tg;

            _createGroupMenu = new CreateGroupMenu();
            Children.Insert(childLoc++, _selectRectangle);

            MouseDown += MouseButtonDownHandler;
            MouseUp += MouseButtonUpHandler;
            MouseMove += MouseMoveEventHandler;
            MouseWheel += MouseWheelHandler;

            _menuTimer.Tick += ShowMenuHandler;

            for (int iCount = 0; iCount < _createGroupMenu.Items.Count; ++iCount)
            {
                MenuItem it = (MenuItem)_createGroupMenu.Items[iCount];
                it.Click += MenuClickedHandler;
            }
            _menuTimer.Tick += ShowMenuHandler;


            DistanceCalculationDelegate = null;
        }
        /// <summary>
        /// Desrializes the mainCanvas file. Uses cached copies of the
        /// desrialization info
        /// </summary>
        /// <param name="mainWindow">Top level window container for canvas</param>
        /// <param name="background">The background canvas with image</param>
        public void Deserialize(MainWindow mainWindow, Canvas background)
        {
            _nextID = _deserializationInfo.GetInt32("nextID");
            _currentScale = _deserializationInfo.GetDouble("currentScale");
            int faceCount = _deserializationInfo.GetInt32("FaceCount");
            int savedFaceCount = _deserializationInfo.GetInt32("savedFaceCount");
            int groupCount = _deserializationInfo.GetInt32("GroupCount");
            int PhotoCount = _deserializationInfo.GetInt32("PhotoCount");

            _allFaceList = new List<Face>();
            _groupList = new List<Group>();
            _photoList = new List<Photo>();
            _sortList = new List<SortGroup>();

            _mainWindow = mainWindow;
            _backgroundCanvas = background;

            int id = 0;

            for (int iPhoto = 0; iPhoto < PhotoCount; ++iPhoto)
            {
                Photo photo = new Photo(_deserializationInfo, _deserializationContext, this, id++);
                _photoList.Add(photo);
                Children.Add(photo);
            }

            for (int iFace = 0; iFace < savedFaceCount; ++iFace)
            {
                Face face = new Face(_deserializationInfo, _deserializationContext, this, id++);
                _allFaceList.Add(face);
                Children.Add(face);
            }
            for (int iGroup = 0; iGroup < groupCount; ++iGroup)
            {
                Group group = new Group(_deserializationInfo, _deserializationContext, this, id++);
                Groups.Add(group);
                Children.Add(group);
            }

            if (faceCount != FaceCount)
            {
                throw new Exception("Error in loading gallery Expected " + faceCount.ToString() + " found " +
                    FaceCount.ToString());
            }

            Initialize(mainWindow, background);

        }

        /// <summary>
        /// Deserialization constructor. This constructor simply 
        /// caches the desrailaization info and context. The
        /// real work of serialization is performed in Deserialize()
        /// </summary>
        /// <param name="info">Deserialization store</param>
        /// <param name="context">DeserializationContext</param>
        protected BackgroundCanvas(SerializationInfo info, StreamingContext context)
        {
            _deserializationInfo = info;
            _deserializationContext = context;

        }

        /// <summary>
        /// Custom serialization. Iteartively serializes all
        /// child faces groups and photos
        /// </summary>
        /// <param name="info">Serialization storage</param>
        /// <param name="context">Not used</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("nextID", _nextID);
            info.AddValue("currentScale", CurrentScale);
            info.AddValue("FaceCount", FaceCount);
            info.AddValue("GroupCount", GroupCount);
            info.AddValue("PhotoCount", PhotoCount);

            int id = 0;
            int savedFaceCount = 0;

            foreach (Photo photo in _photoList)
            {
                photo.SyncParentID();
                photo.GetObjectData(info, context, id++);
            }

            foreach (Face face in _allFaceList)
            {
                face.SyncParentID();
                if (face.CreationSource == Face.CreationSourceEnum.File)
                {
                    face.GetObjectData(info, context, id++, 0, 0);
                    ++savedFaceCount;
                }
            }
            info.AddValue("savedFaceCount", savedFaceCount);

            foreach (Group group in Groups)
            {
                group.SyncParentID();
                group.GetObjectData(info, context, id++);
            }

        }

        /// <summary>
        /// Call faceSort backend to do an initial layout of faces
        /// </summary>
        public void DoComputeDistances()
        {
            if (null == DistanceCalculationDelegate)
            {
                ErrorText = "No distance delegate installed";
                return;
            }

            try
            {
                StatusText = "Starting distance computation ...";

                DataExchange dataExchange = DataExchange;

                DistanceCalculationDelegate(dataExchange);
                CopyBackExchangeData(dataExchange);
                StatusText = "Distance computation done";
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Error Communicating with backed " + "\n" + e.Message);
            }
        }

        /// <summary>
        /// Clear all  currently loaded faces groups and photos
        /// </summary>
        public void Clear()
        {
            foreach (Group group in Groups)
            {
                group.ClearAllFaces();
                Children.Remove(group);
            }
            Groups.Clear();

            foreach (Face face in Faces)
            {
                Children.Remove(face);
            }
            Faces.Clear();

            foreach (Photo photo in Photos)
            {
                photo.Clear();
            }
            Photos.Clear();
        }

        /// <summary>
        /// Call faceSort backend to update the two dimension face layout
        /// </summary>
        public void DoUpdateLayout()
        {
            try
            {
                StatusText = "Update started ...";
                DataExchange dataExchange = DataExchange;

                AddDistanceMatrix(dataExchange, false);

                if (null == runMdsUpdate)
                {
                    ErrorText = "No MDS Update delegate installed";
                    return;
                }
                runMdsUpdate(dataExchange);
                CopyBackExchangeData(dataExchange);
                StatusText = "Update complete";
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Error Communicating with backed " + "\n" + e.Message);
            }

        }

        /// <summary>
        /// Call faceSort backend to construct an initial two dimension face layout
        /// </summary>
        public void DoInitialLayout()
        {

            try
            {
                StatusText = "Starting initial layout ...";
                DataExchange dataExchange = DataExchange;

                AddDistanceMatrix(dataExchange, false);

                if (null == runMdsDelegate)
                {
                    ErrorText = "No MDS delegate installed";
                    return;
                }
                runMdsDelegate(dataExchange);
                CopyBackExchangeData(dataExchange);
                StatusText = "Initial layout done";
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Error Communicating with backed " + "\n" + e.Message);
            }
        }

        /// <summary>
        /// Get the glow effect used when mouse enters a face
        /// or group
        /// </summary>
        public System.Windows.Media.Effects.OuterGlowBitmapEffect GlowEffect
        {
            get
            {
                return _glowEffect;
            }
        }

        /// <summary>
        /// Pulates the Debug panel header text box with text
        /// </summary>
        public string DebugPanelListBoxHeader
        {
            set
            {
                _mainWindow.DebugPanelListBoxHeader = value;
            }
        }

        public bool IsDebugVisible
        {
            get
            {
                return _mainWindow.IsDebugVisible;
            }
        }

        public System.Windows.Controls.ListView DebugPanelListView
        {
            get
            {
                return _mainWindow.DebugPanelListView;
            }
        }
        /// <summary>
        /// checks if photo visible button is enabled
        /// </summary>
        public bool IsPhotoVisible
        {
            get
            {
                return _mainWindow.IsPhotoVisible;
            }
        }

        /// <summary>
        /// Set display String in the status box
        /// </summary>
        public string StatusText
        {
            get
            {
                return _mainWindow.StatusText;
            }

            set
            {
                _mainWindow.StatusText = value;
            }
        }
        /// <summary>
        /// Set display String in the status box
        /// </summary>
        public string ErrorText
        {
            get
            {
                return _mainWindow.ErrorText;
            }

            set
            {
                _mainWindow.ErrorText = value;
            }
        }

        /// <summary>
        /// Get the current mouse selection mode, for example
        /// cornerSelection, faceDragging, etc)
        /// </summary>
        public SelectState SelectionState
        {
            get
            {
                return _selectState;
            }

            set
            {
                if (SelectState.None == value)
                {
                    foreach (Face face in _allFaceList)
                    {
                        face.Selected = Face.SelectionStateEnum.None;
                    }

                    UnHighlightGroups();
                }
                _selectState = value;
            }
        }

        /// <summary>
        /// Get current global zoom scale
        /// </summary>
        public double CurrentScale
        {
            get
            {
                return _currentScale;
            }
        }
        /// <summary>
        /// Get Width of the main Canvaas
        /// </summary>
        public double DisplayWidth
        {
            get
            {
                return _backgroundCanvas.Width;
            }
        }
        /// <summary>
        /// Get Height of main canvas
        /// </summary>
        public double DisplayHeight
        {
            get
            {
                return _backgroundCanvas.Height;
            }
        }
        /// <summary>
        /// Get the region in which a Top left corner of a face can be displayed
        /// </summary>
        public Rect VisibledRegion
        {
            get
            {
                Rect rect = new Rect(0, 0, ActualWidth - OptionDialog.FaceDisplayWidth, ActualHeight - OptionDialog.FaceDisplayWidth);
                return rect;
            }
        }
        /// <summary>
        /// Get the list of all faces
        /// </summary>
        public List<Face> Faces
        {
            get
            {
                return _allFaceList;
            }
        }

        /// <summary>
        /// Get the options
        /// </summary>
        public OptionDialog OptionDialog
        {
            get
            {
                if (null != _mainWindow)
                {
                    return _mainWindow.OptionDialog;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get a list of all Groups
        /// </summary>
        public List<Group> Groups
        {
            get
            {
                return _groupList;
            }
        }

        /// <summary>
        /// Get a list of all Groups
        /// </summary>
        public List<SortGroup> SortGroups
        {
            get
            {
                return _sortList;
            }
        }


        /// <summary>
        /// Get the count of all faces
        /// </summary>
        public int FaceCount
        {
            get
            {
                return Faces.Count;
            }
        }

        /// <summary>
        /// Gets number of loaded photo files
        /// </summary>
        public int PhotoCount
        {
            get
            {
                return _photoList.Count;
            }
        }

        /// <summary>
        /// Get the list of photos
        /// </summary>
        public List<Photo> Photos
        {
            get
            {
                return _photoList;
            }
        }

        /// <summary>
        /// Get number of groups
        /// </summary>
        public int GroupCount
        {
            get
            {
                return _groupList.Count;
            }
        }

        /// <summary>
        /// Get a List of vectors containing the raw data for the faces
        /// </summary>
        public DataExchange DataExchange
        {
            get
            {
                DataExchange dataExchange = new DataExchange(_allFaceList, VisibledRegion, OptionDialog);
                return dataExchange;
            }
        }

        /// <summary>
        /// Get group by index
        /// </summary>
        /// <param name="iGroup"></param>
        /// <returns>Group index</returns>
        public Group GetGroup(int iGroup)
        {
            if (iGroup >= 0 && iGroup < GroupCount)
            {
                return Groups[iGroup];
            }
            return null;
        }

        /// <summary>
        /// Make a face known to me
        /// </summary>
        /// <param name="face">Face to add</param>
        public void AddFace(Face face)
        {
            if (false == _allFaceList.Contains(face))
            {
                if (null != OptionDialog)
                {
                    int FaceSpacing = OptionDialog.GridFaceSpace + OptionDialog.FaceDisplayWidth;
                    int xPos = FaceCount % 10 ; ;
                    int ypos = FaceCount / 10 ;

                    SetTop(face, ypos * FaceSpacing);
                    SetLeft(face, xPos * FaceSpacing);
                }
                _allFaceList.Add(face);

            }
            if (false == Children.Contains(face))
            {
                Children.Add(face);
            }

        }

        /// <summary>
        /// Remove a face from my Children List. There are two levels of
        /// removal,controlled by the flag doDestroy 
        /// </summary>
        /// <param name="face">Face to Remove</param>
        /// <param name="doDestroy">When false just remove from the child list. When true means face should be destroyed
        /// so remove from internall _AllFaceList too</param>
        public void RemoveFace(Face face, bool doDestroy)
        {
            Children.Remove(face);
            if (true == doDestroy)
            {
                _allFaceList.Remove(face);
            }
        }
        /// <summary>
        /// Checks if the source should be dropped into another 
        /// group on the screen
        /// </summary>
        /// <param name="source">Source group</param>
        /// <param name="mousePos">Mouse Position</param>
        public void DragDropGroup(Group source, Point mousePos)
        {
            foreach (Group group in Groups)
            {
                if (group != source && 
                    true == group.HitTest(mousePos))
                {
                    group.DropGroup(source);
                    group.Display();
                    Children.Remove(source);
                    Groups.Remove(source);
                    break;
                }
            }
        }

        /// <summary>
        /// Create and add a Sorting Group
        /// </summary>
        /// <param name="group1"></param>
        public void AddSplitGroup(Group group1)
        {
            if (Children.Contains(group1))
            {
                Children.Remove(group1);
                Groups.Remove(group1);
            }

            SortGroup newSort = new SortGroup(this, group1, CreateNewObjectID());

            Children.Add(newSort);
            SortGroups.Add(newSort);
        }
        /// <summary>
        /// Break up a sort group into separate parts
        /// </summary>
        /// <param name="group1"></param>
        public void RemoveSplitGroup(Group group1)
        {
            if (Children.Contains(group1))
            {
                return;
            }


            SortGroup sortGroup = group1.MyParent as SortGroup;

            if (null != sortGroup)
            {
                List<Group> groups = sortGroup.Groups;

                sortGroup.DestroySortGroup();
                double yAdd = 0.0;
                Point pos = GetPositionRelativeToCanvas(sortGroup);
                foreach (Group group in groups)
                {
                    group.AddToGroup(this);
                    Children.Add(group);
                    Groups.Add(group);
                    group.DisplayMode = Group.DisplayState.Grid;
                    group.AdjustSize();
                    double y = Math.Min(pos.Y + yAdd, ActualHeight - group.Height);
                    y = Math.Min(y, ActualHeight - OptionDialog.FaceDisplayWidth);
                    SetTop(group, y);
                    SetLeft(group, pos.X);
                    yAdd += group.Height + OptionDialog.GridFaceSpace;

                }
            }

        }

        /// <summary>
        /// Returns the position of an element relative to the main canvase
        /// </summary>
        /// <param name="element">the element</param>
        /// <returns>Location</returns>
        public Point GetPositionRelativeToCanvas(Visual element)
        {
            Point pos = new Point();
            UIElement uiElement = element as UIElement;

            if (null == uiElement || this == uiElement)
            {
                return pos;
            }

            pos.X = Canvas.GetLeft(uiElement);
            if (double.IsNaN(pos.X) == true)
            {
                pos.X = 0.0;
            }

            pos.Y = Canvas.GetTop(uiElement);
            if (double.IsNaN(pos.Y) == true)
            {
                pos.Y = 0.0;
            }


            Visual parent = VisualTreeHelper.GetParent(element) as Visual;

            // Terminate when we get to the mainCanvas
            if ( parent == this)
            {
                return pos;
            }


            Point parentPos = GetPositionRelativeToCanvas(parent);
            pos.X += parentPos.X;
            pos.Y += parentPos.Y;
            return pos;
        }
        /// <summary>
        /// Add a new group with a list of faces
        /// </summary>
        /// <param name="faceList">Face list to add</param>
        /// <param name="displayState">Initial display state of group</param>
        public  void AddGroup(List<Face> faceList, Group.DisplayState displayState)
        {
            if (null == faceList || faceList.Count <= 0)
            {
                return;
            }

            Group newGroup = new Group(this, CreateNewObjectID());

            newGroup.DisplayMode = displayState;

            newGroup.AddFaces(faceList);
            Children.Add(newGroup);
            Groups.Add(newGroup);
            ClearSelectionList();

        }
        /// <summary>
        /// Force all groups to update
        /// </summary>
        public void DisplayGroups()
        {
            foreach (Group group in Groups)
            {
                if (group.FaceCount > 0)
                {
                    group.Display();
                }
                else
                {
                    RemoveGroup(group, false);
                }
            }
        }
        /// <summary>
        /// Remove a group from my Children List. There are two levels of
        /// removal,controlled by the flag doDestroy 
        /// </summary>
        /// <param name="group">Group to remove</param>
        /// <param name="doDestroy">When false just remove from the child list. When true means face should be destroyed
        /// so remove from internall _groupList too</param>
        public void RemoveGroup(Group group, bool doDestroy)
        {
            if (true == doDestroy)
            {
                group.ClearAllFaces();
            }

            if (group.FaceCount <= 0)
            {
                Point pos = new Point();
                pos.X = GetLeft(group);
                pos.Y = GetTop(group);

                // This is a hack for the call back.Clearly dangerous if
                // Destroying multiple groups
                _groupToDestroy = group;
                _doDestroyLastRemovedGroup = doDestroy;

                Animate(group, pos, 0.0, -1.0, RemoveGroupCompletedHandler);
            }
        }

        /// <summary>
        /// Higlight groups containing a test point
        /// </summary>
        /// <param name="pos">The test point</param>
        /// <param name="skipGroup">Do not check this group</param>
        public void HighlightGroups(Point pos, Group skipGroup)
        {
            foreach (Group group in Groups)
            {
                if (group == skipGroup)
                {
                    continue;
                }

                if (true == group.HitTest(pos))
                {
                    group.BitmapEffect = GlowEffect;
                }
                else
                {
                    group.BitmapEffect = null;
                }
            }
        }

        /// <summary>
        /// Remove highlights from all groups
        /// </summary>
        public void UnHighlightGroups()
        {
            foreach (Group group in Groups)
            {
                group.BitmapEffect = null;
            }
        }

        /// <summary>
        /// Add a collection of photos. A photo can have faces
        /// </summary>
        /// <param name="pathName">Path to a collection</param>
        /// <param name="maxCount">Maximum photos to add</param>
        /// <returns></returns>
        public void AddPhotoCollection(string pathName, int maxCount)
        {
            List<string> fileNameList = ReadFiles(pathName);
            string[] fileNames = new string[fileNameList.Count];
            int next = 0;
            foreach (string name in fileNameList)
            {
                fileNames[next++] = name;
            }

            AddPhotos(fileNames, maxCount);
        }

        #region some debug code here        
        private void save(string fileName, List<List<string>> listListString)
        {
            StreamWriter saveFile = new StreamWriter(fileName);
            foreach (List<string> listString in listListString)
            {
                foreach (string str in listString)
                {
                    saveFile.WriteLine(str);
                }
            }
            saveFile.Close();
        }
        private List<List<string>> ReShapeString(List<string> lines)
        {
            List<List<string>> listListString = new List<List<string>>();
            List<string> listString;
            int iLine = 0;
            while (iLine < lines.Count)
            {
                listString = new List<string>();
                listString.Add(lines[iLine++]);
                int count = Convert.ToInt32(lines[iLine]);
                while (count >= 0)
                {
                    listString.Add(lines[iLine++]);
                    count--;
                }
                listListString.Add(listString);
            }
            return listListString;
        }
        private void PermuteFiles(ref List<string> lines, ref int maxCount)
        {
            List<List<string>> listListString = ReShapeString(lines);
            int count = listListString.Count;
            List<string> exchangeListString;
            Random randNum=new Random();
            int i=2*count;
            while (i > 0)
            {
                int ii = randNum.Next(count);
                int jj = randNum.Next(count);

                exchangeListString = listListString[ii];
                listListString[ii] = listListString[jj];
                listListString[jj] = exchangeListString;
                i--;
            }

            save("c:/temp/EyeLocationFile.txt", listListString);

            lines.Clear();
            foreach (List<string> listString in listListString)
            {
                foreach (string str in listString)
                {
                    lines.Add(str);
                }
            }           
        }
        #endregion some debug code here

        public int AddPhotoCollectionEyes(string pathName, int maxCount)
        {
            string fileDir = System.IO.Path.GetDirectoryName(pathName);
            List<string> lines = ReadFiles(pathName);

            //Permuate the files
            //PermuteFiles(ref lines, ref maxCount);
           
            int iLine = 0;
            int count = 0;

            while (iLine < lines.Count)
            {
                string file = lines[iLine++];

                if (file.Length <= 0)
                {
                    continue;
                }

                if (!System.IO.Path.IsPathRooted(file))
                    file = System.IO.Path.Combine(fileDir, file);


                int faceCount = Convert.ToInt32(lines[iLine++]);
                List<Point> leftEyeList = new List<Point>();
                List<Point> rightEyeList = new List<Point>();

                for (int iFace = 0 ; iFace < faceCount ; ++iFace)
                {
                    string line = lines[iLine++];
                    string [] vals = line.Split();
                    if (vals.Length == 4)
                    {
                        leftEyeList.Add(new Point(Convert.ToDouble(vals[0]), Convert.ToDouble(vals[1])));
                        rightEyeList.Add(new Point(Convert.ToDouble(vals[2]), Convert.ToDouble(vals[3])));
                    }
                }
                if (leftEyeList.Count > 0)
                {
                    try
                    {
                        Photo newPhoto = new Photo(CreateNewObjectID());

                        int faceCountAdded = newPhoto.InitializeWithEyeList(this, file, leftEyeList, rightEyeList);

                        if (faceCountAdded > 0)
                        {
                            _photoList.Add(newPhoto);
                            count += faceCountAdded;
                            Children.Add(newPhoto);
                            StatusText = "Loaded " + faceCountAdded.ToString() + " from " + file;
                        }

                        if (count >= maxCount)
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        ErrorText = "Face Detection Failed on " + file + " " + e.Message;
                    }
                }
            }            
            return count;

        }

        /// <summary>
        /// Add a collection of face images to the background
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="pathName">Path either a directory or a txt file listing all the faceimages</param>
        /// <param name="maxCount">Maximum number to load</param>
        /// <returns>Number added</returns>
        public int AddImageCollection(string pathName, int maxCount)
        {
            List<string> fileNameList = ReadFiles(pathName);
            string[] fileNames = new string[fileNameList.Count];
            int next = 0;
            foreach (string name in fileNameList)
            {
                fileNames[next++] = name;
            }

            return AddImages(fileNames, maxCount);
        }

        /// <summary>
        /// Add an array of face images to the canvas
        /// </summary>
        /// <param name="fileNames">Arry a face image files</param>
        /// <param name="maxCount">Maximum number to load</param>
        /// <returns>Number added</returns>
        public int AddImages(string [] fileNames, int maxCount)
        {
            int addCount = 0;

            foreach (string file in fileNames)
            {
                try
                {
                    Face newFace = new Face(this, file, CreateNewObjectID());

                    newFace.Width = OptionDialog.FaceDisplayWidth;
                    AddFace(newFace);
                    ++addCount;

                    if (FaceCount >= maxCount)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    // Ignore the failure
                    //throw e;
                    string msg = e.Message;
                }

            }

            return addCount;
        }

        /// <summary>
        /// Add an array of face images to the canvas
        /// </summary>
        /// <param name="fileNames">Arry a face image files</param>
        /// <param name="maxCount">Maximum number to load</param>
        /// <returns>Number added</returns>
        public void AddPhotos(string[] fileNames, int maxCount)
        {

            System.Threading.Thread loadThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(
                this.AddPhotosBackgroundHandler));
            //loadThread.Start(fileNames);
            AddPhotosBackgroundHandler(fileNames);

            //if (null != _backgroundWorker)
            //{
            //    MessageBox.Show("System is busy cannot load new phots");
            //    return;
            //}
            //_backgroundWorker = new BackgroundWorker();
            //_backgroundWorker.DoWork += AddPhotosBackgroundHandler;
            //_backgroundWorker.ProgressChanged += AddPhotosProgressChangedEventHandler;
            //_backgroundWorker.RunWorkerCompleted += AddPhotosCompleteEventHandler;
            //_backgroundWorker.WorkerReportsProgress = true;


            //_backgroundWorker.RunWorkerAsync(fileNames);
        }

        //private void AddPhotosBackgroundHandler(Object sender, DoWorkEventArgs e)
        private void AddPhotosBackgroundHandler(Object filesAsObject)
        {
            //string[] fileNames = e.Argument as string[];
            string[] fileNames = filesAsObject as string [];
            int count = PhotoCount;
            int addCount = 0;

            //BackgroundWorker backgroundWorker = sender as BackgroundWorker;
            foreach (string file in fileNames)
            {
                try
                {
                    Photo newPhoto = new Photo(CreateNewObjectID());

                    int faceCountAdded = newPhoto.InitializeWithFaceDetection(this, file);

                    if (faceCountAdded > 0)
                    {
                        //e.Result = "Loaded " + faceCountAdded.ToString() + " from " + file;
                        //backgroundWorker.ReportProgress(0, newPhoto);
                        AddPhotosProgressChangedEventHandler(newPhoto);
                        count += faceCountAdded;
                        ++addCount;
                    }

                    if (count >= OptionDialog.MaximumImages)
                    {
                        break;
                    }
                }
                catch (Exception exc)
                {
                    //backgroundWorker.ReportProgress(0, "Face Detection Failed on " + file + " " + exc.Message);
                    ErrorText = "Face Detection Failed on " + file + " " + exc.Message;
                }

            }

            ErrorText = "Loaded " + count.ToString() + " images";
        }

        //private void AddPhotosProgressChangedEventHandler(Object sender, ProgressChangedEventArgs e)
        private void AddPhotosProgressChangedEventHandler(Object photo)
        {
            //Photo newPhoto = e.UserState as Photo;
            Photo newPhoto =photo as Photo;
            if (null != newPhoto)
            {
                _photoList.Add(newPhoto);
                Children.Add(newPhoto);
            }
            //else
            //{
            //    string error = e.UserState as string;
            //    if (null != error)
            //    {
            //        ErrorText = error;
            //    }
            //}
        }

        private void AddPhotosCompleteEventHandler(Object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled 
                // the operation.
                // Note that due to a race condition in 
                // the DoWork event handler, the Cancelled
                // flag may not have been set, even though
                // CancelAsync was called.
                StatusText = "Canceled";
            }
            else
            {
                // Finally, handle the case where the operation 
                // succeeded.
                StatusText = e.Result as string;
            }

            _backgroundWorker = null;
        }



        /// <summary>
        /// Copies back data exchanged with teh backend into the UI design
        /// Examples of data copied are distances between faces and location
        /// of images
        /// </summary>
        /// <param name="dataExchange"></param>
        public void CopyBackExchangeData(DataExchange dataExchange) 
        {
            if (dataExchange.FaceList == null || dataExchange.FaceList.Count <= 0)
            {
                return;
            }

            int iFace = 0;
            double[] dist = new double[dataExchange.DistanceMatrix.GetLength(0)];
            Rect coordRect = dataExchange.DataRect;

            Rect rect = VisibledRegion;
            dataExchange.RescaleCoordinates(rect.X, rect.Width, rect.Y, rect.Height);

            for (int idx = 0 ; idx < dataExchange.ElementCount ; ++idx)
            {
                Face face = dataExchange.FaceList[idx];
                ExchangeElement element = dataExchange.GetElement(idx);

                for (int i = 0; i < dist.Length; ++i)
                {
                    face.DistanceVector[dataExchange.FaceList[i]] = dataExchange.DistanceMatrix[iFace, i];
                }


                if (null != dataExchange.Coordinates && 
                    (element.State != ExchangeElement.ElementState.FrozenLocation &&
                     element.State != ExchangeElement.ElementState.Selected))
                {
                    face.TargetAnimationPos_X = dataExchange.Coordinates[iFace, 0];
                    face.TargetAnimationPos_Y = dataExchange.Coordinates[iFace, 1];

                    Animate(face, face.TargetAnimationPos, 1.0, -1.0, face.DisplayCompleteHandler);
                }
                ++iFace;
            }
        }
        /// <summary>
        /// Raises a child element and its parent displays near to top of the Z-order
        /// </summary>
        /// <param name="element">element to move</param>
        public void MoveToFrontDisplayOrder(UIElement element)
        {
            int offSet = 2 * (FaceCount + GroupCount + PhotoCount + 1);


            UIElement elementOrig = element;

           // int id = GetZIndex(elementOrig);

            while (null != element && this != element)
            {
                Debug.Assert(offSet >= 0, "Z - oder position mist be >= 0");

                SetZIndex(element, offSet);
                --offSet;
                element = (UIElement) ((IDisplayableElement)element).MyParent;
            }

            //id = GetZIndex(elementOrig);

            //foreach (Group group in _groupList)
            //{
            //    id = GetZIndex(group);
            //}

        }

        public void ResetDisplayOrder()
        {
            foreach (Face face in _allFaceList)
            {
                SetZIndex(face, 1);
            }

            foreach (Group group in Groups)
            {
                SetZIndex(group, 1);

            }
        }

        /// <summary>
        /// Determines if the start and end positions are sufficient spaced to 
        /// say there has been a move
        /// </summary>
        /// <param name="startPos">Original poistion</param>
        /// <param name="endPOs">ending poistion</param>
        /// <returns>true if distances exceed an internal threshold</returns>
        public bool HasMoved(Point startPos, Point endPos)
        {
            double deltX = (startPos.X - endPos.X);
            double deltY = (startPos.Y - endPos.Y);
            double dist = Math.Sqrt(deltX * deltX + deltY * deltY);

            return (dist > _minRelativeMove * Math.Min(ActualHeight, ActualWidth)) ? true : false;
        }
        /// <summary>
        /// Add a face to the list of faces that are in the Extended list collection
        /// </summary>
        /// <param name="face">Face to add</param>
        public void AddToExtendFaceSelection(Face face)
        {
            if (_extendSelectedFaceList.Contains(face) == false)
            {
                _extendSelectedFaceList.Add(face);
            }
        }
        /// <summary>
        /// Remove a face from teh extended face collection
        /// </summary>
        /// <param name="face"></param>
        public void RemoveExtendedFaceSelection(Face face)
        {
            _extendSelectedFaceList.Remove(face);
        }

        /// <summary>
        /// Clear all selection in face
        /// </summary>
        /// <param name="face"></param>
        public void ClearExtendedFaceSelection()
        {
            _extendSelectedFaceList.Clear();
        }

        public void PropogateMoveCompletionToExtendSelection()
        {
            foreach (Face face in _extendSelectedFaceList)
            {
                face.HandleMoveCompletion(false);
            }

            DisplayGroups();
        }

        /// <summary>
        /// Find a parent type by id
        /// </summary>
        /// <param name="id">ID to search </param>
        /// <returns>Parent null if none found</returns>
        public IDisplayableElement FindParent(int id)
        {
            if (id == NOPARENT)
            {
                return null;
            }
            if (id == 0)
            {
                return this;
            }

            foreach (Face face in _allFaceList)
            {
                if (id == face.MyID)
                {
                    return face;
                }
            }

            foreach (Photo photo in _photoList)
            {
                if (id == photo.MyID)
                {
                    return photo;
                }
            }

            foreach (Group group in Groups)
            {
                if (id == group.MyID)
                {
                    return group;
                }
            }

            throw new Exception("Cannot find a parent for id " + id.ToString());
        }



        #region IDisplayableElementImplementation

        /// <summary>
        /// Get my display parent which is always null
        /// </summary>
        public IDisplayableElement MyParent
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Get my unique ID (inherited form IDisplayableElement
        /// </summary>
        public int MyID
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Ensure that parentID is synced to current parent 
        /// Should sync before serializing
        /// </summary>
        public void SyncParentID()
        {
            foreach (Face face in _allFaceList)
            {
                face.SyncParentID();
            }

            foreach (Photo photo in _photoList)
            {
                photo.SyncParentID();
            }

            foreach (Group group in Groups)
            {
                group.SyncParentID();
            }
        }

        public void RebuildTree(BackgroundCanvas backgroundCanvas)
        {
            foreach (Photo photo in _photoList)
            {
                photo.RebuildTree(this);
            }

            foreach (Group group in Groups)
            {
                group.RebuildTree(this);
            }
            foreach (Face face in _allFaceList)
            {
                face.RebuildTree(this);
            }

        }

        /// <summary>
        /// Return my position relative to the main canvas
        /// </summary>
        /// <returns>Location </returns>
        public Point GetPositionRelativeToCanvas()
        {
            return GetPositionRelativeToCanvas(this);
        }

        /// <summary>
        /// Sync all display elements to current choices in in  options dialog
        /// </summary>
        public void SyncDisplayOptions()
        {
            foreach (Face face in Faces)
            {
                face.SyncDisplayOptions();
            }
        }

        #endregion IDisplayableElementImplementation
        #endregion publicmethods



        /// <summary>
        /// Checks if a point lies within a rectangle
        /// </summary>
        /// <param name="elementPos">Point to check</param>
        /// <param name="rect">Rectangle to check</param>
        /// <returns>True if point is within the rectangle</returns>
        public static bool HitTest(Point elementPos, Rect rect)
        {
            if (elementPos.X >= rect.X &&
                elementPos.X < rect.Right &&
                elementPos.Y >= rect.Top &&
                elementPos.Y < rect.Bottom )
            {
                return true;
            }
            return false;
        }

        private static DoubleAnimation CreateAnimateObject(double from, double to, double duration)
        {
            DoubleAnimation animate = new DoubleAnimation();

            animate.From = from;
            animate.To = to;
            animate.Duration = new Duration(TimeSpan.FromSeconds(duration));
            animate.FillBehavior = FillBehavior.Stop;
            animate.AccelerationRatio = animateAccelerationRatio;
            animate.DecelerationRatio = animateDecelerationRatio;

            return animate;
        }
        public void Animate(IAnimatable element, ScaleTransform start, ScaleTransform end, double duration, 
            EventHandler completedHandler)
        {
            if (duration < 0.0)
            {
                duration = OptionDialog.AnimationDuration;
            }

            if (duration == 0.0)
            {
                if (null != completedHandler)
                {
                    completedHandler(null, EventArgs.Empty);
                }
                return;
            }

            DoubleAnimation xScale = CreateAnimateObject(start.ScaleX, end.ScaleX, duration);
            DoubleAnimation yScale = CreateAnimateObject(start.ScaleY, end.ScaleY, duration);

            if (null != completedHandler)
            {
                xScale.Completed += completedHandler;
            }

            element.BeginAnimation(ScaleTransform.ScaleXProperty, xScale);
            element.BeginAnimation(ScaleTransform.ScaleYProperty, yScale);

        }
        /// <summary>
        /// Animate an element from  current position, size and opacity to
        /// an end state
        /// </summary>
        /// <param name="element">element to animate</param>
        /// <param name="endPos">Final position</param>
        /// <param name="endOpacity">Final opacity</param>
        /// <param name="duration">Animation duration. If < 0 use default value</param>
        public void Animate(UIElement element, Rect endPos, double endOpacity, double duration, 
            EventHandler completedHandler)
        {
            if (duration < 0.0)
            {
                duration = OptionDialog.AnimationDuration;
            }
            if (duration == 0.0)
            {
                element.Opacity = endOpacity;
                SetLeft(element, endPos.X);
                SetTop(element, endPos.Y);
                SetRight(element, endPos.X + endPos.Width);
                SetBottom(element, endPos.Y + endPos.Height);
                if (null != completedHandler)
                {
                    completedHandler(null, EventArgs.Empty);
                }
                return;
            }

            Rect startPos = new Rect();
            startPos.Y = GetTop(element);
            startPos.X = GetLeft(element);
            //startPos.Width = GetRight(element) - startPos.X;
            //startPos.Height = GetBottom(element) - startPos.Y;
            startPos.Width = 0.0;
            startPos.Height = 0.0;
            double startOpacity = element.Opacity;

            DoubleAnimation xAnimation = CreateAnimateObject(startPos.X, endPos.X, duration);
            DoubleAnimation yAnimation = CreateAnimateObject(startPos.Y, endPos.Y, duration);
            DoubleAnimation hAnimation = CreateAnimateObject(startPos.Width, endPos.Width, duration);
            DoubleAnimation wAnimation = CreateAnimateObject(startPos.Height, endPos.Height, duration);
            DoubleAnimation opacityAnimation = CreateAnimateObject(startOpacity, endOpacity, duration);
            if (null != completedHandler)
            {
                xAnimation.Completed += completedHandler;
            }

            // Stop any existing animation
            element.BeginAnimation(Canvas.LeftProperty, null);
            element.BeginAnimation(Canvas.TopProperty, null);
            element.BeginAnimation(Canvas.WidthProperty, null);
            element.BeginAnimation(Canvas.HeightProperty, null);
            element.BeginAnimation(Canvas.OpacityProperty, null);


            element.BeginAnimation(Canvas.LeftProperty, xAnimation);
            element.BeginAnimation(Canvas.TopProperty, yAnimation);
            element.BeginAnimation(Canvas.WidthProperty, wAnimation);
            element.BeginAnimation(Canvas.HeightProperty, hAnimation);
            element.BeginAnimation(Canvas.OpacityProperty, opacityAnimation);
        }

        /// <summary>
        /// Animate an element from  current position and opacity to
        /// an end state
        /// </summary>
        /// <param name="element">element to animate</param>
        /// <param name="endPos">Final position</param>
        /// <param name="endOpacity">Final opacity</param>
        /// <param name="duration">Animation duration. If < 0 use default value</param>
        public void Animate(UIElement element, Point endPos, double endOpacity, double duration,
            EventHandler completedHandler)
        {
            if (duration < 0.0)
            {
                duration = OptionDialog.AnimationDuration;
            }
            if (duration == 0.0)
            {
                element.Opacity = endOpacity;
                SetLeft(element, endPos.X);
                SetTop(element, endPos.Y);
                if (null != completedHandler)
                {
                    completedHandler(null, EventArgs.Empty);
                }
                return;
            }

            Point startPos = new Point();
            startPos.Y = GetTop(element);
            startPos.X = GetLeft(element);
            double startOpacity = element.Opacity;

            DoubleAnimation xAnimation = CreateAnimateObject(startPos.X, endPos.X, duration);
            DoubleAnimation yAnimation = CreateAnimateObject(startPos.Y, endPos.Y, duration);
            DoubleAnimation opacityAnimation = CreateAnimateObject(startOpacity, endOpacity, duration);
            if (null != completedHandler)
            {
                xAnimation.Completed += completedHandler;
            }

            // Stop any existing animation
            element.BeginAnimation(Canvas.LeftProperty, null);
            element.BeginAnimation(Canvas.TopProperty, null);
            element.BeginAnimation(Canvas.OpacityProperty, null);


            element.BeginAnimation(Canvas.LeftProperty, xAnimation);
            element.BeginAnimation(Canvas.TopProperty, yAnimation);
            element.BeginAnimation(Canvas.OpacityProperty, opacityAnimation);
        }

        private void AddDistanceMatrix(DataExchange dataExchange, bool doForce)
        {
            if (null == dataExchange.DistanceMatrix || true == doForce)
            {
                if (null == DistanceCalculationDelegate)
                {
                    StatusText = "No distance delegate installed";
                    return;
                }

                DistanceCalculationDelegate(dataExchange);
            }
        }

        /// <summary>
        /// Obtain a current list of faces not bound to groups)
        /// </summary>
        /// <returns>List of free faces</returns>
        public List<Face> GetFreeFaces()
        {
            List<Face> faceList;

            faceList = new List<Face>();

            foreach (UIElement element in Children)
            {
                if (element.GetType() == typeof(Face))
                {
                    faceList.Add((Face)element);
                }
            }

            return faceList;
        }

        #region privateMethods

        private List<string> ReadFiles(string pathname)
        {
            List<string> fileList = new List<string>();

            if (true == System.IO.File.Exists(pathname))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(pathname))
                {
                    string filename;
                    while (null != (filename = reader.ReadLine()))
                    {
                        fileList.Add(filename);
                    }
                }
            }
            else if (true == System.IO.Directory.Exists(pathname))
            {
                foreach(string ext in OptionDialog.SupportedImageTypes)
                {
                    foreach(string filename in System.IO.Directory.GetFiles(pathname, "*." + ext))
                    {
                        fileList.Add(filename);
                    }
                }
            }
            else
            {
                throw new Exception("Cannot find file " + pathname);
            }

            return fileList;
        }

        /// <summary>
        /// Find list of faces enclosed by a rect
        /// </summary>
        /// <param name="doCreateList">WHen true create a list of selected faces</param>
        /// <returns>Number of faces enclosed</returns>
        private int FindEnclosedFaces(bool doCreateList)
        {
            Rect rect = new Rect();
            int encloseCount = 0;
            if (true == doCreateList)
            {
                _createGroupFaceList.Clear();
            }

            rect.X = GetLeft(_selectRectangle);
            rect.Y = GetTop(_selectRectangle);
            rect.Width = _selectRectangle.Width;
            rect.Height = _selectRectangle.Height;

            foreach (Face face in _allFaceList)
            {
                if (true == face.HitTest(rect))
                {
                    face.Selected = Face.SelectionStateEnum.GroupCreationSelect;
                    if (true == doCreateList)
                    {
                        _createGroupFaceList.Add(face);
                    }
                    ++encloseCount;
                }
                else
                {
                    if (true == doCreateList)
                    {
                        _createGroupFaceList.Remove(face);
                    }
                    face.Selected = Face.SelectionStateEnum.None;
                }
            }

            return encloseCount;
        }

        private void ClearSelectionList()
        {
            foreach (Face face in _createGroupFaceList)
            {
                face.Selected = Face.SelectionStateEnum.None;
            }
            _createGroupFaceList.Clear();
        }
        
        public void MouseButtonDownHandler(object sender, MouseButtonEventArgs e)
        {

            if (SelectState.None == SelectionState)
            {
                if (e.ClickCount == 2)
                {
                    switch (e.ChangedButton)
                    {
                        case MouseButton.Left:
                            ResetZoom();
                            e.Handled = true;
                            break;
                        case MouseButton.Right:
                            DoUpdateLayout();
                            break;                     
                    }
                    return;
                }


                _mouseDownLocation = e.GetPosition(_backgroundCanvas);
                ClearSelectionList();

                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                        SelectionState = SelectState.GroupCreateCorner;
                        break;

                    case MouseButton.Right:
                        SelectionState = SelectState.GroupCreateCenter;
                        break;

                    case MouseButton.Middle:
                        _mouseDownLocation.X -= _translateTransform.X;
                        _mouseDownLocation.Y -= _translateTransform.Y;
                        SelectionState = SelectState.Pan;
                        break;

                    default:
                        SelectionState = SelectState.None;
                        break;
                }

                if (SelectState.GroupCreateCenter == SelectionState || SelectState.GroupCreateCorner == SelectionState)
                {
                    _selectRectangle.Height = 0;
                    _selectRectangle.Width = 0;
                    _selectRectangle.Visibility = Visibility.Visible;
                    _menuTimer.Start();
                }
                else if (SelectState.Pan == SelectionState)
                {
                    // Nothing
                }
                else
                {
                    EndSelect();
                }
            }
        }

        private void EndSelect()
        {
            _selectRectangle.Visibility = Visibility.Hidden;
            _menuTimer.Stop();
            SelectionState = SelectState.None;
        }

        public void MouseButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            switch (SelectionState)
            {
                case(SelectState.GroupCreateCorner):
                    EndSelect();
                    break;

                case (SelectState.GroupCreateCenter):
                    EndSelect();
                    break;

                case(SelectState.Element):
                    EndSelect();
                    break;

                case(SelectState.ExtendElementDrag):
                    break;

                case(SelectState.ExtendElementSelect):
                    break;

                case (SelectState.GroupSelect):
                    EndSelect();
                    break;

                case (SelectState.GroupMenu):
                    EndSelect();
                    break;

                case (SelectState.Pan):
                    SelectionState = SelectState.None;
                    break;
                    
                default:
                    break;
            }
        }

        /// <summary>
        /// Returns a new ID for an object
        /// </summary>
        /// <returns></returns>
        internal int CreateNewObjectID()
        {
            return ++_nextID;
        }

        public void MouseMoveEventHandler(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(_backgroundCanvas);
            StatusText = "Mouse " + mousePos.X.ToString() + " " + mousePos.Y.ToString() + "  " +
                ((int)_mouseDownLocation.X).ToString() + " " + ((int)_mouseDownLocation.Y).ToString() + " " +
                ((int)_translateTransform.X).ToString() + " " + ((int)_translateTransform.Y).ToString() + "  " 
                + CurrentScale.ToString();

            if (SelectionState == SelectState.GroupCreateCorner || SelectionState == SelectState.GroupCreateCenter)
            {
                switch (SelectionState)
                {
                    case SelectState.GroupCreateCorner:
                        _selectRectangle.Width = Math.Abs(mousePos.X - _mouseDownLocation.X);
                        _selectRectangle.Height = Math.Abs(mousePos.Y - _mouseDownLocation.Y);
                        SetLeft(_selectRectangle, Math.Min(mousePos.X, _mouseDownLocation.X));
                        SetTop(_selectRectangle, Math.Min(mousePos.Y, _mouseDownLocation.Y));
                        break;

                    case SelectState.GroupCreateCenter:
                        _selectRectangle.Width = 2.0 * Math.Abs(mousePos.X - _mouseDownLocation.X);
                        _selectRectangle.Height = 2.0 * Math.Abs(mousePos.Y - _mouseDownLocation.Y);
                        SetTop(_selectRectangle, _mouseDownLocation.Y - _selectRectangle.Height / 2.0);
                        SetLeft(_selectRectangle, _mouseDownLocation.X - _selectRectangle.Width / 2.0);
                        break;

                    default:
                        // nothing
                        break;
                }
                _menuTimer.Stop();

                if (FindEnclosedFaces(false) > 0)
                {
                    _menuTimer.Start();
                }
            }
            else if (SelectState.Pan == SelectionState)
            {
                _translateTransform.X = (mousePos.X - _mouseDownLocation.X);
                _translateTransform.Y = (mousePos.Y - _mouseDownLocation.Y);
                //ApplyCurrentTranslation();
            }

        }

        private void ShowMenuHandler(Object sender, EventArgs e)
        {
            _createGroupMenu.IsOpen = true;

        }

        private void MenuClickedHandler(Object sender, RoutedEventArgs e)
        {
            MenuItem clickedItem = e.OriginalSource as MenuItem;
            _menuTimer.Stop();
            Group.DisplayState newGroupDisplayState = Group.DisplayState.None;

            string clickName = (string)clickedItem.Name;
            switch (clickName)
            {
                case "Grid":
                    newGroupDisplayState = Group.DisplayState.Grid;
                    break;

                case "Stack":
                    newGroupDisplayState = Group.DisplayState.Stack;
                    break;

                case "Cancel":
                    newGroupDisplayState = Group.DisplayState.None;
                    break;
            }

            if (newGroupDisplayState != Group.DisplayState.None)
            {
                FindEnclosedFaces(true);
                AddGroup(_createGroupFaceList, newGroupDisplayState);
            }

            EndSelect();
        }


        public void MouseWheelHandler(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                _currentScale *= _scaleIncrement;
            }
            else
            {
                _currentScale /= _scaleIncrement;
            }
            _currentScale = Math.Max(_currentScale, _minZoomScale);
            Point beforePos = e.GetPosition(this);
            _scaleTransform.ScaleX = _currentScale;
            _scaleTransform.ScaleY = _currentScale;

            Point afterPos = e.GetPosition(this);
            _translateTransform.X = _translateTransform.X + afterPos.X - beforePos.X;
            _translateTransform.Y = _translateTransform.Y + afterPos.Y - beforePos.Y;

            ApplyCurrentZoom();
        }

        private void ApplyCurrentZoom()
        {
            _inverseTransform = new ScaleTransform(1.0 / CurrentScale, 1.0 / CurrentScale);

            double fac = (1.0 - CurrentScale) * (1.0 - 1.0 / CurrentScale);
            _inverseTransform.CenterX = _scaleTransform.CenterX * fac;
            _inverseTransform.CenterY = _scaleTransform.CenterY * fac;

            List<Face> faceList = GetFreeFaces();
            foreach (Face face in faceList)
            {
                face.RenderTransform = _inverseTransform;
            }

            foreach (Group group in Groups)
            {
                group.RenderTransform = _inverseTransform;
            }
            if (CurrentScale < 1.0)
            {
                Height = _backgroundCanvas.Height / CurrentScale;
                Width = _backgroundCanvas.Width / CurrentScale;
            }
        }

        private void ApplyCurrentTranslation()
        {
            SetLeft(_backgroundCanvas, -_translateTransform.X);
            SetTop(_backgroundCanvas, -_translateTransform.Y);
        }
        private void ResetZoom()
        {
            _translateTransform.X = 0.0;
            _translateTransform.Y = 0.0;
            _currentScale = 1.0;
            _scaleTransform.ScaleX = CurrentScale;
            _scaleTransform.ScaleY = CurrentScale;
            _scaleTransform.CenterX = 0.0;
            _scaleTransform.CenterY = 0.0;
            ApplyCurrentZoom();
        }

        private void RemoveGroupCompletedHandler(Object sender, EventArgs e)
        {
            if (null != _groupToDestroy)
            {
                Children.Remove(_groupToDestroy);
                if (true == _doDestroyLastRemovedGroup)
                {
                    Groups.Remove(_groupToDestroy);
                }
                _groupToDestroy = null;
                _doDestroyLastRemovedGroup = false;
            }

        }

        public void RunUpdate(bool doBackground)
        {
            _doBackground = doBackground;

            if (true == _doBackground)
            {
                if (null == runMdsUpdate)
                {
                    ErrorText = "No MDS Update delegate installed";
                    return;
                }

                if (null == _backgroundWorker)
                {
                    _backgroundWorker = new BackgroundWorker();
                    _backgroundWorker.DoWork += DoWorkEventHandler;
                    _backgroundWorker.ProgressChanged += DoProgressChangedEventHandler;
                    _backgroundWorker.RunWorkerCompleted += DoWorkCompleteEventHandler;
                    _backgroundWorker.WorkerReportsProgress = true;
                }

                List<DataExchange> dataExchangeList = new List<DataExchange>();

                for (int i = 0; i < 2; ++i)
                {
                    dataExchangeList.Add(DataExchange);
                    AddDistanceMatrix(dataExchangeList[i], false);
                }

                _backgroundWorker.RunWorkerAsync(dataExchangeList);
            }
        }

        private void DoWorkEventHandler(Object sender, DoWorkEventArgs e)
        {
            List<DataExchange> dataExchangeList  = e.Argument as List<DataExchange>;

            while (_doBackground)
            {
                
                BackgroundWorker backgroundWorker = sender as BackgroundWorker;

                try
                {
                    runMdsUpdate(dataExchangeList[0]);
                    dataExchangeList[1].DistanceMatrix = dataExchangeList[0].DistanceMatrix;
                    dataExchangeList[1].Coordinates = dataExchangeList[0].Coordinates;
                    dataExchangeList[1].DataRect = dataExchangeList[0].DataRect;
                }
                catch (Exception ex)
                {
                    String msg = ex.Message;
                }

                backgroundWorker.ReportProgress(0, dataExchangeList[1]);
            }
        }

        private void DoProgressChangedEventHandler(Object sender, ProgressChangedEventArgs e)
        {
            DataExchange dataExchange = e.UserState as DataExchange;
            if (null != dataExchange)
            {
                CopyBackExchangeData(dataExchange);
            }
        }

        private void DoWorkCompleteEventHandler(Object sender, RunWorkerCompletedEventArgs e)
        {
            DataExchange dataExchange = e.UserState as DataExchange;
            if (null != dataExchange)
            {
                CopyBackExchangeData(dataExchange);
            }

            StatusText = "Update complete";
        }
        #endregion privateMethods
    }

}