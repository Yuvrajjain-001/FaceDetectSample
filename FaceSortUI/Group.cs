using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.Serialization;

namespace FaceSortUI
{
    

    /// <summary>
    /// A group holds a collection of faces
    /// </summary>
    [Serializable()]
    public class Group : System.Windows.Controls.Canvas, FaceSortUI.IFaceContainer, IDisplayableElement
    {
        #region classVariables
        /// <summary>
        /// Describes current selection state as:
        /// None - default state
        /// Element Selection of the whole group (Mouse down on group background
        /// ChildrenSelect - One of my child elements selected.
        /// </summary>
        public enum SelectionState { None, ElementSelect, ChildrenSelect };

        /// <summary>
        /// Group display mode
        /// grid - display faces in rectangular grid
        /// stack - display as a stack
        /// SortSplit - Grouped inside a sortSplit structure
        /// </summary>
        public enum DisplayState { None, Grid, Stack, SortSplit };

        private int _ID;
        private int _parentGroupID;
        private TextBox _tagTextBox;

        private SelectionState _selectState;
        private BackgroundCanvas _mainCanvas;
        private IDisplayableElement _parentGroup;
        private Point _mouseDownOffset;     // Offset of mouse down releative to Top Left
        private DisplayState _displayState;
        private Button _splitButton;
        private Face _activeFace;

        static Color _defaultColor = Color.FromArgb(128, 255, 255, 255);
        static Color _selectColor = Color.FromArgb(178, 255, 255, 255);
        static double _textBoxYMargin = 0.001;
        #endregion 

        #region publicMethods
        /// <summary>
        /// Constructor. Initializes static variables.
        /// Use AddFaces to add faces to the group
        /// </summary>
        /// <param name="mainCanvas">The main parent canvas</param>
        /// <param name="ID">Unique identifier for the group</param>
        public Group(BackgroundCanvas mainCanvas, int ID)
        {
            _ID = ID;
            Initialize(mainCanvas);
        }

        private void Initialize(BackgroundCanvas mainCanvas)
        {
            _mainCanvas = mainCanvas;
            _parentGroup = null;
            Background = new SolidColorBrush(_defaultColor);
            _displayState = DisplayState.Grid;
            Selected = SelectionState.None;
            _tagTextBox = new TextBox();
            Children.Add(_tagTextBox);
            _tagTextBox.KeyDown += TextBoxKeyDownHandler;
            _splitButton = new Button();
            _splitButton.Content = @"/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/";
            _splitButton.FontSize = 4;
            Children.Add(_splitButton);
            _splitButton.RenderTransform = new RotateTransform(90);
            _splitButton.Click += SplitButtonClickHandler;
            HideSplitIcon(null);
            _activeFace = null;

            MouseLeftButtonDown += MouseLeftButtonDownHandler;
            MouseLeftButtonUp += MouseLeftButtonUpHandler;
            MouseRightButtonDown += MouseRightButtonDownHandler;
            MouseMove += MouseMoveEventHandler;

            LayoutUpdated += LayoutChangedHandler;
        }

