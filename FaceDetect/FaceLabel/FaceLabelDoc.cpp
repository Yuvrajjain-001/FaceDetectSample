// FaceLabelDoc.cpp : implementation of the CFaceLabelDoc class
//

#include "stdafx.h"
#include "FaceLabel.h"

#include "FaceLabelView.h"
#include "FaceLabelDoc.h"
#include ".\facelabeldoc.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CFaceLabelDoc

IMPLEMENT_DYNCREATE(CFaceLabelDoc, CDocument)

BEGIN_MESSAGE_MAP(CFaceLabelDoc, CDocument)
    ON_COMMAND(ID_FILE_CREATE, OnFileCreate)
    ON_COMMAND(ID_VIEW_STATISTICS, OnViewStatistics)
END_MESSAGE_MAP()


// CFaceLabelDoc construction/destruction

CFaceLabelDoc::CFaceLabelDoc()
{
	// TODO: add one-time construction code here
    m_nImgIdx = -1; 
    m_fDispScale = 1.0f; 
    m_bNewFolder = true; 
}

void CFaceLabelDoc::ReleaseImgInfoVec()
{
    if (!m_ImgInfoVec.empty()) 
    {
        vector<IMGINFO *>::iterator it; 
        for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
        {
            IMGINFO *pInfo = *it; 
            delete pInfo; 
        }
        m_ImgInfoVec.clear(); 
    }
}

CFaceLabelDoc::~CFaceLabelDoc()
{
    ReleaseImgInfoVec(); 
}

BOOL CFaceLabelDoc::OnNewDocument()
{
	if (!CDocument::OnNewDocument())
		return FALSE;

	// TODO: add reinitialization code here
	// (SDI documents will reuse this document)

	return TRUE;
}




// CFaceLabelDoc serialization

//void CFaceLabelDoc::Serialize(CArchive& ar)
//{
//	if (ar.IsStoring())
//	{
//		// TODO: add storing code here
//	}
//	else
//	{
//		// TODO: add loading code here
//	}
//}


// CFaceLabelDoc diagnostics

#ifdef _DEBUG
void CFaceLabelDoc::AssertValid() const
{
	CDocument::AssertValid();
}

void CFaceLabelDoc::Dump(CDumpContext& dc) const
{
	CDocument::Dump(dc);
}
#endif //_DEBUG


// CFaceLabelDoc commands

void CFaceLabelDoc::OnFileCreate()
{
    CDocument::OnNewDocument(); 

    // TODO: Add your command handler code here
    m_nImgIdx = -1;
    ReleaseImgInfoVec(); 
    m_strDirPath = ""; 
    UpdateAllViews(NULL); 

    LPITEMIDLIST pidlRoot = NULL;
    LPITEMIDLIST pidlSelected = NULL;
    BROWSEINFO bi = {0};
    LPMALLOC pMalloc = NULL;

    SHGetMalloc(&pMalloc);
    pidlRoot = NULL;

    bi.hwndOwner = NULL;
    bi.pidlRoot = pidlRoot;
    bi.pszDisplayName = NULL;
    bi.lpszTitle = NULL;
    bi.ulFlags = 0;
    bi.lpfn = NULL;
    bi.lParam = 0;

    pidlSelected = SHBrowseForFolder(&bi);
    bool retval = false; 
    if (pidlSelected != NULL) 
    {
        SHGetPathFromIDList(pidlSelected, m_strDirPath.GetBuffer(MAX_PATH)); 
        m_strDirPath.ReleaseBuffer(); 
        retval = true; 
    }

    if(pidlRoot) pMalloc->Free(pidlRoot);
    if(pidlSelected) pMalloc->Free(pidlSelected); 
    pMalloc->Release(); 

    if (retval) 
        ScanFolder4Images(m_strDirPath); 
    if (m_ImgInfoVec.size() > 0) 
        m_nImgIdx = 0; 

    m_bNewFolder = true; 
    UpdateAllViews(NULL); 
    m_bNewFolder = false; 
}

void CFaceLabelDoc::ScanFolder4Images(CString strFolder)
{
    CFileFind finder;

    // build a string with wildcards
    CString strWildcard = strFolder;
    strWildcard += _T("\\*.*");

    // start working for files
    BOOL bWorking = finder.FindFile(strWildcard);

    while (bWorking)
    {
        bWorking = finder.FindNextFile();

        // skip . and .. files; otherwise, we'd
        // recur infinitely!

        if (finder.IsDots())
            continue;

        // if it's a directory, recursively search it

        if (finder.IsDirectory())
        {
            CString str = finder.GetFilePath();
            cout << (LPCTSTR) str << endl;
            ScanFolder4Images(str);
        }
        
        CString path = finder.GetFilePath(); 
        CString str = finder.GetFileName(); 
        CString strlower = str.MakeLower(); 
        if (strlower.Right(3) == "jpg" || strlower.Right(3) == "bmp" || strlower.Right(3) == "pgm") 
        {
            IMGINFO *pInfo = new IMGINFO; 
            int len = path.GetLength(); 
            pInfo->m_szFileName = new char [len+1]; 
            if (!pInfo->m_szFileName) 
                throw "Out of memory"; 
            strcpy(pInfo->m_szFileName, path.GetBuffer()); 
            path.ReleaseBuffer(); 
            m_ImgInfoVec.push_back(pInfo); 
        }
    }

    finder.Close();
}

