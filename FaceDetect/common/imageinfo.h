#pragma once

#include "wrect.h"
#include "RunLabel.h" 

enum LABELTYPE 
{
    UNANNOTATED = -2, 
    DISCARDED = -1, 
    NO_FACE = 0, 
    ALL_LABELED = 1, 
    PARTIALLY_LABELED = 2
}; 

struct IMGINFO
{
    // most basic stuff 
    char          * m_szFileName; 
    LABELTYPE       m_LabelType; 
    int             m_nNumObj; 
    IRECT         * m_pObjRcs;      // object bounding boxes 
    FEATUREPTS    * m_pObjFPts;     // object feature points 

    // additional parameters used for testing ROC 
    bool          * m_bDetected;        // for each object and each threshold, whether it is detected 
    int           * m_nNumDetectedPos;  // number of detected positive rectangles for each threshold
    int           * m_nNumFPos;         // number of false positive rectangles for each threshold 

    // additional parameters used for resetting thresholds 
    int           * m_nNumMatchRect; 

    // additional parameters used for training 
    int             m_nImgWidth; 
    int             m_nImgHeight; 
    vector<RUNLABEL>    m_LabelVec; 
    int             m_nPosCount;            // total number of positive rectangles 
    int             m_nNegCount;            // total number of negative rectangles 
    int             m_nTotalCount;          // total count include those rectangles that are ignored 

    IMGINFO(); 
    IMGINFO(FILE *fp, const char *szPath); 
    ~IMGINFO(); 
    void ReadInfo(FILE *fp, const char *szPath); 
    void WriteInfo(FILE *fp, const char *szPath); 
    void CheckInfo(); 
    void Release(); 
    void ReleaseObjs(); 
};