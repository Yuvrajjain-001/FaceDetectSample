using System;
using System.Diagnostics;
using System.Collections;

namespace Dpu.Utility
{
    /// <summary>
    /// Generic interface for ConfusionMatrix and CompositeMatrix
    /// </summary>
    public interface IMatrix
    {
        string[] Labels { get; }
        ICollection this[int trueLabel, int predLabel] { get; }
        bool HasItems { get; }
        void FreeMemory();
        int Count(int labelTruth, int labelPred);
    }

    /// <summary>
    /// A class for representing confusion in a classifier.
    /// It consists of a set of N class labels "Labels" and 
    /// an NxN matrix "Matrix", each element of which is an 
    /// array of example objects.  All of the examples in the 
    /// position (i, j) are examples for which the classifier
    /// predicted class Labels[i], but the truth was Labels[j].
    /// </summary>
    [Serializable]
    public class ConfusionMatrix : IMatrix
    {
        #region Constructors
        public ConfusionMatrix() {}

        public ConfusionMatrix(int labelCount)
        {
            string[] labels = new string[labelCount];
            for (int i = 0; i < labelCount; ++i)
            {
                labels[i] = i.ToString();
            }
            _labels = labels;
            _counts = new int[labels.Length, labels.Length]; 
        }
        public ConfusionMatrix(string[] labels) 
        { 
            _labels = labels;
            _matrix = new ArrayList[labels.Length, labels.Length]; 
            _counts = new int[labels.Length, labels.Length]; 
            for(int i = 0; i < labels.Length; i++)
            {
                for(int j = 0; j < labels.Length; j++)
                {
                    _matrix[i, j] = new ArrayList();
                }
            }
        }
        #endregion

        #region Properties
        public static string[] BinaryLabels = new string[] { "False", "True" };
        public string[] Labels { get { return _labels; } }
        #endregion

        #region Fields
        /// <summary>
        /// 
        /// </summary>
        private string[] _labels;

        /// <summary>
        /// 
        /// </summary>
        private ArrayList[,] _matrix;

        /// <summary>
        /// Store the counts in here for serialization if we
        /// don't want to serialize the objects.
        /// </summary>
        private int[,] _counts;
        #endregion

        #region Properties
        /// <summary>
        /// Get the items at the given index.  Only call this
        /// if the HasItems property is true.
        /// </summary>
        public ICollection this[int trueLabel, int predLabel]
        {
            get { return _matrix[trueLabel, predLabel]; }
        }

        /// <summary>
        /// Whether or not this matrix has items.  If not, it
        /// only has counts.
        /// </summary>
        public bool HasItems { get { return _matrix != null; } }
        #endregion

        #region Statistics
        /// <summary>
        /// Compute F1 accuracy measure on the matrix
        /// (geometric avg of precision & recall)
        /// 
        ///    F1 = 2*P*R / (P+R)
        /// </summary>
        public static float F1(ConfusionMatrix mat)
        {
            float precision = Precision(mat);
            float recall = Recall(mat);
            float f1 = 2 * precision*recall / (precision + recall);
            return (Single.IsNaN(f1) ? 0 : f1);
        }

        /// <summary>
        /// Compute Precision accuracy measure on the matrix
        /// 
        ///    P = #correctPos / #predPos
        /// </summary>
        public static float Precision(ConfusionMatrix mat)
        {
            if(mat.Labels.Length != 2) throw new ArgumentException("Recall for 2-class problems");

            float numCorrectPosLabels = mat.Count(1,1);
            float numPredPosLabels = mat.Count(0,1) + numCorrectPosLabels;
            // VIOLA:  if there are no predicted positives and no actual positives
            if (numCorrectPosLabels == 0 && numPredPosLabels == 0)
                return 1;
            float precision = numCorrectPosLabels / numPredPosLabels;
            return Single.IsNaN(precision) ? 0 : precision;
        }

        /// <summary>
        /// Compute Recall measure on the matrix
        /// 
        ///    P = #correctPos / #truePos
        /// </summary>
        public static float Recall(ConfusionMatrix mat)
        {
            if(mat.Labels.Length != 2) throw new ArgumentException("Recall for 2-class problems");
            float numCorrectPosLabels = mat.Count(1,1);
            float numTruePosLabels = mat.Count(1,0) + numCorrectPosLabels;
            if (numTruePosLabels == 0 && numCorrectPosLabels == 0)
                return 1;
            float recall = (float)numCorrectPosLabels / numTruePosLabels;
            return Single.IsNaN(recall) ? 1 : recall;
        }

