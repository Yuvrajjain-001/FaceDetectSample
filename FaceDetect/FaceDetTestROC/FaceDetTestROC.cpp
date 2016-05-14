#include "stdafx.h"
#include "imageinfo.h"
#include "detector.h" 

using namespace std; 

char szClassifierFile[MAX_PATH]; 
float fStepSize; 
float fStepScale; 
int nNumTh; 
float *pfTh; 
bool bFast; 
vector<IMGINFO *> ImgInfoVec; 

void Usage()
{
    char *msg =
        "\n"
        "Tool for generating ROC curves for a given face detector.\n"
        "\n"
        "\n"
        "FaceDetTestROC fileName minTh maxTh stepTh rej\n"
        "\n"
        "    fileName      -- name of a test configuration file\n"
        "    minTh         -- minimum threshold to try\n" 
        "    maxTh         -- maximum threshold to try\n" 
        "    stepTh        -- stepsize of the threshold\n"
        "\n";

    printf("%s\n", msg);
}

void ReleaseImgInfoVec()
{
    if (!ImgInfoVec.empty()) 
    {
        vector<IMGINFO *>::iterator it; 
        for (it=ImgInfoVec.begin(); it!=ImgInfoVec.end(); it++) 
        {
            IMGINFO *pInfo = *it; 
            delete pInfo; 
        }
        ImgInfoVec.clear(); 
    }
}

bool LoadImageInfo(const char *szPath)
{
    char szName[MAX_PATH]; 
    sprintf(szName, "%s\\label.txt", szPath); 
    FILE *fpLabel = fopen(szName, "r"); 
    if (fpLabel == NULL) 
    {
        throw "null file"; 
        return false; 
    }

    int startsize = (int)ImgInfoVec.size(); 
    int nNumImgs; 
    fscanf(fpLabel, "%d\n", &nNumImgs); 
    for (int i=0; i<nNumImgs; i++) 
    {
        IMGINFO *pInfo = new IMGINFO; 
        pInfo->ReadInfo(fpLabel, szPath); 
//        pInfo->CheckInfo(); 
        if (pInfo->m_LabelType == UNANNOTATED || pInfo->m_LabelType == DISCARDED) 
        {
            delete pInfo; 
            continue; 
        }

        pInfo->m_nNumDetectedPos = new int [nNumTh]; 
        pInfo->m_nNumFPos = new int [nNumTh]; 
        if (!pInfo->m_nNumDetectedPos || !pInfo->m_nNumFPos) 
            throw "Out of memory"; 
        memset(pInfo->m_nNumDetectedPos, 0, nNumTh*sizeof(int)); 
        memset(pInfo->m_nNumFPos, 0, nNumTh*sizeof(int)); 
        if (pInfo->m_nNumObj > 0) 
        {
            pInfo->m_bDetected = new bool [nNumTh * pInfo->m_nNumObj]; 
            if (!pInfo->m_bDetected) 
                throw "Out of memory"; 
            memset(pInfo->m_bDetected, 0, nNumTh*pInfo->m_nNumObj*sizeof(bool)); 
        }
        ImgInfoVec.push_back(pInfo); 
        int size = (int)ImgInfoVec.size(); 
        if (size!=startsize && (size-startsize)%500 == 0)
            printf ("%d images have been loaded from %s!\r", size-startsize, szPath); 
    }

    int size = (int)ImgInfoVec.size(); 
    if ((size-startsize)%500 != 0)
        printf ("%d images have been loaded from %s!\n", size-startsize, szPath); 
    
    fclose(fpLabel); 
    return true; 
}

bool LoadTestFile(const char *szTestFile)
{
    FILE *fp = fopen (szTestFile, "r"); 
    if (fp == NULL) 
    {
        throw "null file";
        return false; 
    }

    fscanf(fp, "%s\n", szClassifierFile); 
    fscanf(fp, "stepSize = %f\nstepScale = %f\n", &fStepSize, &fStepScale); 
    int numFolders; 
    char szPath[MAX_PATH]; 
    bool bRetVal = true; 
    fscanf(fp, "numFolders = %d\n", &numFolders); 
    for (int i=0; i<numFolders && bRetVal; i++) 
    {
        fscanf(fp, "%s\n", szPath); 
        bRetVal = bRetVal && LoadImageInfo(szPath); 
    }
    fclose(fp); 
    return bRetVal; 
}

