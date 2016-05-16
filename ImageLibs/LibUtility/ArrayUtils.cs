using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Dpu {
   namespace Utility {


      /// <summary>
      /// Convenient set of array utilities
      /// </summary>
      public sealed class ArrayUtils {

          private ArrayUtils() {}

          /// <summary>
          /// Convenience method for constructing arraylists
          /// </summary>
          public static ArrayList List(object o1)
          {
              ArrayList list = new ArrayList(1);
              list.Add(o1);
              return list;
          }

          /// <summary>
          /// Convenience method for constructing arraylists
          /// </summary>
          public static ArrayList List(object o1, object o2)
          {
              ArrayList list = new ArrayList(2);
              list.Add(o1);
              list.Add(o2);
              return list;
          }

        
          /// <summary>
          /// Convenience method for constructing arraylists
          /// </summary>
          public static ArrayList List(object o1, object o2, object o3)
          {
              ArrayList list = new ArrayList(3);
              list.Add(o1);
              list.Add(o2);
              list.Add(o3);
              return list;
          }

          /// <summary>
          /// Convenience method for constructing arraylists
          /// </summary>
          public static ArrayList List(params object[] contents)
          {
              return new ArrayList(contents);
          }

//#if INTERNAL_DPU
          /// <summary>
          /// Provides for initialization of an array of doubles with a single value.
          /// </summary>
          /// <param name="vector">The array to initialize.</param>
          /// <param name="initialValue">The initialization value.</param>
          /// <returns>A pointer to the input array.</returns>
          public static double[] VectorInit(double[] vector, double initialValue) 
          { 
              for(int n = 0; n < vector.Length; ++n) 
              {
                  vector[n] = initialValue;
              }
              return vector;
          }

          /// <summary>
          /// Provides for initialization of an array of int types with a single value.
          /// </summary>
          public static int[] VectorInit(int[] vector, int init) 
          { 
              for(int n = 0; n < vector.Length; ++n) 
              {
                  vector[n] = init;
              }
              return vector;
          }

          /// <summary>
          /// Provides for initialization of an array of floats with a single value.
          /// </summary>
          public static float[] VectorInit(float[] vector, float init) 
          { 
              for(int n = 0; n < vector.Length; ++n) 
              {
                  vector[n] = init;
              }
              return vector;
          }

          public static void VectorCopy<T>(T[] from, T[] to)
          {
              for (int i = 0; i < from.Length; ++i)
              {
                  to[i] = from[i];
              }
          }

          /// <summary>
          /// Provides for initialization of a 2 dimensional array of floats with a single value.
          /// </summary>
          public static float[,] MatrixInit(float[,] vector, float scoreInit) 
          { 
              int cs1 = vector.GetLength(0);
              int cs2 = vector.GetLength(1);
              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  for(int n2 = 0; n2 < cs2; ++n2) 
                  {
                      vector[n1,n2] = scoreInit;
                  }
              }
              return vector;
          }
//#endif // INTERNAL_DPU

          /// <summary>
          /// Provides for initialization of a 2 dimensional array of doubles with a single value.
          /// </summary>
          public static double[,] MatrixInit(double[,] vector, double scoreInit) 
          { 
              int cs1 = vector.GetLength(0);
              int cs2 = vector.GetLength(1);
              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  for(int n2 = 0; n2 < cs2; ++n2) 
                  {
                      vector[n1,n2] = scoreInit;
                  }
              }
              return vector;
          }

//#if INTERNAL_DPU
          /// <summary>
          /// Creates an array of size doubles and initializes them to initialValue.
          /// </summary>
          public static double[] MakeVectorDouble(int size, double initialValue) 
          { 
             double[] vector = new double[size];
             return VectorInit(vector, initialValue);
          }

          /// <summary>
          /// Creates an array of size doubles and initializes them to initialValue.
          /// </summary>
          public static int[] MakeVectorInt(int size, int initialValue) 
          { 
            int[] vector = new int[size];
            for(int n = 0; n < size; ++n) {
               vector[n] = initialValue;
            }
            return vector;
         }

          /// <summary>
          /// Creates an array of size doubles and initializes them to initialValue.
          /// </summary>
          public static float[] MakeVectorSingle(int size, float initialValue) 
          { 
              float[] vector = new float[size];
              for(int n = 0; n < size; ++n) 
              {
                  vector[n] = initialValue;
              }
              return vector;
          }

         /// <summary>
         /// Make a 2D array of doubles and intialized to scoreInit.
         /// </summary>
         public static double[,] MakeMatrixDouble (int size, double scoreInit) { 
            return MakeMatrixDouble(size, size, scoreInit);
         }

          /// <summary>
          /// Make a 2D array of doubles of size[csize1,csize2] and intialized to scoreInit.
          /// </summary>
          public static double[,] MakeMatrixDouble (int csize1, int csize2, double scoreInit) 
          { 
            double[,] vscore = new double[csize1, csize2];
            for(int n1 = 0; n1 < csize1; ++n1) {
               for(int n2 = 0; n2 < csize2; ++n2) {
                  vscore[n1,n2] = scoreInit;
               }
            }
            return vscore;
         }


         /// <summary>
         /// Make a 2D array of doubles and intialized to scoreInit.
         /// </summary>
         public static float[,] MakeMatrixFloat(int size, float scoreInit)
         {
             return MakeMatrixFloat(size, size, scoreInit);
         }

         /// <summary>
         /// Make a 2D array of doubles of size[csize1,csize2] and intialized to scoreInit.
         /// </summary>
         public static float[,] MakeMatrixFloat(int csize1, int csize2, float scoreInit)
         {
             float[,] vscore = new float[csize1, csize2];
             for (int n1 = 0; n1 < csize1; ++n1)
             {
                 for (int n2 = 0; n2 < csize2; ++n2)
                 {
                     vscore[n1, n2] = scoreInit;
                 }
             }
             return vscore;
         }

         /// <summary>
         /// Make a 2D array of ints and intialized to init.
         /// </summary>
         public static int[,] MakeMatrixInt(int size, int init)
         {
             return MakeMatrixInt(size, size, init);
         }

          /// <summary>
          /// Make a 2D array of ints of size[cs1,cs2] and intialized to init.
          /// </summary>
          public static int[,] MakeMatrixInt (int cs1, int cs2, int init) 
          { 
            int[,] vscore = new int[cs1, cs2];
            for(int n1 = 0; n1 < cs1; ++n1) {
               for(int n2 = 0; n2 < cs2; ++n2) {
                  vscore[n1,n2] = init;
               }
            }
            return vscore;
         }

         /// <summary>
         /// Calculate the natural log for every element in the 2D matrix input array.
         /// </summary>
         public static double[][] Log(double[][] matrix, double[][] result) {
            int cs1 = result.GetLength(0);
            int cs2 = result.GetLength(1);

            for(int n1 = 0; n1 < cs1; ++n1) {
               for(int n2 = 0; n2 < cs2; ++n2) {
                  result[n1][n2] = Math.Log(matrix[n1][n2]);
               }
            }
            return result;
         }

          /// <summary>
          /// Calculate the natural log for every element in the vector input array.
          /// </summary>
          public static double[] Log(double[] vector, double[] result) 
          {
            int cs1 = result.GetLength(0);

            for(int n1 = 0; n1 < cs1; ++n1) {
               result[n1] = Math.Log(vector[n1]);
            }
            return result;
         }

          public static double[] Exp(double[] vector, double[] result)
          {
              int cs1 = result.GetLength(0);
              for (int n1 = 0; n1 < cs1; ++n1)
              {
                  result[n1] = Math.Exp(vector[n1]);
              }
              return result;
          }

          public static double[] Exp(double[] vector)
          {
              int cs1 = vector.GetLength(0);
              double[] result = new double[cs1];
              for (int n1 = 0; n1 < cs1; ++n1)
              {
                  result[n1] = Math.Exp(vector[n1]);
              }
              return result;
          }

          /// <summary>
          /// Compute the value of e raised to each element of vector.  The result is returned directly and via result.
          /// </summary>
          public static float[] Exp(float[] vector, float[] result) 
          {
              int cs1 = result.GetLength(0);

              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  result[n1] = (float) Math.Exp(vector[n1]);
              }
              return result;
          }

          /// <summary>
          /// Add the elements of vec1 and vec2 and return the result in result and as a return value.
          /// </summary>
          public static double[] Add(double[] vec1, double[] vec2, double[] result) 
          {
              int cs1 = result.GetLength(0);

              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  result[n1] = vec1[n1] + vec2[n1];
              }
              return result;
          }

          /// <summary>
          /// Add the elements of vec1 and vec2 and return the result in result and as a return value.
          /// </summary>
          public static float[] Add(float[] vec1, float[] vec2, float[] result) 
          {
              int cs1 = result.GetLength(0);

              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  result[n1] = vec1[n1] + vec2[n1];
              }
              return result;
          }

          /// <summary>
          /// Add the elements of mat1 and mat2 and return the result in result and as a return value.
          /// </summary>
          public static int[,] Add(int[,] mat1, int[,] mat2, int[,] result)
          {
              int cs1 = result.GetLength(0);
              int cs2 = result.GetLength(1);

              for (int n1 = 0; n1 < cs1; ++n1)
              {
                  for (int n2 = 0; n2 < cs2; ++n2)
                  {
                      result[n1,n2] = mat1[n1,n2] + mat2[n1,n2];
                  }
              }
              return result;
          }

          /// <summary>
          /// Multiply the elements of matrix by scale and return the result in result and as a return value.
          /// </summary>
          public static double[,] Multiply(double[,] vector, double scale, double[,] result)
          {
              int cs1 = result.GetLength(0);
              int cs2 = result.GetLength(1);

              for (int n1 = 0; n1 < cs1; ++n1)
              {
                  for (int n2 = 0; n2 < cs2; ++n2)
                  {
                      result[n1,n2] = scale * vector[n1,n2];
                  }
              }
              return result;
          }

          /// <summary>
          /// Multiply the elements of vector by scale and return the result in result and as a return value.
          /// </summary>
          public static double[] Multiply(double[] vector, double scale, double[] result) 
          {
              int cs1 = result.GetLength(0);

              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  result[n1] = scale * vector[n1];
              }
              return result;
          }

          /// <summary>
          /// Multiply the elements of vector by scale and return the result in result and as a return value.
          /// </summary>
          public static float[] Multiply(float[] vector, float scale, float[] result) 
          {
              int cs1 = result.GetLength(0);

              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  result[n1] = scale * vector[n1];
              }
              return result;
          }

          /// <summary>
          /// Create a normalized array, result, based on the input values of vector.
          /// </summary>
          public static double[] Normalize(double[] vector, double[] result) 
          {
              int cs1 = result.GetLength(0);

              double sum = 0;

              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  sum += vector[n1];
              }
              return Multiply(vector, 1/sum, result);
          }

          /// <summary>
          /// Create a normalized array, result, based on the input values of vector.
          /// </summary>
          public static float[] Normalize(float[] vector, float[] result) 
          {
              int cs1 = result.GetLength(0);

              float sum = 0;

              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  sum += vector[n1];
              }
              return Multiply(vector, 1/sum, result);
          }

          /// <summary>
          /// Compute the sum of all elements in the input array vector.
          /// </summary>
          public static double VectorSum(double[] vector) 
          {
              int cs1 = vector.GetLength(0);
              double sum = 0;
              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  sum += vector[n1];
              }
              return sum;
          }

          /// <summary>
          /// Compute the sum of all elements in the input array vector.
          /// </summary>
          public static float VectorSum(float[] vector)
          {
              int cs1 = vector.GetLength(0);
              float sum = 0;
              for (int n1 = 0; n1 < cs1; ++n1)
              {
                  sum += vector[n1];
              }
              return sum;
          }

          /// <summary>
          /// Compute the sum of all elements in the input array vector.
          /// </summary>
          public static int VectorSum(int[] vector) 
          {
              int cs1 = vector.GetLength(0);

              int sum = 0;

              for(int n1 = 0; n1 < cs1; ++n1) 
              {
                  sum += vector[n1];
              }
              return sum;
          }


		  /// <summary>
		  /// Read binary float data from a file and stuff into an array
		  /// </summary>
		  /// <param name="file"></param>
		  /// <returns></returns>
		  public static unsafe float[,] ReadBinaryMatrix(string file, int cols)
		  {
			  System.IO.FileInfo fi = new System.IO.FileInfo(file);
			  System.IO.FileStream fs = System.IO.File.OpenRead(file);
			  int byteCount = (int)fi.Length;
			  int wordCount = byteCount / sizeof(float);
			  int rowCount = wordCount / cols;

			  byte[] data = new byte[byteCount];
			  float[,] res = new float[rowCount, cols];

			  // Painful because the data is column major Matlab.
			  fs.Read(data, 0, byteCount);

			  fixed (byte* pdata = data)
			  {
				  float* pdataFloat = (float*)pdata;  // Cast the data to floats... dangerous

				  fixed (float* presult = res)
				  {
					  float* presultOffset = presult;

					  for (int word = 0; word < wordCount; ++word)
					  {
						  *(presultOffset++) = *(pdataFloat++);
					  }
				  }
			  }
			  fs.Close();
			  return res;
		  }
          /// <summary>
          /// Cacluate the Chi Square distance between
          /// two histogram vectors
          /// </summary>
          /// <param name="hist1">the first histogram</param>
          /// <param name="hist2">the second histogram</param>
          /// <returns>the Chi Square distance</returns>
          public static float ChiSquare(float[] hist1, float[] hist2)
          {
              if (hist1.GetLength(0) != hist2.GetLength(0))
                  throw new ArgumentException("Array size does not match.");
              float chiSquare = 0;
              float sum;
              for (int i = 0; i < hist1.GetLength(0); i++)
              {
                  sum = hist1[i] + hist2[i];
                  if(sum!=0)
                      chiSquare += (hist1[i] - hist2[i]) * (hist1[i] - hist2[i]) / (hist1[i] + hist2[i]);
              }

              return chiSquare;
          }

          /// <summary>
          /// Overloaded with double array
          /// </summary>
          /// <param name="hist1">first histogram</param>
          /// <param name="hist2">second histogram</param>
          /// <returns>the ChiSquare distance</returns>
          public static double ChiSquare(double[] hist1, double[] hist2)
          {
              if (hist1.GetLength(0) != hist2.GetLength(0))
                  throw new ArgumentException("Array size does not match.");
              double chiSquare = 0;
              double sum;
              for (int i = 0; i < hist1.GetLength(0); i++)
              {
                  sum = hist1[i] + hist2[i];
                  if (sum != 0)
                      chiSquare += (hist1[i] - hist2[i]) * (hist1[i] - hist2[i]) / (hist1[i] + hist2[i]);
              }

              return chiSquare;
          }

          /// <summary>
          /// Make vector1 a unit vector
          /// </summary>
          /// <param name="vector1"></param>
          /// <param name="vector2"></param>
          public static void MakeUnit(float[] vector1, float[] vector2)
          {
              if (vector1.Length != vector2.Length)
                  throw new ArgumentException("Array size does not match");
              float sumSquared = 0;
              for (int i = 0; i < vector1.Length; i++)
              {
                  sumSquared += vector1[i] * vector1[i];
              }
              float scale = 1.0f / Convert.ToSingle(Math.Sqrt((double)sumSquared));
              for (int j = 0; j < vector1.Length; j++)
              {
                  vector2[j] = vector1[j] * scale;
              }
              //Multiply(vector1, scale, vector2);
          }

          /// <summary>
          /// make vector1 a unit vector
          /// </summary>
          /// <param name="vector1"></param>
          /// <param name="vector2"></param>
          public static void MakeUnit(double[] vector1, double[] vector2)
          {
              if (vector1.Length != vector2.Length)
                  throw new ArgumentException("Array size does not match");
              double sumSquared = 0;
              for (int i = 0; i < vector1.Length; i++)
              {
                  sumSquared += vector1[i] * vector1[i];
              }
              double scale = 1.0 / Math.Sqrt((double)sumSquared);
              for (int j = 0; j < vector1.Length; j++)
              {
                  vector2[j] = vector1[j] * scale; 
              }
              //Multiply(vector1, scale, vector2);
          }

          /// <summary>
          /// calculate the normalized correlation between 2 vectors
          /// </summary>
          /// <param name="vector1"></param>
          /// <param name="vector2"></param>
          /// <returns></returns>
          public static float NormalizeCorrelation(float[] vector1, float[] vector2)
          {
              if (vector1.Length != vector2.Length)
                  throw new ArgumentException("Array size does not match");

              float corr = 0;

              float sum = VectorSum(vector1);
              float ave1 = sum / vector1.Length;
              sum = VectorSum(vector2);
              float ave2 = sum / vector2.Length;

              float[] _vector1 = new float[vector1.Length];
              float[] _vector2 = new float[vector2.Length];              

              for (int i = 0; i < _vector1.Length; i++)
              {
                  _vector1[i] = vector1[i] - ave1;
                  _vector2[i] = vector2[i] - ave2;
              }

              MakeUnit(vector1, _vector1);
              MakeUnit(vector2, _vector2);
              for (int i = 0; i < _vector1.Length; i++)
              {
                  corr = _vector1[i] * _vector2[i];
              }
              return corr;
          }

          /// <summary>
          /// Overloaded double version
          /// </summary>
          /// <param name="vector1"></param>
          /// <param name="vector2"></param>
          /// <returns></returns>
          public static double NormalizeCorrelation(double[] vector1, double[] vector2)
          {
              if (vector1.Length != vector2.Length)
                  throw new ArgumentException("Array size does not match");

              double corr = 0;

              double sum = VectorSum(vector1);
              double ave1 = sum / vector1.Length;
              sum = VectorSum(vector2);
              double ave2 = sum / vector2.Length;

              double[] _vector1 = new double[vector1.Length];
              double[] _vector2 = new double[vector2.Length];
              for (int i = 0; i < _vector1.Length; i++)
              {
                  _vector1[i] = vector1[i] - ave1;
                  _vector2[i] = vector2[i] - ave2;
              }

              MakeUnit(_vector1, _vector1);
              MakeUnit(_vector2, _vector2);
              for (int i = 0; i < _vector1.Length; i++)
              {
                  corr = _vector1[i] * _vector2[i];
              }
              return corr;
          }

          /// <summary>
          /// Euclidean distance
          /// </summary>
          /// <param name="vector1"></param>
          /// <param name="vector2"></param>
          /// <returns></returns>
          public static float EuclideanDistance(float[] vector1, float[] vector2)
          {
              if (vector1.Length != vector2.Length)
                  throw new ArgumentException("Array size does not match");
              float distance = 0;
              float diff;
              for (int i = 0; i < vector1.Length; i++)
              {
                  diff = vector1[i] - vector2[i];
                  distance += diff * diff;
              }
              distance = Convert.ToSingle(Math.Sqrt(Convert.ToDouble(distance)));
              return distance;
          }

          /// <summary>
          /// Euclidean distance, overloaded double version
          /// </summary>
          /// <param name="vector1"></param>
          /// <param name="vector2"></param>
          /// <returns></returns>
          public static double EuclideanDistance(double[] vector1, double[] vector2)
          {
              if (vector1.Length != vector2.Length)
                  throw new ArgumentException("Array size does not match");
              double distance = 0;
              double diff;
              for (int i = 0; i < vector1.Length; i++)
              {
                  diff = vector1[i] - vector2[i];
                  distance += diff * diff;
              }
              distance = Math.Sqrt(distance);
              return distance;
          }

          public static string ToString(double[] vector, int space, int precision)
          {
              System.Text.StringBuilder sb = new System.Text.StringBuilder();
              string format = "{0," + space.ToString() + ":F" + precision.ToString() + "}";

              int cs1 = vector.GetLength(0);
              for (int i = 0; i < cs1; ++i)
              {
                  sb.AppendFormat(format, vector[i]);
              }
              return sb.ToString();
          }

          public static double LogSumExp(double[] vector)
          {
              if (vector.GetLength(0) == 0) return Double.MinValue;

              int maxIndex = MaxIndex(vector);
              double max = vector[maxIndex];
              double sumExp = 0;
              for (int i = 0; i < vector.GetLength(0); ++i)
              {
                  sumExp += Math.Exp(vector[i] - max);
              }
              return max + Math.Log(sumExp);
          }

          public static int MaxIndex(double[] vector)
          {
              double max = Double.MinValue;
              int maxIndex = 0;
              for (int i = 0; i < vector.GetLength(0); ++i)
              {
                  if (vector[i] > max)
                  {
                      max = vector[i];
                      maxIndex = i;
                  }
              }
              return maxIndex;
          }
      }
  }
}
