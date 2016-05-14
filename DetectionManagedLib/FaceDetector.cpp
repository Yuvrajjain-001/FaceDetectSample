#include "StdAfx.h"
#include "FaceDetector.h"
#include "image.h"

using namespace System::Runtime::InteropServices;

namespace DetectionManagedLib 
{
	FaceDetector::FaceDetector(String ^strFileName, bool fPrune, float eMinThresh)
		: ObjectDetector ()
		, _fPrune (fPrune)
		, _eMinThresh (eMinThresh)
	{
		const char *pszName = (const char *)(Marshal::StringToHGlobalAnsi(strFileName)).ToPointer();

		_pDetector = new DETECTOR (pszName);

		_pDetector->SetReject(fPrune);

		_eDefaultThreshold = _pDetector->GetFinalScoreTh();

		Marshal::FreeHGlobal(IntPtr((void*)pszName));
	}

	FaceDetector::~FaceDetector()
	{
		if (_pDetector != NULL)
		{
			delete _pDetector;
		}
	}

	DetectionResult ^ FaceDetector::DetectObject (const char *pszName)
	{

		IMAGE *pimg = new IMAGE(pszName);
		DetectionResult ^ result =  DetectObjectInternal(pimg);
		delete pimg;
		return result;
	}


	DetectionResult ^ FaceDetector::DetectObject (String ^FileName)
	{

		// load image
		const char *pszName = (const char *)(Marshal::StringToHGlobalAnsi(FileName)).ToPointer();

		DetectionResult ^ detectionResult = nullptr;

		try
		{
			detectionResult = DetectObject (pszName);
		}
		catch (char *pErrMsg)
		{

			throw gcnew Exception("Facedetector : " + Marshal::PtrToStringAnsi((IntPtr)pErrMsg));
		}
		catch (...)
		{
			throw gcnew Exception("Unspecified error in FaceDetector");
		}

		Marshal::FreeHGlobal(IntPtr((void*)pszName));

		return detectionResult;
	}

	DetectionResult ^ FaceDetector::DetectObject (System::Drawing::Imaging::BitmapData ^bitMapData)
	{
		if (nullptr == bitMapData)
		{
			return nullptr;
		}

		BYTE * rgbValues = (BYTE*)bitMapData->Scan0.ToPointer();
		DetectionResult ^ result = nullptr;

		if (NULL != rgbValues)
		{
			int bytePerPix = GetBytePerPix(bitMapData->PixelFormat);
			if (bytePerPix > 0)
			{
				IMAGE *pimg = new IMAGE(bitMapData->Width, bitMapData->Height, bitMapData->Stride, bytePerPix, rgbValues);
				result =  DetectObjectInternal(pimg);
				delete pimg;
			}
			else
			{
				throw gcnew Exception("FaceDetector.DetectObject:\nUnsupported pixel format specified. Only support pixel formats using 8 bits per colour plane");
			}
		}
		return result;
	}

	//DetectionResult ^ FaceDetector::DetectObject(int width, int height, int stride, int bytePerPix, array<System::Byte> ^pixData)
	//{
	//	if (nullptr == pixData)
	//	{
	//		return nullptr;
	//	}

	//	if (stride * height < pixData->Length)
	//	{
	//		return nullptr;
	//	}

	//	IntPtr hRgbValues = System::Runtime::InteropServices::Marshal::AllocHGlobal(pixData->Length);
	//	DetectionResult ^ result = nullptr;

	//	if (IntPtr::Zero != hRgbValues)
	//	{
	//		// Copy the RGB values into the array.
	//		System::Runtime::InteropServices::Marshal::Copy(pixData, 0, hRgbValues, pixData->Length);

	//		IMAGE *pimg = new IMAGE(width, height, stride, bytePerPix, (BYTE *)(hRgbValues.ToPointer()));
	//		result =  DetectObjectInternal(pimg);
	//		System::Runtime::InteropServices::Marshal::FreeHGlobal(hRgbValues);
	//		delete pimg;
	//	}
	//	return result;
	//}

