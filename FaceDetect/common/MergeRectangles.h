#pragma once

#include <vector>
#include <algorithm>
#include "wrect.h"

// Type for Overlapping of IRECTs.
typedef enum
{
   NONE = 0,
   HOR_OVERLAP,
   ALL_OVERLAP
} IRECT_OVERLAP;

// MED_NUM_DET_RECT can be set from MAX_NUM_DET_RECT detector.h, if wanted.
//#include "detector.h"
#define MED_NUM_DET_RECT 1000
#define REQUIRED_OVERLAP 0.4
#define MAX_DET_GROUP	 30

class CMergeRectangles
{

private :
	double	areas	[MED_NUM_DET_RECT];
	int		counts	[MED_NUM_DET_RECT];

	ID_IRECT  group[MED_NUM_DET_RECT];
	ID_IRECT* rectCollections[MED_NUM_DET_RECT][MAX_DET_GROUP];

private :
	// Compute the intersection of rc1 & rc2, and return the intersection is
	// Returns to what extent the rectangles overlap.
	IRECT_OVERLAP _IRECT_Intersect(IRECT* rc1, IRECT* rc2, IRECT* is);

	/// Checks for rectangle overlap and links the two rectangles if they do.
	void _RectangleOverlapHelper(ID_IRECT* group, int n_srcs, double requiredOverlap);

public:
	CMergeRectangles();
	~CMergeRectangles();

	/// Average the coordinates of the rectangles to yield a new rectangle.
	void AverageRectList(ID_IRECT** rectList, int n, IRECT* dst);

	//Find sets of overlapping rectangles and average each set into a single rectangle.
	//From n_srcs number of srcs, find overlapping groups.
	//Return 'n_dts number' of merged rectangles 'dsts', and associated group_ids for every srcs to find appropriate dsts.
	void MergeRectangles(IRECT** srcs, int n_srcs, 
						IRECT* dsts, int* n_dsts, 
						int* src2dst, int Max_merged_detection);
};