//------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='Microsoft Corporation'>
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Information Contained Herein is Proprietary and Confidential.
// </copyright>
//
//------------------------------------------------------------------------------
using System;
using System.Diagnostics;   // For Debug class.

namespace System.Windows.Ink.Analysis.MathLibrary
{
    using Real = System.Single;

    /// <summary>
    /// Common functions for Math Library. Contains only static functions.
    /// </summary>
    internal class Utility
    {

        /// <summary>
        /// Make constructor private to avoid being instantiated.
        /// </summary>
        private Utility()
        {
            // Nothing here.
        }

        /// <summary>
        /// Epsilon to compare double value.
        /// </summary>
        public static double Epsilon = 1e-6;
        public static float FloatEpsilon = 1e-6f;
        public static Real RealEpsilon = FloatEpsilon;

        public static bool IsWithinEpsilon(double value1, double value2)
        {
            return Math.Abs(value1 - value2) < Epsilon;
        }

#if INTERNAL_PARSER
        /// <remarks>To be revisited. Problematic when Real = System.Double.</remarks>
        public static bool IsWithinEpsilon(Real value1, Real value2)
        {
            return Math.Abs(value1 - value2) < RealEpsilon;
        }

        /// <summary>
        /// return the ratio of Max / Min of the two numbers.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>The ratio</returns>
        /// <remarks>Potential problem when the value have different signs.</remarks>
        public static double Ratio(double value1, double value2)
        {
            return Math.Max(value1, value2) / Math.Min(value1, value2);
        }
#endif // INTERNAL_PARSER

        /// <summary>
        /// Swap the values of two doubles.
        /// </summary>
        public static void SwapDouble( ref double x, ref double y )
        {
            double temp = x;
            x = y;
            y = temp;
        }

        #region Hash Code Generator using Fnv1HashCode
        private const uint offsetBasis = 2166136261;
        private const uint fnvPrime = 16777619;

        public static int Fnv1HashCode(byte[] bytes)
        {
            unchecked
            {
                uint hash = offsetBasis;

                for (int i = 0; i < bytes.Length; ++i)
                {
                    hash *= fnvPrime;
                    hash ^= bytes[i];
                }

                return (int)hash;
            }
        }

#if INTERNAL_PARSER
        public static int Fnv1HashCode(int[] ints)
        {
            Debug.Assert(ints.Length > 0, "Should have more than 0 entry");
            byte[] bytes = new byte[ints.Length * 4];
            int byteIndex = 0;
            for (int i = 0; i < ints.Length; ++i)
            {
                int currentInt = ints[i];
                bytes[byteIndex++] = (byte) (currentInt & 0xff);
                bytes[byteIndex++] = (byte) ((currentInt >> 8) & 0xff);
                bytes[byteIndex++] = (byte) ((currentInt >> 16) & 0xff);
                bytes[byteIndex++] = (byte) ((currentInt >> 24) & 0xff);
            }
            return Fnv1HashCode(bytes);
        }
#endif // INTERNAL_PARSER
        #endregion // Hash Code 

	}
}
