// FaceLabel.h : main header file for the FaceLabel application
//
#pragma once

#ifndef __AFXWIN_H__
	#error include 'stdafx.h' before including this file for PCH
#endif

#include "resource.h"       // main symbols


// CFaceLabelApp:
// See FaceLabel.cpp for the implementation of this class
//

class CFaceLabelApp : public CWinApp
{
public:
	CFaceLabelApp();


// Overrides
public:
	virtual BOOL InitInstance();

// Implementation
	afx_msg void OnAppAbout();
	DECLARE_MESSAGE_MAP()
};

extern CFaceLabelApp theApp;