	DetectionResult ^ FaceDetector::DetectObjectInternal (IMAGE *pimg)
	{
		if (_pDetector == NULL)
		{
			return nullptr;
		}

		if (!pimg)
		{	
			return nullptr;
		}

		int scaleFac = ImageRescale(pimg->GetWidth(), pimg->GetHeight());

		IMAGE *pScaledImage = NULL;
		if (scaleFac > 1)
		{
			pScaledImage = ScaleImage(pimg, scaleFac);
			if (NULL != pScaledImage)
			{
				pimg = pScaledImage;
			}
		}

		DetectionResult ^ detectionResult = gcnew DetectionResult(_iSignature, scaleFac);

		// create & integral image
		IN_IMAGE *piimg = new IN_IMAGE ();
		if (!piimg)
		{
			free (pimg);
			return nullptr;
		}

		piimg->Init (pimg);

		// run detector
		DateTime tmStart = DateTime::Now;

		// set the threshold to the minimum so that we get "all the possible" raw rectangles
		_pDetector->SetFinalScoreTh(_eMinThresh);
		_pDetector->DetectObject (piimg);

		detectionResult->tmDetection = DateTime::Now - tmStart;

		// get the detected rectangles
		SCORED_RECT *prect;

		// get raw rectangles
		int cRect = _pDetector->GetDetResults(&prect, false);

		// get the raw rectangles
		for (int iRect = 0; iRect < cRect; iRect++)
		{
			detectionResult->AddRawRect (gcnew ScoredRect(prect + iRect, ScoredRect::RectType::Raw));
		}

		delete piimg;
		delete pScaledImage;

		return detectionResult;
	}

	void FaceDetector::SetTargetDimension(int targetWidth,  int targetHeight)
	{
		_targetWidth = targetWidth;
		_targetHeight = targetHeight;
	}
	bool FaceDetector::Compatible (DetectionResult ^detRes)
	{
		return detRes->Signature == _iSignature;
	}

	void FaceDetector::SetPrune (bool fPrune)
	{
		if (fPrune != _fPrune)
		{
			_fPrune = fPrune;

			_pDetector->SetReject(_fPrune);

			Random ^rand = gcnew Random();
			_iSignature	=	rand->Next();
		}
	}

	IMAGE *FaceDetector::ScaleImage(IMAGE *pInImage, int scaleFac)
	{
		if (scaleFac <= 0)
		{
			return NULL;
		}

		int iNewWidth = pInImage->GetWidth() / scaleFac;
		int iNewHeight = pInImage->GetHeight() / scaleFac;
		int iNewStride = pInImage->GetStride() / scaleFac;

		if (iNewWidth * scaleFac != pInImage->GetWidth() ||
			iNewHeight * scaleFac != pInImage->GetHeight() ||
			iNewStride * scaleFac != pInImage->GetStride())
		{
			return NULL;
		}

		BYTE	*pNewBuf = new BYTE[iNewHeight * iNewStride];
		int		iPix = 0;
		int		iStride =  pInImage->GetStride();
		int		scaleFac2 = scaleFac * scaleFac;
		BYTE	*pInBuf = pInImage->GetDataPtr();

		for (int iRow = 0 ; iRow < pInImage->GetHeight() ; iRow += scaleFac)
		{
			for (int iCol = 0 ; iCol < pInImage->GetWidth() ; iCol += scaleFac)
			{
				int pixVal = 0;

				for (int j = 0 ; j < scaleFac ; ++j)
				{
					BYTE *pLine = pInBuf + (iRow + j)* iStride + iCol;

					for (int i = 0 ; i< scaleFac ; ++i)
					{
						pixVal += pLine[i];
					}
				}
				pNewBuf[iPix++] = (BYTE)(pixVal/scaleFac2);
			}
		}

		IMAGE *pRet = new IMAGE(iNewWidth, iNewHeight, iNewStride, pNewBuf);

		return pRet;
	}

	int FaceDetector::ImageRescale(int actualWidth, int actualHeight)
	{
		int scaleFac = -1;
		if (_targetWidth <= 0 || _targetHeight <= 0)
		{
			return scaleFac;
		}

		int xScale = actualWidth/ _targetWidth;
		int yScale = actualHeight / _targetHeight;

		scaleFac = min(xScale, yScale);

		if (scaleFac <= 0 )
		{
			return scaleFac;
		}

		for ( ; scaleFac > 1 ; --scaleFac)
		{
			if (actualWidth / scaleFac * scaleFac == actualWidth &&
				actualHeight / scaleFac * scaleFac == actualHeight )
			{
				break;
			}
		}

		return scaleFac;
	}
	// Return Bytes per pixels, only for formats that use 8 bits per colour plane

	int FaceDetector::GetBytePerPix(System::Drawing::Imaging::PixelFormat pixFormat)
	{
		int retVal;

		switch (pixFormat)
		{
		case System::Drawing::Imaging::PixelFormat::Format24bppRgb:
		case System::Drawing::Imaging::PixelFormat::Format32bppArgb:
		case System::Drawing::Imaging::PixelFormat::Format32bppPArgb:
		case System::Drawing::Imaging::PixelFormat::Format32bppRgb:
			retVal = 3;
			break;

		default:
			retVal = -1;
		}

		return retVal;
	}
};