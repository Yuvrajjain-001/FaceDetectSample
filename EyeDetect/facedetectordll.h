/*****************************************************************************\

Microsoft Research Asia
Copyright (c) 2002 Microsoft Corporation

Module Name:

  FaceDetectorDLL.h: DLL interface for the face detector.

Notes:

History:

  Created on 1/30/2003 by Lei Zhang, i-lzhang@microsoft.com
  Modified mm/dd/yyyy by email-name

\*****************************************************************************/
#ifndef _FACEDETECTORDLL_H_
#define _FACEDETECTORDLL_H_

#ifdef FACEDETECTORDLL_EXPORTS
#define FACEDETECTORDLL_API __declspec(dllexport)
#else
#define FACEDETECTORDLL_API __declspec(dllimport)
#endif

#ifndef __DETCONFIG_H__
enum 
{    
    E_FRONTAL = 0x01, 
    E_INPLANE22 = 0x02, 
    E_INPLANE45 = 0x04, 
    E_INPLANE67 = 0x08, 
    E_INPLANE90 = 0x10, 
    E_INPLANE112 = 0x20, 
    E_INPLANE135 = 0x40, 
    E_INPLANE157 = 0x80, 
    E_INPLANE180 = 0x100,
    E_FRONTALEXT = 0x200, 
    E_PROFILE = 0x400, 
};

enum 
{ 
    E_INPLANE_LEVEL0 = E_FRONTAL, 
    E_INPLANE_LEVEL1 = E_INPLANE_LEVEL0 | E_INPLANE22,
    E_INPLANE_LEVEL2 = E_INPLANE_LEVEL1 | E_INPLANE45,
    E_INPLANE_LEVEL3 = E_INPLANE_LEVEL2 | E_INPLANE67,
    E_INPLANE_LEVEL4 = E_INPLANE_LEVEL3 | E_INPLANE90,
    E_INPLANE_LEVEL5 = E_INPLANE_LEVEL4 | E_INPLANE112,
    E_INPLANE_LEVEL6 = E_INPLANE_LEVEL5 | E_INPLANE135,
    E_INPLANE_LEVEL7 = E_INPLANE_LEVEL6 | E_INPLANE157,
    E_INPLANE_LEVEL8 = E_INPLANE_LEVEL7 | E_INPLANE180
};
#endif           __DETCONFIG_H__

typedef struct _Rect
{
    RECT rBox;
    int nOverlapTiers;
    float fConfidence;
    float fSumalpha;
 	union {
		DWORD		nRotationDegree;
		struct {
			int nProfileView:16;
			int nInplaneView:16;
		};
	};
	int		nTag;
    int nScaleNo;
    int nLayerPassed;
} FaceRect;

struct ComponentPosition
{
	POINT ptPosition;
	float fConfidence;
};

struct GenderInfo
{
	bool bGender;	// true: male false:female
	float fConfidence;
};

// This class is exported from the FaceDetectorDLL.dll
class FACEDETECTORDLL_API CFaceDetectorDLL 
{
	void *m_pFaceDetector;
	void Destroy();

public:
	CFaceDetectorDLL();
	~CFaceDetectorDLL();

	HRESULT Init();

	void SetParam (BOOL bUseProfile, unsigned uInplaneTag, BOOL bUsePreColor, int iMiniSize, int iMinScale,int iMaxScale );
	void SetOptions( BOOL bSingleDet, BOOL bEyeRecall);

	HRESULT DetectFace(	int iWidth,
						int iHeight,
						BYTE * pbImage,
						int iStride,		// default: 0, (for four bytes align)
						int iBytesPerPixels,
						int iColorSequence,	// 0 for RGB, 1 for BGR
						BOOL bTopDown,		// TRUE for topdown, FALSE otherwise
						LPRECT pRect,		// NULL for entire image
						int * pCount,		// count of detected faces
						FaceRect ** ppFaceRect);// pointer of FaceRect array will be returned here

     HRESULT OrientationDetectionByFace(int iWidth,
                       int iHeight,
                       BYTE * pbImage,
                       int iStride,
                       int iBytesPerPixels,
                       int iColorSequence,
                       BOOL bTopDown,
                       int nPossibleOrientation, 
                       int& OrientationDetected);

	void FreeFaceRect(FaceRect *pFaceRect);
};

class FACEDETECTORDLL_API CComponentDetectorDLL 
{
	void *m_pEyeDetector;
	void *m_pNoseDetector;
	void *m_pLMouthDetector;
	void *m_pRMouthDetector;
	void *m_pLEyeCornerDetector;
	void *m_pREyeCornerDetector;
	void Destroy();

public:
	CComponentDetectorDLL();
	~CComponentDetectorDLL();

