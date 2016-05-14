#pragma once

/******************************************************************************\
*
*   A FILTER object contains a list of weighted rectangles
*
\******************************************************************************/

//#define INCLUDE_RANDOM_RCFEATURES
#if defined(INCLUDE_RANDOM_RCFEATURES) 
#define NUM_RANDOM_RCFEATURES   20000
#endif

#include "wrect.h"
#include "image.h"

struct RCFEATURE
{
    int m_nRects;                       // number of weighted rectangles
    WEIGHTED_RECT *m_wRectArray;        // ptr to an array of weighted rectangle bjects

    RCFEATURE();
    RCFEATURE(const RCFEATURE *src, const float scale = 1);

    void Init(int nRects);
    void Init(FILE *file);
    void Init(const RCFEATURE *src, const float scale = 1); 
    void InitSS(const RCFEATURE *src, const float scale = 1);   // initialize sub-sampled feature (used during training)
    void Write(FILE *file); 
    void Release(); 

    static RCFEATURE* CreateCompleteRCFeatureArray(int width, int height, 
        int fwidth, int fheight, float fssize, float fsscale, int *pCount); 
    static void DeleteRCFeatureArray(RCFEATURE *featureArray);

    // CAUTION: in Eval(), x and y are the top left corner of the measured rectangle, the feature's 
    // offset will be added to the (x,y) coordinate 
    float Eval(I_IMAGE *pIImg, float norm=1.0f, int x=0, int y=0); 

    ~RCFEATURE();
};

struct FEATURE
{
    enum FEATURETYPE
    {
        UNKNOWN, 
        RECTFEATURE, 
        NORMFEATURE
    } m_nType; 
    union FEATUREPTR
    {
        RCFEATURE *pRCF; 
    } m_pF; 

    void Init(const FEATURE *src, const float scale = 1); 
    void Init(FILE *file); 
    void Write(FILE *file); 
    void Release(); 

    float Eval(I_IMAGE *pIImg, float norm=1.0f, int x=0, int y=0); 

    FEATURE(); 
    ~FEATURE(); 
}; 
