using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using FaceSortUI;
using ShoNS.Array;
using LibFaceSort;
using Dpu.ImageProcessing;
using System.IO;
using Dpu.Boost;
using Dpu.ImageFilters;
using Dpu.Utility;

namespace FaceSortBackEnd
{
    /// <summary>
    /// Orthogonal rankone distances
    /// </summary>
    public class Distance
    {
        #region classVariables
            
        private String _leftMatFileName;       //file name of the left projection matrix
        private String _rightMatFileName;       //file name of the right projection matrix
                                            //one column of left matrix and one column of right 
                                            //matrix define a rank-one tensor projection

        private INumArray<double> _rightMatrix; 
        private INumArray<double> _leftMatrix; //left and right matrix loaded from _leftMatFileName
                
        //some control parameters
        private bool _downSample;           //downsample images?
        private bool _glocalTrans;          //perform glocal transform?
 
        private int _localHeight;           //local window height for glocal transform                  
        private int _localWidth;            //local window width for glocal transform

        private bool _reSize;               //when _reSize is true, _downSample is overrided
        private int _reSizeWidth;           //the resized width
        private int _reSizeHeight;          //the resized height
        
        public String m_strLeftMatFileName
        {
            get
            {
                return _leftMatFileName;
            }
            set
            {
                _leftMatFileName = value;
            }
        }
        public String m_strRightMatFileName
        {
            get
            {
                return _rightMatFileName;
            }
            set
            {
                _rightMatFileName = value;
            }
        }  //make matrix file names public

        public bool m_bDownSample
        {
            get
            {
                return _downSample;
            }

            set
            {
                _downSample = value;
            }
        }
        public bool m_bGlocalTrans
        {
            get
            {
                return _glocalTrans;
            }
            set
            {
                _glocalTrans = value;
            }
        }       //make GLocal transform and Downsample field public
        public int m_iLocalHeight
        {
            get
            {
                return _localHeight;
            }
            set
            {
                _localHeight = value;
            }
        }
        public int m_iLocalWidth
        {
            get
            {
                return _localWidth;
            }
            set
            {
                _localWidth = value;
            }
        }         //make the local window size public
        
        public bool m_bResize
        {
            get
            {
                return _reSize;
            }
            set
            {
                _reSize = value;
            }
        }            //make _reSize, _reSizeWidth and _reSizeHeight public
        public int m_iReSizeWidth
        {
            get
            {
                return _reSizeWidth;
            }
            set
            {
                _reSizeWidth = value;
            }
        }
        public int m_iReSizeHeight
        {
            get
            {
                return _reSizeHeight;
            }

            set
            {
                _reSizeHeight = value;
            }
        }

        #endregion classVariables

        #region Constructors
        /// <summary>
        /// Default constructor, it sets the Left and Right projection matrix file names
        /// to be relative to the current path
        /// </summary>
        public Distance()
        {
            m_bDownSample = false;
            m_bGlocalTrans = true;

            m_strLeftMatFileName = "OrthoRankOneL-Boost-NN5_80.dat";//#path for ortho rank-one L Matrix
            m_strRightMatFileName = "OrthoRankOneR-Boost-NN5_80.dat";//#path for ortho rank-one R Matrix

            m_iLocalHeight = 8;
            m_iLocalWidth = 8;

            m_bResize = false;
            m_iReSizeWidth = 0;
            m_iReSizeHeight = 0;
        }

