#pragma once

#include "DetectionResult.h"

using namespace System;

namespace DetectionManagedLib 
{
public ref class ObjectDetector abstract
{
public:
	ObjectDetector(void);

	virtual DetectionResult ^ DetectObject (String ^FileName) abstract = 0;

protected:
	int _iSignature;
};
}