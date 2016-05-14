#pragma once
#include "Detector.h"
#include "ObjectDetector.h"
#include < vcclr.h >

using namespace System;

namespace DetectionManagedLib 
{
public ref class FaceDetector : ObjectDetector
{
public:
	FaceDetector(String ^strFileName, bool fPrune, float eMinThresh);
	~FaceDetector();

	virtual DetectionResult ^ DetectObject (String ^FileName) override;
	DetectionResult ^ DetectObject (const char *pszName);
	DetectionResult ^ DetectObject(System::Drawing::Imaging::BitmapData ^bitMapData);
	//DetectionResult ^ DetectObject(int width, int height, int stride, int bytePerPix, array<System::Byte> ^pixData);

	bool Compatible (DetectionResult ^detRes);

	void SetPrune (bool fPrune);

	void SetTargetDimension(int targetWidth,  int targetHeight);

	property float DefaultThreshold
	{
		float get()
		{
			if (_pDetector == NULL)
			{
				return 0.0f;
			}
			else
			{
				return _eDefaultThreshold;
			}
		}
	}


private:
	bool _fPrune;
	DETECTOR *_pDetector;
	float _eMinThresh;
	float _eDefaultThreshold;
	int	_targetWidth;
	int	_targetHeight;

	DetectionResult ^ DetectObjectInternal (IMAGE *pimg);

	IMAGE * ScaleImage(IMAGE *pinImage, int scaleFac);
	int ImageRescale(int actualWidth, int actualHeight);
	int GetBytePerPix(System::Drawing::Imaging::PixelFormat pixFormat);
};
}