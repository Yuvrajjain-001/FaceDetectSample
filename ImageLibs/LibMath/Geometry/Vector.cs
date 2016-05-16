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
    /// Class for representing a vector which has a list of values.
    /// </summary>
    public class Vector
    {
        #region Fields
        private double[] elements;
        #endregion // Fields

        #region Properties
        /// <summary>
        /// The number of elements in the Vector.
        /// </summary>
        public int Dimension
        {
            get
            {
                return this.elements.Length;
            }
        }

        /// <summary>
        /// Indexer. Gets or sets the elements in the Vector.
        /// </summary>
        public double this[int index]
        {
            get
            {
                return this.elements[index];
            }
            set
            {
                this.elements[index] = value;
            }
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Tests whether every item of this vector is zero.
        /// </summary>
        public bool IsZero
        {
            get
            {
                foreach(double element in this.elements)
                {
                    if (element != 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }


        /// <summary>
        /// The sqrt of the summation of the sqr of every element.
        /// </summary>
        public double Length
        {
            get
            {
                double length=0;
                for(int i = 0;  i < this.Dimension; i ++)
                {
                    length += this.elements[i] * this.elements[i];
                }
                return Math.Sqrt(length);
            }
        }
#endif
        #endregion // Properties

        #region Methods
        public Vector(int dimension)
        {
            Debug.Assert(dimension >= 0, "The dimension is invalid");
            this.elements = new double[dimension];
        }

        public Vector(Vector vector)
        {
            this.elements = new double[vector.Dimension];
            for (int i = 0; i < this.elements.Length; i ++)
            {
                this[i] = vector[i];
            }
        }

#if INTERNAL_PARSER
        /// <summary>
        /// Return the array of the elements in the Vector.
        /// </summary>
        public double[] GetElements()
        {
            return elements;
        }

        
        /// <summary>
        /// Adds two Vectors then gives the result to a new Vector.
        /// The two Vectors must have the same Dimension.
        /// </summary>
        /// <param name="v1">The first Vector.</param>
        /// <param name="v2">The second Vector.</param>
        /// <returns>The Vector representing the result.</returns>
        public static Vector Add(Vector v1, Vector v2)
        {
            Debug.Assert(v1.Dimension == v2.Dimension, "The dimensions of two vectors are not eqaul");

            Vector v = new Vector(v1.Dimension);

            for (int i = 0; i < v1.Dimension; i ++)
            {
                v[i] = v1[i] + v2[i];
            }
            return v;
        }


        /// <summary>
        /// Subtracts two Vectors then gives the result to a new Vector.
        /// The two Vectors must have the same Dimension.
        /// </summary>
        /// <param name="v1">The first Vector.</param>
        /// <param name="v2">The second Vector.</param>
        /// <returns>The Vector representing the result.</returns>
        public static Vector Subtract(Vector v1, Vector v2)
        {
            Debug.Assert(v1.Dimension == v2.Dimension, "The dimensions of two vectors are not eqaul");

            Vector v = new Vector(v1.Dimension);

            for (int i = 0; i < v1.Dimension; i ++)
            {
                v[i] = v1[i] - v2[i];
            }
            return v;
        }
        /// <summary>
        /// Multiplies one Vector by a value then gives the result to a new Vector.
        /// </summary>
        /// <param name="v1">The Vector.</param>
        /// <param name="alpha">The value.</param>
        /// <returns>The Vector representing the result.</returns>
        public static Vector Multiply(Vector v1, double alpha)
        {
            Vector v = new Vector(v1.Dimension);

            for (int i = 0; i < v1.Dimension; i ++)
            {
                v[i] = v1[i] * alpha;
            }
            return v;
        }

        
        /// <summary>
        /// Divides one Vector by a value then gives the result to a new Vector.
        /// </summary>
        /// <param name="v1">The Vector.</param>
        /// <param name="alpha">The value.</param>
        /// <returns>The Vector representing the result.</returns>
        public static Vector Divide(Vector v1, double alpha)
        {
            Vector v = new Vector(v1.Dimension);

            for (int i = 0; i < v1.Dimension; i ++)
            {
                v[i] = v1[i] / alpha;
            }
            return v;
        }

          
        /// <summary>
        /// Returns the minus of one Vector.
        /// </summary>
        /// <param name="v1">The Vector.</param>
        /// <returns>The Vector representing the result.</returns>
        public static Vector Minus(Vector v1)
        {
            Vector v = new Vector(v1.Dimension);

            for (int i = 0; i < v1.Dimension; i ++)
            {
                v[i] = - v1[i];
            }
            return v;
        }
 
        /// <summary>
        /// Sets all of the elements in the Vector as zero.
        /// </summary>
        public void SetZero()
        {
            for(int i = 0; i < this.Dimension; i ++)
            {
                this.elements[i] = 0;
            }
        }
#endif
        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="v1">The first Vector.</param>
        /// <param name="v2">The second Vector.</param>
        /// <returns>The result.</returns>
        public static double operator*(Vector v1, Vector v2)
        {
            Debug.Assert(v1.Dimension == v2.Dimension, "The dimensions of two vectors are not eqaul");

            double result = 0;

            for (int i = 0; i < v1.Dimension; i ++)
            {
                result += v1[i] * v2[i];
            }
            return result;
        }
        #endregion // Methods
    };
}
