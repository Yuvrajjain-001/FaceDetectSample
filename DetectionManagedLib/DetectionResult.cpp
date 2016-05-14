#include "StdAfx.h"
#include "Detector.h"
#include "DetectionResult.h"

using namespace System::Collections::Generic;
using namespace System::Drawing;

namespace DetectionManagedLib 
{
DetectionResult::DetectionResult(int iSignature)
{
	Init(iSignature, -1);
}
DetectionResult::DetectionResult(int iSignature, int iScaleFac)
: _iSignature (iSignature)
{
	Init(iSignature, iScaleFac);
	
}

void DetectionResult::Init(int iSignature, int iScaleFac)
{
	_iSignature = iSignature;
	_iScaleFac = iScaleFac;
	_rawRectList =	nullptr;
}
void DetectionResult::AddRawRect (ScoredRect ^rect)
{
	if (_rawRectList == nullptr)
	{
		_rawRectList = gcnew List<ScoredRect ^> ();
	}

	_rawRectList->Add (rect);
}
	
void DetectionResult::RenderRawRect (float eThreshold, Drawing::Graphics ^graf, Drawing::Rectangle ^rect)
{
	List<ScoredRect ^> ^rawRectList = GetRawRectList(eThreshold);

	if (rawRectList != nullptr)
	{
		for each (ScoredRect ^scoredRect in rawRectList)
		{
			scoredRect->Render (graf, rect, false);
		}
	}
}

void DetectionResult::RenderMergedRect (float eThreshold, Drawing::Graphics ^graf, Drawing::Rectangle ^rect)
{
	List<ScoredRect ^> ^mergedRectList = GetMergedRectList(eThreshold);

	if (mergedRectList != nullptr)
	{
		for each (ScoredRect ^scoredRect in mergedRectList)
		{
			scoredRect->Render (graf, rect, false);
		}
	}
}

List<ScoredRect ^> ^ DetectionResult::GetRawRectList (float eThreshold)
{
	// create an empty list
	List<ScoredRect ^> ^rawRectList = gcnew List<ScoredRect ^> ();

	// add every rectangle whose score exceeds the thresold
	if (_rawRectList != nullptr)
	{
		for each (ScoredRect ^scoredRect in _rawRectList)
		{
			if (scoredRect->Score > eThreshold)
			{
				rawRectList->Add (scoredRect);
			}
		}
	}

	return rawRectList;
}

List<ScoredRect ^> ^ DetectionResult::GetMergedRectList (float eThreshold)
{
	// get the raw rect list
	List<ScoredRect ^> ^rawRectList = GetRawRectList(eThreshold);

	// create an empty merged list
	List<ScoredRect ^> ^mergedRectList = gcnew List<ScoredRect ^> ();

	// merge the raw rectangles
	if (rawRectList->Count > 0)
	{
		MERGERECT mergeRect;

		IRECT	*pSrcRc[MAX_NUM_MERGE_RECT],
				pDstRc[MAX_NUM_MERGE_RECT];

		int		Src2Dst[MAX_NUM_MERGE_RECT],
				cRawRect = rawRectList->Count,
				iRect,
				cMergedRect;

		iRect = 0;

		for each (ScoredRect ^scoredRect in rawRectList)
		{
			pSrcRc[iRect++] = scoredRect->IRECT_ptr;
		}

		mergeRect.MergeRectangles(pSrcRc, cRawRect, pDstRc, &cMergedRect, Src2Dst, cRawRect);

		for(iRect = 0; iRect < cMergedRect; iRect++) 
		{
			SCORED_RECT	*pScoredRect = new SCORED_RECT();

			if (_iScaleFac > 1)
			{
				pDstRc[iRect].m_ixMin *= _iScaleFac;
				pDstRc[iRect].m_ixMax *= _iScaleFac;
				pDstRc[iRect].m_iyMin *= _iScaleFac;
				pDstRc[iRect].m_iyMax *= _iScaleFac;
			}

			pScoredRect->m_rect = pDstRc[iRect];
			pScoredRect->m_score = 1.0f;

			mergedRectList->Add(gcnew ScoredRect(pScoredRect, ScoredRect::RectType::Merged));
		}
	}

	return mergedRectList;
}

}