void ComputeROC()
{
    DETECTOR detector (szClassifierFile, fStepSize, fStepScale, 5000000); 
    detector.SetFinalScoreTh(pfTh[0]); 

    // do the actual detection work 
    vector<IMGINFO *>::iterator it; 
    IMAGE image; 
    IN_IMAGE iimage; 
    int num = 0; 
    int idxStart = 0; 
    for (it=ImgInfoVec.begin(); it!=ImgInfoVec.end(); it++, num++) 
    {
        IMGINFO *pInfo = *it; 
        // load the images 
        image.Load(pInfo->m_szFileName); 
        iimage.Init(&image); 

        detector.DetectObject(&iimage); 
        SCORED_RECT *pRc; 

        // get the raw detected rectangles 
        int numRawDet = detector.GetDetResults(&pRc, false); 

	    IRECT *pSrcRc[MAX_NUM_MERGE_RECT];
	    IRECT pDstRc[MAX_NUM_MERGE_RECT];
	    int numDst, Src2Dst[MAX_NUM_MERGE_RECT];
	    MERGERECT mergeRect;
        for (int idx=idxStart; idx<nNumTh; idx++) 
        {
            int srcIdx = 0; 
            float th = pfTh[idx]; 
            bool bMerge = true; 
            for (int i=0; i<numRawDet; i++) 
            {
                if (pRc[i].m_score >= th) 
                {
                    if (srcIdx >= MAX_NUM_MERGE_RECT)
                    {
                        bMerge = false; 
                        break; 
                    }
                    pSrcRc[srcIdx++] = &pRc[i].m_rect; 
                }
            }
            if (!bMerge)    // in any case if one image has many detections, we will ignore this threshold 
            {
                idxStart = idx+1; 
                continue; 
            }

    	    mergeRect.MergeRectangles(pSrcRc, srcIdx, pDstRc, &numDst, Src2Dst, srcIdx);
            for (int i=0; i<numDst; i++) 
            {
                bool bTPos = false; 
                for (int j=0; j<pInfo->m_nNumObj; j++) 
                {
                    if (pDstRc[i].DetectMatchDetection(pInfo->m_pObjRcs[j]))
                    {
                        pInfo->m_bDetected[idx*pInfo->m_nNumObj+j] = true; 
                        bTPos = true; 
                        break; 
                    }
                }
                if (!bTPos) 
                    pInfo->m_nNumFPos[idx] ++; 
            }
        }

        if ((num+1)%5 == 0)
            printf ("%d images are done! Minimum threshold = %f\r", num+1, pfTh[idxStart]); 
    }
    printf ("%d images are done!\n", num); 

    // now collect the statistics 
    printf ("Threshold\tFalse pos\tDetection rate\n"); 
    double totalObjs; 
    for (int idx=idxStart; idx<nNumTh; idx++) 
    {
        totalObjs = 0.0; 
        float th = pfTh[idx]; 
        double falsePos = 0.0; 
        double detObjs = 0.0; 
        for (it=ImgInfoVec.begin(); it!=ImgInfoVec.end(); it++) 
        {
            IMGINFO *pInfo = *it; 
            falsePos += pInfo->m_nNumFPos[idx]; 
            totalObjs += pInfo->m_nNumObj; 
            for (int i=0; i<pInfo->m_nNumObj; i++) 
                if (pInfo->m_bDetected[idx*pInfo->m_nNumObj+i]) 
                    detObjs += 1; 
        }
        printf ("%f\t%12.0lf\t%lf\n", th, falsePos, detObjs/totalObjs); 
    }

    printf ("The image set contains a total of %d positive objects", (int)totalObjs); 
    ReleaseImgInfoVec(); 
}

int main(int argc, char* argv[])
{
    if (argc != 5) 
    {
        Usage(); 
        return -1; 
    }

    float fMinTh = (float)atof(argv[2]); 
    float fMaxTh = (float)atof(argv[3]); 
    float fStepTh = (float)atof(argv[4]); 

    nNumTh = 0; 
    for (float th = fMinTh; th <=fMaxTh; th+=fStepTh) 
        nNumTh ++; 
    pfTh = new float [nNumTh]; 
    for (int i=0; i<nNumTh; i++) 
        pfTh[i] = fMinTh + fStepTh*i; 

    LoadTestFile(argv[1]); 

    clock_t tStart, tEnd;
    tStart = clock(); 
    ComputeROC(); 
    tEnd = clock(); 
    printf ("Time taken to compute the ROC: %f sec\n", float(tEnd-tStart)/CLOCKS_PER_SEC); 

    delete []pfTh; 
	return 0;
}
