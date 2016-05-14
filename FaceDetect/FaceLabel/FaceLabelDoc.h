// FaceLabelDoc.h : interface of the CFaceLabelDoc class
//


#pragma once

#include "imageinfo.h"

class CFaceLabelView; 

#define MIN_SCALE       0.125f
#define MAX_SCALE       16.0f
#define SCALE_STEP      1.125f

class CFaceLabelDoc : public CDocument
{
protected: // create from serialization only
	CFaceLabelDoc();
	DECLARE_DYNCREATE(CFaceLabelDoc)

// Attributes
public:

// Operations
public:

// Overrides
	public:
	virtual BOOL OnNewDocument();
//	virtual void Serialize(CArchive& ar);

// Implementation
public:
	virtual ~CFaceLabelDoc();
#ifdef _DEBUG
	virtual void AssertValid() const;
	virtual void Dump(CDumpContext& dc) const;
#endif

protected:
    bool m_bNewFolder; 
    CString m_strDirPath; 
    int m_nImgIdx; 
    vector<IMGINFO *> m_ImgInfoVec; 
    float m_fDispScale; 

protected: 
    void ReleaseImgInfoVec(); 
    void ScanFolder4Images(CString strFolder); 

public: 
    vector<IMGINFO *> &GetImgInfoVec() { return m_ImgInfoVec; }; 
    CString GetDirPath() { return m_strDirPath; }; 
    int GetImgIdx() { return m_nImgIdx; }
    float GetDispScale() { return m_fDispScale; } 
    void SetDispScale(float fScale) { m_fDispScale = fScale; };
    void SetImgIdx(int nIdx) { m_nImgIdx = nIdx; }
    void GoToNextImg(); 
    void GoToPrevImg(); 
    void GoToFirstImg(); 
    void GoToLastImg(); 
    void DispZoomIn(); 
    void DispZoomOut(); 
    void DispZoomReset(); 
    void DiscardCurImg(); 
    void NegateCurImg(); 
    void UnannotateCurImg(); 
    void TogglePartialCurImg(); 
    bool IsNewFolder() { return m_bNewFolder; }; 
    CFaceLabelView *GetFaceLabelView(); 

// Generated message map functions
protected:
	DECLARE_MESSAGE_MAP()
public:
    afx_msg void OnFileCreate();
    virtual BOOL OnOpenDocument(LPCTSTR lpszPathName);
    virtual BOOL OnSaveDocument(LPCTSTR lpszPathName);
    afx_msg void OnViewStatistics();
};