	void Init();

	HRESULT DetectEye(	int iWidth,
						int iHeight,
						BYTE * pbImage,
						int iStride,		// default: 0, (for four bytes align)
						int iBytesPerPixels,
						int iColorSequence,	// 0 for RGB, 1 for BGR
						BOOL bTopDown,		// TRUE for topdown, FALSE otherwise
						LPRECT pFaceRect,	// Frontal Face Rectangle, NULL for entire image
						ComponentPosition *pLeftEye,	//position of Left Eye will be returned here
						ComponentPosition *pRightEye);	//position of Right Eye will be returned here
	HRESULT DetectNose(	int iWidth,
						int iHeight,
						BYTE * pbImage,
						int iStride,		// default: 0, (for four bytes align)
						int iBytesPerPixels,
						int iColorSequence,	// 0 for RGB, 1 for BGR
						BOOL bTopDown,		// TRUE for topdown, FALSE otherwise
						LPRECT pFaceRect,	// Frontal Face Rectangle, NULL for entire image
						ComponentPosition *pNose);	//position of nose will be returned here
	HRESULT DetectMouthCorner(int iWidth,
						int iHeight,
						BYTE * pbImage,
						int iStride,		// default: 0, (for four bytes align)
						int iBytesPerPixels,
						int iColorSequence,	// 0 for RGB, 1 for BGR
						BOOL bTopDown,		// TRUE for topdown, FALSE otherwise
						LPRECT pFaceRect,	// Frontal Face Rectangle, NULL for entire image
						ComponentPosition *pLeftMouth,	//position of Left Mouth corner will be returned here
						ComponentPosition *pRightMouth);//position of Right Mouth will be returned here
	HRESULT DetectEyeCorner(int iWidth,
						int iHeight,
						BYTE * pbImage,
						int iStride,		// default: 0, (for four bytes align)
						int iBytesPerPixels,
						int iColorSequence,	// 0 for RGB, 1 for BGR
						BOOL bTopDown,		// TRUE for topdown, FALSE otherwise
						LPRECT pFaceRect,	// Frontal Face Rectangle, NULL for entire image
						ComponentPosition *pLLEyeCorner,
						ComponentPosition *pLREyeCorner,
						ComponentPosition *pRLEyeCorner,
						ComponentPosition *pRREyeCorner);
};

FACEDETECTORDLL_API HRESULT InitFaceDetector();
FACEDETECTORDLL_API void UnInitFaceDetector();
FACEDETECTORDLL_API HRESULT FaceDetection(	int iWidth,
										  int iHeight,
										  BYTE * pbImage,
										  int iStride,		// default: 0, (for four bytes align)
										  int iBytesPerPixels,
										  int iColorSequence,	// 0 for RGB, 1 for BGR
										  BOOL bTopDown,		// TRUE for topdown, FALSE otherwise
										  LPRECT pRect,		// NULL for entire image
										  int * pCount,		// count of detected faces
										  FaceRect ** ppFaceRect);
FACEDETECTORDLL_API void FreeFaceRect(FaceRect *pFaceRect);
FACEDETECTORDLL_API void SetDetectParam (BOOL bUseProfile, unsigned uInplaneTag, BOOL bUsePreColor, int iMiniSize, int iMinScale,int iMaxScale );
FACEDETECTORDLL_API void SetOptions( BOOL bDetSingleFace, BOOL bEyeRecall);

#define		PORTRAIT_MULTIPLE_FACE 1
#define 	PORTRAIT_SINGLE  0
#define		PORTRAIT_NOT_FOUND  -1
#define		PORTRAIT_IGNORE_SMALL_FACE -2
#define		PORTRAIT_REJECT_NONE_FRONTAL_FACE -3
#define		PORTRAIT_REJECT_TILTED_FACE -4

#define		PORTRAIT_OK(x)  (x>=0)

FACEDETECTORDLL_API HRESULT PortraitDetection(	int iWidth,
										  int iHeight,
										  BYTE * pbImage,
										  int iStride,		// default: 0, (for four bytes align)
										  int iBytesPerPixels,
										  int iColorSequence,	// 0 for RGB, 1 for BGR
										  BOOL bTopDown,		// TRUE for topdown, FALSE otherwise
										  RECT * pRect,
										  int* piRet);
FACEDETECTORDLL_API void GetCropRect(	int iWidth,	  int iHeight,  LPRECT pRect );
#endif // _FACEDETECTORDLL_H_