        /// <summary>
        /// Constructors which set up m_bDownSample, m_bGlocalTrans and the file name of the
        /// two matrix file
        /// </summary>
        /// <param name="bValDown">DownSample image</param>
        /// <param name="bValGlocal">Glocal Transform</param>
        /// <param name="strValL">Left Matrix path</param>
        /// <param name="strValR">Right Matrix path</param>
        public Distance(bool bValDown, bool bValGlocal, String strValL, String strValR)
        {
            m_bDownSample = bValDown;
            m_bGlocalTrans = bValGlocal;
            m_strLeftMatFileName = strValL;
            m_strRightMatFileName = strValR;

            m_iLocalHeight = 8;
            m_iLocalWidth = 8;

            m_bResize = false;
            m_iReSizeHeight = 0;
            m_iReSizeWidth = 0;
        }
        /// <summary>
        /// Constructor which also sets up local window size for Glocal transform
        /// </summary>
        /// <param name="bValDown">DownSample image</param>
        /// <param name="bValGlocal">Glocal transform</param>
        /// <param name="strValL">Left Matrix path</param>
        /// <param name="strValR">Right Matrix path</param>
        /// <param name="iValH">Glocal Transform Local Window Height</param>
        /// <param name="iValW">Glocal Transform Local Window Width</param>
        public Distance(bool bValDown, bool bValGlocal, String strValL, String strValR, int iValH, int iValW)
        {
            m_bDownSample = bValDown;
            m_bGlocalTrans = bValGlocal;
            m_strLeftMatFileName = strValL;
            m_strRightMatFileName = strValR;
            m_iLocalHeight = iValH;
            m_iLocalWidth = iValW;

            m_bResize = false;
            m_iReSizeHeight = 0;
            m_iReSizeWidth = 0;
        }
        /// <summary>
        /// Constructors which set up all fields
        /// </summary>
        /// <param name="bValResize">Resize image</param>
        /// <param name="iValResizeH">Resized Height</param>
        /// <param name="iValResizeW">Resized Width</param>
        /// <param name="bValGlocal">Glocal Transform</param>
        /// <param name="strValL">Left Matrix Path</param>
        /// <param name="strValR">Right Matrix Path</param>
        /// <param name="iValH">Glocal Local Window Height</param>
        /// <param name="iValW">Glocal Local Window Width</param>
        public Distance(bool bValResize, int iValResizeH, int iValResizeW, bool bValGlocal, String strValL, String strValR, int iValH, int iValW)
        {
            m_bDownSample = false;   //It is overrided if bValResize=True

            m_bResize = bValResize;
            m_iReSizeHeight = iValResizeH;
            m_iReSizeWidth = iValResizeW;

            m_bGlocalTrans = bValGlocal;
            m_strLeftMatFileName = strValL;
            m_strRightMatFileName = strValR;
            m_iLocalHeight = iValH;
            m_iLocalWidth = iValW;            
        }

        #endregion Constructors

        #region publicMethods
        /// <summary>
        /// The function to calculate the distance matrix of the elements listed in frontUIData
        /// </summary>
        /// <param name="frontUIData">front-backend Data Exchange</param>
        public void RankOneDistance(DataExchange frontUIData)
        {
            ParseConfig(frontUIData);
            string leftMat = m_strLeftMatFileName;
            string rightMat = m_strRightMatFileName;
            
            _leftMatrix = ArrFactory.DoubleArray(leftMat);
            _rightMatrix = ArrFactory.DoubleArray(rightMat);

            List<INumArray<double>> listImgVec = RankOneProjImgList(frontUIData);
            int nEl = listImgVec.Count;            

            double[,] matrixDistance = new double[nEl, nEl];

            for (int i = 0; i < nEl; i++)
            {
                for (int j = i; j < nEl; j++)
                {
                    matrixDistance[i, j] = (listImgVec[i].Sub(listImgVec[j])).Magnitude();
                    matrixDistance[j, i] = matrixDistance[i, j];
                }
            }

            frontUIData.DistanceMatrix=matrixDistance;
        }
        #endregion publicMethods

