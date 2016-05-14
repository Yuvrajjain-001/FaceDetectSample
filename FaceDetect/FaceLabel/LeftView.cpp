// LeftView.cpp : implementation of the CLeftView class
//

#include "stdafx.h"
#include "FaceLabel.h"

#include "FaceLabelView.h"
#include "FaceLabelDoc.h"
#include "LeftView.h"
#include ".\leftview.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CLeftView

IMPLEMENT_DYNCREATE(CLeftView, CTreeView)

BEGIN_MESSAGE_MAP(CLeftView, CTreeView)
    ON_NOTIFY_REFLECT(TVN_SELCHANGED, OnSelchanged)
    ON_WM_KEYDOWN()
    ON_WM_CHAR()
END_MESSAGE_MAP()


// CLeftView construction/destruction

CLeftView::CLeftView()
{
	// TODO: add construction code here
    HICON hIcon[5]; 
    m_ImageList.Create(16, 16, ILC_COLOR, 5, 5); 
    hIcon[0] = AfxGetApp()->LoadIcon(IDI_BLANK); 
    hIcon[1] = AfxGetApp()->LoadIcon(IDI_DISCARD); 
    hIcon[2] = AfxGetApp()->LoadIcon(IDI_NOFACE); 
    hIcon[3] = AfxGetApp()->LoadIcon(IDI_FACE); 
    hIcon[4] = AfxGetApp()->LoadIcon(IDI_PARTFACE); 
    for (int i=0; i<5; i++) 
        m_ImageList.Add(hIcon[i]); 
}

CLeftView::~CLeftView()
{
}

BOOL CLeftView::PreCreateWindow(CREATESTRUCT& cs)
{
	// TODO: Modify the Window class or styles here by modifying the CREATESTRUCT cs

	return CTreeView::PreCreateWindow(cs);
}

void CLeftView::OnInitialUpdate()
{
	CTreeView::OnInitialUpdate();
    CTreeCtrl &treeCtrl = GetTreeCtrl(); 
    treeCtrl.ModifyStyle(0, TVS_SHOWSELALWAYS); 

	// TODO: You may populate your TreeView with items by directly accessing
	//  its tree control through a call to GetTreeCtrl().
    treeCtrl.SetImageList(&m_ImageList, TVSIL_NORMAL); 
}

void CLeftView::OnUpdate(CView* pSender, LPARAM lHint, CObject* pHint)
{
	CTreeView::OnUpdate(pSender, lHint, pHint);

	// TODO: You may populate your TreeView with items by directly accessing
	//  its tree control through a call to GetTreeCtrl().
    CFaceLabelDoc *pDoc = GetDocument(); 
    CString strDirPath = pDoc->GetDirPath(); 
    vector<IMGINFO *> &imgInfoVec = pDoc->GetImgInfoVec(); 
    vector<IMGINFO *>::iterator it; 
    CTreeCtrl &treeCtrl = GetTreeCtrl(); 

    if (pDoc->IsNewFolder())
    {
        treeCtrl.DeleteAllItems(); 

        HTREEITEM hItem0 = NULL; 
        int idx = 0; 
        for (it=imgInfoVec.begin(); it!=imgInfoVec.end(); it++, idx++) 
        {
            IMGINFO *pInfo = *it; 
            int len = strDirPath.GetLength(); 
            CString str = &pInfo->m_szFileName[len+1]; 
            HTREEITEM hItem = treeCtrl.InsertItem(LPCTSTR(str)); 
            if (pInfo->m_LabelType == UNANNOTATED) 
            {
                treeCtrl.SetItemState(hItem, TVIS_BOLD, TVIS_BOLD); 
                BOOL bRet = treeCtrl.SetItemImage(hItem, 0, 0); 
                if (bRet) 
                {
                    int x = 0; 
                }
            }
            else if (pInfo->m_LabelType == DISCARDED) 
            {
                treeCtrl.SetItemImage(hItem, 1, 1); 
            }
            else if (pInfo->m_LabelType == NO_FACE) 
            {
                treeCtrl.SetItemImage(hItem, 2, 2); 
            }
            else if (pInfo->m_LabelType == ALL_LABELED) 
            {
                treeCtrl.SetItemImage(hItem, 3, 3); 
            }
            else if (pInfo->m_LabelType == PARTIALLY_LABELED) 
            {
                treeCtrl.SetItemImage(hItem, 4, 4); 
            }
            if (idx == pDoc->GetImgIdx()) 
                hItem0 = hItem; 
        }

        if (hItem0 != NULL) 
        {
            treeCtrl.Select(hItem0, TVGN_CARET); 
//            treeCtrl.SelectSetFirstVisible(hItem0); 
        }
    }
    else
    {
        HTREEITEM hItem = treeCtrl.GetRootItem();
        int idx = 0; 
        for (it=imgInfoVec.begin(); it!=imgInfoVec.end(); it++, idx++) 
        {
            IMGINFO *pInfo = *it; 
            if (idx == pDoc->GetImgIdx())
            {
                treeCtrl.SetItemState(hItem, 0, TVIS_BOLD); 
                if (pInfo->m_LabelType == UNANNOTATED) 
                {
                    treeCtrl.SetItemState(hItem, TVIS_BOLD, TVIS_BOLD); 
                    treeCtrl.SetItemImage(hItem, 0, 0); 
                }
                else if (pInfo->m_LabelType == DISCARDED) 
                {
                    treeCtrl.SetItemImage(hItem, 1, 1); 
                }
                else if (pInfo->m_LabelType == NO_FACE) 
                {
                    treeCtrl.SetItemImage(hItem, 2, 2); 
                }
                else if (pInfo->m_LabelType == ALL_LABELED) 
                {
                    treeCtrl.SetItemImage(hItem, 3, 3); 
                }
                else if (pInfo->m_LabelType == PARTIALLY_LABELED) 
                {
                    treeCtrl.SetItemImage(hItem, 4, 4); 
                }
                treeCtrl.Select(hItem, TVGN_CARET); 
                break;
            }
            hItem = treeCtrl.GetNextItem(hItem, TVGN_NEXT); 
        }
    }
}

