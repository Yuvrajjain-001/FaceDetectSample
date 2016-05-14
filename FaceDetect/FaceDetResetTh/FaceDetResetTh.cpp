#include "stdafx.h"
#include "classifier.h"
#include "image.h"
#include "imageinfo.h"

using namespace std; 

#define DIRECT_BACKWARD_PRUNE		0
#define MULTIPLE_INSTANCE_PRUNE		1
#define SOFT_CASCADE_PRUNE			2
#define METHOD						SOFT_CASCADE_PRUNE

const float epslon = 1e-6f; 
char szClassifierFile[MAX_PATH]; 
char szNewClassifierFile[MAX_PATH]; 
float fStepSize; 
float fStepScale; 
int nNumTh; 
float *pfTh; 
vector<IMGINFO *> ImgInfoVec; 
int nClassifiers, nBaseWidth, nBaseHeight, nNumFeatureTh; 
float fThreshold; 
int nWidth[MAX_NUM_SCALE]; 
int nHeight[MAX_NUM_SCALE]; 
int nStepW[MAX_NUM_SCALE]; 
int nStepH[MAX_NUM_SCALE]; 
CLASSIFIER *pOriClassifiers = NULL; 
int nTotalPosObjs = 0; 
int nMaxNumMatchRect = 0;
float dRate; 

struct POS_EXAMPLE_SCORES
{
    int m_nNum;         // number of examples that matches the ground truth 
    bool *m_pbValid;      // whether the example has already been pruned 
    float *m_pfScores;     // array of scores
}; 

int compare_float( const void *arg1, const void *arg2 )
{
    return (*((const float *)arg1) > *((const float *)arg2)) ? 
            1 : ((*((const float *)arg1) < *((const float *)arg2)) ? -1 : 0); 
}

void Usage()
{
    char *msg =
        "\n"
        "Tool for resetting the minimum positive threshold for a given face detector.\n"
        "\n"
        "\n"
        "FaceDetResetTh fileName newclassifer minTh maxTh stepTh\n"
        "\n"
        "    fileName      -- name of a test configuration file\n"
        "    newclassifier -- name of the new classifier\n" 
        "    minTh         -- minimum threshold to try\n" 
        "    maxTh         -- maximum threshold to try\n" 
        "    stepTh        -- stepsize of the threshold\n"
        "\n";

    printf("%s\n", msg);
}