        #region privateMethods
        /// <summary>
        /// Read the image data from ExchangeElement, subject to the transform, i.e., reSize, downsampling, etc.
        /// specified by the parameters.
        /// </summary>
        /// <param name="exEl">element in DataExchange</param>
        /// <returns></returns>
        private INumArray<double> GetData(ExchangeElement exEl)
        {
            INumArray<double> data;
            int nHeight = exEl.Height;
            int nWidth = exEl.Width;

            int i, j;
            int ipx = 0;

            if (m_bResize)
            {
                //if m_bResize=true, resize the data to certain size
                char[] charArr = new char[exEl.ByteData.GetLength(0)];
                for (i = 0; i < exEl.ByteData.GetLength(0); i++)
                {
                    charArr[i] = Convert.ToChar(exEl.ByteData[i]);
                }
                

                Dpu.ImageProcessing.Image dpuImgData = new Dpu.ImageProcessing.Image(charArr, nWidth,nHeight);                
                Dpu.ImageProcessing.Image rstImgData = new Dpu.ImageProcessing.Image(m_iReSizeWidth, m_iReSizeHeight);
                Dpu.ImageProcessing.Image.BilinearResample(dpuImgData, rstImgData);
                
                data = ArrFactory.DoubleArray(m_iReSizeHeight, m_iReSizeWidth);
                float[] pixelData = rstImgData.Pixels;
                ipx = 0;
                for (i = 0; i < m_iReSizeHeight; i++)
                {
                    for (j = 0; j < m_iReSizeWidth; j++)
                    {
                        data[i, j] = Convert.ToDouble(pixelData[ipx]);
                        ipx += 1;
                    }
                }
                
            }
            else
            {
                if (m_bDownSample)
                {
                    data = ArrFactory.DoubleArray(nHeight / 2, nWidth / 2);
                    ipx = 0;
                    Byte[] imData = exEl.ByteData;
                    for (i = 0; i < nHeight; i++)
                    {
                        for (j = 0; j < nWidth; j++)
                        {
                            data[i, j] = Convert.ToDouble(imData[ipx]);
                            ipx += 2;
                        }
                    }
                }
                else
                {
                    data = ArrFactory.DoubleArray(exEl.DoubleDataMatrix);
                }
            }

            if (m_bGlocalTrans)
            {
                data = DataTransform.GlocalTransform(data, _localWidth, _localHeight);
            }

            return data;
        }

        /// <summary>
        /// Project the list of image data in frontUIData into the embedding space
        /// </summary>
        /// <param name="frontUIData">Front Data Exchange</param>
        /// <returns></returns>
        private List<INumArray<double>> RankOneProjImgList(DataExchange frontUIData)
        {
            INumArray<double> l;
            INumArray<double> r;
            INumArray<double> data;
            INumArray<double> vecData;

            List<INumArray<double>> listImgVec = new List<INumArray<double>>();//list of projected image data

            int nEl = frontUIData.ElementCount;
            int nProj = _leftMatrix.size1;
            ExchangeElement exEl;

            for (int i = 0; i < nEl; i++)
            {
                exEl = frontUIData.GetElement(i);
                data = GetData(exEl);

                vecData = ArrFactory.DoubleArray(nProj);
                for (int j = 0; j < nProj; j++)
                {
                    l = (INumArray<double>)(_leftMatrix.GetCol(j));
                    r = (INumArray<double>)(_rightMatrix.GetCol(j));
                    vecData[j] = (((INumArray<double>)(l.Transpose())).Mult(data).Mult(r))[0];
                }
                listImgVec.Add(vecData);
            }
            return listImgVec;
        }

