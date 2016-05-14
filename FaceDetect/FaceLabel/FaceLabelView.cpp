// FaceLabelView.cpp : implementation of the CFaceLabelView class
//

#include "stdafx.h"
#include "FaceLabel.h"

#include "FaceLabelDoc.h"
#include "FaceLabelView.h"
#include ".\facelabelview.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CFaceLabelView

IMPLEMENT_DYNCREATE(CFaceLabelView, CScrollView)

BEGIN_MESSAGE_MAP(CFaceLabelView, CScrollView)
    ON_WM_KEYDOWN()
    ON_WM_CHAR()
    ON_WM_LBUTTONDBLCLK()
    ON_WM_LBUTTONDOWN()
    ON_WM_MOUSEWHEEL()
    ON_WM_RBUTTONDOWN()
    ON_WM_RBUTTONUP()
    ON_WM_MOUSEMOVE()
END_MESSAGE_MAP()

// CFaceLabelView construction/destruction

CFaceLabelView::CFaceLabelView()
{
	// TODO: add construction code here
    m_nDispImgIdx = -1; 
    m_bInAnnotate = false; 
    m_bInQuickZoom = false; 
    m_bTargetedQuickZoom = false; 
    m_Img.Release(); 
}

CFaceLabelView::~CFaceLabelView()
{
}

BOOL CFaceLabelView::PreCreateWindow(CREATESTRUCT& cs)
{
	// TODO: Modify the Window class or styles here by modifying
	//  the CREATESTRUCT cs

	return CScrollView::PreCreateWindow(cs);
}

// CFaceLabelView drawing

void CFaceLabelView::OnDraw(CDC* pDC)
{
	CFaceLabelDoc* pDoc = GetDocument();
	ASSERT_VALID(pDoc);
	if (!pDoc)
		return;

	// TODO: add draw code for native data here
    int nWidth = m_Img.GetWidth(); 
    int nHeight = m_Img.GetHeight(); 
    if (m_nDispImgIdx == -1 || nWidth==0 || nHeight==0) return; 

    // draw the image 
	struct	{
		BITMAPINFOHEADER header;
		unsigned long mpbul[256]; // 256 grey level image
	}	bmi;

    ZeroMemory(&bmi.header, sizeof(BITMAPINFOHEADER)); 
    bmi.header.biSize = 40; 
    bmi.header.biWidth = nWidth; 
    bmi.header.biHeight = -nHeight; 
    bmi.header.biPlanes = 1; 
    bmi.header.biSizeImage = m_Img.GetStride()*nHeight; 
    bmi.header.biBitCount = 8; 
	// Fill the color table for displaying grey image
	bmi.mpbul[0] = 0;
	for(int iColor = 1; iColor < 256; iColor ++)
        bmi.mpbul[iColor] = bmi.mpbul[iColor - 1] + 0x010101;

	CSize sizeTotal;
    float fScale = pDoc->GetDispScale(); 
    sizeTotal.cx = (LONG)(nWidth*fScale); 
    sizeTotal.cy = (LONG)(nHeight*fScale);
	SetScrollSizes(MM_TEXT, sizeTotal);

    if (m_bTargetedQuickZoom) 
    {
        RECT rc; 
        GetClientRect(&rc); 
        POINT pt; 
        pt.x = LONG((m_QuickZoomRc.m_ixMin+m_QuickZoomRc.m_ixMax)*fScale/2-rc.right/2); 
        pt.y = LONG((m_QuickZoomRc.m_iyMin+m_QuickZoomRc.m_iyMax)*fScale/2-rc.bottom/2); 
        ScrollToPosition(pt); 
        m_bTargetedQuickZoom = false; 
    }

    HDC hdc = pDC->GetSafeHdc(); 
    int oldMode = SetStretchBltMode(hdc, COLORONCOLOR); 
	StretchDIBits(hdc, 0, 0, sizeTotal.cx, sizeTotal.cy,
                  0,0,nWidth,nHeight,m_Img.GetDataPtr(),
       	          (BITMAPINFO *) &bmi, DIB_RGB_COLORS, SRCCOPY);
    SetStretchBltMode(hdc, oldMode); 

    // now put on the face marks 
    for (int i=0; i<MAX_NUM_FACES; i++) 
    {
        if (m_GTObjFlags[i] == VALID_GT) 
        {
            DrawFeatures(pDC, &m_GTObjFPts[i]); 
            DrawRect(pDC, &m_GTObjRcs[i], RGB(0,0,255)); 
        }
        else if (m_GTObjFlags[i] == PARTIAL_GT)
        {
            DrawFeatures(pDC, &m_GTObjFPts[i], m_nAnnotateStep); 
        }
    }

    // draw the quick zoom rectangle if necessary 
    if (m_bInQuickZoom)
    {
        DrawRect(pDC, &m_QuickZoomRc, RGB(255,255,0)); 
    }
}