void Usage_SCP()
{
    char *msg =
        "\n"
		"Tool for resetting the minimum positive threshold for a given face detector (soft cascade approach).\n"
        "\n"
        "\n"
        "FaceDetResetTh fileName newclassifer minAlpha maxAlpha stepAlpha dRate\n"
        "\n"
        "    fileName      -- name of a test configuration file\n"
        "    newclassifier -- name of the new classifier\n" 
        "    minAlpha      -- minimum threshold to try\n" 
        "    maxAlpha      -- maximum threshold to try\n" 
        "    stepAlpha     -- stepsize of the threshold\n"
		"    dRate         -- detection rate on the training set\n"
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
    IMAGE image; 
    for (int i=0; i<nNumImgs; i++) 
    {
        IMGINFO *pInfo = new IMGINFO; 
        pInfo->ReadInfo(fpLabel, szPath); 

         // for resetting the threshold, we can use unlabeled data, so we don't need to check the label type
        pInfo->CheckInfo(); 
        if (pInfo->m_LabelType == UNANNOTATED || pInfo->m_LabelType == DISCARDED) 
        {
            delete pInfo; 
            continue; 
        }

        if (pInfo->m_nNumObj == 0) 
            continue; 

        pInfo->m_nNumMatchRect = new int [pInfo->m_nNumObj]; 
        for (int j=0; j<pInfo->m_nNumObj; j++) 
            pInfo->m_nNumMatchRect[j] = 0; 

        image.Load(pInfo->m_szFileName, false); 
        pInfo->m_nImgWidth = image.GetWidth(); 
        pInfo->m_nImgHeight = image.GetHeight(); 
        for (int m=0; m<MAX_NUM_SCALE; m++) 
        {
            IRECT rect (0, nWidth[m], 0, nHeight[m]); 
            while (rect.m_iyMax <= pInfo->m_nImgHeight) 
            {
                while (rect.m_ixMax <= pInfo->m_nImgWidth) 
                {
                    for (int i=0; i<pInfo->m_nNumObj; i++) 
                    {
                        if (rect.DetectMatchDetection(pInfo->m_pObjRcs[i]))
                        {
                            pInfo->m_nNumMatchRect[i] += 1;
                        }
                    }
                    rect.m_ixMin += nStepW[m]; 
                    rect.m_ixMax = rect.m_ixMin + nWidth[m]; 
                }
                rect.m_iyMin += nStepH[m]; 
                rect.m_iyMax = rect.m_iyMin + nHeight[m]; 
                rect.m_ixMin = 0; 
                rect.m_ixMax = nWidth[m]; 
            }
        }

        if (pInfo->m_nNumObj > 0) 
        {
            bool bDiscard = true; 
            for (int j=0; j<pInfo->m_nNumObj; j++) 
            {
                if (pInfo->m_nNumMatchRect[j] > 0)
                {
                    bDiscard = false;    // discard the image if all postive example are not scanned 
                                         // note this is different from the criterion in boost.cpp
                    nTotalPosObjs ++; 
                    if (nMaxNumMatchRect < pInfo->m_nNumMatchRect[j]) 
                        nMaxNumMatchRect = pInfo->m_nNumMatchRect[j]; 
                }
            }
            if (bDiscard) 
            {
                delete pInfo; 
                continue; 
            }
        }
        ImgInfoVec.push_back(pInfo); 
        int size = (int)ImgInfoVec.size(); 
        if (size!=startsize && (size-startsize)%500 == 0)
            printf ("%d images have been loaded from %s!\r", size-startsize, szPath); 
    }

    int size = (int)ImgInfoVec.size(); 
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

    pOriClassifiers = CLASSIFIER::ReadClassifierFile(&nClassifiers, 
                                                     &nBaseWidth,  
                                                     &nBaseHeight, 
                                                     &nNumFeatureTh, 
                                                     &fThreshold, 
                                                     szClassifierFile); 
    if (pOriClassifiers)
    {
        for (int i=0; i<MAX_NUM_SCALE; i++) 
        {
            float scale = pow(fStepScale, i); 
            nWidth[i] = int(nBaseWidth * scale + 0.5); 
            nHeight[i] = int(nBaseHeight * scale + 0.5); 
            nStepW[i] = int(nWidth[i] * fStepSize + 0.5); 
            nStepH[i] = int(nHeight[i] * fStepSize + 0.5); 
        }
    }
    else
        throw "out of memory"; 

    nTotalPosObjs = 0; 
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
    printf ("%d faces are included in the loaded images.\n", nTotalPosObjs); 
    return bRetVal; 
}