        /// <summary>
        /// Deserialization constructor for a group
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        /// <param name="mainCanvas">Main canvas</param>
        /// <param name="id">Unique id identifying group</param>
        public Group(SerializationInfo info, StreamingContext context, BackgroundCanvas mainCanvas, int id)
        {
            _ID = info.GetInt32("GroupID" + id.ToString());
            Initialize(mainCanvas);

            _parentGroupID = info.GetInt32("_GroupParentGroupID" + MyID.ToString());
            _tagTextBox.Text = info.GetString("_GroupTagName" + MyID.ToString());
            Canvas.SetLeft(this, info.GetDouble("GroupLeft" + MyID.ToString()));
            Canvas.SetTop(this, info.GetDouble("GroupTop" + MyID.ToString()));
            DisplayMode = (DisplayState)info.GetInt32("GroupDisplayState" + MyID.ToString());
        }
        /// <summary>
        /// Serialization
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        /// <param name="id">Unique identifier for this group. In range [0:numGroups]</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context, int id)
        {
            info.AddValue("GroupID" + id.ToString(), MyID);
            info.AddValue("_GroupParentGroupID" + MyID.ToString(), _parentGroupID);
            info.AddValue("_GroupTagName" + MyID.ToString(), _tagTextBox.Text);
            info.AddValue("GroupLeft" + MyID.ToString(), Canvas.GetLeft(this));
            info.AddValue("GroupTop" + MyID.ToString(), Canvas.GetTop(this));
            info.AddValue("GroupDisplayState" + MyID.ToString(), (int)DisplayMode);

        }
        /// <summary>
        /// Get/Set the tag associated with the group
        /// </summary>
        public new string Tag
        {
            get
            {
                return _tagTextBox.Text;
            }
            set
            {
                _tagTextBox.Text = value;
                Canvas.SetTop(_tagTextBox, (ExtentRect.Height * _textBoxYMargin));
                Canvas.SetLeft(_tagTextBox, (ExtentRect.Width - _tagTextBox.ActualWidth) / 2.0);
                _tagTextBox.IsEnabled = false;
            }
        }
        /// <summary>
        /// Return a the current extent of the group as a Rect
        /// </summary>
        public Rect ExtentRect
        {
            get
            {
                Rect rect = new Rect();
                Point topLeft = GetPositionRelativeToCanvas();
                rect.X = topLeft.X;
                rect.Y = topLeft.Y;
                rect.Width = Width;
                rect.Height = Height;
                return rect;
            }
        }
        /// <summary>
        /// Current  Display mode (either stack or grid)
        /// </summary>
        public DisplayState DisplayMode
        {
            get
            {
                return _displayState;
            }
            set
            {
                _displayState = value;
            }
        }

        /// <summary>
        /// Returns count of faces
        /// </summary>
        public int FaceCount
        {
            get
            {
                int faceCount = 0;
                foreach (UIElement element in Children)
                {
                    if (element.GetType() == typeof(Face))
                    {
                        ++faceCount;
                    }
                }
                return faceCount;
            }
        }

        /// <summary>
        /// Gets or sets the face selection mode
        /// </summary>
        public SelectionState Selected
        {
            get
            {
                return _selectState;
            }

            set
            {
                _selectState = value;
                if (_selectState == SelectionState.ElementSelect)
                {
                    Background = new SolidColorBrush(_selectColor);
                }
                else
                {
                    Background = new SolidColorBrush(_defaultColor);
                }
            }
        }

        /// <summary>
        /// Add a list of faces to the group. Clears out all previous
        /// faces
        /// </summary>
        /// <param name="faces">Faces to add</param>
        public void AddFaces(List<Face> faces)
        {
            ClearAllFaces();
            AppendFaces(faces);
            if (Tag.Length <= 0)
            {
                Tag = "Group_"+_ID.ToString();
            }

        }
        /// <summary>
        /// Move a face out of my group into another sort group
        /// </summary>
        /// <param name="face">Face to move</param>
        public void FaceSortMove(Face face)
        {
            if (DisplayState.SortSplit == DisplayMode && MyParent is SortGroup)
            {
                ((SortGroup)MyParent).MoveFace(this, face);
            }
        }

        /// <summary>
        /// Move this and all reamaining faces in the group to another sortGroup
        /// </summary>
        /// <param name="face"></param>
        public void FaceSortMoveAll(Face face)
        {
            if (DisplayState.SortSplit == DisplayMode && MyParent is SortGroup)
            {
                List<Face> faceList = GetFaceList();
                bool found = false;

                foreach (Face nextFace in faceList)
                {
                    if (true == found || face == nextFace)
                    {
                        ((SortGroup)MyParent).MoveFace(this, nextFace);
                        found = true;
                    }
                }
            }
        }
        /// <summary>
        /// Reparent s group 
        /// </summary>
        /// <param name="newParent">My new parent</param>
        public void AddToGroup(Panel newParent)
        {
            if (newParent == _mainCanvas)
            {
                _parentGroup = null;
            }
            else
            {
                _parentGroup = (IDisplayableElement)newParent;
            }
        }
        /// <summary>
        /// Add a face to my child list
        /// </summary>
        /// <param name="face">Face to add</param>
        public void AddFace(Face face)
        {
            Children.Add(face);
        }
        /// <summary>
        /// Remove a face from my ChildList
        /// </summary>
        /// <param name="face">Face to Remove</param>
        /// <param name="doDestroy">Not used</param>
        public void RemoveFace(Face face, bool doDestroy)
        {
            Children.Remove(face);
            if (FaceCount <= 0)
            {
                _mainCanvas.RemoveGroup(this, false);
            }
            //Display();
        }


