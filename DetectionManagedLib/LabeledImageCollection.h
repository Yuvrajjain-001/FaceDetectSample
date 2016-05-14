#pragma once

#include "imageinfo.h"
#include "LabeledImg.h"

using namespace System;
using namespace System::Collections::Generic;


namespace DetectionManagedLib 
{
public ref class LabeledImageCollection
{
public:
	LabeledImageCollection(String ^strFileName);

	property  List<LabeledImg ^> ^ ImgList 
	{ 
		List<LabeledImg ^> ^ get()
		{
			return _LabeledImgList; 
		}
	}

private:
	List<LabeledImg ^> ^ _LabeledImgList;
};
}