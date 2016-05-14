/******************************************************************************\
*
*   Member functions for the DETECTOR class
*
\******************************************************************************/

#include "stdafx.h"
#include <windows.h>
#include <math.h>

#ifndef _DETECTION_ONLY
#include <vector>
#include <algorithm>
#endif

#include "detector.h"


/******************************************************************************\
*
*
*
\******************************************************************************/
// Constructor
MERGERECT::MERGERECT() {}

// Destructor
MERGERECT::~MERGERECT() {}


/// Average the coordinates of the rectangles to yield a new rectangle.
void MERGERECT::AverageRectList(ID_IRECT** rectList, int n, IRECT* dst) 
{
    float xMin   = 0.0f;
    float xMax	 = 0.0f;
    float yMin   = 0.0f;
    float yMax	 = 0.0f;

	for(int i = 0; i < n; i++)
	{
		xMin	+= rectList[i]->rc->m_ixMin;
		xMax	+= rectList[i]->rc->m_ixMax;
		yMin	+= rectList[i]->rc->m_iyMin;
		yMax	+= rectList[i]->rc->m_iyMax;
	}

	dst->m_ixMin = (int) (xMin / n + 0.5);
	dst->m_ixMax = (int) (xMax / n + 0.5);
	dst->m_iyMin = (int) (yMin / n + 0.5);
	dst->m_iyMax = (int) (yMax / n + 0.5);
}



// Compute the intersection of rc1 & rc2, and return the intersection is
IRECT_OVERLAP MERGERECT::_IRECT_Intersect(IRECT* rc1, IRECT* rc2, IRECT* is)
{
	is -> m_ixMin = max(rc1 -> m_ixMin, rc2 -> m_ixMin);
	is -> m_ixMax = min(rc1 -> m_ixMax, rc2 -> m_ixMax);

	if((is -> m_ixMin) > (is -> m_ixMax)) return NONE;

	is -> m_iyMin = max(rc1 -> m_iyMin, rc2 -> m_iyMin);
	is -> m_iyMax = min(rc1 -> m_iyMax, rc2 -> m_iyMax);

	if((is -> m_iyMin) > (is -> m_iyMax)) return HOR_OVERLAP;

	return ALL_OVERLAP;
}

int compare_idirect(const void *arg1, const void *arg2)
{
    return (((const ID_IRECT *)arg1)->rc->m_ixMin > ((const ID_IRECT *)arg2)->rc->m_ixMin) ? 
        1 : ((((const ID_IRECT *)arg1)->rc->m_ixMin < ((const ID_IRECT *)arg2)->rc->m_ixMin) ? -1 : 0);
}

/// Checks for rectangle overlap and links the two rectangles if they do.
void MERGERECT::_RectangleOverlapHelper(ID_IRECT* group, int n_srcs, double requiredOverlap)
{
    // Faster rectangle overlap detectors.  First checks for horizontal overlap...  if that exists 
    // tests for vertical.  Horizontal overlap can be computed in nlogn + kn where n is the number of 
    // rectangles and K is the maximal number of overlapping rectangles.

    qsort(group, n_srcs, sizeof(ID_IRECT), compare_idirect); 

	for(int i = 0; i < n_srcs; i++)
	{
		group[i].sid = i;
		group[i].group_id = i;
	}


	// Compute areas of every rectangle.
	for(int i = 0; i < n_srcs; i++)
		areas[i] = group[i].rc -> Area();

	// Loop from left to right.
	IRECT is;
	IRECT_OVERLAP  overlap;
	double iarea;
	for(int i = 0; i < n_srcs; i++)
	{
		for(int j = i+1; j < n_srcs; j++)
		{
			overlap = _IRECT_Intersect(group[i].rc, group[j].rc, &is);
			if (overlap == NONE) break;
			if (overlap == HOR_OVERLAP) continue;



			// If sufficient areas intersect, Setup a pointer form j -> i 
			iarea = is.Area();


			if( (2.0 * iarea / (areas[i]+areas[j]) ) > requiredOverlap)
			{
				if(group[i].group_id > group[j].group_id)
					group[i].group_id = group[j].group_id;
				else
					group[j].group_id = group[i].group_id;
			}
		}
	}
}