void CFaceLabelView::OnInitialUpdate()
{
	CScrollView::OnInitialUpdate();

    ModifyStyle(0, WS_VSCROLL); 

	CSize sizeTotal;
	// TODO: calculate the total size of this view
	sizeTotal.cx = sizeTotal.cy = 100;
	SetScrollSizes(MM_TEXT, sizeTotal);
}


float CFaceLabelView::FindScale(int imgs, int clnts)
{
    float scale = 1.0; 
    if (imgs == 0) 
        return MAX_SCALE; 
    if (clnts == 0)
        return MIN_SCALE; 

    if (imgs > clnts) 
    {
        while (imgs*scale > clnts)
            scale /= SCALE_STEP; 
    }
    else if (imgs < clnts) 
    {
        while (imgs*scale < clnts) 
            scale *= SCALE_STEP; 
        scale /= SCALE_STEP; 
    }
    scale = min(scale, MAX_SCALE); 
    scale = max(scale, MIN_SCALE); 
    return scale; 
}

float CFaceLabelView::FindBestDispScale()
{
    RECT rc; 
    GetClientRect(&rc); 

    int width = m_Img.GetWidth(); 
    int height = m_Img.GetHeight(); 

    float scale1 = FindScale(width, rc.right); 
    float scale2 = FindScale(height, rc.bottom); 
    float scale = min(scale1, scale2); 
    return scale; 
}

void CFaceLabelView::OnUpdate(CView* pSender, LPARAM lHint, CObject* pHint)
{
    CScrollView::OnUpdate(pSender, lHint, pHint); 
    CFaceLabelDoc *pDoc = GetDocument(); 
    if (pDoc->IsNewFolder()) 
    {
        m_nDispImgIdx = -1; 
        m_Img.Release(); 
    }

    vector<IMGINFO *> &imgInfoVec = pDoc->GetImgInfoVec(); 
    int nIdx = pDoc->GetImgIdx(); 
    if (m_nDispImgIdx!=nIdx) 
    {
        // load the image 
        if (nIdx>=0 && nIdx<(int)imgInfoVec.size())
        {
            vector<IMGINFO *>::iterator it; 
            int idx = 0; 
            for (it=imgInfoVec.begin(); it!=imgInfoVec.end(); it++, idx++) 
            {
                if (idx == nIdx) 
                {
                    m_pImgInfo = *it; 
                    m_strImgName = m_pImgInfo->m_szFileName; 
                }
            }
            m_Img.Load(LPCTSTR(m_strImgName)); 
            pDoc->SetDispScale(FindBestDispScale()); 
            m_nDispImgIdx = nIdx; 
        }
        else
        {
            m_pImgInfo = NULL; 
            m_Img.Release(); 
            m_nDispImgIdx = -1; 
        }
        // prepare for annotation 
        if (m_nDispImgIdx >= 0) 
        {
            ResetGTObjFlags();
            for (int i=0; i<m_pImgInfo->m_nNumObj; i++) 
            {
                m_GTObjFlags[i] = VALID_GT; 
                m_GTObjFPts[i] = m_pImgInfo->m_pObjFPts[i]; 
                MapFPts2Rc(&m_GTObjFPts[i], &m_GTObjRcs[i]); 
            }
        }
    }
}

// CFaceLabelView diagnostics

#ifdef _DEBUG
void CFaceLabelView::AssertValid() const
{
	CScrollView::AssertValid();
}

void CFaceLabelView::Dump(CDumpContext& dc) const
{
	CScrollView::Dump(dc);
}

CFaceLabelDoc* CFaceLabelView::GetDocument() const // non-debug version is inline
{
	ASSERT(m_pDocument->IsKindOf(RUNTIME_CLASS(CFaceLabelDoc)));
	return (CFaceLabelDoc*)m_pDocument;
}
#endif //_DEBUG


