#pragma once

#include "ScoredRect.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Drawing;

namespace DetectionManagedLib 
{
public ref class DetectionResult
{
public:
	TimeSpan tmDetection;

	DetectionResult(int iSignature);
	DetectionResult(int iSignature, int iScaleFac);

	property int Signature
	{
		int get()
		{
			return _iSignature;
		}
	}

	List<ScoredRect ^> ^GetRawRectList (float eThreshold);
	List<ScoredRect ^> ^GetMergedRectList (float eThreshold);

	void AddRawRect (ScoredRect ^rect);
	
	void RenderRawRect (float eThreshold, Drawing::Graphics ^graf, Drawing::Rectangle ^rect);
	void RenderMergedRect (float eThreshold, Drawing::Graphics ^graf, Drawing::Rectangle ^rect);

private:
	void Init(int iSignature, int iScaleFac);

	List <ScoredRect ^> ^_rawRectList;
	int _iSignature;
	int _iScaleFac;

};
}