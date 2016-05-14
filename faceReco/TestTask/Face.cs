using System;
using System.Collections.Generic;
using System.Text;

namespace TestTask
{
    public struct NeighbourCount
    {
        public int id;
        public int count;
        public double distance;
    }

    public class NeighbourCountSorter : System.Collections.IComparer
    {
        virtual public int Compare(Object a, Object b)
        {
            NeighbourCount a1 = (NeighbourCount)a;
            NeighbourCount b1 = (NeighbourCount)b;

            int countDiff = b1.count - a1.count;
            if (0 == countDiff)
            {
                // Sort on Distance
                double distDiff = a1.distance - b1.distance;
                if (distDiff == 0.0)
                {
                    countDiff = 0;
                }
                else if (distDiff > 0)
                {
                    countDiff = 1;
                }
                else
                {
                    countDiff = -1;
                }
            }

            return countDiff;
        }
    }
    public class Face
    {
        int _id;
        FaceCollection  _parent;
        LiveLabs.TrainDataSample    _data;
        DistanceMeasure[] _distances;

        public Face(FaceCollection parent, LiveLabs.TrainDataSample data, int id)
        {
            _parent = parent;
            _data = data;
            _id = id;
        }

        public DistanceMeasure [] Distances
        {
            get
            {
                return _distances;
            }
            set
            {
                _distances = value;
            }
        }

        public int GroupId
        {
            get
            {
                return _parent.ID;
            }
        }

        public int ID
        {
            get
            {
                return _id;
            }
        }

        public void Sort()
        {
            DistanceMeasureSort sorter = new DistanceMeasureSort();

            Array.Sort(_distances, sorter);
        }

        public double[] Data
        {
            get
            {
                return _data.Inputs;
            }
        }

        public void DoDistances(List<Face> FaceList, TestCollection.DistanceComputeDelegate DistanceComputer)
        {

            _distances = new DistanceMeasure[FaceList.Count];

            for (int otherFaceCount = 0; otherFaceCount < FaceList.Count; ++otherFaceCount)
            {
                Face otherFace = FaceList[otherFaceCount];
                _distances[otherFaceCount].Distance = DistanceComputer(this.Data, otherFace.Data);
                _distances[otherFaceCount].face = otherFace;
            }

        }

        public void Dispose()
        {
            _distances = null;
        }
        public Dictionary<int, NeighbourCount> ClosestNeighbours(int count)
        {
            count = Math.Min(count, _distances.Length);

            Dictionary<int, NeighbourCount> accum = new Dictionary<int, NeighbourCount>();

            int i = 0;
            for( ; i < count ; ++i)
            {
                int id = _distances[i].face.GroupId;

                if (accum.ContainsKey(id))
                {
                    NeighbourCount n = accum[id];
                    n.count++;
                    n.distance += _distances[i].Distance;
                    accum[id] = n;
                }
                else
                {
                    NeighbourCount n = new NeighbourCount();
                    n.count = 1;
                    n.distance = _distances[i].Distance;
                    accum.Add(id, n);
                }
            }

            return accum;
        }

        public NeighbourCount[] CountNeighbours(Dictionary<int, NeighbourCount> accum)
        {
            NeighbourCount[] ret = new NeighbourCount[accum.Count];
            int i = 0;
            foreach (KeyValuePair<int, NeighbourCount> keyVal in accum)
            {
                ret[i].count = keyVal.Value.count;
                ret[i].id = keyVal.Key;
                ret[i].distance = keyVal.Value.distance;
                ++i;
            }

            NeighbourCountSorter neighSorter = new NeighbourCountSorter();
            Array.Sort(ret, neighSorter);
            return ret;
        }
    }
}