// CFaceLabelView message handlers

void CFaceLabelView::OnKeyDown(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    // TODO: Add your message handler code here and/or call default

    CScrollView::OnKeyDown(nChar, nRepCnt, nFlags);
    CFaceLabelDoc *pDoc = GetDocument(); 

    switch (nChar) 
    {
    case VK_HOME:
        pDoc->GoToFirstImg(); 
        break; 
    case VK_END:
        pDoc->GoToLastImg(); 
        break; 
    case VK_PRIOR:
    case VK_UP:
        pDoc->GoToPrevImg(); 
        break;
    case VK_NEXT:
    case VK_DOWN:
        pDoc->GoToNextImg(); 
        break; 
    case VK_ESCAPE:
        // stop partial annotation 
        //if (m_bInAnnotate) 
        //{
        //    m_nAnnotateStep=0; 
        //    m_bInAnnotate = false; 
        //    for (int i=0; i<MAX_NUM_FACES; i++) 
        //    {
        //        if (m_GTObjFlags[i] == PARTIAL_GT) 
        //            m_GTObjFlags[i] = INVALID_GT; 
        //    }
        //    Invalidate(0); 
        //    SetClassLong(m_hWnd, GCL_HCURSOR, (LONG)LoadCursor(NULL, IDC_ARROW));
        //}
        break; 
    default: 
        break; 
    }
}

void CFaceLabelView::OnChar(UINT nChar, UINT nRepCnt, UINT nFlags)
{
    // TODO: Add your message handler code here and/or call default

    CScrollView::OnChar(nChar, nRepCnt, nFlags);
    CFaceLabelDoc *pDoc = GetDocument(); 

    switch(nChar) 
    {
    case '=':
    case '+': 
        pDoc->DispZoomIn(); 
        break; 
    case '-':
    case '_':
        pDoc->DispZoomOut(); 
        break; 
    case '0':
    case ')': 
        pDoc->DispZoomReset(); 
        break;
    case '9': 
    case '(': 
    case 'e':
    case 'E':
        pDoc->SetDispScale(FindBestDispScale()); 
        Invalidate(0);
        break; 
    case 'd':
    case 'D': 
        if (!m_bInAnnotate)
        {
            pDoc->DiscardCurImg(); 
            pDoc->GoToNextImg(); 
        }
        break; 
    case 's':
    case 'S': 
        if (!m_bInAnnotate)
        {
            pDoc->NegateCurImg(); 
            pDoc->GoToNextImg(); 
        }
        break; 
    case 'w':
    case 'W': 
        // wipe all existing labels
        if (!m_bInAnnotate)
        {
            pDoc->UnannotateCurImg(); 
            ResetGTObjFlags();
        }
        break; 
    case 'a':
    case 'A': 
        // mark the image as partially annotated (the background image can't be used as negative data)
        if (!m_bInAnnotate)
        {
            pDoc->TogglePartialCurImg(); 
        }
    case 'q':
    case 'Q': 
        // stop partial annotation 
        if (m_bInAnnotate) 
        {
            m_nAnnotateStep=0; 
            m_bInAnnotate = false; 
            for (int i=0; i<MAX_NUM_FACES; i++) 
            {
                if (m_GTObjFlags[i] == PARTIAL_GT) 
                    m_GTObjFlags[i] = INVALID_GT; 
            }
            Invalidate(0); 
            SetClassLong(m_hWnd, GCL_HCURSOR, (LONG)LoadCursor(NULL, IDC_ARROW));
        }
        break; 
    case 'f':
    case 'F': 
        if (!m_bInAnnotate)
        {
            pDoc->GoToNextImg(); 
        }
        break; 
    case 'v':
    case 'V': 
        if (!m_bInAnnotate)
        {
            pDoc->GoToPrevImg(); 
        }
        break; 
    default:
        break; 
    }
}

