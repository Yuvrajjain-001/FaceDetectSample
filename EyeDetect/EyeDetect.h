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

#pragma once

#include "faceDetectorDll.h"
#include <classifier.h>

using namespace System;
using namespace System::Windows;



	public ref class EyeDetectResult
    {
    public:
        EyeDetectResult(POINT &leftEye, POINT &rightEye);
        EyeDetectResult(Point leftEye, Point rightEye);

        property Point LeftEye { Point get(); }
        property Point RightEye { Point get(); }



    private:

        Point   m_leftEye;
        Point   m_rightEye;

    };


    public ref class FaceFeatureResult : EyeDetectResult
    {
    public:
        FaceFeatureResult(POINT &leftEye, POINT &rightEye, POINT &nose, POINT &leftMouth, POINT &rightMouth);
        FaceFeatureResult(Point &leftEye, Point &rightEye, Point &nose, Point &leftMouth, Point &rightMouth);

        property Point Nose { Point get(); }
        property Point LeftMouth { Point get(); }
        property Point RightMouth { Point get(); }


    private:

        Point   m_nose;
        Point   m_leftMouth;
        Point   m_rightMouth;

    };


	public ref class EyeDetect
	{
    public:
        enum class AlgorithmEnum : unsigned int {NONE, MSRA, NN};
        EyeDetect();
        ~EyeDetect();

        EyeDetectResult ^ Detect(array<Byte> ^inputImage, int width, int height);
        EyeDetectResult ^ Detect(array<Byte> ^inputImage, int width, int height, int stride, System::Drawing::Rectangle ^faceRect);

        bool SetAlgorithm(AlgorithmEnum algoType, String ^ dataFilename);
		array<Byte> ^ ScaleImage(array<Byte> ^inputImage, int width, int height, int stride, System::Drawing::Rectangle ^extractRect, int bytePerPixel);

        property int ClassifierWidth { int get() { return m_iWidthNN;} };
        property int ClassifierHeight { int get() { return m_iHeightNN;} };

    private:
#ifdef USE_MSRA_DLL
        CComponentDetectorDLL   *m_pComponentDetector;
#endif
        ::LiveLabs::Classifier   *m_pNNClassifier;


        EyeDetectResult ^ DetectMSRA(array<Byte> ^inputImage, int width, int height, int bytePerPixel);
        EyeDetectResult ^ DetectNN(array<Byte> ^inputImage, int width, int height, int bytePerPixel);
        Byte BilinearTxfm(array<Byte> ^inputImage, int width, int stride, float x, float y, int bytePerPixel);
        Byte PixAt(array<Byte> ^inputImage, int stride, int x, int y, int iPlane, int bytePerPixel);

        AlgorithmEnum m_currentAlgo;
        int m_iWidthNN;
        int m_iHeightNN;
        bool m_isInitialized;

	};