        /// <summary>
        /// Clears out all existing faces from the group
        /// Sending them back to the main Canvas
        /// </summary>
        public void ClearAllFaces()
        {
            List<Face> faceList = GetFaceList();
            foreach (Face face in faceList)
            {
                Point pos = face.GetPositionRelativeToCanvas();
                face.RemoveFromGroup(_mainCanvas);
                SetTop(face, pos.Y);
                SetLeft(face, pos.X);
            }
        }

        /// <summary>
        /// Append a list of faces to the group
        /// </summary>
        /// <param name="faceList">List of faces to add</param>
        public void AppendFaces(List<Face> faceList)
        {
            Rect maxListExtent = ListExtent(faceList);
            foreach (Face face in faceList)
            {
                face.AddToGroup(this, maxListExtent);
            }

            maxListExtent.Width += 10;
            maxListExtent.Height += 50;

            SetTop(this, maxListExtent.Top);
            SetLeft(this, maxListExtent.Left);
            Width = 0.0;
            Height = maxListExtent.Height;

            Display();
        }

        /// <summary>
        /// Default wrapper method for display current group
        /// // Calls the real method with faceCount=-1
        /// </summary>
        public void Display()
        {
            Display(-1);
        }

        /// <summary>
        /// Display the group using the current display mode.  
        /// </summary>
        /// <param name="faceCount">Suggested count. If less 0, use actual count</param>
        public void Display(int faceCount)
        {
            List<Face> faceList = GetFaceList();
            if (faceCount < 0)
            {
                faceCount = faceList.Count;
            }

            switch (_displayState)
            {
                case DisplayState.Grid:
                    DisplayAsGrid(faceList, faceCount);
                    break;

                case DisplayState.Stack:
                    DisplayAsStack(faceList, faceCount);
                    break;

                case DisplayState.SortSplit:
                    DisplayAsGrid(faceList, faceCount);
                    break;

                default:
                    DisplayAsGrid(faceList, faceCount);
                    break;
            }
        }

        /// <summary>
        /// Handle redisplay completion, typically after content is added/removed. Recomputes Required size 
        /// and repositions the tag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DisplayCompleteHandler(Object sender, EventArgs e)
        {
            AdjustSize();
            // Wierd but this repositions the tag
            Tag = Tag;
        }

        /// <summary>
        /// Toggles the display mode, for example changes 
        /// stack mode to grid mode
        /// </summary>
        public void ToggleDisplay()
        {
            if (DisplayState.Grid == _displayState)
            {
                _displayState = DisplayState.Stack;
            }
            else if (DisplayState.Stack == _displayState)
            {
                _displayState = DisplayState.Grid;
            }

            Display();
            HideSplitIcon(null);
        }

        /// <summary>
        /// Return the enclosing rectangle for a list of elements
        /// </summary>
        /// <param name="elementList">List of elements</param>
        /// <returns></returns>
        public static Rect ListExtent(List <Face > elementList)
        {
            Rect maxExtent = new Rect();

            if (elementList.Count > 0)
            {
                double maxRight = 0.0;
                double maxBottom = 0.0;

                maxExtent.Y = Int32.MaxValue;
                maxExtent.X = Int32.MaxValue;
                maxExtent.Width = 0;
                maxExtent.Height = 0;

                foreach (FrameworkElement element in elementList)
                {
                    double top = GetTop(element);
                    double left = GetLeft(element);
                    maxExtent.Y = Math.Min(maxExtent.Top, top);
                    maxExtent.X = Math.Min(maxExtent.Left, left);

                    maxRight = Math.Max(maxRight, left + element.ActualWidth);
                    maxBottom = Math.Max(maxBottom, top + element.ActualHeight);
                }
                maxExtent.Width = maxRight - maxExtent.X;
                maxExtent.Height = maxBottom - maxExtent.Y;
            }

            return maxExtent;
        }

