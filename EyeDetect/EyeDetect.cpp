//-----------------------------------------------------------------------
// <copyright file="" company="Microsoft">
//      Copyright (c) 1998-2003 Microsoft Corporation.  All rights
//  reserved.  (Need to copyright from the date of first code written.)
// </copyright>
//
// Module:   EyeDetection    
//	
//
// Description:
//	Runs eye detection on an image. Two algorithms are potentially supported. 
//      1) Neural net based version
//      2) MSRA - To enable #define USE_MSRA_DLL and then make sure that the run time FaceDetectorDLL.dll 
//         is available
//
// The default is to use NN. Select the algorithm using SetAlgorithm()
//
// Author:
//	mrevow
//-----------------------------------------------------------------------


#include "stdafx.h"
#include "resource.h"
#include <windows.h>
#include "EyeDetect.h"
#include <neuralNet.h>
#include < vcclr.h >


    EyeDetect::EyeDetect()
    {
#ifdef USE_MSRA_DLL
        m_pComponentDetector = NULL;
#endif
        m_pNNClassifier = NULL;
        m_isInitialized = false;
        if (true != SetAlgorithm(AlgorithmEnum::NN, nullptr))
        {
            throw gcnew Exception ("EyeDetect: Failed to initialize the default NN algorithm");
        }
    }

    EyeDetect::~EyeDetect()
    {
#ifdef USE_MSRA_DLL
        delete m_pComponentDetector;
#endif
        delete m_pNNClassifier;
    }

    bool EyeDetect::SetAlgorithm(AlgorithmEnum algoType, String ^ dataFilename)
    {
        HRESULT hr;
        m_isInitialized = false;

        if (algoType == AlgorithmEnum::NN)
        {
            delete m_pNNClassifier;
            pin_ptr<const wchar_t> wFileName = nullptr;

            if (nullptr != dataFilename)
            {
                wFileName = PtrToStringChars(dataFilename);
            }

            m_pNNClassifier = new ::LiveLabs::Classifier();
            hr = m_pNNClassifier->SetAlgorithm(wFileName, 1);
            if (!SUCCEEDED(hr))
            {
                // Try load from resource
                HMODULE hInst = ::GetModuleHandle(L"EyeDetect.dll");
                hr = m_pNNClassifier->SetAlgorithm(hInst, IDR_EYE_NET, 1);
            }
            if (SUCCEEDED(hr))
            {
                ULONG cInput;
                if (SUCCEEDED(m_pNNClassifier->GetNumInputs(&cInput)))
                {
                    m_iWidthNN = (int)Math::Sqrt(cInput);
                    if (m_iWidthNN > 0)
                    {
                        m_iHeightNN = cInput / m_iWidthNN;
                        if (m_iWidthNN == m_iHeightNN)
                        {
                            m_isInitialized = true;
                        }
                    }
                }
            }
        }

        if (true == m_isInitialized)
        {
            m_currentAlgo = algoType;
        }
        else
        {
            m_currentAlgo = AlgorithmEnum::NONE;
        }

        return m_isInitialized;
    }

    // Run eye detection. Greyscaled face image dayta i s passed in
    EyeDetectResult ^ EyeDetect::Detect(array<Byte> ^inputImage, int width, int height)
    {
        int totBytes = (width * height);
        if (totBytes < 0)
        {
            return nullptr;
        }
        int bytePerPixel = inputImage->Length / totBytes;

        if (m_currentAlgo == AlgorithmEnum::MSRA)
        {
            return DetectMSRA(inputImage, width, height, bytePerPixel);
        }
        else if (m_currentAlgo == AlgorithmEnum::NN)
        {
            return DetectNN(inputImage, width, height, bytePerPixel);
        }
        return nullptr;
    }

    // Run eye detection. 
    // inputImage - Data from a full image
    // width - Image width
    // height - Image height
    // faceRect - Demarcates the face portion of inputImage 
    //
    // First extracts the face portion and scales it before running eye detection
    //
    EyeDetectResult ^ EyeDetect::Detect(array<Byte> ^inputImage, int width, int height, int stride, System::Drawing::Rectangle ^faceRect)
    {
        int totBytes = (height * width);
        if (totBytes < 0)
        {
            return nullptr;
        }
        int bytePerPixel = inputImage->Length / totBytes;

        if (m_currentAlgo == AlgorithmEnum::MSRA)
        {
            // unsupported
            return nullptr;
        }
        else if (m_currentAlgo == AlgorithmEnum::NN)
        {
            array<Byte> ^scaledImage = ScaleImage(inputImage, width, height, stride, faceRect, bytePerPixel);
            EyeDetectResult ^res =  DetectNN(scaledImage, m_iWidthNN, m_iHeightNN, 1);
            Point leftEye;
            Point rightEye;
            leftEye.X = res->LeftEye.X * faceRect->Width / m_iWidthNN + faceRect->X;
            leftEye.Y = res->LeftEye.Y * faceRect->Height / m_iHeightNN + faceRect->Y;
            rightEye.X = res->RightEye.X * faceRect->Width / m_iWidthNN + faceRect->X;
            rightEye.Y = res->RightEye.Y * faceRect->Height / m_iHeightNN + faceRect->Y;

            
            // This checks if other face features are available
            FaceFeatureResult ^faceResult = dynamic_cast<FaceFeatureResult ^>(res);
            if (nullptr != faceResult)
            {
                Point nose;
                Point leftMouth;
                Point rightMouth;

                nose.X = faceResult->Nose.X * faceRect->Width / m_iWidthNN + faceRect->X;
                nose.Y = faceResult->Nose.Y * faceRect->Height / m_iHeightNN + faceRect->Y;

                leftMouth.X = faceResult->LeftMouth.X * faceRect->Width / m_iWidthNN + faceRect->X;
                leftMouth.Y = faceResult->LeftMouth.Y * faceRect->Height / m_iHeightNN + faceRect->Y;
                rightMouth.X = faceResult->RightMouth.X * faceRect->Width / m_iWidthNN + faceRect->X;
                rightMouth.Y = faceResult->RightMouth.Y * faceRect->Height / m_iHeightNN + faceRect->Y;

                return gcnew FaceFeatureResult(leftEye, rightEye, nose, leftMouth, rightMouth);
            }
            else
            {
                return gcnew EyeDetectResult(leftEye, rightEye);
            }
        }
        return nullptr;
    }
    EyeDetectResult ^ EyeDetect::DetectMSRA(array<Byte> ^inputImage, int width, int height, int bytePerPixel)
    {
        EyeDetectResult ^ detectResult = nullptr;
#ifdef USE_MSRA_DLL
        if (NULL == m_pComponentDetector)
        {
            m_pComponentDetector = new CComponentDetectorDLL();
            m_pComponentDetector->Init();
        }

        BYTE *pData = new BYTE[inputImage->Length];
        for (int i = 0 ;i < inputImage->Length ; ++i)
        {
            pData[i] = inputImage[i];
        }

        HRESULT hr;

        ComponentPosition leftEye;
        ComponentPosition rightEye;

        int iStride = width * bytePerPixel;

        RECT rect;
        rect.left = 0;
        rect.top = 0;
        rect.right = width;
        rect.bottom = height;

        hr = m_pComponentDetector->DetectEye(width, 
                                        height, 
                                        pData, 
                                        iStride, 
                                        bytePerPixel, 
                                        0, 
                                        TRUE,
                                        &rect,
                                        &leftEye,
                                        &rightEye
                                        );

        if (SUCCEEDED(hr))
        {
            detectResult = gcnew EyeDetectResult(leftEye.ptPosition, rightEye.ptPosition);
        }
#endif

        return detectResult;

    }

    Byte EyeDetect::PixAt(array<Byte> ^inputImage, int stride, int x, int y, int iPlane, int bytePerPixel)
    {
        return inputImage[y*stride + x*bytePerPixel+iPlane];
    }
    Byte EyeDetect::BilinearTxfm(array<Byte> ^inputImage, int width, int stride, float x, float y, int bytePerPixel)
    {
        int ixFloor = (int)Math::Floor(x);
        int ixCeil = (int)Math::Ceiling(x);
        float xFrac = x - ixFloor;

        int iyFloor = (int)Math::Floor(y);
        int iyCeil = (int)Math::Ceiling(y);
        float yFrac = y - iyFloor;

        int ixStep = 0;
        if (ixFloor != ixCeil && ixCeil < width)
        {
            ixStep = 1;
        }

        int iyStep = 0;
        if (iyFloor != iyCeil && iyCeil < width)
        {
            iyStep = 1;
        }

        // pix00  pix01
        // pix10  pix11

        float val = 0;
        for (int iPlane = 0 ; iPlane < bytePerPixel ; ++iPlane)
        {
            Byte pix00 = PixAt(inputImage,stride, ixFloor, iyFloor, iPlane, bytePerPixel);
            Byte pix01 = PixAt(inputImage,stride, ixFloor+ixStep, iyFloor, iPlane, bytePerPixel);
            Byte pix10 = PixAt(inputImage,stride, ixFloor, iyFloor+iyStep, iPlane, bytePerPixel);
            Byte pix11 = PixAt(inputImage,stride, ixFloor+ixStep, iyFloor+iyStep, iPlane, bytePerPixel);

            float pixUpRow = (1.0F - xFrac) * pix00 + xFrac * pix01;
            float pixDownRow = (1.0F - xFrac) * pix10 + xFrac * pix11;
            val += ( (1.0F - yFrac)*pixUpRow + yFrac*pixDownRow);
        }
        val /= bytePerPixel;
        return (Byte)val;
    }
    array<Byte> ^ EyeDetect::ScaleImage(array<Byte> ^inputImage, int width, int height, int stride, System::Drawing::Rectangle ^extractRect, int bytePerPixel)
    {
        if (extractRect->Right > stride ||  extractRect->Bottom >  height)
        {
            throw gcnew Exception("EyeDetect supplied rect is not contained in input image");
        }

        float xScale = (float)extractRect->Width / (float)m_iWidthNN;
        float yScale = (float)extractRect->Height / (float)m_iHeightNN;

        array<Byte> ^retImage = gcnew array<Byte>(m_iWidthNN * m_iHeightNN);

        int iPix = 0;
        for(int iy = 0 ; iy < m_iHeightNN ; ++iy)
        {
            for(int ix = 0 ; ix < m_iWidthNN ; ++ix)
            {
                float x = ix*xScale + extractRect->Left;
                float y = iy*yScale + extractRect->Top;

                retImage[iPix++] = BilinearTxfm(inputImage, width, stride, x, y, bytePerPixel);
            }
        }

        return retImage;
    }

    EyeDetectResult ^ EyeDetect::DetectNN(array<Byte> ^inputImage, int width, int height, int bytePerPixel)
    {
        static int s_normalizationSum = 211676;     // Aprox 41x41 x 126
        array<Byte> ^scaledImage;
        if (width != m_iWidthNN && height != m_iHeightNN)
        {
            scaledImage = ScaleImage(inputImage, width, height, width*bytePerPixel, gcnew System::Drawing::Rectangle(0, 0, width, height), bytePerPixel);
        }
        else
        {
            scaledImage = inputImage;
        }

        UINT cInput = m_iWidthNN * m_iHeightNN *bytePerPixel;
        UINT iPixSum  = 0;

        for (UINT i = 0 ; i < cInput ; ++i)
        {
            iPixSum += scaledImage[i];
        }

        PFLOAT pData = new FLOAT[cInput];
        for (UINT i = 0 ; i < cInput ; ++i)
        {
            pData[i] = (FLOAT)(scaledImage[i] * s_normalizationSum / iPixSum);
        }

        HRESULT hr;
        EyeDetectResult ^ detectResult = nullptr;
        float aResult[10];
        ULONG ulWin;
        UINT cResult = ARRAYSIZE(aResult);

        hr = m_pNNClassifier->Run(pData, cInput, &ulWin, aResult, cResult);
		ULONG cClass;
		m_pNNClassifier->GetNumClasses(&cClass);
		cClass = min(cClass, cResult);

        for (UINT i = 0 ; i < cClass ; ++i)
        {
            aResult[i] = aResult[i] *  SOFT_MAX_UNITY / USHRT_MAX;
        }

        if (SUCCEEDED(hr))
        {
            Point leftEye;
            Point rightEye;
            Point nose;
            Point leftMouth;
            Point rightMouth;

            UINT iPos = 0;
            leftEye.X = aResult[iPos++] * width;
            leftEye.Y = aResult[iPos++] * height;
            rightEye.X = aResult[iPos++] * width;
            rightEye.Y = aResult[iPos++] * height;

            if (cClass > iPos)
            {
                nose.X = aResult[iPos++] * width;
                nose.Y = aResult[iPos++] * height;
                leftMouth.X = aResult[iPos++] * width;
                leftMouth.Y = aResult[iPos++] * height;
                rightMouth.X = aResult[iPos++] * width;
                rightMouth.Y = aResult[iPos++] * height;
                detectResult = gcnew FaceFeatureResult(leftEye, rightEye, nose, leftMouth, rightMouth);
            }
            else
            {
                detectResult = gcnew EyeDetectResult(leftEye, rightEye);
            }
        }

        delete pData;

        return detectResult;

    }


    EyeDetectResult::EyeDetectResult(POINT &leftEye, POINT &rightEye)
    {
        m_leftEye.X = leftEye.x;
        m_leftEye.Y = leftEye.y;

        m_rightEye.X = rightEye.x;
        m_rightEye.Y = rightEye.y;

    }

    EyeDetectResult::EyeDetectResult(Point leftEye, Point rightEye)
    {
        m_leftEye.X = leftEye.X;
        m_leftEye.Y = leftEye.Y;

        m_rightEye.X = rightEye.X;
        m_rightEye.Y = rightEye.Y;

    }

    Point EyeDetectResult::LeftEye::get()
    {
            return m_leftEye;
    }

    Point EyeDetectResult::RightEye::get()
    {
            return m_rightEye;
    }


    FaceFeatureResult::FaceFeatureResult(POINT &leftEye, POINT &rightEye, POINT &nose, POINT &leftMouth, POINT &rightMouth): EyeDetectResult(leftEye, rightEye)
    {
        m_nose.X = nose.x;
        m_nose.Y = nose.y;

        m_leftMouth.X = leftMouth.x;
        m_leftMouth.Y = leftMouth.y;

        m_rightMouth.X = rightMouth.x;
        m_rightMouth.Y = rightMouth.y;

    }

    FaceFeatureResult::FaceFeatureResult(Point &leftEye, Point &rightEye, Point &nose, Point &leftMouth, Point &rightMouth) : EyeDetectResult(leftEye, rightEye)
    {
        m_nose.X = nose.X;
        m_nose.Y = nose.Y;

        m_leftMouth.X = leftMouth.X;
        m_leftMouth.Y = leftMouth.Y;

        m_rightMouth.X = rightMouth.X;
        m_rightMouth.Y = rightMouth.Y;

    }


    Point FaceFeatureResult::Nose::get()
    {
            return m_nose;
    }

    Point FaceFeatureResult::LeftMouth::get()
    {
            return m_leftMouth;
    }

    Point FaceFeatureResult::RightMouth::get()
    {
            return m_rightMouth;
    }