        /// <summary>
        /// Parse the configuration file
        /// </summary>
        /// <param name="frontUIData">front-backend data exchange</param>
        private void ParseConfig(DataExchange frontUIData)
        {
            string configureFile = frontUIData.BackEndConfigFile;
            string configureDir = System.IO.Path.GetDirectoryName(configureFile);
            StreamReader ConfigReader = new StreamReader(configureFile);
            string strLine;
            string[] strArr;

            try
            {
                m_strLeftMatFileName = _RootedPath(ConfigReader.ReadLine(), configureDir);
                m_strRightMatFileName = _RootedPath(ConfigReader.ReadLine(), configureDir);

                strLine = ConfigReader.ReadLine();
                strArr = strLine.Split(new char[] { ' ' });
                m_iReSizeHeight = Convert.ToInt32(strArr[0]);
                m_iReSizeWidth = Convert.ToInt32(strArr[1]);

                strLine = ConfigReader.ReadLine();
                strArr = strLine.Split(new char[] { ' ' });

                m_iLocalHeight = Convert.ToInt32(strArr[0]);
                m_iLocalWidth = Convert.ToInt32(strArr[1]);

                if ((m_iLocalWidth == 0) || (m_iLocalHeight == 0))
                {
                    m_bGlocalTrans = false;
                }
                else
                {
                    m_bGlocalTrans = true;
                }

                ExchangeElement el = frontUIData.GetElement(0);

                if ((m_iReSizeHeight == (el.Height / 2)) && (m_iReSizeWidth == (el.Width / 2)))
                {
                    m_bDownSample = true;
                    m_bResize = false;
                }
                else
                {
                    if ((m_iReSizeHeight == el.Height) && (m_iReSizeWidth == el.Width))
                    {
                        m_bDownSample = false;
                        m_bResize = false;
                    }
                    else
                    {
                        m_bDownSample = false;
                        m_bResize = true;
                    }
                }
            }
            catch
            {
                throw(new Exception("Exception: Configuration File Format Error"));
            }
        }

        /// <summary>
        /// Obtained file name with path
        /// </summary>
        /// <param name="path">file path</param>
        /// <param name="dir">working directory</param>
        /// <returns></returns>
        private string _RootedPath(string path, string dir)
        {
            if (System.IO.Path.IsPathRooted(path))
                return path;
            else
                return System.IO.Path.Combine(dir, path);
        }

        #endregion privateMethods
    }

    /// <summary>
    /// Test the LBP distance firstly
    /// </summary>
    public class BoostLBPDistance
    {
        #region private member variables
        /// <summary>
        /// Name of the classifier file
        /// </summary>       
        private string _classifierFileName;
        /// <summary>
        /// name of the rectangular filter name
        /// </summary>
        private string _rectangularFileName;
        /// <summary>
        /// width of images to work on
        /// </summary>
        private int _imgWidth;
        /// <summary>
        /// height of images to work on
        /// </summary>
        private int _imgHeight;
        /// <summary>
        /// number of stages to be used
        /// </summary>
        private int _stage;
        /// <summary>
        /// decision threshold
        /// </summary>
        private float _thresh;
        #endregion private member variables

        #region public fields
        /// <summary>
        /// Access _classifierFilename
        /// </summary>
        public string m_classifierFileName
        {
            get
            {
                return _classifierFileName;
            }
            set
            {
                _classifierFileName = value;
            }
        }
        /// <summary>
        /// Access _rectangularFileName;
        /// </summary>
        public string m_rectangularFileName
        {
            get
            {
                return _rectangularFileName;
            }
            set
            {
                _rectangularFileName = value;
            }
        }
        #endregion public fields

        #region Constructors
        public BoostLBPDistance()
        {
            _classifierFileName = "//Llrwcfil001/Home/ganghua/BoostDecisionStumpClassifier.xml";
            _rectangularFileName = "//Llrwcfil001/Home/ganghua/filterSet.bin";
            _imgHeight = 32;
            _imgWidth = 32;
            _stage = 500;
        }
        #endregion Constructors

