// FaceLabelView.h : interface of the CFaceLabelView class
//


#pragma once

#include "image.h"
#include "imageinfo.h"

#define MAX_NUM_FACES       100

#define INVALID_GT         0
#define VALID_GT            1
#define PARTIAL_GT          2

class CFaceLabelDoc; 

class CFaceLabelView : public CScrollView
{
protected: // create from serialization only
	CFaceLabelView();
	DECLARE_DYNCREATE(CFaceLabelView)

// Attributes
public:
	CFaceLabelDoc* GetDocument() const;

protected: 
    int m_nDispImgIdx; 
    CString m_strImgName; 
    IMGINFO *m_pImgInfo; 
    IMAGE m_Img; 

    bool m_bInAnnotate; 
    int m_nAnnotateIdx; 
    int m_nAnnotateStep; 
    int m_GTObjFlags[MAX_NUM_FACES];    // flags takes values of INVALID_GT, VALID_GT, or PARTIAL_GT
    FEATUREPTS m_GTObjFPts[MAX_NUM_FACES];
    IRECT m_GTObjRcs[MAX_NUM_FACES]; 

    bool m_bInQuickZoom; 
    CPoint m_MouseDownPos; 
    IRECT m_QuickZoomRc; 
    bool m_bTargetedQuickZoom; 

// Operations
public:

// Overrides
	public:
	virtual void OnDraw(CDC* pDC);  // overridden to draw this view
virtual BOOL PreCreateWindow(CREATESTRUCT& cs);
protected:
	virtual void OnInitialUpdate(); // called first time after construct
    virtual void OnUpdate(CView* pSender, LPARAM lHint, CObject* pHint); 

    void ResetGTObjFlags()
    {
        for (int i=0; i<MAX_NUM_FACES; i++) 
            m_GTObjFlags[i] = INVALID_GT; 
    }; 
    float FindScale(int imgs, int clnts); 
    float FindBestDispScale(); 
    void DrawFeatures(CDC* pDC, FEATUREPTS *pFPts, int step = 5); 
    void DrawRect(CDC *pDC, IRECT *pRc, COLORREF crColor, bool bScale=true); 
    void UpdateDocObjs(); 

// Implementation
public:
	virtual ~CFaceLabelView();
#ifdef _DEBUG
	virtual void AssertValid() const;
	virtual void Dump(CDumpContext& dc) const;
#endif

protected:

// Generated message map functions
protected:
	DECLARE_MESSAGE_MAP()
public:
    afx_msg void OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags);
    afx_msg void OnChar(UINT nChar, UINT nRepCnt, UINT nFlags);
    afx_msg void OnLButtonDblClk(UINT nFlags, CPoint point);
    afx_msg void OnLButtonDown(UINT nFlags, CPoint point);
    afx_msg BOOL OnMouseWheel(UINT nFlags, short zDelta, CPoint pt);
    afx_msg void OnRButtonDown(UINT nFlags, CPoint point);
    afx_msg void OnRButtonUp(UINT nFlags, CPoint point);
    afx_msg void OnMouseMove(UINT nFlags, CPoint point);
};

#ifndef _DEBUG  // debug version in FaceLabelView.cpp
inline CFaceLabelDoc* CFaceLabelView::GetDocument() const
   { return reinterpret_cast<CFaceLabelDoc*>(m_pDocument); }
#endif

