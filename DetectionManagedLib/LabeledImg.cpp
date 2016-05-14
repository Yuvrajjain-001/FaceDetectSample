#include "StdAfx.h"
#include "LabeledImg.h"

namespace DetectionManagedLib 
{
LabeledImg::LabeledImg(IMGINFO *pImgInfo)
{
	_pImgInfo = pImgInfo;
	_FaceDetectionResult = nullptr;
}

LabeledImg::~LabeledImg()
{
	if (_pImgInfo != NULL)
	{
		delete _pImgInfo;
	}
}

String ^ LabeledImg::ToString()
{
	if (_pImgInfo == NULL)
	{
		return "";
	}
	else
	{
		return gcnew String(_pImgInfo->m_szFileName);
	}
}

void LabeledImg::Render (Drawing::Graphics ^graf, Drawing::Rectangle ^rect)
{
	if (_pImgInfo != NULL)
	{
		Bitmap ^bmp = gcnew Bitmap (gcnew String(_pImgInfo->m_szFileName));

		graf->DrawImage (bmp, 
			rect->X, rect->Y,
			bmp->Width, bmp->Height);
	}
}


void LabeledImg::Render (Drawing::Graphics ^graf, Drawing::Rectangle ^rectImg, Drawing::Rectangle ^rectDraw)
{
	if (_pImgInfo != NULL)
	{
		Bitmap ^bmp = gcnew Bitmap (gcnew String(_pImgInfo->m_szFileName));

		graf->DrawImage (bmp, *rectDraw, *rectImg, Drawing::GraphicsUnit::Pixel);
	}
}


void LabeledImg::RenderAnnotations (Drawing::Graphics ^graf, Drawing::Rectangle ^rect)
{
	if (_pImgInfo != NULL)
	{
		if (FaceRectList != nullptr)
		{
			for each (Drawing::Rectangle rectFace in FaceRectList)
			{
				rectFace.Offset (rect->X, rect->Y);

				graf->DrawRectangle (gcnew Pen(Color::White), rectFace);
			}
		}

		if (FeaturePtsList != nullptr)
		{
			for each (FeaturePts ^ featPts  in FeaturePtsList)
			{
				featPts->Render (graf, rect->X, rect->Y);
			}
		}
	}
}

DetectionResult ^ LabeledImg::GetDetectionResult (FaceDetector ^detector)
{
	if (_FaceDetectionResult == nullptr || !detector->Compatible(_FaceDetectionResult))
	{
		_FaceDetectionResult = detector->DetectObject(pszFileName);
	}

	return _FaceDetectionResult;
}
}