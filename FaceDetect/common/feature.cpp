/******************************************************************************\
*
*   Member fuctions for the RCFEATURE class   
*
\******************************************************************************/

#include "stdafx.h"
#include "feature.h"

#ifndef _DETECTION_ONLY
#include "rand.h"
#endif

/******************************************************************************\
*
*   
*
\******************************************************************************/

RCFEATURE::~RCFEATURE()
{
    Release(); 
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

RCFEATURE::RCFEATURE() :
    m_nRects(0),
    m_wRectArray(NULL)
{
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

RCFEATURE::RCFEATURE(const RCFEATURE *src, const float scale) :
    m_nRects(0),
    m_wRectArray(NULL)
{
    Init(src, scale); 
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

void RCFEATURE::Init(int nRects)
{
    Release(); 
    m_nRects = nRects;
    m_wRectArray = new WEIGHTED_RECT[nRects];
}

/******************************************************************************\
*
*   Initializes this RCFEATURE object by reading it's internal values out
*   of a text file. We expect the text stream to have the following
*   format:
*
*   RCFEATURE := number WRECT_LIST
*
*   WRECT_LIST := WRECT
*                 WRECT_LIST WRECT
*
\******************************************************************************/

void RCFEATURE::Init(FILE *file)
{
    Release(); 
    int nRects = 0, nImgIdx = 0;
    if (fscanf(file, "%d", &nRects) != 1)
        throw "unexpected";
    m_nRects  = nRects;
    m_wRectArray = new WEIGHTED_RECT[nRects];
    if (m_wRectArray == NULL)
        throw "out of memory";
    for (int i = 0; i < nRects; i++)
    {
        m_wRectArray[i].Init(file);
    }
}

void RCFEATURE::Init(const RCFEATURE *src, const float scale)
{
    ASSERT(src != NULL);
    //ASSERT(scale >= 1);
    Release(); 

    m_nRects = src->m_nRects;
    ASSERT(m_nRects > 0);

    m_wRectArray = new WEIGHTED_RECT[m_nRects];
    if (m_wRectArray == NULL)
    {
        m_nRects = 0; 
        return;
    }

    for (int i = 0; i < m_nRects; i++)
    {
        WEIGHTED_RECT &wRect = m_wRectArray[i];
        wRect = src->m_wRectArray[i];
        IRECT &iRect = wRect.m_rect;

        int oldArea = (iRect.m_ixMax-iRect.m_ixMin) * (iRect.m_iyMax-iRect.m_iyMin);

        iRect.m_ixMin = (int)(scale * iRect.m_ixMin + 0.5f);
        iRect.m_ixMax = (int)(scale * iRect.m_ixMax + 0.5f);
        iRect.m_iyMin = (int)(scale * iRect.m_iyMin + 0.5f);
        iRect.m_iyMax = (int)(scale * iRect.m_iyMax + 0.5f);

        int newArea = (iRect.m_ixMax-iRect.m_ixMin) * (iRect.m_iyMax-iRect.m_iyMin);

        wRect.m_weight = (wRect.m_weight * oldArea) / newArea;
    }
}

void RCFEATURE::InitSS(const RCFEATURE *src, const float scale)
{
    ASSERT(src != NULL);
    //ASSERT(scale >= 1);
    Release(); 

    m_nRects = src->m_nRects;
    ASSERT(m_nRects > 0);

    m_wRectArray = new WEIGHTED_RECT[m_nRects];
    if (m_wRectArray == NULL)
    {
        m_nRects = 0; 
        return;
    }

    for (int i = 0; i < m_nRects; i++)
    {
        WEIGHTED_RECT &wRect = m_wRectArray[i];
        wRect = src->m_wRectArray[i];   // in this case, we don't modify the rectangles
        IRECT &iRect = wRect.m_rect; 

        int oldArea = (iRect.m_ixMax-iRect.m_ixMin) * (iRect.m_iyMax-iRect.m_iyMin);

        int xMin = (int)(scale * iRect.m_ixMin + 0.5f);
        int xMax = (int)(scale * iRect.m_ixMax + 0.5f);
        int yMin = (int)(scale * iRect.m_iyMin + 0.5f);
        int yMax = (int)(scale * iRect.m_iyMax + 0.5f);

        int newArea = (xMax-xMin) * (yMax-yMin);

        wRect.m_weight = (wRect.m_weight * oldArea) / newArea;  // but we do update the weights
    }
}


void RCFEATURE::Write(FILE *file)
{
    fprintf(file, "%d\n", m_nRects); 
    for (int j=0; j<m_nRects; j++) 
        m_wRectArray[j].Write(file); 
}

void RCFEATURE::Release()
{
    if (m_wRectArray != NULL)
    {
        delete [] m_wRectArray;
        m_wRectArray = NULL; 
    }
}

/******************************************************************************\
*
* Create a complete array of features 
*
\******************************************************************************/

RCFEATURE* RCFEATURE::CreateCompleteRCFeatureArray(int width, int height, 
                                                int fwidth, int fheight, float fssize, float fsscale, int *pCount)
{
    RCFEATURE *FeatureArray = NULL; 

    // in the first pass, only do counting 
    int count = 0; 
    int w, h;
    
    // 2-rect features 

    // (2,1) split
    w = fwidth; 
    h = fheight; 
    while (w <= width/2)
    {
        while (h<=height)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+2*w<=width; x+=xstep) 
                for (int y=0; y+h<=height; y+=ystep)
                    count ++; 
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }
    // (1,2) split
    w = fwidth; 
    h = fheight; 
    while (w <= width)
    {
        while (h<=height/2)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+w<=width; x+=xstep) 
                for (int y=0; y+2*h<=height; y+=ystep)
                    count ++; 
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }
    // (3,1) split  
    w = fwidth; 
    h = fheight; 
    while (w <= width/3)
    {
        while (h<=height)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+3*w<=width; x+=xstep) 
                for (int y=0; y+h<=height; y+=ystep)
                    count += 2; 
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }
    // (1,3) split
    w = fwidth; 
    h = fheight; 
    while (w <= width)
    {
        while (h<=height/3)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+w<=width; x+=xstep) 
                for (int y=0; y+3*h<=height; y+=ystep)
                    count += 2; 
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }

    // (2,2) split
    w = fwidth; 
    h = fheight; 
    while (w <= width/2)
    {
        while (h<=height/2)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+2*w<=width; x+=xstep) 
                for (int y=0; y+2*h<=height; y+=ystep)
                    count += 7; 
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }
    // (3,3) split 
    w = fwidth; 
    h = fheight; 
    while (w <= width/3)
    {
        while (h<=height/3)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+3*w<=width; x+=xstep) 
                for (int y=0; y+3*h<=height; y+=ystep)
                    count += 2; 
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }

