#pragma once

#include "imageinfo.h"
#include "FeaturePts.h"
#include "DetectionResult.h"
#include "FaceDetector.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Drawing;

namespace DetectionManagedLib 
{
public ref class LabeledImg
{
public:
	LabeledImg(IMGINFO *pImgInfo);
	~LabeledImg ();

	virtual String ^ ToString() override;

	property String ^ FileName
	{
		String ^ get()
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
	}

	property char * pszFileName
	{
		char * get()
		{
			if (_pImgInfo == NULL)
			{
				return NULL;
			}
			else
			{
				return _pImgInfo->m_szFileName;
			}
		}
	}

	// returns a list of face rectangles
	property List<Drawing::Rectangle> ^ FaceRectList
	{
		List<Drawing::Rectangle> ^ get()
		{
			if (_pImgInfo == NULL || _pImgInfo->m_nNumObj <= 0)
			{
				return nullptr;
			}
			else
			{
				List<Drawing::Rectangle> ^rectList = gcnew List<Drawing::Rectangle>();

				for (int iRect = 0; iRect < _pImgInfo->m_nNumObj; iRect++)
				{
					Drawing::Rectangle ^ rect = gcnew Drawing::Rectangle (_pImgInfo->m_pObjRcs[iRect].m_ixMin,
						_pImgInfo->m_pObjRcs[iRect].m_iyMin,
						_pImgInfo->m_pObjRcs[iRect].m_ixMax - _pImgInfo->m_pObjRcs[iRect].m_ixMin,
						_pImgInfo->m_pObjRcs[iRect].m_iyMax - _pImgInfo->m_pObjRcs[iRect].m_iyMin);

					rectList->Add(*rect);
				}

				return rectList;
			}
		}
	}

	// returns a list of face feature pts
	property List<FeaturePts ^> ^ FeaturePtsList
	{
		List<FeaturePts ^> ^ get()
		{
			if (_pImgInfo == NULL || _pImgInfo->m_nNumObj <= 0)
			{
				return nullptr;
			}
			else
			{
				List<FeaturePts ^> ^featurePtsList = gcnew List<FeaturePts ^>();

				for (int iRect = 0; iRect < _pImgInfo->m_nNumObj; iRect++)
				{
					featurePtsList->Add(gcnew FeaturePts (_pImgInfo->m_pObjFPts + iRect));
				}

				return featurePtsList;
			}
		}
	}
	
	// renders image to a rectangle on the given graphics surface 
	void Render (Drawing::Graphics ^graf,  Drawing::Rectangle ^rect);
	void Render (Drawing::Graphics ^graf,  Drawing::Rectangle ^rectImg, Drawing::Rectangle ^rectDraw);

	// renders the image annotations (labels) to a rectangle on the given graphics surface 
	void RenderAnnotations (Drawing::Graphics ^graf, Drawing::Rectangle ^rect);

	DetectionResult ^ GetDetectionResult (FaceDetector ^detector);

private:
	IMGINFO *_pImgInfo;
	DetectionResult ^_FaceDetectionResult;
};
}