// Find sets of overlapping rectangles and average each set into a single rectangle.
// From n_srcs number of srcs, find overlapping groups.
// Return 'n_dts number' of merged rectangles 'dsts', and associated group_ids for every srcs to find appropriate dsts.
void MERGERECT::MergeRectangles(IRECT** srcs, int n_srcs, 
										IRECT* dsts, int* n_dsts, 
										int* src2dst, int Max_merged_detection)
{
    // Basically a union find type of algorithm.  Initially each rectangle is
    // in its own group -> groupId[i] = i;
	//ID_IRECT* group = new ID_IRECT [n_srcs];
	for(int i = 0; i < n_srcs; i++)
	{
		group[i].id = i;
		group[i].rc = srcs[i];
	}

	// For each pair of overlapping rectangles arrange that the group
    // id of the pair is the same.  By convention the smaller of the two.
    // This amounts to a pointer from one rectangle to the one earlier in
    // the list.
	// NOTE that group is to be sorted, and original ordering will not be preserved.
	_RectangleOverlapHelper(group, n_srcs, REQUIRED_OVERLAP);

    // Loop over rectangles to find the best ID for each group.
	int sid;
	for(int i = 0; i < n_srcs; i++)	counts[i] = 0;

	for(int i = 0; i < n_srcs; i++)
	{
		sid = group[i].group_id;
        // The id is the true group id of the example refers to itself.  If
        // not follow the pointer until the example refs to itself.
		while (sid != group[sid].group_id)
			sid = group[sid].group_id;
	
		group[i].group_id = sid;

		rectCollections[sid][counts[sid]] = &group[i];
		counts[sid]++;
	}

	// compute merged rectnagles and return n_dsts & src2dst mapping.
	int temp_n_dts = 0;
	for(int i = 0; i < n_srcs; i++)
	{
		if( counts[i] > 0) 
		{
			AverageRectList(rectCollections[i], counts[i], &dsts[temp_n_dts]);

			for(int j = 0; j < counts[i]; j++)
			{
				src2dst[ rectCollections[i][j]->id ] = temp_n_dts;				
			}
			temp_n_dts++;
		}

		if(temp_n_dts == Max_merged_detection) break;
	}
	*(n_dsts) = temp_n_dts;
}


/******************************************************************************\
*
*
*
\******************************************************************************/