void ResetTh()
{
    CLASSIFIER * ClassifierArray[MAX_NUM_SCALE]; 

    if (pOriClassifiers)
    {
        for (int i=0; i<MAX_NUM_SCALE; i++) 
        {
            float scale = pow(fStepScale, i); 
            ClassifierArray[i] = NULL; 
            ClassifierArray[i] = CLASSIFIER::CreateScaledClassifierArray(pOriClassifiers, nClassifiers, scale); 
            if (!ClassifierArray[i])
                throw "out of memory"; 
        }
    }
    else
        throw "Should never happen"; 

    POS_EXAMPLE_SCORES *pPosExScores = new POS_EXAMPLE_SCORES [nTotalPosObjs]; 
    if (!pPosExScores) 
        throw "out of memory"; 
    for (int i=0; i<nTotalPosObjs; i++) 
        pPosExScores[i].m_nNum = 0; 
    float *pfScores = new float [nClassifiers*nMaxNumMatchRect]; 

    // do the actual detection work 
    vector<IMGINFO *>::iterator it; 
    IMAGE image; 
    IN_IMAGE iimage; 
    int k, num=0, nActualNumPosObjs = 0; 
    for (it=ImgInfoVec.begin(); it!=ImgInfoVec.end(); it++,num++) 
    {
        IMGINFO *pInfo = *it; 
        // load the images 
        image.Load(pInfo->m_szFileName); 
        iimage.Init(&image); 

        for (int i=0; i<pInfo->m_nNumObj; i++) 
        {
            int nNum = 0; 
            float *pfS = &pfScores[nNum*nClassifiers]; 
            for (int m=0; m<MAX_NUM_SCALE; m++) 
            {
                CLASSIFIER *pC = ClassifierArray[m]; 
                IRECT rect (0, nWidth[m], 0, nHeight[m]); 
                while (rect.m_iyMax <= image.GetHeight()) 
                {
                    while (rect.m_ixMax <= image.GetWidth()) 
                    {
#if METHOD == DIRECT_BACKWARD_PRUNE
                        if (rect.DetectMatchTight(pInfo->m_pObjRcs[i],0.1,1.25))
#else if METHOD == MULTIPLE_INSTANCE_PRUNE
                        if (rect.DetectMatchDetection(pInfo->m_pObjRcs[i]))
#endif
                        {
                            float score = 0.0f; 
                            float norm = iimage.ComputeNorm(&rect); 
                            int j; 
                            for (j=0; j<nClassifiers; j++)
                            {
                                float fVal = pC[j].m_Feature.Eval(&iimage, norm, rect.m_ixMin, rect.m_iyMin); 
                                float *pTh = pC[j].GetFeatureTh(); 
                                float *pScore = pC[j].GetDScore(); 
                                for (k=0; k<pC[j].m_nNumTh; k++) 
                                {
                                    if (fVal > pTh[k]) 
                                    {
                                        score += pScore[k]; 
                                        break;
                                    }
                                }
                                if (k == pC[j].m_nNumTh) score += pScore[k]; 
                                pfS[j] = score; 
                                if (score < pC[j].GetMinPosScoreTh())
                                    break; 
                            }
                            if (j == nClassifiers && score >= pfTh[0])  // this generate a positive detection
                            // note here we use the minimum threshold to get all possible positive detections
                            {
                                nNum ++; 
                                pfS = &pfScores[nNum*nClassifiers];
                            }
                        }
                        rect.m_ixMin += nStepW[m]; 
                        rect.m_ixMax = rect.m_ixMin + nWidth[m]; 
                    }
                    rect.m_iyMin += nStepH[m]; 
                    rect.m_iyMax = rect.m_iyMin + nHeight[m]; 
                    rect.m_ixMin = 0; 
                    rect.m_ixMax = nWidth[m]; 
                }
            }
            if (nNum > 0) 
            {
                pPosExScores[nActualNumPosObjs].m_nNum = nNum; 
                pPosExScores[nActualNumPosObjs].m_pfScores = new float [nClassifiers*nNum]; 
                pPosExScores[nActualNumPosObjs].m_pbValid = new bool [nNum]; 
                if (!pPosExScores[nActualNumPosObjs].m_pfScores || !pPosExScores[nActualNumPosObjs].m_pbValid)
                    throw "out of memory"; 
                memcpy (pPosExScores[nActualNumPosObjs].m_pfScores, pfScores, nClassifiers*nNum*sizeof(float)); 
                for (int j=0; j<nNum; j++) 
                    pPosExScores[nActualNumPosObjs].m_pbValid[j] = true; 
                nActualNumPosObjs ++; 
            }
        }
        if ( (num+1)%500 == 0 || nActualNumPosObjs%500 == 0 )
            printf ("%d images with %d faces are processed\r", num+1, nActualNumPosObjs); 
    }
    printf ("%d images with %d faces are done! The rest faces are discarded because they didn't pass the minimum threshold.\n", num, nActualNumPosObjs); 

    for (int n=0; n<nNumTh; n++) 
    {
        if (n > 0) 
        {
			int nTmpActualNumPosObjs = 0; 
            for (int j=0; j<nActualNumPosObjs; j++) 
            {
                POS_EXAMPLE_SCORES *p = &pPosExScores[j]; 
				int cnt = 0; 
                for (int k=0; k<p->m_nNum; k++) 
                {
                    p->m_pbValid[k] = true; 
                    if (p->m_pfScores[k*nClassifiers + nClassifiers-1] < pfTh[n]) 
					{
                        p->m_pbValid[k] = false; 
						cnt ++; 
					}
                }
				if (cnt < p->m_nNum) 
					nTmpActualNumPosObjs ++; 
            }
			printf ("Threshold = %f, %d faces are left\n", pfTh[n], nTmpActualNumPosObjs); 
        }

        for (int i=0; i<nClassifiers; i++) 
        {
            float totalMinScore = 100000; 
            for (int j=0; j<nActualNumPosObjs; j++) 
            {
#if METHOD == DIRECT_BACKWARD_PRUNE
                float minScore = 100000; 
                POS_EXAMPLE_SCORES *p = &pPosExScores[j]; 
                for (int k=0; k<p->m_nNum; k++) 
                {
                    if (p->m_pbValid[k] && p->m_pfScores[k*nClassifiers+i]<minScore)
                        minScore = p->m_pfScores[k*nClassifiers+i]; 
                }
                if (minScore < totalMinScore) 
                    totalMinScore = minScore; 

#else if METHOD == MULTIPLE_INSTANCE_PRUNE
                float maxScore = -100000; 
                POS_EXAMPLE_SCORES *p = &pPosExScores[j]; 
                for (int k=0; k<p->m_nNum; k++) 
                {
                    if (p->m_pbValid[k] && p->m_pfScores[k*nClassifiers+i]>maxScore)
                        maxScore = p->m_pfScores[k*nClassifiers+i]; 
                }
                if (maxScore < totalMinScore && maxScore > -100000) 
                    totalMinScore = maxScore; 
#endif
            }
            float th = totalMinScore-epslon; 
            pOriClassifiers[i].SetMinPosScoreTh(th);
            for (int j=0; j<nActualNumPosObjs; j++) 
            {
                POS_EXAMPLE_SCORES *p = &pPosExScores[j]; 
                for (int k=0; k<p->m_nNum; k++) 
                {
                    if (p->m_pbValid[k] && p->m_pfScores[k*nClassifiers+i] < th)
                        p->m_pbValid[k] = false; 
                }
            }
        }

        char szNewFileName[MAX_PATH]; 
        sprintf(szNewFileName, "%s_%.2f.txt", szNewClassifierFile, pfTh[n]); 
        CLASSIFIER::WriteClassifierFile(pOriClassifiers, 
                                        nClassifiers, 
                                        nBaseWidth,  
                                        nBaseHeight, 
                                        nNumFeatureTh, 
                                        pfTh[n], 
                                        szNewFileName); 
    }

    for (int i=0; i<nTotalPosObjs; i++) 
    {
        if (pPosExScores[i].m_nNum > 0) 
        {
            delete []pPosExScores[i].m_pbValid; 
            delete []pPosExScores[i].m_pfScores; 
        }
    }
    delete []pPosExScores; 
    delete []pfScores; 

    for (int i=0; i<MAX_NUM_SCALE; i++) 
        CLASSIFIER::DeleteClassifierArray(ClassifierArray[i]);
    ReleaseImgInfoVec(); 
}