// CLeftView diagnostics

#ifdef _DEBUG
void CLeftView::AssertValid() const
{
	CTreeView::AssertValid();
}

void CLeftView::Dump(CDumpContext& dc) const
{
	CTreeView::Dump(dc);
}

CFaceLabelDoc* CLeftView::GetDocument() // non-debug version is inline
{
	ASSERT(m_pDocument->IsKindOf(RUNTIME_CLASS(CFaceLabelDoc)));
	return (CFaceLabelDoc*)m_pDocument;
}
#endif //_DEBUG


// CLeftView message handlers
void CLeftView::OnSelchanged(NMHDR* pNMHDR, LRESULT* pResult) 
{
    CFaceLabelDoc *pDoc = GetDocument(); 
    CTreeCtrl &treeCtrl = GetTreeCtrl(); 
    HTREEITEM hItem = treeCtrl.GetSelectedItem(); 
    CString strName = treeCtrl.GetItemText(hItem); 
    CString strPath = pDoc->GetDirPath() + "\\" + strName; 

    vector<IMGINFO *> &imgInfoVec = pDoc->GetImgInfoVec(); 
    vector<IMGINFO *>::iterator it; 
    HTREEITEM hItem0 = NULL; 
    int idx = 0; 
    for (it=imgInfoVec.begin(); it!=imgInfoVec.end(); it++, idx++) 
    {
        IMGINFO *pInfo = *it; 
        CString str = pInfo->m_szFileName; 
        if (strPath.CompareNoCase(str) == 0)
            pDoc->SetImgIdx(idx); 
    }
    pDoc->UpdateAllViews(this); 
}

void CLeftView::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    // TODO: Add your message handler code here and/or call default

//    CTreeView::OnKeyDown(nChar, nRepCnt, nFlags);

    CFaceLabelDoc *pDoc = GetDocument(); 
    CFaceLabelView *pView = pDoc->GetFaceLabelView(); 
    pView->OnKeyDown(nChar, nRepCnt, nFlags); 
}

void CLeftView::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    // TODO: Add your message handler code here and/or call default

//    CTreeView::OnChar(nChar, nRepCnt, nFlags);

    CFaceLabelDoc *pDoc = GetDocument(); 
    CFaceLabelView *pView = pDoc->GetFaceLabelView(); 
    pView->OnChar(nChar, nRepCnt, nFlags); 
}

