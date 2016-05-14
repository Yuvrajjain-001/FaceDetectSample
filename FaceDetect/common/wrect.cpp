/******************************************************************************\
*
*   Member functions for the WEIGHTED_RECT and IRECT classes.
*
\******************************************************************************/

#include "stdafx.h"
#include "wrect.h"

/******************************************************************************\
*
*   Fills out the members of a weighted rectangle by reading them from a
*   stream. We expect the text stream to supply stuff in the following
*   format:
*
*   WRECT := int float int int int int
*
\******************************************************************************/

void WEIGHTED_RECT::Init(FILE *file)
{
    float weight;
    int nResult = 
        fscanf(file, 
            "%f %d %d %d %d", 
            &weight, 
            &(m_rect.m_ixMin),
            &(m_rect.m_iyMin),
            &(m_rect.m_ixMax),
            &(m_rect.m_iyMax)
            );

    m_weight = weight;

    ASSERT(nResult == 5);
    ASSERT(m_rect.m_ixMin <= m_rect.m_ixMax);
    ASSERT(m_rect.m_iyMin <= m_rect.m_iyMax);
}

void WEIGHTED_RECT::Write(FILE *file)
{
    fprintf(file, "%f\n%d %d %d %d\n", 
        m_weight, 
        m_rect.m_ixMin, 
        m_rect.m_iyMin, 
        m_rect.m_ixMax, 
        m_rect.m_iyMax); 
}

void MapFPts2Rc(FEATUREPTS *pPts, IRECT *pRc)
{
/* old code for computing the face rectangle 
    float xctr = (pPts->leye.x + pPts->reye.x)/2.0f; 
    float yEyectr = (pPts->leye.y + pPts->reye.y)/2.0f; 
    float yctr = pPts->nose.y; 
    float size = max((pPts->reye.x - pPts->leye.x)*1.8f, (yctr-yEyectr)*2.4f); 
    pRc->Reset(xctr-size/2, yctr-size/2, size, size); 
*/

#if MAP_METHOD == MAP_FRONTAL

    float xctr = (pPts->leye.x + pPts->reye.x)/2.0f;
    float yctr = (pPts->leye.y + 
                  pPts->reye.y +
                  pPts->lmouth.y + 
                  pPts->rmouth.y) / 4.0f;
    float size1 = abs((pPts->leye.y + pPts->reye.y)/2.0f - yctr)*4.0f; 
    float size2 = abs(pPts->reye.x - xctr)*4.0f; 
    float size = max(size1, size2); 
    pRc->Reset(xctr-size/2, yctr-size/2, size, size); 

#else

    // new method based on Paul's suggestion 
    float xMean, yMean, xVar, yVar; 
    // including nose
    //xMean = (pPts->leye.x + 
    //         pPts->reye.x +
    //         pPts->nose.x + 
    //         pPts->lmouth.x + 
    //         pPts->rmouth.x) / 5.0f; 
    //yMean = (pPts->leye.y + 
    //         pPts->reye.y +
    //         pPts->nose.y + 
    //         pPts->lmouth.y + 
    //         pPts->rmouth.y) / 5.0f; 
    //xVar  = (pPts->leye.x * pPts->leye.x + 
    //         pPts->reye.x * pPts->reye.x +
    //         pPts->nose.x * pPts->nose.x + 
    //         pPts->lmouth.x * pPts->lmouth.x + 
    //         pPts->rmouth.x * pPts->rmouth.x) / 5.0f - xMean*xMean; 
    //yVar  = (pPts->leye.y * pPts->leye.y + 
    //         pPts->reye.y * pPts->reye.y +
    //         pPts->nose.y * pPts->nose.y + 
    //         pPts->lmouth.y * pPts->lmouth.y + 
    //         pPts->rmouth.y * pPts->rmouth.y) / 5.0f - yMean*yMean; 
    xMean = (pPts->leye.x + 
             pPts->reye.x +
             pPts->lmouth.x + 
             pPts->rmouth.x) / 4.0f; 
    yMean = (pPts->leye.y + 
             pPts->reye.y +
             pPts->nose.y + 
             pPts->lmouth.y + 
             pPts->rmouth.y) / 5.0f; 
    xVar  = (pPts->leye.x * pPts->leye.x + 
             pPts->reye.x * pPts->reye.x +
             pPts->lmouth.x * pPts->lmouth.x + 
             pPts->rmouth.x * pPts->rmouth.x) / 4.0f - xMean*xMean; 
    yVar  = ((pPts->leye.y - yMean) * (pPts->leye.y  - yMean) + 
             (pPts->reye.y - yMean) * (pPts->reye.y  - yMean) +
             (pPts->lmouth.y - yMean) * (pPts->lmouth.y - yMean) + 
             (pPts->rmouth.y - yMean) * (pPts->rmouth.y - yMean)) / 4.0f; 

    float w = 2.25f * sqrt(xVar); 
    float h = 2.25f * sqrt(yVar); 
    float WH = 2.0f * max(w,h); 
    pRc->Reset(xMean-WH/2, yMean-WH/2, WH, WH); 

#endif
}