        /// <summary>
        /// Display faces as a grid
        /// </summary>
        /// <param name="faceList">List of faces</param>
        public void DisplayAsGrid(List<Face> faceList, int faceCount)
        {
            if (faceCount < 1)
            {
                return;
            }

            Point center = ListCenter(faceList);
            int leftMargin = 15;
            int topMargin = 40;
            int faceSpacing = _mainCanvas.OptionDialog.FaceDisplayWidth + _mainCanvas.OptionDialog.GridFaceSpace;
            int width = (int)Width / faceSpacing;
            if (width <= 0)
            {
                width = (int)Math.Ceiling(Math.Sqrt(faceCount));
            }
            int count = 0;

            int iFace = faceList.Count;
            foreach (Face face in faceList)
            {
                face.TargetAnimationPos_X = leftMargin + (count%width) * faceSpacing;
                face.TargetAnimationPos_Y = topMargin + (count/width) * faceSpacing;

                --iFace;
                if (0 == iFace)
                {
                    _mainCanvas.Animate(face, face.TargetAnimationPos, 1.0, -1.0, this.DisplayCompleteHandler);
                }
                else
                {
                    _mainCanvas.Animate(face, face.TargetAnimationPos, 1.0, -1.0, face.DisplayCompleteHandler);
                }
                ++count;
            }
        }
        /// <summary>
        /// Display faces as a stack
        /// </summary>
        public void DisplayAsStack(List<Face> faceList, int faceCount)
        {
            if (faceCount < 1)
            {
                return;
            }

            Point center = ListCenter(faceList);
            double xOffset = 2.0;
            double yOffset = 0.5;
            int leftMargin = 10;
            int topMargin = 40;
            int count = 0;
            SortDrawOrder(faceList);
            faceList.Reverse();

            foreach (Face face in faceList)
            {
                face.TargetAnimationPos_X = (int)Math.Round(leftMargin + count * xOffset);
                face.TargetAnimationPos_Y = (int)Math.Round(topMargin + count * yOffset);
                ++count;
                if (count == faceList.Count)
                {
                    _mainCanvas.Animate(face, face.TargetAnimationPos, 1.0, -1.0, this.DisplayCompleteHandler);
                }
                else
                {
                    _mainCanvas.Animate(face, face.TargetAnimationPos, 1.0, -1.0, face.DisplayCompleteHandler);
                }
            }
        }
        /// <summary>
        /// Resize the group outline to fit all its enclosed faces
        /// </summary>
        public void AdjustSize()
        {
            List<Face> faceList = GetFaceList();
            AdjustSize(faceList);
        }

        /// <summary>
        /// Resize the group outline to fit all supplied faces
        /// </summary>
        /// <param name="faceList">List of faces to use</param>
        public void AdjustSize(List<Face> faceList)
        {
            if (faceList.Count == 0)
            {
                Width = 0;
                Height = 0;
                return;
            }

            int maxTop = Int32.MaxValue;
            int maxLeft = Int32.MaxValue;
            int maxBottom = 0;
            int maxRight = 0;

            int iFace = faceList.Count;
            foreach (Face face in faceList)
            {
                int top = (int)face.TargetAnimationPos.Y;
                int left = (int)face.TargetAnimationPos.X;
                int width = (int)face.MyWidth;
                int height = (int)face.MyHeight;

                maxTop = Math.Min(maxTop, top);
                maxLeft = Math.Min(maxLeft, left);
                maxRight = Math.Max(maxRight, left + width);
                maxBottom = Math.Max(maxBottom, top + height);

                --iFace;
                if (iFace == 0)
                {
                    // Capture the last face's final position
                    SetTop(face, face.TargetAnimationPos.Y);
                    SetLeft(face, face.TargetAnimationPos.X);
                }
            }

            Width = maxRight - maxLeft + 30;
            Height = maxBottom - maxTop + 60;

        }