void CFaceLabelDoc::GoToNextImg()
{
    if (m_nImgIdx < (int)m_ImgInfoVec.size()-1) 
    {
        m_nImgIdx ++; 
        UpdateAllViews(NULL); 
    }
}

void CFaceLabelDoc::GoToPrevImg()
{
    if (m_nImgIdx > 0) 
    {
        m_nImgIdx --; 
        UpdateAllViews(NULL); 
    }
}

void CFaceLabelDoc::GoToFirstImg()
{
    if ((int)m_ImgInfoVec.size() > 0)
    {
        m_nImgIdx = 0; 
        UpdateAllViews(NULL); 
    }
}

void CFaceLabelDoc::GoToLastImg()
{
    if ((int)m_ImgInfoVec.size() > 0)
    {
        m_nImgIdx = (int)m_ImgInfoVec.size()-1; 
        UpdateAllViews(NULL); 
    }
}

void CFaceLabelDoc::DispZoomIn()
{
    if (m_fDispScale <= MAX_SCALE)
    {
        m_fDispScale *= SCALE_STEP; 
        UpdateAllViews(NULL); 
    }
}

void CFaceLabelDoc::DispZoomOut()
{
    if (m_fDispScale >= MIN_SCALE)
    {
        m_fDispScale /= SCALE_STEP; 
        UpdateAllViews(NULL); 
    }
}

void CFaceLabelDoc::DispZoomReset()
{
    m_fDispScale = 1.0f; 
    UpdateAllViews(NULL); 
}

CFaceLabelView *CFaceLabelDoc::GetFaceLabelView()
{
    POSITION pos = GetFirstViewPosition();
    CView* pView; 
    while (pos != NULL)
    {
        pView = GetNextView(pos);
        if (pView->IsKindOf(RUNTIME_CLASS(CFaceLabelView))) 
            break;  // this should always happen so we don't check here 
    }
    return (CFaceLabelView *)pView; 
}

void CFaceLabelDoc::DiscardCurImg()
{
    vector<IMGINFO *>::iterator it; 
    int idx = 0; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++, idx++) 
    {
        IMGINFO *pInfo = *it; 
        if (idx == m_nImgIdx)
        {
            pInfo->m_LabelType = DISCARDED; 
            pInfo->m_nNumObj = 0; 
            SetModifiedFlag(TRUE); 
            UpdateAllViews(NULL); 
            break;
        }
    }
}

void CFaceLabelDoc::NegateCurImg()
{
    vector<IMGINFO *>::iterator it; 
    int idx = 0; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++, idx++) 
    {
        IMGINFO *pInfo = *it; 
        if (idx == m_nImgIdx)
        {
            pInfo->m_LabelType = NO_FACE; 
            pInfo->m_nNumObj = 0; 
            SetModifiedFlag(TRUE); 
            UpdateAllViews(NULL); 
            break;
        }
    }
}

void CFaceLabelDoc::UnannotateCurImg()
{
    vector<IMGINFO *>::iterator it; 
    int idx = 0; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++, idx++) 
    {
        IMGINFO *pInfo = *it; 
        if (idx == m_nImgIdx)
        {
            pInfo->m_LabelType = UNANNOTATED; 
            pInfo->m_nNumObj = 0; 
            SetModifiedFlag(TRUE); 
            UpdateAllViews(NULL); 
            break;
        }
    }
}

void CFaceLabelDoc::TogglePartialCurImg()
{
    vector<IMGINFO *>::iterator it; 
    int idx = 0; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++, idx++) 
    {
        IMGINFO *pInfo = *it; 
        if (idx == m_nImgIdx && pInfo->m_nNumObj > 0)
        {
            if (pInfo->m_LabelType == ALL_LABELED) 
                pInfo->m_LabelType = PARTIALLY_LABELED; 
            else if (pInfo->m_LabelType == PARTIALLY_LABELED)
                pInfo->m_LabelType = ALL_LABELED; 
            SetModifiedFlag(TRUE); 
            UpdateAllViews(NULL); 
            break;
        }
    }
}