void ResetTh_SCP()
{
    CLASSIFIER * ClassifierArray[MAX_NUM_SCALE]; 

    if (pOriClassifiers)
    {
        for (int i=0; i<MAX_NUM_SCALE; i++) 
        {
            float scale = pow(fStepScale, i); 
            ClassifierArray[i] = NULL; 
            ClassifierArray[i] = CLASSIFIER::CreateScaledClassifierArray(pOriClassifiers, nClassifiers, scale); 
            if (!ClassifierArray[i])
                throw "out of memory"; 
        }
    }
    else
        throw "Should never happen"; 

    POS_EXAMPLE_SCORES *pPosExScores = new POS_EXAMPLE_SCORES [nTotalPosObjs]; 
    if (!pPosExScores) 
        throw "out of memory"; 
    for (int i=0; i<nTotalPosObjs; i++) 
        pPosExScores[i].m_nNum = 0; 
    float *pfScores = new float [nClassifiers*nMaxNumMatchRect]; 

    // do the actual detection work 
    vector<IMGINFO *>::iterator it; 
    IMAGE image; 
    IN_IMAGE iimage; 
    int k, num=0, nActualNumPosObjs = 0, nActualNumRects = 0; 
    for (it=ImgInfoVec.begin(); it!=ImgInfoVec.end(); it++,num++) 
    {
        IMGINFO *pInfo = *it; 
        // load the images 
        image.Load(pInfo->m_szFileName); 
        iimage.Init(&image); 

        for (int i=0; i<pInfo->m_nNumObj; i++) 
        {
            int nNum = 0; 
            float *pfS = &pfScores[nNum*nClassifiers]; 
            for (int m=0; m<MAX_NUM_SCALE; m++) 
            {
                CLASSIFIER *pC = ClassifierArray[m]; 
                IRECT rect (0, nWidth[m], 0, nHeight[m]); 
                while (rect.m_iyMax <= image.GetHeight()) 
                {
                    while (rect.m_ixMax <= image.GetWidth()) 
                    {
                        if (rect.DetectMatchTight(pInfo->m_pObjRcs[i],0.1,1.25))
                        {
                            float score = 0.0f; 
                            float norm = iimage.ComputeNorm(&rect); 
                            int j; 
                            for (j=0; j<nClassifiers; j++)
                            {
                                float fVal = pC[j].m_Feature.Eval(&iimage, norm, rect.m_ixMin, rect.m_iyMin); 
                                float *pTh = pC[j].GetFeatureTh(); 
                                float *pScore = pC[j].GetDScore(); 
                                for (k=0; k<pC[j].m_nNumTh; k++) 
                                {
                                    if (fVal > pTh[k]) 
                                    {
                                        score += pScore[k]; 
                                        break;
                                    }
                                }
                                if (k == pC[j].m_nNumTh) score += pScore[k]; 
                                pfS[j] = score; 
                                if (score < pC[j].GetMinPosScoreTh())
                                    break; 
                            }
                            if (j == nClassifiers)  // this generate a positive detection (ignoring the last threshold)
                            // note here we use the minimum threshold to get all possible positive detections
                            {
                                nNum ++; 
                                pfS = &pfScores[nNum*nClassifiers];
                            }
                        }
                        rect.m_ixMin += nStepW[m]; 
                        rect.m_ixMax = rect.m_ixMin + nWidth[m]; 
                    }
                    rect.m_iyMin += nStepH[m]; 
                    rect.m_iyMax = rect.m_iyMin + nHeight[m]; 
                    rect.m_ixMin = 0; 
                    rect.m_ixMax = nWidth[m]; 
                }
            }
            if (nNum > 0) 
            {
                pPosExScores[nActualNumPosObjs].m_nNum = nNum; 
                pPosExScores[nActualNumPosObjs].m_pfScores = new float [nClassifiers*nNum]; 
                pPosExScores[nActualNumPosObjs].m_pbValid = new bool [nNum]; 
                if (!pPosExScores[nActualNumPosObjs].m_pfScores || !pPosExScores[nActualNumPosObjs].m_pbValid)
                    throw "out of memory"; 
                memcpy (pPosExScores[nActualNumPosObjs].m_pfScores, pfScores, nClassifiers*nNum*sizeof(float)); 
                for (int j=0; j<nNum; j++) 
                    pPosExScores[nActualNumPosObjs].m_pbValid[j] = true; 
                nActualNumPosObjs ++; 
                nActualNumRects += nNum; 
            }
        }
        if ( (num+1)%500 == 0 || nActualNumPosObjs%500 == 0 )
            printf ("%d images with %d faces are processed\r", num+1, nActualNumPosObjs); 
    }

    printf ("%d images with %d faces are done! The rest faces are discarded because they didn't pass the intermediate minimum positive thresholds.\n", num, nActualNumPosObjs); 
    // note: nActualNumPosObjs should be identical to nTotalPosObjs if we use the same traing dataset for pruning,
    // as that's how the minimum positive thresholds are set. 

    int nTargetNumRejPosRects = (int) (nActualNumRects * (1-dRate)); 
    double *pfARejVec = new double [nClassifiers]; 
    float *pfRankScore = new float [nActualNumRects]; 
    for (int n=0; n<nNumTh; n++) 
    {
        // reset the valid flags 
        if (n > 0) 
        {
            for (int j=0; j<nActualNumPosObjs; j++) 
            {
                POS_EXAMPLE_SCORES *p = &pPosExScores[j]; 
                for (int k=0; k<p->m_nNum; k++) 
                {
                    p->m_pbValid[k] = true; 
                }
            }
        }

        // first compute the accumulative rejection distribution vector 
        float alpha = pfTh[n]; 
        double sum = 0.0; 
        for (int i=0; i<nClassifiers; i++) 
        {
            if (alpha < 0) 
                sum += exp(-alpha*(1-i*1.0/nClassifiers)); 
            else 
                sum += exp(alpha*i*1.0/nClassifiers); 
        }
        double k = nTargetNumRejPosRects / sum; 
        if (alpha < 0) 
            pfARejVec[0] = k*exp(-alpha); 
        else 
            pfARejVec[0] = k; 
        for (int i=1; i<nClassifiers; i++) 
        {
            if (alpha < 0) 
                pfARejVec[i] = pfARejVec[i-1] + k*exp(-alpha*(1-i*1.0/nClassifiers)); 
            else
                pfARejVec[i] = pfARejVec[i-1] + k*exp(alpha*i*1.0/nClassifiers); 
        }

        // work on pruning 
        for (int i=0; i<nClassifiers; i++) 
        {
            int cnt = 0; 
            for (int j=0; j<nActualNumPosObjs; j++) 
            {
                POS_EXAMPLE_SCORES *p = &pPosExScores[j]; 
                for (int k=0; k<p->m_nNum; k++) 
                {
                    if (p->m_pbValid[k])
                        pfRankScore[cnt++] = p->m_pfScores[k*nClassifiers+i];
                }
            }
            qsort(pfRankScore, cnt, sizeof(float), compare_float); 
            float th = -1000000; 
            for (int j=pfARejVec[i] - (nActualNumRects - cnt); j>=0; j--) 
            {
                if (pfRankScore[j] < pfRankScore[j+1]) 
                {
                    th = max(pfRankScore[j+1]-epslon, (pfRankScore[j]+pfRankScore[j+1]/2.0)); 
                    break; 
                }
            }
            pOriClassifiers[i].SetMinPosScoreTh(th);
            for (int j=0; j<nActualNumPosObjs; j++) 
            {
                POS_EXAMPLE_SCORES *p = &pPosExScores[j]; 
                for (int k=0; k<p->m_nNum; k++) 
                {
                    if (p->m_pbValid[k] && p->m_pfScores[k*nClassifiers+i] < th)
                        p->m_pbValid[k] = false; 
                }
            }
        }

        char szNewFileName[MAX_PATH]; 
        sprintf(szNewFileName, "%s_%.2f.txt", szNewClassifierFile, pfTh[n]); 
        CLASSIFIER::WriteClassifierFile(pOriClassifiers, 
                                        nClassifiers, 
                                        nBaseWidth,  
                                        nBaseHeight, 
                                        nNumFeatureTh, 
                                        pOriClassifiers[nClassifiers-1].GetMinPosScoreTh(), 
                                        szNewFileName); 
    }

    for (int i=0; i<nTotalPosObjs; i++) 
    {
        if (pPosExScores[i].m_nNum > 0) 
        {
            delete []pPosExScores[i].m_pbValid; 
            delete []pPosExScores[i].m_pfScores; 
        }
    }
    delete []pPosExScores; 
    delete []pfScores; 
    delete []pfARejVec; 
    delete []pfRankScore; 

    for (int i=0; i<MAX_NUM_SCALE; i++) 
        CLASSIFIER::DeleteClassifierArray(ClassifierArray[i]);
    ReleaseImgInfoVec(); 
}