#if defined(INCLUDE_RANDOM_RCFEATURES) 
    count += NUM_RANDOM_RCFEATURES; 
#endif

    // in the second pass, create the features 
    FeatureArray = new RCFEATURE [count]; 
    if (FeatureArray == NULL)
        throw "out of memory";

    int idx = 0; 

    // (2,1) split 
    w = fwidth; 
    h = fheight; 
    while (w <= width/2) 
    {
        while (h<=height)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+2*w<=width; x+=xstep) 
                for (int y=0; y+h<=height; y+=ystep)
                {
                    RCFEATURE *pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = 1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x+w, y, w, h);
                    idx ++;
                }
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }
    // (1,2) split 
    w = fwidth; 
    h = fheight; 
    while (w <= width) 
    {
        while (h<=height/2)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+w<=width; x+=xstep) 
                for (int y=0; y+2*h<=height; y+=ystep)
                {
                    RCFEATURE *pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = 1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x, y+h, w, h);
                    idx ++;
                }
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }

    // (3,1) split  
    w = fwidth; 
    h = fheight; 
    while (w <= width/3) 
    {
        while (h<=height)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+3*w<=width; x+=xstep) 
                for (int y=0; y+h<=height; y+=ystep)
                {
                    RCFEATURE *pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = -1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, 3*w, h);
                    pF->m_wRectArray[1].m_weight = 3; 
                    pF->m_wRectArray[1].m_rect.Reset(x+w, y, w, h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = -1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, w, h);
                    pF->m_wRectArray[1].m_weight = 1; 
                    pF->m_wRectArray[1].m_rect.Reset(x+2*w, y, w, h);
                    idx ++;
                }
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }
    // (1,3) split 
    w = fwidth; 
    h = fheight; 
    while (w <= width)
    {
        while (h<=height/3)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+w<=width; x+=xstep) 
                for (int y=0; y+3*h<=height; y+=ystep)
                {
                    RCFEATURE *pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = -1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, w, 3*h);
                    pF->m_wRectArray[1].m_weight = 3; 
                    pF->m_wRectArray[1].m_rect.Reset(x, y+h, w, h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = -1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, w, h);
                    pF->m_wRectArray[1].m_weight = 1; 
                    pF->m_wRectArray[1].m_rect.Reset(x, y+2*h, w, h);
                    idx ++;
                }
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }


    // (2,2) split  
    w = fwidth; 
    h = fheight; 
    while (w <= width/2) 
    {
        while (h<=height/2)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+2*w<=width; x+=xstep) 
                for (int y=0; y+2*h<=height; y+=ystep)
                {
                    RCFEATURE *pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = 1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x+w, y+h, w, h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = 1; 
                    pF->m_wRectArray[0].m_rect.Reset(x+w, y, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x, y+h, w, h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = 4; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x, y, 2*w, 2*h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = 4; 
                    pF->m_wRectArray[0].m_rect.Reset(x+w, y, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x, y, 2*w, 2*h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = 4; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y+h, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x, y, 2*w, 2*h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = 4; 
                    pF->m_wRectArray[0].m_rect.Reset(x+w, y+h, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x, y, 2*w, 2*h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 4; 
                    pF->m_wRectArray = new WEIGHTED_RECT [4]; 
                    pF->m_wRectArray[0].m_weight = 1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, w, h);
                    pF->m_wRectArray[1].m_weight = -1; 
                    pF->m_wRectArray[1].m_rect.Reset(x+w, y, w, h);
                    pF->m_wRectArray[2].m_weight = -1; 
                    pF->m_wRectArray[2].m_rect.Reset(x, y+h, w, h);
                    pF->m_wRectArray[3].m_weight = 1; 
                    pF->m_wRectArray[3].m_rect.Reset(x+w, y+h, w, h);
                    idx ++;
                }
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }

    // (3,3) split 
    w = fwidth; 
    h = fheight; 
    while (w <= width/3)
    {
        while (h<=height/3)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+3*w<=width; x+=xstep) 
                for (int y=0; y+3*h<=height; y+=ystep)
                {
                    RCFEATURE *pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = -1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y, 3*w, 3*h);
                    pF->m_wRectArray[1].m_weight = 9; 
                    pF->m_wRectArray[1].m_rect.Reset(x+w, y+h, w, h);
                    idx ++;

                    pF = &FeatureArray[idx]; 
                    pF->m_nRects = 2; 
                    pF->m_wRectArray = new WEIGHTED_RECT [2]; 
                    pF->m_wRectArray[0].m_weight = -1; 
                    pF->m_wRectArray[0].m_rect.Reset(x, y+h, 3*w, h);
                    pF->m_wRectArray[1].m_weight = 1; 
                    pF->m_wRectArray[1].m_rect.Reset(x+w, y, w, 3*h);
                    idx ++;
                }
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }

#ifndef _DETECTION_ONLY

#if defined(INCLUDE_RANDOM_RCFEATURES) 
    // generate all rectangles that can be contained in the detection window 
    w = fwidth; 
    h = fheight; 
    int rccount = 0; 
    while (w <= width) 
    {
        while (h<=height)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+w<=width; x+=xstep) 
                for (int y=0; y+h<=height; y+=ystep)
                    rccount ++; 
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }
    IRECT *pRc = new IRECT[rccount]; 
    w = fwidth; 
    h = fheight; 
    int rcidx = 0; 
    while (w <= width)
    {
        while(h<=height)
        {
            int xstep = max(int(w * fssize + 0.5),1); 
            int ystep = max(int(h * fssize + 0.5),1);
            for (int x=0; x+w<=width; x+=xstep) 
                for (int y=0; y+h<=height; y+=ystep)
                    pRc[rcidx++].Reset(x,y,w,h); 
            h = max(int(h*fsscale + 0.5), h+1); 
        }
        w = max(int(w*fsscale + 0.5), w+1); 
        h = fheight; 
    }

//    CRand cRand(0);  
    CRand cRand((int)time(NULL)); 
    for (int i=0; i<NUM_RANDOM_RCFEATURES; i++)
    {   // randomly pick two rectangles, and make it a feature
        int idx1 = cRand.IRand(rccount); 
        int idx2 = cRand.IRand(rccount); 
        while (idx2 == idx1) 
            idx2 = cRand.IRand(rccount); 
        RCFEATURE *pF = &FeatureArray[idx]; 
        pF->m_nRects = 2; 
        pF->m_wRectArray = new WEIGHTED_RECT [2]; 
        pF->m_wRectArray[0].m_weight = float(1.0/pRc[idx1].Area()); 
        pF->m_wRectArray[0].m_rect.Reset(pRc[idx1]);
        pF->m_wRectArray[1].m_weight = -float(1.0/pRc[idx2].Area()); 
        pF->m_wRectArray[1].m_rect.Reset(pRc[idx2]);
        idx ++;
    }
    delete []pRc; 