void CFaceLabelView::OnLButtonDblClk(UINT nFlags, CPoint point)
{
    // TODO: Add your message handler code here and/or call default

    CScrollView::OnLButtonDblClk(nFlags, point);
    CFaceLabelDoc *pDoc = GetDocument(); 

    CPoint pointScroll = GetScrollPosition(); 
    float fScale = pDoc->GetDispScale(); 
    FPOINT pt; 
    pt.x = float((point.x+pointScroll.x)/fScale); 
    pt.y = float((point.y+pointScroll.y)/fScale); 

    if (!m_bInAnnotate && m_nDispImgIdx >= 0)
    {
        m_bInAnnotate = true; 
        m_nAnnotateStep = 0; 
        m_nAnnotateIdx = -1; 

        // check if the clicked point is within some annotated faces 
        for (int i=MAX_NUM_FACES-1; i>=0; i--) 
        {
            if (m_GTObjFlags[i] == VALID_GT) 
            {
                if (m_GTObjRcs[i].Contains(pt)) 
                {
                    m_GTObjFlags[i] = INVALID_GT; 
                    m_bInAnnotate = false; 
                    UpdateDocObjs(); 
                    Invalidate(0); 
                    return; 
                }
            }
            else if (m_GTObjFlags[i] == INVALID_GT) 
            {
                m_nAnnotateIdx = i; 
            }
        }
        if (m_nAnnotateIdx == -1) 
        {
            MessageBox("Too many faces in one image!"); 
            m_bInAnnotate = false; 
            return; 
        }
        
        Invalidate(0); 
        SetClassLong(m_hWnd, GCL_HCURSOR, (LONG)LoadCursor(NULL, IDC_CROSS));
    }

}

void CFaceLabelView::OnLButtonDown(UINT nFlags, CPoint point)
{
    // TODO: Add your message handler code here and/or call default

    CScrollView::OnLButtonDown(nFlags, point);
    CFaceLabelDoc *pDoc = GetDocument(); 

    CPoint pointScroll = GetScrollPosition(); 
    float fScale = pDoc->GetDispScale(); 
    float x = float((point.x+pointScroll.x)/fScale); 
    float y = float((point.y+pointScroll.y)/fScale); 

    if (m_bInAnnotate)
    {
        switch (m_nAnnotateStep)
        {
        case 0: 
            m_GTObjFPts[m_nAnnotateIdx].leye.x = x; 
            m_GTObjFPts[m_nAnnotateIdx].leye.y = y; 
            m_GTObjFlags[m_nAnnotateIdx] = PARTIAL_GT; 
            m_nAnnotateStep++; 
            break;
        case 1: 
            m_GTObjFPts[m_nAnnotateIdx].reye.x = x; 
            m_GTObjFPts[m_nAnnotateIdx].reye.y = y; 
            m_nAnnotateStep++; 
            break;
        case 2: 
            m_GTObjFPts[m_nAnnotateIdx].nose.x = x; 
            m_GTObjFPts[m_nAnnotateIdx].nose.y = y; 
            m_nAnnotateStep++; 
            break;
        case 3: 
            m_GTObjFPts[m_nAnnotateIdx].lmouth.x = x; 
            m_GTObjFPts[m_nAnnotateIdx].lmouth.y = y; 
            m_nAnnotateStep++; 
            break;
        case 4: 
            m_GTObjFPts[m_nAnnotateIdx].rmouth.x = x; 
            m_GTObjFPts[m_nAnnotateIdx].rmouth.y = y; 
            MapFPts2Rc(&m_GTObjFPts[m_nAnnotateIdx], &m_GTObjRcs[m_nAnnotateIdx]); 
            m_GTObjFlags[m_nAnnotateIdx] = VALID_GT; 
            UpdateDocObjs(); 
            m_nAnnotateStep=0; 
            m_bInAnnotate = false; 
            SetClassLong(m_hWnd, GCL_HCURSOR, (LONG)LoadCursor(NULL, IDC_ARROW));
            break;
        }
        Invalidate(0); 
    }
}