DETECTOR::DETECTOR(const char *fileName, 
                   float stepSize,
                   float stepScale,
                   int maxNumRawDetRect,
				   bool record_Features)
{
	m_bRejAtNodes = true;
    m_bValid = false; 
    m_nClassifiers = 0; 
    m_fStepSize = stepSize; 
    m_fStepScale = stepScale; 

    m_nMaxNumRawDetRect = maxNumRawDetRect; 
    m_pRawDetRect = new SCORED_RECT [maxNumRawDetRect]; 
    m_pMergedDetRect = new SCORED_RECT [maxNumRawDetRect]; 
    if (!m_pRawDetRect || !m_pMergedDetRect)
        throw "out of memory"; 

    CLASSIFIER *pOriClassifiers = CLASSIFIER::ReadClassifierFile(&m_nClassifiers, 
                                                                 &m_nBaseWidth,  
                                                                 &m_nBaseHeight, 
                                                                 &m_nNumFeatureTh, 
                                                                 &m_fFinalScoreTh, 
                                                                 fileName); 

    if (pOriClassifiers)
    {
        for (int i=0; i<MAX_NUM_SCALE; i++) 
        {
            float scale = pow(m_fStepScale, i); 
            m_nWidth[i] = int(m_nBaseWidth * scale + 0.5); 
            m_nHeight[i] = int(m_nBaseHeight * scale + 0.5); 
            m_nStepW[i] = int(m_nWidth[i] * m_fStepSize + 0.5); 
            m_nStepH[i] = int(m_nHeight[i] * m_fStepSize + 0.5); 
            m_ClassifierArray[i] = NULL; 
            m_ClassifierArray[i] = CLASSIFIER::CreateScaledClassifierArray(pOriClassifiers, m_nClassifiers, scale); 
            if (!m_ClassifierArray[i])
                throw "out of memory"; 
        }
        CLASSIFIER::DeleteClassifierArray(pOriClassifiers); 
        m_bValid = true; 
    }
    else
        throw "out of memory"; 

#if defined(COUNT_PRUNE_EFFECT)
    m_pnPruneCount = new __int64 [m_nClassifiers]; 
    for (int i=0; i<m_nClassifiers; i++) 
        m_pnPruneCount[i] = 0; 
#endif

	// if(record_Features) pre-allocate memory.
	m_record_Features = record_Features;
	if(m_record_Features)
	{
		m_raw       = new float* [maxNumRawDetRect];
		m_thresh    = new float* [maxNumRawDetRect];
		m_raw   [0] = new float  [m_nClassifiers * maxNumRawDetRect];
		m_thresh[0] = new float  [m_nClassifiers * maxNumRawDetRect];
		for (int i = 1; i <maxNumRawDetRect; i++)
		{
			m_raw   [i] = m_raw   [i-1] + m_nClassifiers;
			m_thresh[i] = m_thresh[i-1] + m_nClassifiers;
		}
	}
	else
		m_raw = m_thresh = NULL;
}

DETECTOR::~DETECTOR()
{
    Release(); 
}