#endif

#endif

    *pCount = count; 
    return FeatureArray; 
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

void RCFEATURE::DeleteRCFeatureArray(RCFEATURE *featureArray)
{
    if (featureArray)
        delete []featureArray;
}

/******************************************************************************\
*
*   
*
\******************************************************************************/
float RCFEATURE::Eval(I_IMAGE *pIImg, float norm, int x, int y)
{
//    I_IMAGE *p = pIImg; 
    unsigned int *pData = pIImg->GetDataPtr(); 
    int nIWidth = pIImg->GetIWidth(); 
    float value = 0.0f;
    for (int i = 0; i < m_nRects; i++)
    {
        const WEIGHTED_RECT& wRect = m_wRectArray[i];
        const int x0 = x + wRect.m_rect.m_ixMin;
        const int x1 = x + wRect.m_rect.m_ixMax;
        const int y0 = y + wRect.m_rect.m_iyMin;
        const int y1 = y + wRect.m_rect.m_iyMax;
        const unsigned int v00 = pData[y0*nIWidth+x0]; 
        const unsigned int v01 = pData[y1*nIWidth+x0];
        const unsigned int v10 = pData[y0*nIWidth+x1];
        const unsigned int v11 = pData[y1*nIWidth+x1];
        //const unsigned int v00 = p->GetValue(x0, y0);
        //const unsigned int v01 = p->GetValue(x0, y1);
        //const unsigned int v10 = p->GetValue(x1, y0);
        //const unsigned int v11 = p->GetValue(x1, y1);
        const float subTotal = (float)((v11 - v01) - (v10 - v00));
        value += wRect.m_weight * subTotal;
    }

    return value*norm;
}

FEATURE::FEATURE() : 
    m_nType(UNKNOWN)
{
    m_pF.pRCF = 0; 
}

FEATURE::~FEATURE()
{
    Release(); 
}

void FEATURE::Init(const FEATURE *src, const float scale)
{
    Release(); 
    m_nType = src->m_nType; 
    switch(src->m_nType) 
    {
    case RECTFEATURE: 
        m_pF.pRCF = new RCFEATURE(src->m_pF.pRCF, scale); 
        break; 
    case NORMFEATURE:
        break; 
    }
}

void FEATURE::Init(FILE *file)
{
    Release(); 
    if (fscanf(file, "%d", &m_nType) != 1)
        throw "unexpected";
    switch(m_nType) 
    {
    case RECTFEATURE: 
        m_pF.pRCF = new RCFEATURE(); 
        m_pF.pRCF->Init(file); 
        break; 
    case NORMFEATURE:
        break; 
    }
}

void FEATURE::Write(FILE *file)
{
    fprintf(file, "%d\n", m_nType); 
    switch(m_nType) 
    {
    case RECTFEATURE: 
        m_pF.pRCF->Write(file); 
        break; 
    case NORMFEATURE:
        break; 
    }
}

void FEATURE::Release()
{
    switch (m_nType) 
    {
    case RECTFEATURE: 
        delete m_pF.pRCF; 
        break; 
    case NORMFEATURE:
        break; 
    }
    m_nType = UNKNOWN; 
}

float FEATURE::Eval(I_IMAGE *pIImg, float norm, int x, int y)
{
    switch (m_nType) 
    {
    case RECTFEATURE: 
        return m_pF.pRCF->Eval(pIImg, norm, x, y); 
    case NORMFEATURE:
        return norm; 
    }
    return 0; 
}