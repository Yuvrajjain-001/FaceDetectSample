/******************************************************************************\
*
*   Member fuctions for the IMAGEINFO structure
*
\******************************************************************************/

#include "stdafx.h"
#include "imageinfo.h"

IMGINFO::IMGINFO() :
    m_szFileName(NULL),
    m_LabelType(UNANNOTATED),
    m_nNumObj(0),
    m_pObjRcs(NULL), 
    m_pObjFPts(NULL),
    m_bDetected(NULL),
    m_nNumDetectedPos(NULL), 
    m_nNumFPos(NULL),
    m_nNumMatchRect(NULL), 
    m_nImgWidth(0), 
    m_nImgHeight(0), 
    m_nPosCount(0), 
    m_nNegCount(0), 
    m_nTotalCount(0)
{
    m_LabelVec.clear(); 
}

IMGINFO::IMGINFO(FILE *fp, const char *szPath) :
    m_szFileName(NULL),
    m_LabelType(UNANNOTATED),
    m_nNumObj(0),
    m_pObjRcs(NULL), 
    m_pObjFPts(NULL),
    m_bDetected(NULL),
    m_nNumDetectedPos(NULL), 
    m_nNumFPos(NULL),
    m_nNumMatchRect(NULL), 
    m_nImgWidth(0), 
    m_nImgHeight(0), 
    m_nPosCount(0), 
    m_nNegCount(0), 
    m_nTotalCount(0)
{
    m_LabelVec.clear(); 

    ReadInfo(fp, szPath); 
}

IMGINFO::~IMGINFO()
{
    Release(); 
}

void IMGINFO::ReadInfo(FILE *fp, const char *szPath)
{
    char szName[MAX_PATH]; 
    // sorry, I am not checking any file reading errors here 
    fgets(szName, MAX_PATH, fp);    // file name may contain spaces, so we have to read the whole line here 
    int len = (int)strlen(szName); 
    szName[len-1] = '\0';           // remove the new line character 
    len = (int)(strlen(szPath) + strlen(szName) + 2); 
    m_szFileName = new char [len]; 
    sprintf(m_szFileName, "%s\\%s", szPath, szName); 

    /* // code to read old file
    fscanf(fp, "%d\n", &m_nNumObj); 
    if (m_nNumObj <= 0) 
    {
        m_LabelType = (LABELTYPE)m_nNumObj; 
        m_nNumObj = 0; 
    }
    else
        m_LabelType = ALL_LABELED; 
    */  // end of code to read old file

    fscanf(fp, "%d\n", &m_LabelType); 
    if (m_LabelType == ALL_LABELED || m_LabelType == PARTIALLY_LABELED) 
    {
        fscanf(fp, "%d\n", &m_nNumObj); 
        if (m_nNumObj <= 0) 
            throw "Corrupted label file"; 
        m_pObjFPts = new FEATUREPTS [m_nNumObj]; 
        if (!m_pObjFPts) 
            throw "Out of memory"; 
        for (int i=0; i<m_nNumObj; i++) 
        {
            fscanf(fp, "leye {%f,%f}\t", &m_pObjFPts[i].leye.x, &m_pObjFPts[i].leye.y); 
            fscanf(fp, "reye {%f,%f}\t", &m_pObjFPts[i].reye.x, &m_pObjFPts[i].reye.y); 
            fscanf(fp, "nose {%f,%f}\t", &m_pObjFPts[i].nose.x, &m_pObjFPts[i].nose.y); 
            fscanf(fp, "lmouth {%f,%f}\t", &m_pObjFPts[i].lmouth.x, &m_pObjFPts[i].lmouth.y); 
            fscanf(fp, "rmouth {%f,%f}\n", &m_pObjFPts[i].rmouth.x, &m_pObjFPts[i].rmouth.y); 
        }
        // translate to bounding box using the eye and nose locations 
        // this part may be further tuned in the future 
        m_pObjRcs = new IRECT [m_nNumObj]; 
        if (!m_pObjRcs) 
            throw "Out of memory"; 
        for (int i=0; i<m_nNumObj; i++) 
            MapFPts2Rc(&m_pObjFPts[i], &m_pObjRcs[i]); 
    }
}

