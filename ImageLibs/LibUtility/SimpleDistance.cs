using System;
using System.Drawing;
using System.Collections;

namespace Dpu.Utility
{
	/// <summary>
    /// Summary description for SimpleDistance.
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(false)]
    public class SimpleDistance
	{
        [System.Runtime.InteropServices.ComVisible(false)]
        struct ItemBounds
        {
            public Rectangle Pixels;

            public Rectangle Buckets;

            public float[] PixDistCache;

            public int[] BucketDistCache;

            public ItemBounds(Rectangle PixelBounds, Rectangle BucketBounds)
            {
                Pixels = PixelBounds;
                Buckets = BucketBounds;
                PixDistCache = null;
                BucketDistCache = null;
            }
        }
        [System.Runtime.InteropServices.ComVisible(false)]
        enum RectanglePosition
        {
            Intersect = 0,
            Right = 1,
            Left = 2,
            Above = 4,
            AboveRight = 5,
            AboveLeft = 6,
            Below = 8,
            BelowRight = 9,
            BelowLeft = 10
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        [Flags]
        public enum SearchDirections
        {
            Right = 1,
            Left = 2,
            Above = 4,
            Below = 8,
            All = 15
        }

        Size m_BucketSize;
        ArrayList[,] m_Buckets;
        ArrayList m_Items;
        Rectangle m_Bounds;
        int m_Capacity;
        int[] m_NearestNeigborCache;

        public SimpleDistance(Size SearchArea, int ItemCount)
        {
            m_Bounds = new Rectangle(new Point(0, 0), SearchArea);
            m_BucketSize = new Size((int)Math.Pow(SearchArea.Width, .66), (int)Math.Pow(SearchArea.Height, .66));
            m_Items = new ArrayList(ItemCount);
            m_Capacity = ItemCount;
            m_NearestNeigborCache = new int[m_Capacity];
            for (int i = 0; i < m_NearestNeigborCache.Length; ++i)
            {
                m_NearestNeigborCache[i] = -1;
            }

            int x = SearchArea.Width / m_BucketSize.Width;
            if (SearchArea.Width % m_BucketSize.Width > 0)
            {
                x++;
            }

            int y = SearchArea.Height / m_BucketSize.Height;
            if (SearchArea.Height % m_BucketSize.Height > 0)
            {
                y++;
            }

            m_Buckets = new ArrayList[x, y];
        }

        public int Add(Rectangle item)
        {
            if (item.Left < m_Bounds.Left || item.Right > m_Bounds.Right || item.Top < m_Bounds.Top || item.Bottom > m_Bounds.Bottom)
            {
                throw new ArgumentException("The bounds of the item passed to Add are outside the search area.");
            }

            if (m_Items.Count == m_Capacity)
            {
                throw new ApplicationException("Illegal add of item to SimpleDistance object.  This object is at the maximum specified capacity.");
            }

            int itemId = m_Items.Count;
            int startx = item.Left / m_BucketSize.Width;
            int starty = item.Top / m_BucketSize.Height;
            int endx = (item.Right-1) / m_BucketSize.Width;
            int endy = (item.Bottom-1) / m_BucketSize.Height;

            for (int y = starty; y <= endy; ++y)
            {
                for (int x = startx; x <= endx; ++x)
                {
                    if (m_Buckets[x, y] == null)
                    {
                        m_Buckets[x, y] = new ArrayList();
                    }

                    m_Buckets[x, y].Add(itemId);
                }
            }

            ItemBounds newItem = new ItemBounds(item, Rectangle.FromLTRB(startx, starty, endx, endy));

            if (m_Items.Count < this.m_Capacity - 1)
            {
                newItem.PixDistCache = new float[m_Capacity - 1 - m_Items.Count];
                newItem.BucketDistCache = new int[m_Capacity - 1 - m_Items.Count];
            }

            return m_Items.Add(newItem);
        }

        public float PixelDistance(int item1, int item2)
        {
            int i1, i2;

            if (item1 < item2)
            {
                i1 = item1;
                i2 = item2 - item1 - 1;
            }
            else if (item1 > item2)
            {
                i1 = item2;
                i2 = item1 - item2 - 1;
            }
            else
            {
                return 0f;
            }

            if (((ItemBounds)m_Items[i1]).PixDistCache[i2] == 0F)
            {
                ((ItemBounds)m_Items[i1]).PixDistCache[i2] = Distance(((ItemBounds)m_Items[item1]).Pixels, ((ItemBounds)m_Items[item2]).Pixels);
            }

            return ((ItemBounds)m_Items[i1]).PixDistCache[i2];
        }

        public int BucketDistance(int item1, int item2)
        {
            int i1, i2;

            if (item1 < item2)
            {
                i1 = item1;
                i2 = item2 - item1 - 1;
            }
            else if (item1 > item2)
            {
                i1 = item2;
                i2 = item1 - item2 - 1;
            }
            else
            {
                return 0;
            }

            if (((ItemBounds)m_Items[i1]).BucketDistCache[i2] == 0)
            {
                ((ItemBounds)m_Items[i1]).BucketDistCache[i2] = (int)Distance(((ItemBounds)m_Items[item1]).Buckets, ((ItemBounds)m_Items[item2]).Buckets);
            }

            return ((ItemBounds)m_Items[i1]).BucketDistCache[i2];
        }

        private float Distance(Rectangle rect1, Rectangle rect2)
        {
            int x=0;
            int y=0;

            switch (GetSpacialRelationFlags(rect1,rect2))
            {
                case RectanglePosition.Intersect:  // The two rectangles intersect
                    return 0.0F;

                case RectanglePosition.Right:  
                    return rect2.Left - (rect1.Right-1);

                case RectanglePosition.Left: 
                    return rect1.Left - (rect2.Right-1);

                case RectanglePosition.Above:
                    return rect1.Top - (rect2.Bottom-1);

                case RectanglePosition.Below:
                    return rect2.Top - (rect1.Bottom-1);

                case RectanglePosition.AboveRight:
                    x = rect2.Left - (rect1.Right-1);
                    y = rect1.Top - (rect2.Bottom-1);
                    break;

                case RectanglePosition.AboveLeft:
                    x = rect1.Left - (rect2.Right-1);
                    y = rect1.Top - (rect2.Bottom-1);
                    break;

                case RectanglePosition.BelowRight:
                    x = rect2.Left - (rect1.Right-1);
                    y = rect2.Top - (rect1.Bottom-1);
                    break;

                case RectanglePosition.BelowLeft:
                    x = rect1.Left - (rect2.Right-1);
                    y = rect2.Top - (rect1.Bottom-1);
                    break;
            }
            return (float)Math.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// Gets a value containing bit flags that indicate the position of rect2 in relation to rect1.
        /// Four bits are used :
        /// position    meaning
        /// --------    -----------------------
        ///   0         rect2 is right of rect1
        ///   1         rect2 is left of rect1
        ///   2         rect2 is above rect1
        ///   3         rect2 is below rect1
        /// 
        /// Using these flags, one of nine values will be selected:
        /// 00 00 = the rectangles overlap
        /// 00 01 = rect2 is to the right of rect1 and the two overlap vertically
        /// 00 10 = rect2 is to the left of rect1 and the two overlap vertically.
        /// 01 00 = rect2 is above rect1 and the two overlap horizontally.
        /// 01 01 = rect2 is above and to the right of rect1
        /// 01 10 = rect2 is above and to the left of rect1
        /// 10 00 = rect2 is below rect1 and the two overlap horizontally
        /// 10 01 = rect2 is below and to the right of rect1
        /// 10 10 = rect2 is below and to the left of rect1
        /// </summary>
        /// <returns>A value indicating the relative position of the second rectangle in relation to the first.</returns>
        private RectanglePosition GetSpacialRelationFlags(Rectangle rect1, Rectangle rect2)
        {
            UInt32 positionflags = 0;

            if (rect1.Left > rect2.Right) // item1 is to the right of item2
            {
                positionflags = 2;
            }
            else if (rect1.Right < rect2.Left) // item2 is to the right of item1
            {
                positionflags = 1;
            }

            if (rect1.Bottom < rect2.Top)
            {
                positionflags |= 8;
            }
            else if (rect1.Top > rect2.Bottom)
            {
                positionflags |= 4;
            }

            return (RectanglePosition)positionflags;
        }

        public int[] GetItemsWithinDist(int itemIndex, int searchDistance)
        {
            Rectangle itemBounds = ((ItemBounds)m_Items[itemIndex]).Pixels;
            int PixYStart = Math.Max(0,itemBounds.Top - searchDistance);
            int PixYStop  = Math.Min(this.m_Bounds.Bottom-1, itemBounds.Bottom + searchDistance);
            int BucketYStart = PixYStart / m_BucketSize.Height;
            int BucketYStop = PixYStop / m_BucketSize.Height;
            int MinX = Math.Max(0,itemBounds.Left - searchDistance) / m_BucketSize.Width;
            int MaxX = Math.Min(this.m_Bounds.Right-1, itemBounds.Right + searchDistance) / m_BucketSize.Width;
            int DistSq = searchDistance * searchDistance;
            int[] Flags = new int[m_Items.Count];

            for (int y = BucketYStart; y <= BucketYStop; ++y)
            {
                int BucketXStart;
                int BucketXStop;

                if (y < ((ItemBounds)m_Items[itemIndex]).Buckets.Top)
                {
                    int diff = itemBounds.Top - y*m_BucketSize.Height;
                    int xdiff = (int)Math.Sqrt(Math.Abs(diff * diff - DistSq));

                    BucketXStart = Math.Max(0,itemBounds.Left - xdiff)/m_BucketSize.Width;
                    BucketXStop = Math.Min(this.m_Bounds.Right - 1, itemBounds.Right + xdiff) / m_BucketSize.Width;
                }
                else if (y > ((ItemBounds)m_Items[itemIndex]).Buckets.Bottom)
                {
                    int diff = y * m_BucketSize.Height - itemBounds.Bottom;
                    int xdiff = (int)Math.Sqrt(Math.Abs(diff * diff - DistSq));

                    BucketXStart = Math.Max(0, itemBounds.Left - xdiff) / m_BucketSize.Width;
                    BucketXStop = Math.Min(this.m_Bounds.Right - 1, itemBounds.Right + xdiff) / m_BucketSize.Width;
                }
                else
                {
                    BucketXStart = MinX;
                    BucketXStop = MaxX;
                }

                bool check = (y == BucketYStart || y == BucketYStop ? true : false);

                for (int x = BucketXStart; x <= BucketXStop; ++x)
                {
                    if (check==false && (x == BucketXStart || x == BucketXStop))
                    {
                        check = true;
                    }

                    if (m_Buckets[x, y] != null)
                    {
                        foreach (int idx in m_Buckets[x, y])
                        {
                            if (check && (int)(PixelDistance(itemIndex, idx)+.5) > searchDistance) continue;

                            if (idx == itemIndex) continue;

                            Flags[idx] = 1;
                        }
                    }
                }
            }

           ArrayList targets = new ArrayList();

            for (int x = 0; x < Flags.Length; ++x)
            {
                if (Flags[x] != 0 && x != itemIndex)
                {
                    targets.Add(x);
                }
            }

			Array arry = targets.ToArray(typeof(int));
            return (int[])arry;
        }

        public int[] GetItemsWithinDistInDirection(int itemIndex, int searchDistance, SearchDirections direction)
        {
            int[] allwithin = GetItemsWithinDist(itemIndex, searchDistance);

            if (direction == SearchDirections.All) return allwithin;

            ArrayList targets = new ArrayList();

            Rectangle item = ((ItemBounds)m_Items[itemIndex]).Pixels;
            foreach (int idx in allwithin)
            {
                int relation = (int)GetSpacialRelationFlags(item, ((ItemBounds)m_Items[idx]).Pixels);

                if ((int)direction == relation ||            // exact match between criteria and relation.
                    relation == 0 ||                         // Overlap
                    relation  == ((int)direction & 0x03) ||  // if search direction allows left/right & the relation is exactly that.
                    relation  == ((int)direction & 0x0c))    // if search direction allows above/below & the relation is exactly that.
                {
                    targets.Add(idx);
                }
            }

			Array arry = targets.ToArray(typeof(int));
            return (int[])arry;
        }

        public int NearestNeighbor(int itemIndex)
        {
            int ret = -1;

            if (m_Items.Count > 1)
            {
                if (m_NearestNeigborCache[itemIndex] > -1) return m_NearestNeigborCache[itemIndex];

                if (m_Items.Count > 2)
                {
                    int[] Flags = new int[m_Items.Count];
                    bool found = false;
                    Rectangle item = ((ItemBounds)m_Items[itemIndex]).Pixels;
                    int startx = item.Left / m_BucketSize.Width;
                    int starty = item.Top / m_BucketSize.Height;
                    int endx = (item.Right - 1) / m_BucketSize.Width;
                    int endy = (item.Bottom - 1) / m_BucketSize.Height;
                    int lastx = (m_Bounds.Right - 1) / m_BucketSize.Width;
                    int lasty = (m_Bounds.Bottom - 1) / m_BucketSize.Height;

                    while (found == false)
                    {
                        for (int x = startx; x <= endx; ++x)
                        {
                            if (m_Buckets[x, starty] != null)
                            {
                                foreach (int idx in m_Buckets[x, starty])
                                {
                                    if (idx != itemIndex)
                                    {
                                        Flags[idx] = 1;
                                        found = true;
                                    }
                                }
                            }

                            if (m_Buckets[x, endy] != null)
                            {
                                foreach (int idx in m_Buckets[x, endy])
                                {
                                    if (idx != itemIndex)
                                    {
                                        Flags[idx] = 1;
                                        found = true;
                                    }
                                }
                            }
                        }

                        for (int y = starty + 1; y < endy; ++y)
                        {
                            if (m_Buckets[startx, y] != null)
                            {
                                foreach (int idx in m_Buckets[startx, y])
                                {
                                    if (idx != itemIndex)
                                    {
                                        Flags[idx] = 1;
                                        found = true;
                                    }
                                }
                            }

                            if (m_Buckets[endx, y] != null)
                            {
                                foreach (int idx in m_Buckets[endx, y])
                                {
                                    if (idx != itemIndex)
                                    {
                                        Flags[idx] = 1;
                                        found = true;
                                    }
                                }
                            }
                        }

                        if(startx > 0) startx--;
                        if(endx < lastx) endx++;
                        if(starty > 0) starty--;
                        if(endy < lasty) endy++;
                    }

                    // Now find the closest of all the items in the list.
                    float dist = float.MaxValue;

                    for (int i = 0; i < Flags.Length; ++i)
                    {
                        if (Flags[i] == 1)
                        {
                            float d = PixelDistance(itemIndex, i);

                            if (d < dist)
                            {
                                ret = i;
                                dist = d;
                            }
                        }
                    }
                }
                else
                {
                    ret = 1 - itemIndex;
                }
            }

            m_NearestNeigborCache[itemIndex] = ret;
            return ret;
        }
    }
}