BOOL CFaceLabelDoc::OnOpenDocument(LPCTSTR lpszPathName)
{
    //if (!CDocument::OnOpenDocument(lpszPathName))
    //    return FALSE;

    // TODO:  Add your specialized creation code here
    m_nImgIdx = -1;
    ReleaseImgInfoVec(); 
    m_strDirPath = ""; 
    UpdateAllViews(NULL); 

    m_strDirPath = lpszPathName; 
    int loc = m_strDirPath.ReverseFind('\\'); 
    m_strDirPath = m_strDirPath.Left(loc); 
    FILE *fp = fopen(lpszPathName, "r"); 
    if (fp == NULL) 
    {
        throw "Unable to read file"; 
        return FALSE; 
    }
    int nNumImgs; 
    fscanf(fp, "%d\n", &nNumImgs); 
    for (int i=0; i<nNumImgs; i++) 
    {
        IMGINFO *pInfo = new IMGINFO; 
        pInfo->ReadInfo(fp, LPCTSTR(m_strDirPath)); 
        m_ImgInfoVec.push_back(pInfo); 
    }
    fclose(fp); 
    if (m_ImgInfoVec.size() > 0) 
        m_nImgIdx = 0; 

    m_bNewFolder = true; 
    UpdateAllViews(NULL); 
    m_bNewFolder = false; 

    return TRUE;
}

BOOL CFaceLabelDoc::OnSaveDocument(LPCTSTR lpszPathName)
{
    // TODO: Add your specialized code here and/or call the base class

//    return CDocument::OnSaveDocument(lpszPathName);

    FILE *fp = fopen(lpszPathName, "w"); 
    if (fp == NULL) 
    {
        throw "Unable to write file"; 
        return FALSE; 
    }
    fprintf(fp, "%d\n", (int)m_ImgInfoVec.size()); 

    vector<IMGINFO *>::iterator it; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        pInfo->WriteInfo(fp, LPCTSTR(m_strDirPath)); 
    }
    fclose(fp); 

	SetModifiedFlag(FALSE);     // back to unmodified
    return TRUE; 
}

void CFaceLabelDoc::OnViewStatistics()
{
    // TODO: Add your command handler code here
    if (m_ImgInfoVec.size() == 0) 
        AfxMessageBox("No image loaded!"); 
    else
    {
        int totalNumImg = (int)m_ImgInfoVec.size(); 
        int totalNumNonFaceImg = 0; 
        int totalNumFaceImg = 0; 
        int totalNumFaces = 0; 
        int totalNumFrontalFaces = 0; 
        vector<IMGINFO *>::iterator it; 
        for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
        {
            IMGINFO *pInfo = *it; 
            if (pInfo->m_LabelType == ALL_LABELED || pInfo->m_LabelType == PARTIALLY_LABELED) 
            {
                totalNumFaceImg ++; 
                totalNumFaces += pInfo->m_nNumObj; 

                for (int i=0; i<pInfo->m_nNumObj; i++) 
                {
                    bool bOK = true; 
                    float xdist = pInfo->m_pObjFPts[i].reye.x - pInfo->m_pObjFPts[i].leye.x; 
                    float ydist = pInfo->m_pObjFPts[i].reye.y - pInfo->m_pObjFPts[i].leye.y; 
                    if (xdist <= 0) 
                        bOK = false; 
                    else
                    {
                        // check rotated faces 
                        float ratio = ydist/xdist; 
                        if (abs(ratio) > 0.25)  // tilt angle tan(theta) > 0.25
                            bOK = false; 
                    }

                    // check profile faces 
                    if (pInfo->m_pObjFPts[i].nose.x < pInfo->m_pObjFPts[i].leye.x || 
                        pInfo->m_pObjFPts[i].nose.x > pInfo->m_pObjFPts[i].reye.x || 
                        pInfo->m_pObjFPts[i].nose.x < pInfo->m_pObjFPts[i].lmouth.x || 
                        pInfo->m_pObjFPts[i].nose.x > pInfo->m_pObjFPts[i].rmouth.x)
                        bOK = false; 

                    if (bOK) 
                        totalNumFrontalFaces ++; 
                }
            }
            else if (pInfo->m_LabelType == NO_FACE)
                totalNumNonFaceImg ++; 
        }
        char msg[1000]; 
        sprintf(msg, "Total number of images = %d\n"
            "Total number of images containing faces = %d\n"
            "Total number of non-face images = %d\n"
            "Total number of labeled faces = %d\n"
            "Total number of labeled frontal faces = %d", 
            totalNumImg, totalNumFaceImg, totalNumNonFaceImg, totalNumFaces, totalNumFrontalFaces); 
        AfxMessageBox(msg, MB_OK | MB_ICONINFORMATION); 
    }
}