        #region public functions
        /// <summary>
        /// Calculate the LBP distance
        /// </summary>
        /// <param name="frontUIData"></param>
        public void LBPDistance(DataExchange frontUIData)
        {
            ParseConfig(frontUIData);

            List<Image> listFilteredImage = new List<Image>();
            int nEl = frontUIData.ElementCount;
            ExchangeElement exEl = null;
            Image filteredImage = null;
            LBPFilter lbpFilter = new LBPFilter(1, 8, true, true);
            int i, j;
            for (i = 0; i < nEl; i++)
            {
                exEl = frontUIData.GetElement(i);
                filteredImage = GetData(exEl, lbpFilter);
                listFilteredImage.Add(filteredImage);
            }

            SparseFilterExample example;
            LBPIntegralHistogrom lbpHist1 = new LBPIntegralHistogrom();
            LBPIntegralHistogrom lbpHist2 = new LBPIntegralHistogrom();

            StrongClassifier strongClassifier = LoadClassifier(_classifierFileName);
            RectangularFilter rectangularFilters = LoadFilterSet(_rectangularFileName);

            List<int> listRectangularIndices=GetFilterIndex(strongClassifier);

            float[] score = new float[2];
            double[] expScore=new double[2];

            double[,] distanceMatrix = new double[nEl, nEl];

            for (i = 0; i < nEl; i++)
            {
                lbpHist1.Create(listFilteredImage[i], lbpFilter.m_nFilterRange + 1);
                for (j = 0; j < i; j++)
                {
                    lbpHist2.Create(listFilteredImage[j], lbpFilter.m_nFilterRange + 1);
                    example = CreateFilterResponses(lbpHist1, lbpHist2, 
                                                    rectangularFilters, listRectangularIndices);
                    score[0] = score[1] = 0;
                    score = strongClassifier.Vote(example, score, _stage);
                    
                    expScore[0]=Math.Exp(Convert.ToDouble(score[0]));
                    expScore[1]=Math.Exp(Convert.ToDouble(score[1]));
                    distanceMatrix[i, j] = expScore[0] / (expScore[0] + expScore[1]) +0.05;
                    distanceMatrix[j, i] = distanceMatrix[i, j];
                }
            }
            frontUIData.DistanceMatrix = distanceMatrix;
        }
        #endregion public functions

        #region private functions
        /// <summary>
        /// Get the filter response images from exEl
        /// </summary>
        /// <param name="exEl"></param>
        /// <returns></returns>
        private Image GetData(ExchangeElement exEl, LBPFilter lbpFilter)
        {            
            int nHeight = exEl.Height;
            int nWidth = exEl.Width;

            int i;            
            
            char[] charArr = new char[exEl.ByteData.GetLength(0)];
            for (i = 0; i < exEl.ByteData.GetLength(0); i++)
            {
                charArr[i] = Convert.ToChar(exEl.ByteData[i]);
            }

            Image dpuImgData = new Image(charArr, nWidth, nHeight);            
            Image rstImgData = null;

            if ((_imgWidth != nWidth) || (_imgHeight != nHeight))
            {
                rstImgData = new Dpu.ImageProcessing.Image(_imgWidth, _imgHeight);
                Image.BilinearResample(dpuImgData, rstImgData);
            }
            else
            {
                rstImgData = dpuImgData;
            }
            
            dpuImgData = new Image(_imgWidth, _imgHeight);
            lbpFilter.FilterImage(rstImgData, dpuImgData);

            return dpuImgData;
        }
        /// <summary>
        /// Get the list of rectangular filter ID
        /// </summary>
        /// <param name="strongClassifier"></param>
        /// <returns></returns>
        private List<int> GetFilterIndex(StrongClassifier strongClassifier)
        {
            List<int> listRectangleIndex = new List<int>();

            if (strongClassifier.Classifiers[0] is Feature)
            {
                foreach (Feature feature in strongClassifier.Classifiers)
                {
                    if (!listRectangleIndex.Contains(feature.Nfilter))
                        listRectangleIndex.Add(feature.Nfilter);
                }
            }
            else
            {
                if (strongClassifier.Classifiers[0] is DecisionTree)
                {
                    foreach (DecisionTree decisionTree in strongClassifier.Classifiers)
                    {
                        DecisionTree.SubTreeCollection treeNodeCollection = new DecisionTree.SubTreeCollection(decisionTree);
                        foreach (DecisionTree treenode in treeNodeCollection)
                        {
                            if (treenode.Decision != null)
                            {
                                if (!listRectangleIndex.Contains(treenode.Decision.Nfilter))
                                    listRectangleIndex.Add(treenode.Decision.Nfilter);
                            }
                        }
                    }
                }
            }
            return listRectangleIndex;
        }