void CFaceLabelView::DrawFeatures(CDC* pDC, FEATUREPTS *pFPts, int step)
{
    CFaceLabelDoc *pDoc = GetDocument(); 
    float fScale = pDoc->GetDispScale(); 

    CPen newPen(PS_SOLID, 1, RGB(255,0,0)); 
    CPen *pOldPen = pDC->SelectObject(&newPen); 

    if (step > 0) 
    {
        int x = (int)(pFPts->leye.x*fScale+0.5); 
        int y = (int)(pFPts->leye.y*fScale+0.5); 
        pDC->MoveTo(x-3, y); 
        pDC->LineTo(x+4, y); 
        pDC->MoveTo(x, y-3); 
        pDC->LineTo(x, y+4); 
    }
    if (step > 1) 
    {
        int x = (int)(pFPts->reye.x*fScale+0.5); 
        int y = (int)(pFPts->reye.y*fScale+0.5); 
        pDC->MoveTo(x-3, y); 
        pDC->LineTo(x+4, y); 
        pDC->MoveTo(x, y-3); 
        pDC->LineTo(x, y+4); 
    }
    if (step > 2) 
    {
        int x = (int)(pFPts->nose.x*fScale+0.5); 
        int y = (int)(pFPts->nose.y*fScale+0.5); 
        pDC->MoveTo(x-3, y); 
        pDC->LineTo(x+4, y); 
        pDC->MoveTo(x, y-3); 
        pDC->LineTo(x, y+4); 
    }
    if (step > 3) 
    {
        int x = (int)(pFPts->lmouth.x*fScale+0.5); 
        int y = (int)(pFPts->lmouth.y*fScale+0.5); 
        pDC->MoveTo(x-3, y); 
        pDC->LineTo(x+4, y); 
        pDC->MoveTo(x, y-3); 
        pDC->LineTo(x, y+4); 
    }
    if (step > 4) 
    {
        int x = (int)(pFPts->rmouth.x*fScale+0.5); 
        int y = (int)(pFPts->rmouth.y*fScale+0.5); 
        pDC->MoveTo(x-3, y); 
        pDC->LineTo(x+4, y); 
        pDC->MoveTo(x, y-3); 
        pDC->LineTo(x, y+4); 
    }

    pDC->SelectObject(pOldPen); 
}

void CFaceLabelView::DrawRect(CDC *pDC, IRECT *pRc, COLORREF crColor, bool bScale)
{
    CFaceLabelDoc *pDoc = GetDocument(); 
    
    float fScale = pDoc->GetDispScale(); 
    if (!bScale)
        fScale = 1.0f; 

    CPen newPen(PS_SOLID, 2, crColor); 
    CPen *pOldPen = pDC->SelectObject(&newPen); 

    int xmin = (int)(pRc->m_ixMin*fScale+0.5); 
    int ymin = (int)(pRc->m_iyMin*fScale+0.5); 
    int xmax = (int)(pRc->m_ixMax*fScale+0.5); 
    int ymax = (int)(pRc->m_iyMax*fScale+0.5); 
    pDC->MoveTo(xmin, ymin); 
    pDC->LineTo(xmax, ymin); 
    pDC->LineTo(xmax, ymax); 
    pDC->LineTo(xmin, ymax); 
    pDC->LineTo(xmin, ymin); 

    pDC->SelectObject(pOldPen); 
}

void CFaceLabelView::UpdateDocObjs()
{
    CFaceLabelDoc *pDoc = GetDocument(); 
    int nCount = 0; 
    for (int i=0; i<MAX_NUM_FACES; i++)
        if (m_GTObjFlags[i] == VALID_GT) 
            nCount ++; 
    int nIdx = 0; 
    vector<IMGINFO *> &imgInfoVec = pDoc->GetImgInfoVec(); 
    vector<IMGINFO *>::iterator it; 
    for (it=imgInfoVec.begin(); it!=imgInfoVec.end(); it++, nIdx++) 
    {
        if (m_nDispImgIdx == nIdx) 
        {
            IMGINFO *pInfo = *it; 
            LABELTYPE oldType = pInfo->m_LabelType; 
            pInfo->ReleaseObjs(); 
            pInfo->m_nNumObj = nCount; 

            if (nCount <= 0) 
                pInfo->m_LabelType = NO_FACE; 
            else if (oldType == PARTIALLY_LABELED)
                pInfo->m_LabelType = PARTIALLY_LABELED; 
            else
                pInfo->m_LabelType = ALL_LABELED; 

            pInfo->m_pObjRcs = new IRECT [nCount]; 
            pInfo->m_pObjFPts = new FEATUREPTS [nCount]; 
            if (!pInfo->m_pObjRcs || !pInfo->m_pObjFPts) 
                throw "out of memory"; 
            int nCnt = 0; 
            for (int i=0; i<MAX_NUM_FACES; i++)
            {
                if (m_GTObjFlags[i] == VALID_GT) 
                {
                    pInfo->m_pObjRcs[nCnt] = m_GTObjRcs[i]; 
                    pInfo->m_pObjFPts[nCnt++] = m_GTObjFPts[i]; 
                }
            }
            break; 
        }
    }
    pDoc->SetModifiedFlag(TRUE); 
    pDoc->UpdateAllViews(this); 
}