void CreateScoreTraces()
{
    CLASSIFIER * ClassifierArray[MAX_NUM_SCALE]; 

    if (pOriClassifiers)
    {
        for (int i=0; i<MAX_NUM_SCALE; i++) 
        {
            float scale = pow(fStepScale, i); 
            ClassifierArray[i] = NULL; 
            ClassifierArray[i] = CLASSIFIER::CreateScaledClassifierArray(pOriClassifiers, nClassifiers, scale); 
            if (!ClassifierArray[i])
                throw "out of memory"; 
        }
    }
    else
        throw "Should never happen"; 

    float *pfScores = new float [nClassifiers]; 

    FILE *fp = fopen(szNewClassifierFile, "w"); 

    // do the actual detection work 
    vector<IMGINFO *>::iterator it; 
    IMAGE image; 
    IN_IMAGE iimage; 
    int k, num=0, nActualNumPosObjs = 0; 
    for (it=ImgInfoVec.begin(); it!=ImgInfoVec.end(); it++,num++) 
    {
        IMGINFO *pInfo = *it; 
        // load the images 
        image.Load(pInfo->m_szFileName); 
        iimage.Init(&image); 

        for (int i=0; i<pInfo->m_nNumObj; i++) 
        {
            int nNum = 0; 
            for (int m=0; m<MAX_NUM_SCALE; m++) 
            {
                CLASSIFIER *pC = ClassifierArray[m]; 
                IRECT rect (0, nWidth[m], 0, nHeight[m]); 
                while (rect.m_iyMax <= image.GetHeight()) 
                {
                    while (rect.m_ixMax <= image.GetWidth()) 
                    {
                        float score = 0.0f; 
                        float norm = iimage.ComputeNorm(&rect); 
                        int j; 
                        for (j=0; j<nClassifiers; j++)
                        {
                            float fVal = pC[j].m_Feature.Eval(&iimage, norm, rect.m_ixMin, rect.m_iyMin); 
                            float *pTh = pC[j].GetFeatureTh(); 
                            float *pScore = pC[j].GetDScore(); 
                            for (k=0; k<pC[j].m_nNumTh; k++) 
                            {
                                if (fVal > pTh[k]) 
                                {
                                    score += pScore[k]; 
                                    break;
                                }
                            }
                            if (k == pC[j].m_nNumTh) score += pScore[k]; 
                            pfScores[j] = score; 
                        }
                        if (j == nClassifiers && score >= pfTh[0])  // this generate a positive detection
                        {
                            for (j=0; j<nClassifiers; j++) 
                                fprintf(fp, "%f ", pfScores[j]); 
                            fprintf(fp, "%d %d %d %d ", rect.m_ixMin, rect.m_iyMin, rect.m_ixMax, rect.m_iyMax); 
                            if (rect.DetectMatchLooseTrace(pInfo->m_pObjRcs[i]))
                                fprintf(fp, "1\n"); 
                            else 
                                fprintf(fp, "0\n"); 
                        }

                        rect.m_ixMin += nStepW[m]; 
                        rect.m_ixMax = rect.m_ixMin + nWidth[m]; 
                    }
                    rect.m_iyMin += nStepH[m]; 
                    rect.m_iyMax = rect.m_iyMin + nHeight[m]; 
                    rect.m_ixMin = 0; 
                    rect.m_ixMax = nWidth[m]; 
                }
            }
        }
    }

    fclose(fp); 
    delete []pfScores; 

    for (int i=0; i<MAX_NUM_SCALE; i++) 
        CLASSIFIER::DeleteClassifierArray(ClassifierArray[i]);
    ReleaseImgInfoVec(); 
}

