#pragma once

#include "wrect.h"

using namespace System;
using namespace System::Drawing;

namespace DetectionManagedLib 
{
public ref class FeaturePts
{
public:
	FeaturePts(struct FEATUREPTS *pFeaturePts);
	
	property PointF ^ ptLeftEye
	{
		PointF ^ get()
		{
			if (_pFeaturePts == NULL)
			{
				return nullptr;
			}
			else
			{
				return PointF(_pFeaturePts->leye.x, _pFeaturePts->leye.y);
			}
		}
	}

	property PointF ^ ptRightEye
	{
		PointF ^ get()
		{
			if (_pFeaturePts == NULL)
			{
				return nullptr;
			}
			else
			{
				return PointF(_pFeaturePts->reye.x, _pFeaturePts->reye.y);
			}
		}
	}

	property PointF ^ ptLeftMouth
	{
		PointF ^ get()
		{
			if (_pFeaturePts == NULL)
			{
				return nullptr;
			}
			else
			{
				return PointF(_pFeaturePts->lmouth.x, _pFeaturePts->lmouth.y);
			}
		}
	}

	property PointF ^ ptRightMouth
	{
		PointF ^ get()
		{
			if (_pFeaturePts == NULL)
			{
				return nullptr;
			}
			else
			{
				return PointF(_pFeaturePts->rmouth.x, _pFeaturePts->rmouth.y);
			}
		}
	}

	property PointF ^ ptNose
	{
		PointF ^ get()
		{
			if (_pFeaturePts == NULL)
			{
				return nullptr;
			}
			else
			{
				return PointF(_pFeaturePts->nose.x, _pFeaturePts->nose.y);
			}
		}
	}

	void Render (Drawing::Graphics ^graf,  int x, int y);

private:
	struct FEATUREPTS *_pFeaturePts;
};
}