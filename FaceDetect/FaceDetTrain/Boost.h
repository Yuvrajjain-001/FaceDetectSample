#pragma once

#include <vector>
#include "rand.h"
#include "image.h"
#include "imageinfo.h"
#include "feature.h"
#include "classifier.h"
#include "BitCodec.h"

//#define USE_ROBUST_SAMPLING
#if defined(USE_ROBUST_SAMPLING)
#define START_RS_NUM    64      // robust sampling starts at feature #START_RS_NUM
#define RS_PERCENT      0.01f 
#endif


//#define WRITE_POS_EXAMPLES
#define NUM_HIST_BIN            256
#define NUM_TOP_FEATURES        10
#define MAX_NUM_FEATURE_TH      31
#define MAX_ITER                5
#define MIN_SCORE               (-30)
#define MAX_SCORE               (-MIN_SCORE)
#define NUM_PROC_THREADS        4

struct VALUEINDEX
{
    int         m_nIdx; 
    float       m_fVal; 
}; 

struct LOOP_PARA
{
    float m_fScale;     // scale of the rectangle 
    int m_nW;           // rectangle width
    int m_nH;           // rectangle height
    int m_nSW;          // rectangle step in width
    int m_nSH;          // rectangle step in height 
}; 

struct REMASK_PARA
{
    float m_fScoreTh; 
    double m_dTotalPosCount; 
    double m_dNegScoreThCount; 
    double m_dSampleRatio; 
}; 

struct EXAMPLE
{
    char    m_nLabel;       // label 
    char    m_nSizeIdx;     // size index of the example (index into BOOST::m_LoopPara)
    short   m_nSampled;     // sampled counter 
    float   m_fScore;       // score 
    float   m_fWeight;      // weight 
    float   m_fFVal;        // feature value 
    float   m_fNorm;        // normalization factor of the integral image 
    I_IMAGE *m_pIImg;       // the integral image of the example (subsampled to fixed size) 
};

struct SAMPLED_EXAMPLE
{
    // for LOGITBOOST or ADABOOST, we have three kinds of labels: -1: negative example, 1: positive example, 0: invalid example 
    char      m_nLabel; 
    float   * m_fFeature; 
}; 

class BOOST
{
    CRand           m_Rand; 
    double         *m_dRandNums; 

    float           m_fNegRejPercent; 
    int             m_nMaskFreq; 
    int             m_nWidth; 
    int             m_nHeight;
    int             m_nNumFeatureTh; 
    float           m_fStepSize; 
    float           m_fStepScale; 

    int             m_nfWidth; 
    int             m_nfHeight; 
    float           m_ffStepSize;   // step size for generating features 
    float           m_ffStepScale; 

    char            m_fScoreFilePrefix[MAX_PATH]; 
    int             m_nScoreFileSize; 

    int             m_nNumBoostFeatures; 
    int             m_nNumRCFeatures; 
    RCFEATURE      * m_RCFeatures[MAX_NUM_SCALE]; 

    LOOP_PARA       m_LoopPara[MAX_NUM_SCALE]; 
    REMASK_PARA     m_RemaskPara; 

    vector<IMGINFO *> m_ImgInfoVec; 

    float           m_fInitScore; 

    int             m_nMaxNumExamples; 
    int             m_nNumExamples; 
    EXAMPLE       * m_pExamples;
    int             m_nNumSampledExamples; 
    SAMPLED_EXAMPLE * m_pSampledExamples; 

    int             m_nScoreBufSize; 
    float         * m_pfScoreBuf; 

    int             m_nUpdateScoreIdx; 

private: 
    void            ReleaseRCFeatures(); 
    void            ReleaseImgInfoVec(); 
    void            ReleaseExamples(); 
    void            GenerateRCFeatures(); 

    float           FindInitScore(); 
    inline float    ComputeWeight(int label, float score)
    {
        return 1.0f/(1.0f + exp(float(label)*score)); 
    }; 

    void            ReadScoreFile(int idx); 
    void            WriteScoreFile(int idx); 
    void            UpdateAllScores(CLASSIFIER *pC, int num); 
//    float           FindNegScoreTh(); 

    void            SetFeature(int idx, FEATURE *pF);
    float           FindOneFeatureThreshold(double *pdPosWeight, 
                                            double *pdNegWeight, 
                                            int numBin, 
                                            int &th, 
                                            float score[2]); 
    float           FindFeatureThresholds(double *pdPosWeight,  // array of positive weights
                                          double *pdNegWeight,  // array of negative weights
                                          int numBin,           // number of bins in the above weight arrays
                                          float minFVal,        // minimum feature value 
                                          float step,           // step size of the bins
                                          int numFTh,           // number of feature thresholds
                                          float *pfFTh,         // feature threshold array (output)
                                          float *pfDScore);     // dscore array (output)

public: 
    BOOST (); 
    BOOST (const char *szListFile); 
    ~BOOST (); 

    bool            LoadListFile(const char *szListFile);
    bool            LoadImageInfo(const char *szPath, int *index); 
    bool            AllocateExamples(int maxNumExmaples, int numSampledExamples); 

    void            ComputeAllFeatures(int nStart, int nEnd, int nIdx); 
    void            SelectOneFeatureProc(int nStart, int nEnd, VALUEINDEX *pVI); 
    void            InitAllExamples(); 
    void            SampleExamples4FeatureSelection(); 
    int             SelectNormFeature(CLASSIFIER *pC); 
    int             SelectOneFeature(CLASSIFIER *pC); 
    void            UpdateExampleWeights(CLASSIFIER *pC, int num, int fidx); 
    void            ReInitAllExamples(CLASSIFIER *pC, int num); 

    int             GetNumBoostFeatures() { return m_nNumBoostFeatures; }; 
    int             GetWidth() { return m_nWidth; };
    int             GetHeight() { return m_nHeight; };
    int             GetNumFeatureTh() { return m_nNumFeatureTh; }; 
    int             GetNumFeatures() { return m_nNumRCFeatures; }; 
    int             GetMaskFreq() { return m_nMaskFreq; }; 
};

struct THREADPROC_PARA
{
    BOOST  *m_pBoost; 
    int     m_nStart; 
    int     m_nEnd; 
    int     m_nIdx; 
    VALUEINDEX *m_pVI; 
}; 