int main(int argc, char* argv[])
{
#if METHOD == DIRECT_BACKWARD_PRUNE || METHOD == MULTIPLE_INSTANCE_PRUNE
    if (argc != 6) 
    {
        Usage(); 
        return -1; 
    }
#else if METHOD == SOFT_CASCADE_PRUNE 
    if (argc != 7)
    {
		Usage_SCP(); 
        return -1; 
    }
#endif

    float fMinTh = (float)atof(argv[3]); 
    float fMaxTh = (float)atof(argv[4]); 
    float fStepTh = (float)atof(argv[5]); 

    nNumTh = 0; 
    for (float th = fMinTh; th <=fMaxTh; th+=fStepTh) 
        nNumTh ++; 
    pfTh = new float [nNumTh]; 
    for (int i=0; i<nNumTh; i++) 
        pfTh[i] = fMinTh + fStepTh*i; 

#if METHOD == SOFT_CASCADE_PRUNE
    dRate = (float)atof(argv[6]); 
#endif 

    strcpy(szNewClassifierFile, argv[2]); 
    LoadTestFile(argv[1]); 

#if METHOD == DIRECT_BACKWARD_PRUNE || METHOD == MULTIPLE_INSTANCE_PRUNE
//    CreateScoreTraces();    // create score traces to plot the football figure, use pfTh[0] only
    ResetTh(); 
#else if METHOD == SOFT_CASCADE_PRUNE 
	ResetTh_SCP(); 
#endif 

    if (pOriClassifiers)
        CLASSIFIER::DeleteClassifierArray(pOriClassifiers); 
	return 0;
}