        /// <summary>
        /// Test if a point lies within the Group
        /// </summary>
        /// <param name="pos">Test position</param>
        /// <returns>true if point is within the rect </returns>
        public bool HitTest(Point pos)
        {
            if (pos.X >= ExtentRect.Left &&
                pos.X < ExtentRect.Right &&
                pos.Y >= ExtentRect.Top &&
                pos.Y < ExtentRect.Bottom)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sort  current group based on distance to an input Face
        /// </summary>
        /// <param name="face">Face to do the sort</param>
        public void SortOnFace(Face face)
        {
            SortedDictionary<double, Face> newSortOrder = new SortedDictionary<double,Face>();

            foreach (KeyValuePair<Face, double> keyVal in face.DistanceVector)
            {
                if (Children.Contains((UIElement)keyVal.Key))
                {
                    newSortOrder.Add(keyVal.Value, keyVal.Key);
                }
            }

            List<Face> newFaceOrder = new List<Face>();

            foreach (KeyValuePair<double, Face> keyVal in newSortOrder)
            {
                newFaceOrder.Add(keyVal.Value);
            }

            SortDrawOrder(newFaceOrder);
            Display();

        }

        /// <summary>
        /// Reposition a face within the group based on its current display position
        /// </summary>
        /// <param name="face">Face to reposition</param>
        public void Reposition(Face face)
        {
            Point pos = new Point(GetLeft(face), GetTop(face));
            Children.Remove(face);

            int id = 0;
            foreach (UIElement testFace in Children)
            {
                if (testFace is Face)
                {
                    if (false == ((Face)testFace).IsDisplayedBefore(pos))
                    {
                        Children.Insert(id, face);
                        break;
                    }
                }
                ++id;
            }

            if (id >= Children.Count)
            {
                Children.Add(face);
            }
        }
        /// <summary>
        /// Drop another group into me. This transfer entire contents of otherGroup
        /// to me and otherGroup is deleted
        /// </summary>
        /// <param name="otherGroup"></param>
        public void DropGroup(Group otherGroup)
        {
            foreach (Face face in otherGroup.GetFaceList())
            {
                face.RemoveFromGroup(this);
            }
        }

        public void ShowSplitIcon(Face face)
        {
            _splitButton.Visibility = Visibility.Visible;
            double left = Canvas.GetLeft(face);
            double top = Canvas.GetTop(face);
            double right = left + face.MyWidth;

            Canvas.SetLeft(_splitButton, left + _mainCanvas.OptionDialog.GridFaceSpace/2);
            Canvas.SetTop(_splitButton, top - _mainCanvas.OptionDialog.BorderWidth);

            Canvas.SetZIndex(_splitButton, FaceCount + 2);
            _activeFace = face;
        }

        public void HideSplitIcon(Face face)
        {
            _splitButton.Visibility = Visibility.Hidden;
            _activeFace = null;
        }

        #region IDisplayableElementImplementation
        /// <summary>
        /// Returns a reference to my parent canvas
        /// </summary>
        public IDisplayableElement MyParent
        {
            get
            {
                return _parentGroup;
            }
        }

        /// <summary>
        /// Returns my unique ID (inherited form IDisplayableElement
        /// </summary>
        public int MyID
        {
            get
            {
                return _ID;
            }
        }

        /// <summary>
        /// Ensure that parentID is synced to current parent 
        /// Should sync before serializing
        /// </summary>
        public void SyncParentID()
        {

            if (null != _parentGroup)
            {
                _parentGroupID = _parentGroup.MyID;
            }
            else
            {
                _parentGroupID = BackgroundCanvas.NOPARENT;
            }
        }
        /// <summary>
        /// Rebuild my parent hierachy, typically following deserialization
        /// </summary>
        /// <param name="backgroundCanvas"></param>
        public void RebuildTree(BackgroundCanvas backgroundCanvas)
        {
            _mainCanvas = backgroundCanvas;
            _parentGroup = (Group)_mainCanvas.FindParent(_parentGroupID);
            Display();
        }

        /// <summary>
        /// Return my position relative to teh main canvas
        /// </summary>
        /// <returns></returns>
        public Point GetPositionRelativeToCanvas()
        {
            return _mainCanvas.GetPositionRelativeToCanvas(this);
        }

        /// <summary>
        /// Sync with options dialog
        ///  Currently Nothing
        /// </summary>
        public void SyncDisplayOptions()
        {
        }

        #endregion IDisplayableElementImplementation
        #endregion publicmethods

        #region privateMethods

        private void SplitButtonClickHandler(Object sender, RoutedEventArgs e)
        {
            if (null != _activeFace)
            {
                _activeFace.MenuSplitAndMove(null, null);
            }
        }

        private void SortDrawOrder(List<Face> faceList)
        {

            foreach (Face face in faceList)
            {
                Children.Remove(face);
                Children.Add(face);
            }
        }
        private void Animate(Face face, int toLeft, int toTop, double opacity, double duration)
        {
            face.Opacity = opacity;
            SetLeft(face, toLeft);
            SetTop(face, toTop);
        }

        private List<Face> GetFaceList()
        {
            List<Face> faceList = new List<Face>();

           foreach (UIElement obj in Children)
            {
                if (obj.GetType() == typeof(Face))
                {
                    faceList.Add((Face)obj);
                }
            }

            return faceList;

        }

        private void ShowContextMenu()
        {
            GroupContextMenu menu = new GroupContextMenu();

            for (int iCount = 0; iCount < menu.Items.Count; ++iCount)
            {
                MenuItem it = (MenuItem)menu.Items[iCount];
                
                switch (it.Name)
                {
                    case "Delete":
                        if (DisplayState.SortSplit == DisplayMode)
                        {
                            it.IsEnabled = false;
                        }
                        break;

                    case "Split":
                        if (DisplayState.SortSplit == DisplayMode)
                        {
                            it.IsEnabled = false;
                        }
                        break;

                    case "Unsplit":
                        if (DisplayState.SortSplit != DisplayMode)
                        {
                            it.IsEnabled = false;
                        }
                        break;

                    default:
                        break;
                }

                if (it.IsEnabled == true)
                {
                    it.Click += MenuClickedHandler;
                }
            }
            menu.IsOpen = true;
        }

        private void MenuClickedHandler(Object sender, RoutedEventArgs e)
        {
            MenuItem clickedItem = e.OriginalSource as MenuItem;
            string clickName = (string)clickedItem.Name;

            switch (clickName)
            {
                case "Edit":
                    _tagTextBox.IsEnabled = true;
                    _tagTextBox.Focus();
                    _tagTextBox.Select(0, Tag.Length);
                    break;

                case "Delete":
                    _mainCanvas.RemoveGroup(this, true);
                    break;

                case "Split":
                    _mainCanvas.AddSplitGroup(this);
                    break;

                case "Unsplit":
                    _mainCanvas.RemoveSplitGroup(this);
                    break;

                default:
                    break;
            }

            _mainCanvas.SelectionState = BackgroundCanvas.SelectState.None;
        }

        private Point ListCenter(List<Face> faceList)
        {
            Point center = new Point();
            int count = 0;
            center.X = 0;
            center.Y = 0;

            foreach (Face face in faceList)
            {
                center.Y += GetTop(face);
                center.X += GetLeft(face);
                ++count;
            }

            if (count > 0)
            {
                center.X /= count;
                center.Y /= count;
            }

            return center;
        }

        private void OnMouseEnterHandler(object sender, MouseEventArgs e)
        {
            BitmapEffect = _mainCanvas.GlowEffect;
        }

        private void OnMouseLeaveHandler(object sender, MouseEventArgs e)
        {
            BitmapEffect = null;
        }

        private void MouseRightButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            ShowContextMenu();
            e.Handled = true;
        }
        private void MouseLeftButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (_mainCanvas.SelectionState == BackgroundCanvas.SelectState.None)
            {
                if (e.ClickCount > 1)
                {
                    ToggleDisplay();
                    return;
                }

                Selected = SelectionState.ElementSelect;
                _mouseDownOffset = e.GetPosition(this);
                _mainCanvas.MouseMove += MouseMoveEventHandler;
                _mainCanvas.MoveToFrontDisplayOrder(this);
                _mainCanvas.SelectionState = BackgroundCanvas.SelectState.GroupSelect;
            }

        }

