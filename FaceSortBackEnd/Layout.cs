using System;
using System.Collections.Generic;
using System.Text;

using FaceSortUI;
using ShoNS.Array;
using LibFaceSort;

namespace FaceSortBackEnd
{
    public class PairDistance:IComparable
    {
        /// <summary>
        /// the data id 1
        /// </summary>
        public int m_id1;
        /// <summary>
        /// the data id 2
        /// </summary>
        public int m_id2;
        /// <summary>
        /// the distance between the two 
        /// </summary>
        public double m_distance;
        /// <summary>
        /// default constructor
        /// </summary>
        public PairDistance()
        {
            m_id1 = 0;
            m_id2 = 0;
            m_distance = 0;
        }
        /// <summary>
        /// constructor with value
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="id2"></param>
        /// <param name="distance"></param>
        public PairDistance(int id1, int id2, double distance)
        {
            m_id1 = id1;
            m_id2 = id2;
            m_distance = distance;
        }
        /// <summary>
        /// Compare the object based on their distances
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (obj is PairDistance)
            {
                PairDistance tempDistance = (PairDistance)obj;
                return m_distance.CompareTo(tempDistance.m_distance);
            }
            throw new ArgumentException("object is not a PairDistance");
        }
    }

    public class Layout
    {
        #region classVariables
        private double[] _maxCoord;         //This array defines the maximum x-y coordinates

        private double[,] _minMaxMDS;     //The min max coordinate of the MDS results
        public double[] m_arrMaxCoord
        {
            get
            {
                return _maxCoord;
            }
            set
            {
                _maxCoord = new double[value.GetLength(0)];
                for (int i = 0; i < value.GetLength(0); i++)
                {
                    _maxCoord[i] = value[i];
                }
            }
        }    //make it public acessible

        #endregion classVariables

        #region Constuctors
        /// <summary>
        /// Default constructor, set maximum coordinate to be one
        /// </summary>
        public Layout()
        {
            _maxCoord = new double[2];
            _maxCoord[0] = 1.0;
            _maxCoord[1] = 1.0;
            _minMaxMDS = new double[2, 2];
            _minMaxMDS[0, 0] = 0;
            _minMaxMDS[0, 1] = 1;
            _minMaxMDS[1, 0] = 0;
            _minMaxMDS[1, 1] = 1;
        }                                           
        /// <summary>
        /// Constructors
        /// </summary>
        /// <param name="dMaxDim1">Max Coord One</param>
        /// <param name="dMaxDim2">Max Coord Two</param>
        public Layout(double dMaxDim1, int dMaxDim2)
        {
            _maxCoord = new double[2];
            _maxCoord[0] = dMaxDim1;
            _maxCoord[1] = dMaxDim2;
            _minMaxMDS = new double[2, 2];
            _minMaxMDS[0, 0] = 0;
            _minMaxMDS[0, 1] = 1;
            _minMaxMDS[1, 0] = 0;
            _minMaxMDS[1, 1] = 1;
        }
        /// <summary>
        /// Constructors overrided
        /// </summary>
        /// <param name="arrMaxCoordVal">Array MaxCoord</param>
        public Layout(double[] arrMaxCoordVal)
        {
            m_arrMaxCoord = arrMaxCoordVal;
            _minMaxMDS = new double[2, 2];
            _minMaxMDS[0, 0] = 0;
            _minMaxMDS[0, 1] = 1;
            _minMaxMDS[1, 0] = 0;
            _minMaxMDS[1, 1] = 1;
        }
        #endregion Constructors

        #region privateMethods
        /// <summary>
        /// Returns the min and max in the 2D array for each column, stored row by row
        /// in the array returned
        /// </summary>
        /// <param name="arrData">the input data array</param>
        /// <returns></returns>
        private double[,] MinMax(double[,] arrData)
        {
            int nEl = arrData.GetLength(0);
            int nDim = arrData.GetLength(1);

            double[,] arrMinMax = new double[2, nDim];

            int i, j;
            for (i = 0; i < nDim; i++)
            {
                arrMinMax[0, i] = arrData[0, i];
                arrMinMax[1, i] = arrData[0, i];
            }

            for (i = 1; i < nEl; i++)
            {
                for (j = 0; j < nDim; j++)
                {
                    arrMinMax[0, j] = Math.Min(arrMinMax[0, j], arrData[i, j]);
                    arrMinMax[1, j] = Math.Max(arrMinMax[1, j], arrData[i, j]);
                }
            }
            return arrMinMax;
        }
        /// <summary>
        /// maps the 2D array to screen coordinates bounded by arrMinMax
        /// </summary>
        /// <param name="arrData">Input data array</param>
        /// <returns></returns>
        private double[,] MapData2Screen(double[,] arrData)
        {
            int nEl = arrData.GetLength(0);
            int nDim = arrData.GetLength(1);

            double[,] Coord = new double[nEl, nDim];
            double[,] arrMinMax = MinMax(arrData);
            double[] arrRange = new double[arrMinMax.GetLength(1)];

            int i, j;

            for (i = 0; i < arrMinMax.GetLength(1); i++)
            {
                arrRange[i] = arrMinMax[1, i] - arrMinMax[0, i];
            }

            for (i = 0; i < nEl; i++)
            {
                for (j = 0; j < nDim; j++)
                {
                    Coord[i, j] = Convert.ToDouble((arrData[i, j] - arrMinMax[0, j]) * (_maxCoord[j]) / (arrRange[j]));
                }
            }           

            return Coord;
        }
        /// <summary>
        /// place the coordinates to more efficiently use the screen
        /// space based on their orders in both x and y directions
        /// </summary>
        /// <param name="arrData">input data array</param>
        /// <returns></returns>
        private double[,] RMapData2Screen(double[,] arrData)
        {
            INumArray<double> shoArrData = ArrFactory.DoubleArray(arrData);
            INumArray<double> shoArrDataSorted;
            int nEl = shoArrData.size0;
            int nDim = shoArrData.size1;

            INumArray<int> shoArrInd = ArrFactory.IntArray(nEl, nDim);
            shoArrDataSorted = shoArrData.SortIndex(1, out shoArrInd);

            double[] arrCellStep = new double[nDim];
            double[] arrCurCoord = new double[nDim];
            double[,] arrNewData = new double[nEl, nDim];

            int i, j;
            int index;

            for (i = 0; i < nDim; i++)
            {
                arrCellStep[i] = (double)((_maxCoord[i] - 10) / nEl);
                arrCurCoord[i] = 5.0;
            }

            for (i = 0; i < nEl; i++)
            {
                for (j = 0; j < nDim; j++)
                {
                    index = shoArrInd[i, j];
                    arrNewData[index, j] = arrCurCoord[j];
                    arrCurCoord[j] += arrCellStep[j];
                }
            }

            return MapData2Screen(arrNewData);
        }

        /// <summary>
        /// Read the status of each of the elements in frontUIData. An element can be
        /// in the mode of frozen, selected, or free
        /// </summary>
        /// <param name="frontUIData">DataExchange data structure</param>
        /// <param name="arrCoord">coordinate of elements</param>
        /// <param name="listFixedImg">list of indices of images with fixed position</param>
        /// <param name="nImgIndex">the selected image, if none, -1</param>
        private void ReadStatus(DataExchange frontUIData, out double[,] arrCoord, 
                                out List<int> listFixedImg, out int nImgIndex)
        {
            int nEl=frontUIData.ElementCount;
            int nDim=frontUIData.Coordinates.GetLength(1);
            
            ExchangeElement ele=null;

            arrCoord=new double[nEl, nDim];
            listFixedImg=new List<int>();
            nImgIndex=-1;

            Random randGen = new Random();
            int i, j;
            double dCoordVal;

            for(i=0; i<nEl; i++)
            {
                ele = frontUIData.GetElement(i);
                switch(ele.State)
                {
                    case ExchangeElement.ElementState.Selected:
                        nImgIndex=i;
                        break;
                    case ExchangeElement.ElementState.FrozenLocation:
                        listFixedImg.Add(i);
                        for(j=0; j<nDim; j++)
                        {
                            dCoordVal = frontUIData.Coordinates[i, j];
                            arrCoord[i, j] = dCoordVal + (randGen.NextDouble() / 100);
                        }
                        break;
                    default:
                        for (j = 0; j < nDim; j++)
                        {
                            arrCoord[i,j]=frontUIData.Coordinates[i,j];
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// From a distance matrix, generate a list of paired distances
        /// </summary>
        /// <param name="distanceMatrix"></param>
        /// <param name="listPairDistance"></param>
        private void PairedDistances(double[,] distanceMatrix, out List<PairDistance> listPairDistance)
        {
            listPairDistance = new List<PairDistance>();
            PairDistance pairDistance = null;
            for (int i = 0; i < distanceMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < i; j++)
                {
                    pairDistance = new PairDistance(i, j, distanceMatrix[i, j]);
                    listPairDistance.Add(pairDistance);
                }
            }
        }

        /// <summary>
        /// Select the elements with the largest distances
        /// </summary>
        /// <param name="listPairDistance"></param>
        /// <param name="nBase"></param>
        /// <param name="listSelIndices"></param>
        private int OrderWithLargeDistance(List<PairDistance> listPairedDistances,
                                           out List<int> listSortedIndices)
        {
            listPairedDistances.Sort();
            listSortedIndices = new List<int>();

            for (int i = listPairedDistances.Count - 1; i >= 0; i--)
            {
                if (!listSortedIndices.Contains(listPairedDistances[i].m_id1))
                {
                    listSortedIndices.Add(listPairedDistances[i].m_id1);
                }
                if (!listSortedIndices.Contains(listPairedDistances[i].m_id2))
                {
                    listSortedIndices.Add(listPairedDistances[i].m_id2);
                }                
            }
            return listSortedIndices.Count;
        }
        #endregion privateMethods

        #region publicMethods
        /// <summary>
        /// Initial Layout by MDS from the distance matrix calculated
        /// </summary>
        /// <param name="frontUIData">DataExchange Element</param>
        public void InitialLayout(DataExchange frontUIData)
        {            
            frontUIData.RescaleCoordinates(0, _maxCoord[0], 0, _maxCoord[1]);

            int size0 = frontUIData.DistanceMatrix.GetLength(0);
            int size1 = frontUIData.DistanceMatrix.GetLength(1);
            double[,] mtxDistance = new double[size0, size1];
            Array.Copy(frontUIData.DistanceMatrix, mtxDistance, size0*size1);

            double dStress=0;
            //Multiple Dimension Scaling by Conjugate Gradient initialized by classical MDS
            double[,] arrDimRedData = LibFaceSort.DimReduction.MultiDimScaling(mtxDistance, 2);
            double[,] arrDimRedDataCG = LibFaceSort.DimReduction.MultiDimScalingCG(mtxDistance, arrDimRedData, false, ref dStress);            
            //Temporary hard code them

            double[,] arrMinMax = MinMax(arrDimRedDataCG);
            _minMaxMDS[0, 0] = arrMinMax[0, 0];
            _minMaxMDS[0, 1] = arrMinMax[1, 0];
            _minMaxMDS[1, 0] = arrMinMax[0, 1];
            _minMaxMDS[1, 1] = arrMinMax[1, 1];

            //double[,] Coord = RMapData2Screen(arrDimRedDataCG);
            double[,] Coord = MapData2Screen(arrDimRedDataCG);

            frontUIData.Coordinates = Coord;
            frontUIData.DataRect = new System.Windows.Rect(0, 0, _maxCoord[0], _maxCoord[1]);
        }
        /// <summary>
        /// Dynamicly change the layout of the face images
        /// some of the images could be fixed
        /// </summary>
        /// <param name="frontUIData">Data Exchange Element</param>
        public void DynamicLayout(DataExchange frontUIData)
        {            
            frontUIData.RescaleCoordinates(0, _maxCoord[0], 0, _maxCoord[1]);
            frontUIData.RescaleCoordinates(_minMaxMDS[0, 0], _minMaxMDS[0, 1], _minMaxMDS[1, 0], _minMaxMDS[1, 1]);

            int size0 = frontUIData.DistanceMatrix.GetLength(0);
            int size1 = frontUIData.DistanceMatrix.GetLength(1);
            double[,] matrixDistance = new double[size0, size1];
            Array.Copy(frontUIData.DistanceMatrix, matrixDistance, size0 * size1);

            List<int> listFixedImg = null;
            int nImgIndex;
            double[,] arrDimRedData = null;

            double dStress = 0;

            ReadStatus(frontUIData, out arrDimRedData, out listFixedImg, out nImgIndex);             
            double dNonIndexWeight=0.01;
            double[,] arrDimRedDataCG=null;
            if (nImgIndex != -1)
            {
                arrDimRedDataCG = LibFaceSort.DimReduction.WeightedMultiDimScalingCG(matrixDistance, nImgIndex, dNonIndexWeight, arrDimRedData, false, ref dStress);
            }
            else
            {
                arrDimRedDataCG = LibFaceSort.DimReduction.ConMultiDimScalingCG(matrixDistance, listFixedImg, arrDimRedData, false, ref dStress);
            }
            double[,] Coord = RMapData2Screen(arrDimRedDataCG);
            //double[,] Coord = MapData2Screen(arrDimRedDataCG);

            for (int i = 0; i < frontUIData.Coordinates.GetLength(0); i++)
            {
                for (int j = 0; j < frontUIData.Coordinates.GetLength(1); j++)
                {
                    if ((!(listFixedImg.Contains(i))) && (i != nImgIndex))
                    {
                        frontUIData.Coordinates[i, j] = Coord[i, j];
                    }
                }                
            }

            frontUIData.DataRect = new System.Windows.Rect(0, 0, _maxCoord[0], _maxCoord[1]);
        }
        /// <summary>
        /// Calculate the MDS in a hierarchical clustering sense
        /// </summary>
        /// <param name="frongUIData"></param>
        public void HierachicalLayout(DataExchange frontUIData)
        {
            int nBase=Math.Min(20, frontUIData.ElementCount);
            int nBatch = 10;
            frontUIData.RescaleCoordinates(0, _maxCoord[0], 0, _maxCoord[1]);

            int size0 = frontUIData.DistanceMatrix.GetLength(0);
            int size1 = frontUIData.DistanceMatrix.GetLength(1);

            double[,] mtxDistances = new double[size0, size1];
            Array.Copy(frontUIData.DistanceMatrix, mtxDistances, size0 * size1);
            List<PairDistance> listPairedDistances = null;
            PairedDistances(mtxDistances, out listPairedDistances);

            List<int> listSortedIndices = null;
            int nEl = OrderWithLargeDistance(listPairedDistances, out listSortedIndices);
            double[,] selMtxDistance = new double[nBase, nBase];

            for (int i = 0; i < nBase; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    selMtxDistance[i, j] = mtxDistances[listSortedIndices[i], listSortedIndices[j]];
                    selMtxDistance[j, i] = selMtxDistance[i, j];
                }
            }

            List<int> listSelectedIndices = new List<int>();
            for (int i = 0; i < nBase; i++)
            {
                listSelectedIndices.Add(i);
            }

            double[,] arrDimRedData = new double[size0, 2];
            double[,] arrSelDimRedData = DimReduction.MultiDimScaling(selMtxDistance, 2);

            double strainValue = 0;
            double[,] arrSelDimRedDataCG = DimReduction.MultiDimScalingCG(selMtxDistance, arrSelDimRedData, 
                                                                          false, ref strainValue);

            for (int i = 0; i < arrSelDimRedDataCG.GetLength(0); i++)
            {
                arrDimRedData[i, 0] = arrSelDimRedDataCG[i, 0];
                arrDimRedData[i, 1] = arrSelDimRedDataCG[i, 1];
            }
                                   
            int nStartIndex = nBase;
            double[,] arrCurDimRedData = null;            
            
            while (listSortedIndices.Count - nStartIndex > 0)
            {
                int elLeft = listSortedIndices.Count - nStartIndex;
                int incDim = Math.Min(nBatch, elLeft);
                int nDim = nStartIndex + incDim;                
                
                double[,] minMaxValue = MinMax(arrDimRedData);
                arrCurDimRedData = new double[nDim, 2];

                double rangex = minMaxValue[1, 0] - minMaxValue[0, 0];
                double rangey = minMaxValue[1, 1] - minMaxValue[0, 1];

                Random randNumber=new Random();

                int iter_no = 0;
                double best_stress = 100.0;
                while (iter_no < 1)
                {
                    for (int i = 0; i < nStartIndex; i++)
                    {
                        arrCurDimRedData[i, 0] = arrDimRedData[i, 0];
                        arrCurDimRedData[i, 1] = arrDimRedData[i, 1];
                    }

                    for (int i = nStartIndex; i < nDim; i++)
                    {
                        arrCurDimRedData[i, 0] = minMaxValue[0, 0] + randNumber.NextDouble() * rangex;
                        arrCurDimRedData[i, 1] = minMaxValue[1, 0] + randNumber.NextDouble() * rangey;
                    }

                    selMtxDistance = new double[nDim, nDim];
                    for (int i = 0; i < nDim; i++)
                    {
                        int ii=listSortedIndices[i];
                        for (int j = 0; j < nDim; j++)
                        {                            
                            int jj=listSortedIndices[j];
                            selMtxDistance[i, j] = mtxDistances[ii, jj];
                            selMtxDistance[j, i] = selMtxDistance[j, i];
                        }
                    }

                    arrSelDimRedDataCG = DimReduction.ConMultiDimScalingCG(selMtxDistance, listSelectedIndices,
                                                                           arrCurDimRedData, false, ref strainValue);
                    arrSelDimRedDataCG = DimReduction.MultiDimScalingCG(selMtxDistance, arrSelDimRedDataCG, false, ref strainValue);

                    if ((strainValue < best_stress) || (iter_no == 0))
                    {
                        best_stress = strainValue;
                        for (int i = 0; i < nDim; i++)
                        {
                            arrDimRedData[i, 0] = arrSelDimRedDataCG[i, 0];
                            arrDimRedData[i, 1] = arrSelDimRedDataCG[i, 1];
                        }
                    }
                    iter_no++;
                }

                for (int i = nStartIndex; i < nDim; i++)
                    listSelectedIndices.Add(listSortedIndices[i]);
                nStartIndex += incDim;                
            }

            double[,] arrDimRedData1 = new double[arrDimRedData.GetLength(0), arrDimRedData.GetLength(1)];
            for (int i = 0; i < arrDimRedData.GetLength(0); i++)
            {
                int ii = listSelectedIndices[i];
                arrDimRedData1[ii, 0] = arrDimRedData[i, 0];
                arrDimRedData1[ii, 1] = arrDimRedData[i, 1];
            }

            double[,] arrMinMax = MinMax(arrDimRedData1);
            _minMaxMDS[0, 0] = arrMinMax[0, 0];
            _minMaxMDS[0, 1] = arrMinMax[1, 0];
            _minMaxMDS[1, 0] = arrMinMax[0, 1];
            _minMaxMDS[1, 1] = arrMinMax[1, 1];

            //double[,] Coord = RMapData2Screen(arrDimRedDataCG);
            double[,] Coord = MapData2Screen(arrDimRedData1);

            frontUIData.Coordinates = Coord;
            frontUIData.DataRect = new System.Windows.Rect(0, 0, _maxCoord[0], _maxCoord[1]);
        }
        #endregion publicMethods
    }
}
