using System;
using System.Collections.Generic;
using System.Text;
using LiveLabs;

namespace TestTask
{
    public class FacePairSorter : System.Collections.IComparer
    {
        private Face _refFace;
        private TestCollection.DistanceComputeDelegate _neuralNetComparer;
        private double[] _netdata;

        public FacePairSorter(TestCollection.DistanceComputeDelegate neuralNetComparer)
        {
            _neuralNetComparer = neuralNetComparer;
        }

        public Face RefFace
        {
            get
            {
                return _refFace;
            }

            set
            {
                _refFace = value;
            }
        }

        virtual public int Compare(Object a, Object b)
        {
            Face a1 = (Face)a;
            Face b1 = (Face)b;

            if (a1.Equals(b1))
            {
                return 0;
            }

            int combinedLen = _refFace.Data.Length + a1.Data.Length + b1.Data.Length + 3;

            if (null == _netdata || combinedLen != _netdata.Length)
            {
                _netdata = new double[combinedLen];
            }
            int copyCount = 0;
            Array.Copy(_refFace.Data, _netdata, _refFace.Data.Length);
            copyCount += _refFace.Data.Length;
            
            Array.Copy(a1.Data, 0, _netdata, copyCount, a1.Data.Length);
            copyCount += a1.Data.Length;

            Array.Copy(b1.Data, 0, _netdata, copyCount, b1.Data.Length);
            copyCount += a1.Data.Length;

            _netdata[copyCount++] = Math.Max(_refFace.Distances[a1.ID].Distance, 1.0);
            _netdata[copyCount++] = _refFace.Distances[b1.ID].Distance;
            _netdata[copyCount] = (_netdata[copyCount - 2] - _netdata[copyCount - 1]) / _netdata[copyCount - 2];
            _netdata[copyCount] = Math.Max(-3.0, _netdata[copyCount]);
            _netdata[copyCount] = Math.Min(3.0, _netdata[copyCount]) * 10000;
            ++copyCount;

            double diff = _neuralNetComparer(_netdata, null);


            int countDiff = 1;
            if (diff >= 0.5)
            {
                countDiff = -1;
            }
            return countDiff;
        }
    }

    public class TestCollection
    {
        public delegate double DistanceComputeDelegate(double [] data1, double [] data2); 

        private Dictionary<int, FaceCollection> _faceCollections;
        private List<Face> _allFaces;

        public DistanceComputeDelegate DistanceComputer = null;

        public TestCollection(string filename)
        {
            TrainDataFileReader reader = new TrainDataFileReader(filename, false);

            _faceCollections = new Dictionary<int,FaceCollection>();
            _allFaces = new List<Face>();


            int faceId = 0;
            foreach (TrainDataSample sample in reader)
            {
                int id = (int)sample.Targets[0];
                FaceCollection collection = null;

                if (_faceCollections.ContainsKey(id))
                {
                    collection = _faceCollections[id];
                }
                else
                {
                    collection = new FaceCollection(id);
                    _faceCollections.Add(id, collection);
                }

                Face face = collection.Add(sample, id, faceId);
                _allFaces.Add(face);
                ++faceId;
            }

        }

        public List<Face> Faces
        {
            get
            {
                return _allFaces;
            }
        }

        public void DoDistances(TestCollection otherCollection)
        {
            for (int myFaceCount = 0 ; myFaceCount < Faces.Count ; ++myFaceCount)
            {
                Face myFace = Faces[myFaceCount];

                myFace.DoDistances(otherCollection.Faces, DistanceComputer);
                //DistanceMeasure[] distances = new DistanceMeasure[otherCollection.Faces.Count];

                //for (int otherFaceCount = 0; otherFaceCount < otherCollection.Faces.Count; ++otherFaceCount)
                //{
                //    Face otherFace = otherCollection.Faces[otherFaceCount];
                //    distances[otherFaceCount].Distance = ComputeDistance(myFace, otherFace);
                //    distances[otherFaceCount].face = otherFace;
                //}

                //myFace.Distances = distances;
            }
        }

        private double ComputeDistance(Face face1, Face face2)
        {
            double distance = DistanceComputer(face1.Data, face2.Data);

            return distance;
        }

        public void Sort()
        {
            foreach (Face face in _allFaces)
            {
                face.Sort();
            }
        }
    }
}
