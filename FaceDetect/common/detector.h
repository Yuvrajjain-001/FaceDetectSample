#pragma once

#include <float.h>
#include "classifier.h"
#include "image.h"
#include "feature.h"

#define COUNT_PRUNE_EFFECT  
#define DEFAULT_MAX_NUM_RAW_DET_RECT        1000
#define MAX_NUM_MERGE_RECT                  1000
#define REQUIRED_OVERLAP                    0.4
#define MAX_DET_GROUP	                    30

#ifndef MAX_NUM_SCALE
#define MAX_NUM_SCALE                       32
#endif


// Type for Overlapping of IRECTs.
typedef enum
{
   NONE = 0,
   HOR_OVERLAP,
   ALL_OVERLAP
} IRECT_OVERLAP;


class MERGERECT
{

private :
	double	areas	[MAX_NUM_MERGE_RECT];
	int		counts	[MAX_NUM_MERGE_RECT];

	ID_IRECT  group[MAX_NUM_MERGE_RECT];
	ID_IRECT* rectCollections[MAX_NUM_MERGE_RECT][MAX_DET_GROUP];

private :
	// Compute the intersection of rc1 & rc2, and return the intersection is
	// Returns to what extent the rectangles overlap.
	IRECT_OVERLAP _IRECT_Intersect(IRECT* rc1, IRECT* rc2, IRECT* is);

	/// Checks for rectangle overlap and links the two rectangles if they do.
	void _RectangleOverlapHelper(ID_IRECT* group, int n_srcs, double requiredOverlap);

public:
	MERGERECT();
	~MERGERECT();

	/// Average the coordinates of the rectangles to yield a new rectangle.
	void AverageRectList(ID_IRECT** rectList, int n, IRECT* dst);

	//Find sets of overlapping rectangles and average each set into a single rectangle.
	//From n_srcs number of srcs, find overlapping groups.
	//Return 'n_dts number' of merged rectangles 'dsts', and associated group_ids for every srcs to find appropriate dsts.
	void MergeRectangles(IRECT** srcs, int n_srcs, 
						IRECT* dsts, int* n_dsts, 
						int* src2dst, int Max_merged_detection);
};

/******************************************************************************\
*
*   DETECTOR
*
\******************************************************************************/

class DETECTOR 
{
public: 
    DETECTOR( const char *fileName, 
              float stepSize = 0.1f, 
              float stepScale = 1.25f,
              int maxNumRawDetRect = DEFAULT_MAX_NUM_RAW_DET_RECT, 
			  bool  record_Features = false); 

    ~DETECTOR();
    void Release(); 

private: 
    IN_IMAGE    *m_IImg;                // ptr to integral image

    int          m_nClassifiers;        // number of classifiers
    int          m_nWidth[MAX_NUM_SCALE]; 
    int          m_nHeight[MAX_NUM_SCALE]; 
    int          m_nStepW[MAX_NUM_SCALE]; 
    int          m_nStepH[MAX_NUM_SCALE]; 
    CLASSIFIER * m_ClassifierArray[MAX_NUM_SCALE]; 

    bool         m_bValid; 
    int          m_nBaseWidth;          // width of the smallest rectangle that will be scanned for a face
    int          m_nBaseHeight;         // height of the smallest rectangle that will be searched for a face
    int          m_nNumFeatureTh;       // number of thresholds for each feature 
    float        m_fFinalScoreTh;
    float        m_fStepSize;
    float        m_fStepScale;
    int          m_nMaxNumRawDetRect; 

	bool         m_record_Features;		// store_Features in detection for future Regression.

	int			 m_nTotalWindows;          // total number of scanned detection windows per image.
    int          m_nNumRawDetRect; 
    SCORED_RECT *m_pRawDetRect; 
    int          m_nNumMergedDetRect; 
    SCORED_RECT *m_pMergedDetRect; 

	float**      m_raw;					// raw and thresh filter returns.
	float**      m_thresh;

    bool Classify (IRECT *rc, int nScale, float *score); 
    //bool ClassifyWithFeatures (IRECT *rc, int nScale, float *score, 
				//			   float* raw, float* thresh); 
    void SetPruneMinPosThreshold (IRECT *rc, int nScale); 
    bool MergeRawDetRect(); 

	bool     m_bRejAtNodes;

#if defined(COUNT_PRUNE_EFFECT)
    __int64 *m_pnPruneCount; 
#endif 

public:
    float GetFinalScoreTh()		{ return m_fFinalScoreTh; }; 
    void  SetFinalScoreTh(float th) { m_fFinalScoreTh = th; }; 
    int   GetNumClassifiers()	{ return m_nClassifiers; }; 
	int   GetTotalWindows()		{ return m_nTotalWindows; };

	void     SetReject(bool rej) { m_bRejAtNodes = rej; };

#if defined(COUNT_PRUNE_EFFECT)
    __int64 *GetPruneCount()	{ return m_pnPruneCount; }; 
#endif

    // the return value is the number of rectangles detected, up to MAX_NUM_DET_RECT
    void DetectObject (IN_IMAGE* pIImg, int minScale=0, int maxScale=MAX_NUM_SCALE-1);
	// Additionally allocate memory to store the computed feature values for all detected faces.	
	//void DetectObjectWithFeatures (I_IMAGE* pIImg, int minScale=0, int maxScale=MAX_NUM_SCALE-1);
    int	 GetDetResults(SCORED_RECT **ppRc, bool merged);
	// threshold values first, raw values later for det_i'th detection
	//void GetFeatureValues(float *values, int det_i);
    bool IsValid() { return m_bValid; }; 

    void SaveNewClassifier(char *fileName); 
};

