using System;
using System.Collections.Generic;
using System.Text;
using LiveLabs;

namespace TestTask
{
    public class FaceCollection
    {
        private int _id;
        List<Face> _faces;


        public FaceCollection(int id)
        {
            _id = id;
            _faces = new List<Face>();
        }

        public Face Add(TrainDataSample sample, int groupId, int faceId)
        {
            Face face;
            if (groupId == ID)
            {
                face = new Face(this, sample, faceId);
                _faces.Add(face);
            }
            else
            {
                throw new Exception("FaceCollection attempt to add id " + groupId.ToString() + " to collection of id " + ID.ToString());
            }

            return face;
        }

        public int ID
        {
            get
            {
                return _id;
            }
        }
    }
}