        /// <summary>
        /// Get the filter responses based on the LBP integral histogram
        /// </summary>
        private SparseFilterExample CreateFilterResponses(LBPIntegralHistogrom iHist1, LBPIntegralHistogrom iHist2, 
                                           RectangularFilter rectangularFilters, List<int> listFilterIndices)
        {
            SparseFilterExample example = new SparseFilterExample();
            System.Drawing.Rectangle rectangle;
            float[] hist1;
            float[] hist2;
            foreach (int indexFilters in listFilterIndices)
            {
                rectangle = rectangularFilters.m_listRectangles[indexFilters];
                hist1 = LBPIntegralHistogrom.Histogram(iHist1, rectangle);
                hist2 = LBPIntegralHistogrom.Histogram(iHist2, rectangle);
                example.Add(indexFilters, ArrayUtils.ChiSquare(hist1, hist2));
            }
            return example;
        }

        /// <summary>
        /// Parse the config file for LBP distances
        /// </summary>
        /// <param name="frontUIData">the data exchange data structure</param>
        private void ParseConfig(DataExchange frontUIData)
        {
            string configureFile = frontUIData.BackEndConfigFile;
            string configureDir = System.IO.Path.GetDirectoryName(configureFile);
            StreamReader ConfigReader = new StreamReader(configureFile);
            string strLine;
            string[] strArr;

            try
            {
                _classifierFileName = ConfigReader.ReadLine();
                _rectangularFileName = ConfigReader.ReadLine();

                _classifierFileName = _RootedPath(_classifierFileName, configureDir);
                _rectangularFileName = _RootedPath(_rectangularFileName, configureDir);

                strLine = ConfigReader.ReadLine();
                strArr = strLine.Split();
                _imgHeight = Convert.ToInt32(strArr[0]);
                _imgWidth = Convert.ToInt32(strArr[1]);
                strLine = ConfigReader.ReadLine();
                strArr = strLine.Split();
                _stage = Convert.ToInt32(strArr[0]);
                _thresh = Convert.ToSingle(strArr[1]);
            }
            catch
            {
                ConfigReader.Close();
                throw new Exception("Config file reading error!!");
            }

            ConfigReader.Close();
        }
        #endregion private functions

        #region private IO functions
        /// <summary>
        /// Load the classifier file
        /// </summary>
        /// <param name="loadFileName"></param>
        /// <returns></returns>
        public static StrongClassifier LoadClassifier(string loadFileName)
        {
            StreamReader sr = new StreamReader(loadFileName, System.Text.Encoding.Unicode);
            XmlReader xr = new XmlTextReader(sr);
            XmlSerializer ser = new XmlSerializer(typeof(StrongClassifier));

            StrongClassifier classifier = (StrongClassifier)ser.Deserialize(xr);
            xr.Close();
            return classifier;
        }
        /// <summary>
        /// load the set of rectangular filters
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="rectangularFilter"></param>
        public static RectangularFilter LoadFilterSet(string filename)
        {
            RectangularFilter rectangularFilter = null;
            FileStream fstream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            BinaryFormatter fmt = new BinaryFormatter();
            rectangularFilter = (RectangularFilter)fmt.Deserialize(fstream);
            fstream.Close();
            return rectangularFilter;
        }
        /// <summary>
        /// Obtained file name with path
        /// </summary>
        /// <param name="path">file path</param>
        /// <param name="dir">working directory</param>
        /// <returns></returns>
        private string _RootedPath(string path, string dir)
        {
            if (System.IO.Path.IsPathRooted(path))
                return path;
            else
                return System.IO.Path.Combine(dir, path);
        }
        #endregion private IO functions
    }
}