void IMGINFO::WriteInfo(FILE *fp, const char *szPath)
{
    int len=-1; 
    if (szPath != NULL) 
        len = (int)strlen(szPath); 
    fprintf(fp, "%s\n%d\n", &m_szFileName[len+1], m_LabelType); 
    if (m_LabelType == ALL_LABELED || m_LabelType == PARTIALLY_LABELED) 
    {
        fprintf (fp, "%d\n", m_nNumObj); 
        for (int i=0; i<m_nNumObj; i++) 
        {
            fprintf(fp, "leye {%.2f,%.2f}\t", m_pObjFPts[i].leye.x, m_pObjFPts[i].leye.y); 
            fprintf(fp, "reye {%.2f,%.2f}\t", m_pObjFPts[i].reye.x, m_pObjFPts[i].reye.y); 
            fprintf(fp, "nose {%.2f,%.2f}\t", m_pObjFPts[i].nose.x, m_pObjFPts[i].nose.y); 
            fprintf(fp, "lmouth {%.2f,%.2f}\t", m_pObjFPts[i].lmouth.x, m_pObjFPts[i].lmouth.y); 
            fprintf(fp, "rmouth {%.2f,%.2f}\n", m_pObjFPts[i].rmouth.x, m_pObjFPts[i].rmouth.y); 
        }
    }
}

void IMGINFO::CheckInfo()
{
    if (m_LabelType == ALL_LABELED || m_LabelType == PARTIALLY_LABELED)
    {
        int numObj = 0; 
        FEATUREPTS *pObjFPts = new FEATUREPTS [m_nNumObj]; 

        for (int i=0; i<m_nNumObj; i++) 
        {
            bool bOK = true; 
            float xdist = m_pObjFPts[i].reye.x - m_pObjFPts[i].leye.x; 
            float ydist = m_pObjFPts[i].reye.y - m_pObjFPts[i].leye.y; 
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
            if (m_pObjFPts[i].nose.x < m_pObjFPts[i].leye.x || 
                m_pObjFPts[i].nose.x > m_pObjFPts[i].reye.x || 
                m_pObjFPts[i].nose.x < m_pObjFPts[i].lmouth.x || 
                m_pObjFPts[i].nose.x > m_pObjFPts[i].rmouth.x)
                bOK = false; 

            if (bOK) 
            {
                pObjFPts[numObj] = m_pObjFPts[i]; 
                numObj ++; 
            }
        }

        if (numObj == m_nNumObj) 
        {   // all faces are valid, do nothing
        }
        else if (numObj <= 0) 
        {
            m_nNumObj = 0; 
            m_LabelType = DISCARDED; 
        }
        else
        {
            m_nNumObj = numObj; 
            for (int i=0; i<numObj; i++) 
            {
                m_pObjFPts[i] = pObjFPts[i]; 
                MapFPts2Rc(&m_pObjFPts[i], &m_pObjRcs[i]); 
            }
            m_LabelType = PARTIALLY_LABELED; 
        }
        delete []pObjFPts; 
    }
}

void IMGINFO::Release()
{
    if (m_szFileName) { delete []m_szFileName; m_szFileName=NULL; }
    if (m_pObjRcs) { delete []m_pObjRcs; m_pObjRcs=NULL; }
    if (m_pObjFPts) { delete []m_pObjFPts; m_pObjFPts=NULL; }
    if (m_bDetected) { delete []m_bDetected; m_bDetected=NULL; } 
    if (m_nNumDetectedPos) { delete []m_nNumDetectedPos; m_nNumDetectedPos=NULL; }
    if (m_nNumFPos) { delete []m_nNumFPos; m_nNumFPos=NULL; }
    if (m_nNumMatchRect) { delete []m_nNumMatchRect; m_nNumMatchRect=NULL; }

    m_LabelType = UNANNOTATED; 
    m_nNumObj = 0; 
    m_nImgWidth = 0;  
    m_nImgHeight = 0; 
    m_nPosCount = 0; 
    m_nNegCount = 0; 
    m_nTotalCount = 0; 
    m_LabelVec.clear(); 
}

void IMGINFO::ReleaseObjs()
{
    if (m_pObjRcs) { delete []m_pObjRcs; m_pObjRcs=NULL; }
    if (m_pObjFPts) { delete []m_pObjFPts; m_pObjFPts=NULL; }
    m_nNumObj = 0; 
    m_LabelType = UNANNOTATED; 
}