void DETECTOR::Release()
{
    m_bValid = false; 
    if (m_pRawDetRect) { delete []m_pRawDetRect; m_pRawDetRect = NULL; }
    if (m_pMergedDetRect) { delete []m_pMergedDetRect; m_pMergedDetRect = NULL; }

    for (int i=0; i<MAX_NUM_SCALE; i++) 
        CLASSIFIER::DeleteClassifierArray(m_ClassifierArray[i]);

#if defined(COUNT_PRUNE_EFFECT)
    if (m_pnPruneCount) {delete []m_pnPruneCount; m_pnPruneCount = NULL; } 
#endif

	if(m_record_Features)
	{
		delete m_raw[0];
		delete m_raw;
		delete m_thresh[0];
		delete m_thresh;

	}
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

bool DETECTOR::Classify (IRECT *rc, int nScale, float *score)
{
    ASSERT (nScale >= 0 && nScale < MAX_NUM_SCALE); 

    float value, wScore = 0.0f;
    CLASSIFIER * pC = m_ClassifierArray[nScale]; 
    int i, j; 

#if defined(COUNT_PRUNE_EFFECT)
    bool bPruned = false; 
#endif

    float norm = m_IImg->ComputeNorm(rc); 
    for (i=0; i<m_nClassifiers; i++) 
    {
        switch(pC[i].m_Feature.m_nType) 
        {
        case FEATURE::RECTFEATURE:
            value = pC[i].m_Feature.m_pF.pRCF->Eval(m_IImg, norm, rc->m_ixMin, rc->m_iyMin); 
            break; 
        case FEATURE::NORMFEATURE: 
            value = pC[i].m_Feature.Eval(m_IImg, norm); 
            break; 
        default:
            throw "Unknown feature"; 
        }

        float *pTh = pC[i].GetFeatureTh(); 
        float *pScore = pC[i].GetDScore(); 

        // knowing the classifier uses 7 thresholds, the following code could speed up the detector by around 5%. 
        //if (value > pTh[3]) 
        //{
        //    if (value>pTh[1]) 
        //    {
        //        if (value>pTh[0]) 
        //            wScore += pScore[0]; 
        //        else
        //            wScore += pScore[1]; 
        //    }
        //    else
        //    {
        //        if (value > pTh[2]) 
        //            wScore += pScore[2]; 
        //        else
        //            wScore += pScore[3]; 
        //    }
        //}
        //else
        //{
        //    if (value>pTh[5]) 
        //    {
        //        if (value>pTh[4]) 
        //            wScore += pScore[4]; 
        //        else
        //            wScore += pScore[5]; 
        //    }
        //    else
        //    {
        //        if (value > pTh[6]) 
        //            wScore += pScore[6]; 
        //        else
        //            wScore += pScore[7]; 
        //    }
        //}

        for (j=0; j<pC[i].m_nNumTh; j++) 
        {
            if (value > pTh[j]) 
            {
                wScore += pScore[j]; 
                break;
            }
        }
        if (j == pC[i].m_nNumTh) wScore += pScore[j]; 
        if (m_bRejAtNodes && wScore < pC[i].GetMinPosScoreTh()) 
        {
#if defined(COUNT_PRUNE_EFFECT)
            bPruned = true; 
            m_pnPruneCount[i] += 1; 
#endif
            break; 
        }
    }

#if defined(COUNT_PRUNE_EFFECT)
    if (!bPruned)
        m_pnPruneCount[m_nClassifiers-1] += 1; 
#endif

    *score = wScore; 

    return (i==m_nClassifiers) && (wScore > m_fFinalScoreTh); 
}


//bool DETECTOR::ClassifyWithFeatures (IRECT *rc, int nScale, float *score,
//									 float* raw, float* thresh)
//{
//    ASSERT (nScale >= 0 && nScale < MAX_NUM_SCALE); 
//
//    float value, thresh_i, wScore = 0.0f;
//    CLASSIFIER * pC = m_ClassifierArray[nScale]; 
//    int i; 
//
//#if defined(COUNT_PRUNE_EFFECT)
//    bool bPruned = false; 
//#endif
//
//    for (i=0; i<m_nClassifiers; i++) 
//    {
//        switch(pC[i].m_Feature.m_nType) 
//        {
//        case FEATURE::RECTFEATURE:
//            value = pC[i].m_Feature.m_pF.pRCF->Eval(m_IImg, rc->m_ixMin, rc->m_iyMin); 
//			raw [i] = value;
//            break; 
//        default:
//            throw "Unknown feature"; 
//        }
//
//		thresh_i = pC[i].GetThreshold();
//        wScore  += (value > thresh_i? pC[i].GetAlpha() : pC[i].GetBeta()); 
//
//		thresh[i]= (value > thresh_i? 1.0f : 0.0f);
//
//#if defined(COUNT_PRUNE_EFFECT)
//        if (wScore < pC[i].GetMinPosScoreTh()) 
//        {
//            bPruned = true; 
//            m_pnPruneCount[i] += 1; 
//            break; 
//        }
//#endif
//    }
//
//#if defined(COUNT_PRUNE_EFFECT)
//    if (!bPruned)
//        m_pnPruneCount[m_nClassifiers-1] += 1; 
//#endif
//
//    *score = wScore; 
//
//    return (i==m_nClassifiers) && (wScore > m_fFinalScoreTh); 
//}


void DETECTOR::SetPruneMinPosThreshold (IRECT *rc, int nScale)
{
    ASSERT (nScale >= 0 && nScale < MAX_NUM_SCALE); 

    float value, wScore = 0.0f;
    float norm = m_IImg->ComputeNorm(rc); 
    CLASSIFIER * pC = m_ClassifierArray[nScale]; 
    int i, j; 
    bool bPruned = false; 
    for (i=0; i<m_nClassifiers; i++) 
    {
        switch(pC[i].m_Feature.m_nType) 
        {
        case FEATURE::RECTFEATURE:
            value = pC[i].m_Feature.m_pF.pRCF->Eval(m_IImg, norm, rc->m_ixMin, rc->m_iyMin); 
            break; 
        default:
            throw "Unknown feature"; 
        }

        float *pTh = pC[i].GetFeatureTh(); 
        float *pScore = pC[i].GetDScore(); 
        for (j=0; j<pC[i].m_nNumTh; j++) 
        {
            if (value > pTh[j]) 
            {
                wScore += pScore[j]; 
                break;
            }
        }
        if (j == pC[i].m_nNumTh) wScore += pScore[j]; 
        // since pC[i].GetMinPosScoreTh() is the same as m_ClassifierArray[0]->GetMinPosScoreTh(), 
        // we only change the threshold value for m_ClassifierArray[0]
        if (wScore < m_ClassifierArray[0][i].GetMinPosScoreTh()) 
            m_ClassifierArray[0][i].SetMinPosScoreTh(wScore-1e-5f);
    }
}

void DETECTOR::DetectObject (IN_IMAGE* pIImg, int minScale, int maxScale)
{
    ASSERT(m_bValid); 
    if (minScale < 0 || maxScale >= MAX_NUM_SCALE || minScale > maxScale)
        throw "scale out of range"; 

    // copy the pointers 
    m_IImg = pIImg; 

    int width = pIImg->GetWidth(); 
    int height = pIImg->GetHeight(); 
    m_nNumRawDetRect = 0; 

    bool bCont = true; 
	m_nTotalWindows = 0;
    for (int nScale = minScale; nScale <= maxScale && bCont; nScale++) 
    {
        IRECT rect (0, m_nWidth[nScale], 0, m_nHeight[nScale]); 
        while (rect.m_iyMax <= height && bCont) 
        {
            while (rect.m_ixMax <= width  && bCont) 
            {
                float score; 
				m_nTotalWindows ++;
                if (Classify(&rect, nScale, &score)) 
                {
                    m_pRawDetRect[m_nNumRawDetRect].m_rect.Reset((float)rect.m_ixMin, (float)rect.m_iyMin, 
                        (float)(rect.m_ixMax-rect.m_ixMin), (float)(rect.m_iyMax-rect.m_iyMin)); 
                    m_pRawDetRect[m_nNumRawDetRect++].m_score = score; 
                    if (m_nNumRawDetRect >= m_nMaxNumRawDetRect) 
                        bCont = false; 
                }
                rect.m_ixMin += m_nStepW[nScale]; 
                rect.m_ixMax = rect.m_ixMin + m_nWidth[nScale]; 
            }
            rect.m_iyMin += m_nStepH[nScale]; 
            rect.m_iyMax = rect.m_iyMin + m_nHeight[nScale]; 
            rect.m_ixMin = 0; 
            rect.m_ixMax = m_nWidth[nScale]; 
        }
    }

    MergeRawDetRect(); 
}

// Only differs from the function above in using ClasifyWithFeatures.
//void DETECTOR::DetectObjectWithFeatures (I_IMAGE* pIImg, int minScale, int maxScale)
//{
//    ASSERT(m_bValid); 
//    if (minScale < 0 || maxScale >= MAX_NUM_SCALE || minScale > maxScale)
//        throw "scale out of range"; 
//
//    // copy the pointers 
//    m_IImg = pIImg; 
//
//    int width = pIImg->GetWidth(); 
//    int height = pIImg->GetHeight(); 
//    m_nNumRawDetRect = 0; 
//
//    bool bCont = true; 
//	m_nTotalWindows = 0;
//    for (int nScale = minScale; nScale <= maxScale && bCont; nScale++) 
//    {
//        IRECT rect (0, m_nWidth[nScale], 0, m_nHeight[nScale]); 
//        while (rect.m_iyMax <= height && bCont) 
//        {
//            while (rect.m_ixMax <= width && bCont) 
//            {
//                float score; 
//				m_nTotalWindows++;
//				if (ClassifyWithFeatures(&rect, nScale, &score, 
//										 &m_raw    [m_nNumRawDetRect][0], 
//										 &m_thresh [m_nNumRawDetRect][0]))
//                {
//
//                    m_RawDetRect[m_nNumRawDetRect].m_rect.Reset((float)rect.m_ixMin, (float)rect.m_iyMin, 
//                        (float)(rect.m_ixMax-rect.m_ixMin), (float)(rect.m_iyMax-rect.m_iyMin)); 
//                    m_RawDetRect[m_nNumRawDetRect++].m_score = score; 
//                    if (m_nNumRawDetRect >= MAX_NUM_DET_RECT) 
//                        bCont = false; 
//                }
//                rect.m_ixMin += m_nStepW[nScale]; 
//                rect.m_ixMax = rect.m_ixMin + m_nWidth[nScale]; 
//            }
//            rect.m_iyMin += m_nStepH[nScale]; 
//            rect.m_iyMax = rect.m_iyMin + m_nHeight[nScale]; 
//            rect.m_ixMin = 0; 
//            rect.m_ixMax = m_nWidth[nScale]; 
//        }
//    }
//	//    MergeRawDetRect(width); 
//}

int compare_int( const void *arg1, const void *arg2 )
{
    return (*((const int *)arg1) > *((const int *)arg2)) ? 
            1 : ((*((const int *)arg1) < *((const int *)arg2)) ? -1 : 0); 
}

bool DETECTOR::MergeRawDetRect()
{
    if (m_nNumRawDetRect == 0) 
    {
        m_nNumMergedDetRect = 0; 
        return false; 
    }

	if (m_nNumRawDetRect >= MAX_NUM_MERGE_RECT) 
    {
		m_nNumMergedDetRect = m_nNumRawDetRect; 
		for (int i=0; i<m_nNumMergedDetRect; i++) 
			m_pMergedDetRect[i] = m_pRawDetRect[i]; 
		return false;
	}

	IRECT *pSrcRc[MAX_NUM_MERGE_RECT];
	IRECT pDstRc[MAX_NUM_MERGE_RECT];
	int Src2Dst[MAX_NUM_MERGE_RECT];

	for(int i=0; i<m_nNumRawDetRect; i++)
		pSrcRc[i]=&(m_pRawDetRect[i].m_rect);

	MERGERECT mergeRect;
	mergeRect.MergeRectangles(pSrcRc, m_nNumRawDetRect, pDstRc, &m_nNumMergedDetRect, Src2Dst, m_nNumRawDetRect);
	for(int i=0; i<m_nNumMergedDetRect; i++) 
    {
		m_pMergedDetRect[i].m_rect=pDstRc[i];
        m_pMergedDetRect[i].m_score = 1.0f; 
    }
    return true; 
}

int DETECTOR::GetDetResults(SCORED_RECT **ppRc, bool merged)
{
    if (merged) 
    {
        *ppRc = m_pMergedDetRect; 
        return m_nNumMergedDetRect; 
    }
    else
    {
        *ppRc = m_pRawDetRect; 
        return m_nNumRawDetRect; 
    }
}

// threshold values first, raw values later for det_i'th detection
//void DETECTOR::GetFeatureValues(float *values, int det_i)
//{
//	for(int i = 0; i < m_nClassifiers; i++)
//	{
//		values[i]                = m_thresh[det_i][i];
//		values[i+m_nClassifiers] = m_raw   [det_i][i]; 
//	}
//}

void DETECTOR::SaveNewClassifier(char *fileName)
{
    CLASSIFIER::WriteClassifierFile(m_ClassifierArray[0], m_nClassifiers, m_nBaseWidth, m_nBaseHeight, m_nNumFeatureTh, m_fFinalScoreTh, fileName); 
}