        /// <summary>
        /// Compute Error Rate on the matrix
        ///   Error Rate = #wrongLabels / (#correctLabels + #wrongLabels)
        /// </summary>
        public double ErrorRate()
        {
            int correctLabels = 0;
            int wrongLabels = 0;
            for (int i = 0; i < _counts.GetLength(0); ++i)
            {
                for (int j = 0; j < _counts.GetLength(1); ++j)
                {
                    if (i == j) correctLabels += _counts[i,i];
                    else wrongLabels += _counts[i,j];
                }
            }
            return (double)wrongLabels / (double)(correctLabels + wrongLabels);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Free the contents of the matrix so that only the
        /// counts remain
        /// </summary>
        public void FreeMemory()
        {
            /* Assume _counts are already filled in
            int len = Labels.Length;
            _counts = new int[len, len];
            for(int i = 0; i < len; i++)
            {
                for(int j = 0; j < len; j++)
                {
                    _counts[i,j] = _matrix[i,j].Count;
                }
            }
            */
            _matrix = null;
        }

        /// <summary>
        /// Count the number of items at the given truth/pred index
        /// This is preferable to Items[truth, pred] because Items
        /// might not always exist
        /// </summary>
        public int Count(int truthLbl, int predLbl)
        {
            return (_matrix == null) ? _counts[truthLbl, predLbl] : _matrix[truthLbl,predLbl].Count;
        }
        /// <summary>
        /// Return the number of true examples for a given label
        /// </summary>
        public int TrueCount(int label)
        {
            int cnt = 0;
            for(int i = 0; i < Labels.Length; i++)
            {
                cnt += Count(label, i);
            }
            return cnt;
        }

        /// <summary>
        /// Add the contents of src to this matrix.  Src
        /// must have the same # of labels as this.
        /// </summary>
        public void Add(ConfusionMatrix src)
        {
            if(src.Labels.Length != Labels.Length) throw new ArgumentException();
            for(int i = 0; i < Labels.Length; i++)
            {
                for(int j = 0; j < Labels.Length; j++)
                {
                    _counts[i,j] += src._counts[i,j];
                    if(src._matrix != null)
                    {
                        _matrix[i,j].AddRange(src._matrix[i,j]);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Add(int labelTruth, int labelPred, object obj)
        {
            _matrix[labelTruth, labelPred].Add(obj);
            _counts[labelTruth, labelPred]++;
        }


        public void SetData(IList truth, IList predicted)
        {
            _counts.Initialize();
            Debug.Assert(truth.Count == predicted.Count);
            for (int i = 0; i < truth.Count; i++)
            {
                _counts[(int)truth[i], (int)predicted[i]]++;
            }
        }
            

        /// <summary>
        /// 
        /// </summary>
        public void SetData(IList truth, IList predicted, IList objects, string[] labels)
        {
            Debug.Assert(truth.Count == predicted.Count);

            _labels = labels;
            _matrix = new ArrayList[labels.Length,labels.Length];
            _counts = new int[labels.Length,labels.Length];

            for(int i = 0; i < labels.Length; i++)
            {
                for(int j = 0; j < labels.Length; j++)
                {
                    _matrix[i,j] = new ArrayList();
                }
            }
            for(int i = 0; i < truth.Count; i++)
            {
                _matrix[(int)truth[i], (int)predicted[i]].Add(objects[i]);
                _counts[(int)truth[i], (int)predicted[i]]++;
            }
        }

        public void WriteDebug()
        {
            int maxLen = 0;
            foreach(string lbl in Labels)
            {
                maxLen = Math.Max(maxLen, lbl.Length);
            }

            for(int trueLabel = 0; trueLabel < Labels.Length; trueLabel++)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                string header = String.Format("{0}:", Labels[trueLabel]);
                header = header.PadRight(1+maxLen, ' ');
                sb.Append(header);
                for(int predLabel = 0; predLabel < Labels.Length; predLabel++)
                {
                    sb.AppendFormat(" {0:D6}", Count(trueLabel, predLabel));
                }

                Log.WriteLine(sb.ToString());
            }
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int maxLen = 0;
            foreach(string lbl in Labels)
            {
                maxLen = Math.Max(maxLen, lbl.Length);
            }

            for(int trueLabel = 0; trueLabel < Labels.Length; trueLabel++)
            {
                string header = String.Format("{0,5}: ", Labels[trueLabel]);
                header.PadRight(12+maxLen, ' ');
                sb.Append(header);
                for(int predLabel = 0; predLabel < Labels.Length; predLabel++)
                {
                    sb.AppendFormat(" {0,6}", Count(trueLabel, predLabel));
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }
        #endregion
    }


    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class CompositeMatrix : IMatrix
    {
        #region Constructors
        public CompositeMatrix() { }
        public CompositeMatrix(string[] labels, int count)
        {
            Matrices = new ConfusionMatrix[count];
            for(int i = 0; i < count; i++)
            {
                Matrices[i] = new ConfusionMatrix(labels);
            }
        }
        public CompositeMatrix(ConfusionMatrix[] matrices) 
        {
            Matrices = matrices;
        }
        #endregion

        #region Properties
        public IMatrix[] Matrices;
        public string[] Labels { get { return Matrices[0].Labels; } }
        public bool HasItems 
        { 
            get 
            {
                foreach(ConfusionMatrix mat in Matrices)
                {
                    if(!mat.HasItems) return false;
                }
                return true;
            }
        }

        public ICollection this[int trueLabel, int predLabel] 
        {
            get
            {
                ICollection[] contents = new ICollection[Matrices.Length];
                for(int i = 0; i < Matrices.Length; i++)
                {
                    IMatrix matrix = Matrices[i];
                    contents[i] = matrix[trueLabel,predLabel]; 
                }
                return new CompositeCollection(contents);
            }
        }
        #endregion

        #region Methods
        public virtual void FreeMemory()
        {
            for(int i = 0; i < Matrices.Length; i++)
            {
                Matrices[i].FreeMemory();
            }
        }

        public int Count(int trueLabel, int predLabel)
        {
            int count = 0;
            for(int i = 0; i < Matrices.Length; i++)
            {
                count += Matrices[i].Count(trueLabel, predLabel);
            }
            return count;
        }
        #endregion
    }
}
