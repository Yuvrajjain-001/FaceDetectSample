#include "StdAfx.h"
#include "ObjectDetector.h"

using namespace System;

namespace DetectionManagedLib 
{
ObjectDetector::ObjectDetector(void)
{
	Random ^rand = gcnew Random();

	_iSignature	=	rand->Next();
}
}