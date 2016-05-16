//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // Debug functionalities.

namespace System.Windows.Ink.Analysis.MathLibrary
{
    /// <summary>
    /// Class for representing a matrix which has RowDimension rows by ColumnDimension columns.
    /// </summary>
    internal class MatrixExt
    {
        #region Fields
        private Vector[] vectors;
        #endregion // Fields

        #region Properties
        /// <summary>
        /// The number of rows in the MatrixExt.
        /// </summary>
#if INTERNAL_PARSER
        public int RowDimension
        {
            get
            {
                return this.vectors.Length;
            }
        }

        /// <summary>
        /// The number of columns in the MatrixExt.
        /// </summary>
        public int ColumnDimension
        {
            get
            {
                if (this.vectors.Length == 0)
                {
                    return 0;
                }
                else
                {
                    return this.vectors[0].Dimension;
                }
            }
        }
        
#endif // INTERNAL_PARSER

        /// <summary>
        /// Indexer. Gets or sets the row vectors in the MatrixExt.
        /// </summary>
        public Vector this[int index]
        {
            get
            {
                return this.vectors[index];
            }

#if INTERNAL_PARSER
            set
            {
                Debug.Assert(value.Dimension == this.ColumnDimension, "The dimension of vector isn't equal to the original");
                Array.Copy(value.GetElements(), this.vectors[index].GetElements(), value.Dimension);
            }
#endif
        }

#if INTERNAL_PARSER
        /// <summary>
        /// The sqrt of the summation of the sqr of every row's length.
        /// For the explanation of row's length, see Vector.length.
        /// </summary>
        public double Length
        {
            get
            {
                double length = 0;
                foreach(Vector vector in this.vectors){
                    double vectorLength = vector.Length;
                    length += vectorLength * vectorLength;
                }
                return Math.Sqrt(length);
            }
        }

