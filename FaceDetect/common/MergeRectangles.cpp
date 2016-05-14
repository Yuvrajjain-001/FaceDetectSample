#include "MergeRectangles.h"

// Constructor
CMergeRectangles::CMergeRectangles() {}

// Destructor
CMergeRectangles::~CMergeRectangles() {}


/// Average the coordinates of the rectangles to yield a new rectangle.
void CMergeRectangles::AverageRectList(ID_IRECT** rectList, int n, IRECT* dst) 
{
    float xMin   = 0.0f;
    float xMax	 = 0.0f;
    float yMin   = 0.0f;
    float yMax	 = 0.0f;

	for(int i = 0; i < n; i++)
	{
		xMin	+= rectList[i]->rc->m_xMin;
		xMax	+= rectList[i]->rc->m_xMax;
		yMin	+= rectList[i]->rc->m_yMin;
		yMax	+= rectList[i]->rc->m_yMax;
	}

	dst->m_xMin = (int) (xMin / n + 0.5);
	dst->m_xMax = (int) (xMax / n + 0.5);
	dst->m_yMin = (int) (yMin / n + 0.5);
	dst->m_yMax = (int) (yMax / n + 0.5);

}



// Compute the intersection of rc1 & rc2, and return the intersection is
IRECT_OVERLAP CMergeRectangles::_IRECT_Intersect(IRECT* rc1, IRECT* rc2, IRECT* is)
{
	is -> m_xMin = max(rc1 -> m_xMin, rc2 -> m_xMin);
	is -> m_xMax = min(rc1 -> m_xMax, rc2 -> m_xMax);

	if((is -> m_xMin) > (is -> m_xMax)) return NONE;

	is -> m_yMin = max(rc1 -> m_yMin, rc2 -> m_yMin);
	is -> m_yMax = min(rc1 -> m_yMax, rc2 -> m_yMax);

	if((is -> m_yMin) > (is -> m_yMax)) return HOR_OVERLAP;

	return ALL_OVERLAP;
}



/// Checks for rectangle overlap and links the two rectangles if they do.
void CMergeRectangles::_RectangleOverlapHelper(ID_IRECT* group, int n_srcs, double requiredOverlap)
{
    // Faster rectangle overlap detectors.  First checks for horizontal overlap...  if that exists 
    // tests for vertical.  Horizontal overlap can be computed in nlogn + kn where n is the number of 
    // rectangles and K is the maximal number of overlapping rectangles.

	int i;
	std::sort(group, group + n_srcs);

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
void CMergeRectangles::MergeRectangles(IRECT** srcs, int n_srcs, 
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