BOOL CFaceLabelView::OnMouseWheel(UINT nFlags, short zDelta, CPoint pt)
{
    // TODO: Add your message handler code here and/or call default
    CFaceLabelDoc *pDoc = GetDocument(); 
    if (zDelta > 0) 
    {
        pDoc->DispZoomIn(); 
    }
    else if (zDelta < 0) 
    {
        pDoc->DispZoomOut(); 
    }

    return CScrollView::OnMouseWheel(nFlags, zDelta, pt);
}

void CFaceLabelView::OnRButtonDown(UINT nFlags, CPoint point)
{
    // TODO: Add your message handler code here and/or call default
    CFaceLabelDoc *pDoc = GetDocument(); 
    CPoint pointScroll = GetScrollPosition(); 
    float fScale = pDoc->GetDispScale(); 
    m_bInQuickZoom = true; 
    m_MouseDownPos = point; 
    m_QuickZoomRc.m_ixMin = m_QuickZoomRc.m_ixMax = (int)((point.x+pointScroll.x)/fScale+0.5); 
    m_QuickZoomRc.m_iyMin = m_QuickZoomRc.m_iyMax = (int)((point.y+pointScroll.y)/fScale+0.5); 

//    CScrollView::OnRButtonDown(nFlags, point);
}

void CFaceLabelView::OnMouseMove(UINT nFlags, CPoint point)
{
    // TODO: Add your message handler code here and/or call default
    if (m_bInQuickZoom)
    {
        CFaceLabelDoc *pDoc = GetDocument(); 
        CPoint pointScroll = GetScrollPosition(); 
        float fScale = pDoc->GetDispScale(); 
        m_QuickZoomRc.m_ixMin = (int)((min(point.x, m_MouseDownPos.x)+pointScroll.x)/fScale+0.5); 
        m_QuickZoomRc.m_iyMin = (int)((min(point.y, m_MouseDownPos.y)+pointScroll.y)/fScale+0.5); 
        m_QuickZoomRc.m_ixMax = (int)((max(point.x, m_MouseDownPos.x)+pointScroll.x)/fScale+0.5); 
        m_QuickZoomRc.m_iyMax = (int)((max(point.y, m_MouseDownPos.y)+pointScroll.y)/fScale+0.5); 
        Invalidate(0); 
    }

//    CScrollView::OnMouseMove(nFlags, point);
}

void CFaceLabelView::OnRButtonUp(UINT nFlags, CPoint point)
{
    // TODO: Add your message handler code here and/or call default
    if (m_bInQuickZoom) 
    {
        CFaceLabelDoc *pDoc = GetDocument(); 
        CPoint pointScroll = GetScrollPosition(); 
        float fScale = pDoc->GetDispScale(); 
        m_QuickZoomRc.m_ixMin = (int)((min(point.x, m_MouseDownPos.x)+pointScroll.x)/fScale+0.5); 
        m_QuickZoomRc.m_iyMin = (int)((min(point.y, m_MouseDownPos.y)+pointScroll.y)/fScale+0.5); 
        m_QuickZoomRc.m_ixMax = (int)((max(point.x, m_MouseDownPos.x)+pointScroll.x)/fScale+0.5); 
        m_QuickZoomRc.m_iyMax = (int)((max(point.y, m_MouseDownPos.y)+pointScroll.y)/fScale+0.5); 
        int nWidth = m_QuickZoomRc.m_ixMax-m_QuickZoomRc.m_ixMin; 
        int nHeight = m_QuickZoomRc.m_iyMax-m_QuickZoomRc.m_iyMin; 
        RECT rc; 
        GetClientRect(&rc); 

        float scale1 = FindScale(nWidth, rc.right); 
        float scale2 = FindScale(nHeight, rc.bottom); 
        float scale = min(scale1, scale2); 
        pDoc->SetDispScale(scale); 

        m_bInQuickZoom = false; 
        m_bTargetedQuickZoom = true; 
        Invalidate(0); 
    }
//    CScrollView::OnRButtonUp(nFlags, point);
}