        /// <summary>
        /// Tests whether this matrix has 0 elements.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return this.RowDimension == 0;
            }
        }


        /// <summary>
        /// Tests whether this matrix's number of rows and columns is the same.
        /// </summary>
        public bool IsSquare
        {
            get
            {
                return this.RowDimension == this.ColumnDimension;
            }
        }

        /// <summary>
        /// Tests whether this matrix has only 1 row.
        /// </summary>
        public bool IsRowVector
        {
            get
            {
                return this.RowDimension == 1;
            }
        }

        /// <summary>
        /// Tests whether this matrix has only 1 column.
        /// </summary>
        public bool IsColumnVector
        {
            get
            {
                return this.ColumnDimension == 1;
            }
        }

        /// <summary>
        /// Tests whether every item of this matrix is zero.
        /// </summary>
        public bool IsZero
        {
            get
            {
                foreach(Vector v in this.vectors)
                {
                    if (!v.IsZero)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the transpose of this matrix.
        /// </summary>
        /// <returns>The transpose matrix.</returns>
        public MatrixExt Transpose
        {
            get
            {
                MatrixExt m = new MatrixExt(this.ColumnDimension, this.RowDimension);

                for (int rowIndex = 0; rowIndex < this.RowDimension; ++rowIndex)
                {
                    for (int columnIndex = 0; columnIndex < this.ColumnDimension; ++columnIndex)
                    {
                        m[columnIndex][rowIndex] = this[rowIndex][columnIndex];
                    }
                }

                return m;
            }
        }
#endif // INTERNAL_PARSER

        #endregion // Properties
        
        #region Methods
        public MatrixExt(int row, int col)
        {
            Debug.Assert(row >= 0 && col >= 0, "The row or col are invalid");
            this.vectors = new Vector[row];
            for (int rowIndex = 0; rowIndex < row; ++rowIndex)
            {
                this.vectors[rowIndex] = new Vector(col);
            }
        }

/*        public MatrixExt(MatrixExt matrix)
        {
            this.vectors = new Vector[matrix.RowDimension];
            for (int rowIndex = 0; rowIndex < matrix.RowDimension; ++rowIndex)
            {
                this.vectors[rowIndex] = new Vector(matrix.ColumnDimension);
            }
            
            for (int rowIndex = 0; rowIndex < this.RowDimension; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < this.ColumnDimension; ++columnIndex)
                {
                    this[rowIndex][columnIndex] = matrix[rowIndex][columnIndex];
                }
            }
        }*/

#if INTERNAL_PARSER
        /// <summary>
        /// Gets the nth column vector specified by index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The returned vector</returns>
        public Vector GetColumnVector(int index)
        {
            Vector v = new Vector(this.RowDimension);

            for (int rowIndex = 0; rowIndex < this.RowDimension; ++rowIndex)
            {
                v[rowIndex] = this[rowIndex][index];
            }
            return v;
        }

        /// <summary>
        /// Tests whether this matrix has the same number of rows and columns with another matrix.
        /// </summary>
        /// <param name="m">The another matrix to test</param>
        /// <returns>True if they are the same.</returns>
        private bool IsSameDimension(MatrixExt m)
        {
            return this.RowDimension == m.RowDimension && this.ColumnDimension == m.ColumnDimension;
        }

        /// <summary>
        /// Adds two MatrixExts then gives the result to a new MatrixExt.
        /// The two MatrixExts must have the same RowDimension and ColumnDimension.
        /// </summary>
        /// <param name="m1">The first MatrixExt.</param>
        /// <param name="m2">The second MatrixExt.</param>
        /// <returns>The MatrixExt representing the result.</returns>
        public static MatrixExt Add(MatrixExt m1, MatrixExt m2)
        {
            Debug.Assert(m1.IsSameDimension(m2), "The dimensions of m1 and m2 are not equal");
            MatrixExt m = new MatrixExt(m1.RowDimension, m1.ColumnDimension);

            for (int rowIndex = 0; rowIndex < m1.RowDimension; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < m1.ColumnDimension; ++columnIndex)
                {
                    m[rowIndex][columnIndex] = m1[rowIndex][columnIndex] + m2[rowIndex][columnIndex];
                }
            }

            return m;
        }

        /// <summary>
        /// Subtracts two MatrixExts then gives the result to a new MatrixExt.
        /// The two MatrixExts must have the same RowDimension and ColumnDimension.
        /// </summary>
        /// <param name="m1">The first MatrixExt.</param>
        /// <param name="m2">The second MatrixExt.</param>
        /// <returns>The MatrixExt representing the result.</returns>
        public static MatrixExt Subtract(MatrixExt m1, MatrixExt m2)
        {
            Debug.Assert(m1.IsSameDimension(m2), "The dimensions of m1 and m2 are not equal");
            MatrixExt m = new MatrixExt(m1.RowDimension, m1.ColumnDimension);

            for (int rowIndex = 0; rowIndex < m1.RowDimension; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < m1.ColumnDimension; ++columnIndex)
                {
                    m[rowIndex][columnIndex] = m1[rowIndex][columnIndex] - m2[rowIndex][columnIndex];
                }
            }

            return m;
        }

        /// <summary>
        /// Multiplies one MatrixExt by a value then gives the result to a new MatrixExt.
        /// </summary>
        /// <param name="m1">The MatrixExt.</param>
        /// <param name="alpha">The value.</param>
        /// <returns>The MatrixExt representing the result.</returns>
        public static MatrixExt Multiply(MatrixExt m1, double alpha)
        {
            MatrixExt m = new MatrixExt(m1.RowDimension, m1.ColumnDimension);

            for (int rowIndex = 0; rowIndex < m1.RowDimension; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < m1.ColumnDimension; ++columnIndex)
                {
                    m[rowIndex][columnIndex] = m1[rowIndex][columnIndex] * alpha;
                }
            }

            return m;
        }

        /// <summary>
        /// Divides one MatrixExt by a value then gives the result to a new MatrixExt.
        /// </summary>
        /// <param name="m1">The MatrixExt.</param>
        /// <param name="alpha">The value.</param>
        /// <returns>The MatrixExt representing the result.</returns>
        public static MatrixExt Divide(MatrixExt m1, double alpha)
        {
            MatrixExt m = new MatrixExt(m1.RowDimension, m1.ColumnDimension);

            for (int rowIndex = 0; rowIndex < m1.RowDimension; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < m1.ColumnDimension; ++columnIndex)
                {
                    m[rowIndex][columnIndex] = m1[rowIndex][columnIndex] / alpha;
                }
            }

            return m;
        }

        /// <summary>
        /// Returns the minus of one MatrixExt.
        /// </summary>
        /// <param name="m1">The MatrixExt.</param>
        /// <returns>The MatrixExt representing the result.</returns>
        public static MatrixExt Minus(MatrixExt m1)
        {
            MatrixExt m = new MatrixExt(m1.RowDimension, m1.ColumnDimension);

            for (int rowIndex = 0; rowIndex < m1.RowDimension; ++rowIndex)
            {
                for (int columnIndex = 0; columnIndex < m1.ColumnDimension; ++columnIndex)
                {
                    m[rowIndex][columnIndex] = -m1[rowIndex][columnIndex];
                }
            }

            return m;
        }
#endif // INTERNAL_PARSER
        #endregion // Methods
    }
}