        private void MouseLeftButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (Selected == SelectionState.ElementSelect)
            {
                Selected = SelectionState.None;
                _mainCanvas.MouseMove -= MouseMoveEventHandler;
                _mainCanvas.SelectionState = BackgroundCanvas.SelectState.None;
                _mainCanvas.ResetDisplayOrder();
                _mainCanvas.DragDropGroup(this, e.GetPosition(_mainCanvas));
            }
        }

        private void MouseMoveEventHandler(object sender, MouseEventArgs e)
        {
            if (Selected == SelectionState.ElementSelect)
            {
                Point mousePos = e.GetPosition(_mainCanvas);

                Canvas.SetLeft(this, mousePos.X - _mouseDownOffset.X);
                Canvas.SetTop(this, mousePos.Y - _mouseDownOffset.Y);

                _mainCanvas.HighlightGroups(mousePos, this);
            }
        }

        private void TextBoxKeyDownHandler(Object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                _tagTextBox.IsEnabled = false;
            }
        }
        private void LayoutChangedHandler(Object sender, EventArgs e)
        {
            Canvas.SetTop(_tagTextBox, (ExtentRect.Height * _textBoxYMargin));
            Canvas.SetLeft(_tagTextBox, (ExtentRect.Width - _tagTextBox.ActualWidth) / 2.0);

        }

        #endregion privateMethods

    }
}
