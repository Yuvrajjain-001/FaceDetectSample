using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FaceSortUI
{
    /// <summary>
    /// Container for 1-D sorting. Two groups contained are attached to the container and
    /// faces can be transfered between the groups.
    /// 
    /// </summary>
    public class SortGroup : DockPanel, FaceSortUI.IDisplayableElement
    {
        /// <summary>
        /// Describes current selection state as:
        /// None - default state
        /// Element Selection of the whole group (Mouse down on group background
        /// ChildrenSelect - One of my child elements selected.
        /// </summary>
        public enum SelectionState { None, ElementSelect, ChildrenSelect };

        /// <summary>
        /// Enclosed groups
        /// </summary>
        private List<Group> _groups;
        private List<Border> _borderGroups;

        private BackgroundCanvas _mainCanvas;

        private int _ID;
        private int _parentGroupID;
        private IDisplayableElement _parentGroup;
        private Point _mouseDownOffset;     // Offset of mouse down releative to Top Left
        private SelectionState _selectState;

        private SortGroup(BackgroundCanvas mainCanvas, Group group1, Group group2, int id)
        {
            Initialize(mainCanvas, group1, group2, id);

        }
        public SortGroup(BackgroundCanvas mainCanvas, Group group1, int id)
        {
            Group group2 = new Group(mainCanvas, mainCanvas.CreateNewObjectID());
            group2.DisplayMode = Group.DisplayState.Grid;
            group2.Tag = group1.Tag + "_Other";

            Initialize(mainCanvas, group1, group2, id);
        }



        private void Initialize(BackgroundCanvas mainCanvas, Group group1, Group group2, int id)
        {
            _mainCanvas = mainCanvas;

            _groups = new List<Group>();
            _borderGroups = new List<Border>();

            Canvas.SetTop(this, Canvas.GetTop(group1));
            Canvas.SetLeft(this, Canvas.GetLeft(group1));

            Border border1 = AddGroup(group1);
            border1.BorderThickness = new Thickness(0, 0, 0, _mainCanvas.OptionDialog.BorderWidth);
            SetDock(border1, Dock.Top);
            Children.Add(border1);

            Border border2 = AddGroup(group2);
            border2.BorderThickness = new Thickness(0, _mainCanvas.OptionDialog.BorderWidth, 0, 0);
            SetDock(border2, Dock.Bottom);
            Children.Add(border2);

            MouseDown += MouseButtonDownHandler;
            MouseUp += MouseButtonUpHandler;
            MouseMove += MouseMoveEventHandler;


            _ID = id;
            LastChildFill = true;
            Background = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255));

        }
        #region publicmethods

        /// <summary>
        /// Destroy this group by unhooking all its children
        /// </summary>
        public void DestroySortGroup()
        {
            foreach (UIElement element in Children)
            {
                if (element.GetType() == typeof(Border))
                {
                    ((Border)element).Child = null;
                }
            }
            Children.Clear();
        }
        /// <summary>
        /// Move a face out of the group into another group
        /// </summary>
        /// <param name="from">Group to move from</param>
        /// <param name="face">The face to move</param>
        public void MoveFace(Group from, Face face)
        {
            Group to = null;
            foreach (Group toGroup in Groups)
            {
                if (toGroup != from)
                {
                    to = toGroup;
                    break;
                }
            }

            int maxFaceCount = 0;
            foreach (Group group in Groups)
            {
                maxFaceCount = Math.Max(maxFaceCount, group.FaceCount);
            }

            if (to != null)
            {
                face.RemoveFromGroup(to);
                if (true == _mainCanvas.OptionDialog.DoGroupRedisplay)
                {
                    from.Display(maxFaceCount);
                }
                to.Display(maxFaceCount);
            }

            double width = 0;
            foreach (Group group in Groups)
            {
                if (true == _mainCanvas.OptionDialog.DoGroupRedisplay || group == to)
                {
                    group.DisplayCompleteHandler(null, null);
                }
                width = Math.Max(width, group.Width);
            }

            foreach (Group group in Groups)
            {
                Canvas.SetLeft(group, 0);
                group.Width = width;
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
        }


        /// <summary>
        /// Get/set teh display state of the sort group
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
            }
        }
        /// <summary>
        /// Get a List of groups associated with teh sortGroup
        /// </summary>
        public List<Group> Groups
        {
            get
            {
                return _groups;
            }
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
        /// Find absolute location of the group (relative to main canvas)
        /// </summary>
        /// <returns></returns>
        public Point GetPositionRelativeToCanvas()
        {
            Point pos = new Point();
            pos.X = Canvas.GetLeft(this);
            pos.Y = Canvas.GetTop(this);


            return pos;
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

        private Border AddGroup(Group group)
        {
            Border border = new Border();
            border.BorderThickness = new Thickness(0);
            border.BorderBrush = Brushes.Black;
            border.Background = Brushes.Transparent;

            border.Child = group;
            group.AddToGroup(this);
            group.DisplayMode = Group.DisplayState.SortSplit;

            Groups.Add(group);
            _borderGroups.Add(border);
            Canvas.SetTop(group, 0);
            Canvas.SetLeft(group, 0);


            return border;
        }

        private void MouseButtonDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (SelectionState.None == Selected)
            {
                if (MouseButton.Left == e.ChangedButton)
                {
                    _mouseDownOffset = e.GetPosition(this);
                    _mainCanvas.MouseMove += MouseMoveEventHandler;
                    _mainCanvas.MoveToFrontDisplayOrder(this);
                    _mainCanvas.SelectionState = BackgroundCanvas.SelectState.GroupSelect;
                    Selected = SelectionState.ElementSelect;
                }
            }

        }

        private void MouseButtonUpHandler(object sender, MouseButtonEventArgs e)
        {
            Selected = SelectionState.None;
            _mainCanvas.MouseMove -= MouseMoveEventHandler;
            _mainCanvas.SelectionState = BackgroundCanvas.SelectState.None;
            _mainCanvas.ResetDisplayOrder();
        }

        private void MouseMoveEventHandler(object sender, MouseEventArgs e)
        {
            if (Selected == SelectionState.ElementSelect)
            {
                Point mousePos = e.GetPosition(_mainCanvas);

                Canvas.SetLeft(this, mousePos.X - _mouseDownOffset.X);
                Canvas.SetTop(this, mousePos.Y - _mouseDownOffset.Y);
            }
        }
    }
}
