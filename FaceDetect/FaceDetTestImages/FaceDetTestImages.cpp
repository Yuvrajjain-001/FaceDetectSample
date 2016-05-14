#include "stdafx.h"
#include "detector.h"
#include "imageinfo.h"

using namespace std; 

//#define DRAW_RAW_RECTS
#define DRAW_GT_RECTS
#define DRAW_MERGED_RECTS
//#define DRAW_FALSE_POS_RECTS
//#define DRAW_FALSE_NEG_RECTS

char szClassifierFile[MAX_PATH]; 
float fStepSize; 
float fStepScale; 
vector<IMGINFO *> ImgInfoVec; 

char szResultFile[MAX_PATH]; 
bool bOutputResult = false;

void Usage()
{
    char *msg =
        "\n"
        "Tool for testing a given face detector with a set of images.\n"
        "\n"
        "\n"
        "FaceDetTestImages fileName [resultName]\n"
        "\n"
        "    fileName      -- name of a test configuration file\n"
        "    resultName    -- name of the result file listing all detected faces\n"
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
        //pInfo->CheckInfo(); 
        //if (pInfo->m_LabelType == UNANNOTATED || pInfo->m_LabelType == DISCARDED) 
        //{
        //    delete pInfo; 
        //    continue; 
        //}
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

void TestImages()
{
//    float totalTime; 
//    LONGLONG PerformanceCountBegin=0,PerformanceCountEnd=0, PeformanceCounterFrequency;
//    ::QueryPerformanceCounter((LARGE_INTEGER*)&PerformanceCountBegin);
    DETECTOR detector (szClassifierFile, fStepSize, fStepScale); 
//    ::QueryPerformanceCounter((LARGE_INTEGER*)&PerformanceCountEnd);
//    ::QueryPerformanceFrequency( (LARGE_INTEGER*)&PeformanceCounterFrequency);
//    totalTime = (PerformanceCountEnd-PerformanceCountBegin)/(float)PeformanceCounterFrequency;
//    printf ("Initialize detector takes %f second\n", totalTime); 

    vector<IMGINFO *>::iterator it; 
    IMAGE image; 
    IN_IMAGE iimage; 

    FILE *fp = NULL; 
    if (bOutputResult) 
    {
        fp = fopen(szResultFile, "w"); 
        if (fp == NULL) 
        {
            throw "null file";
            return; 
        }
    }

    int count = 0, num; 
    int fnCount = 0, fpCount = 0; 
    for (it=ImgInfoVec.begin(); it!=ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        // load the images 
        image.Load(pInfo->m_szFileName); 
        float totalTime; 
        LONGLONG PerformanceCountBegin=0,PerformanceCountEnd=0, PeformanceCounterFrequency;
        //::QueryPerformanceCounter((LARGE_INTEGER*)&PerformanceCountBegin);
        iimage.Init(&image); 
	    //::QueryPerformanceCounter((LARGE_INTEGER*)&PerformanceCountEnd);
	    //::QueryPerformanceFrequency( (LARGE_INTEGER*)&PeformanceCounterFrequency);
        //totalTime = (PerformanceCountEnd-PerformanceCountBegin)/(float)PeformanceCounterFrequency;
        //printf ("Construct iimage time on %s (%dx%d): %f second\n", pInfo->m_szFileName, 
        //                                                     image.GetWidth(), 
        //                                                     image.GetHeight(), 
        //                                                     totalTime); 

        //PerformanceCountBegin=0,PerformanceCountEnd=0;
        //::QueryPerformanceCounter((LARGE_INTEGER*)&PerformanceCountBegin);
        detector.DetectObject(&iimage); 
	    //::QueryPerformanceCounter((LARGE_INTEGER*)&PerformanceCountEnd);
	    //::QueryPerformanceFrequency( (LARGE_INTEGER*)&PeformanceCounterFrequency);
        //totalTime = (PerformanceCountEnd-PerformanceCountBegin)/(float)PeformanceCounterFrequency;
        //printf ("Detection time on %s (%dx%d): %f second\n", pInfo->m_szFileName, 
        //                                                  image.GetWidth(), 
        //                                                  image.GetHeight(), 
        //                                                  totalTime); 

        SCORED_RECT *pRc; 

        // get the raw detected rectangles 
#if defined (DRAW_RAW_RECTS)
        num = detector.GetDetResults(&pRc, false); 
        for (int i=0; i<num; i++) 
            image.DrawRectAlpha(&pRc[i].m_rect, 0.1f); 
#endif


        // draw the ground truth rectangle
#if defined(DRAW_GT_RECTS)
        for (int i=0; i<pInfo->m_nNumObj; i++) 
            image.DrawRect(&pInfo->m_pObjRcs[i], 0); 
#endif

        num = detector.GetDetResults(&pRc, true); 
        if (bOutputResult)
        {
            fprintf(fp, "%s\n%d\n", pInfo->m_szFileName, num); 
            for (int i=0; i<num; i++)
                fprintf(fp, "%d %d %d %d\n", pRc[i].m_rect.m_ixMin, pRc[i].m_rect.m_iyMin, pRc[i].m_rect.m_ixMax, pRc[i].m_rect.m_iyMax);
        }
        // draw the merged rectangle 
#if defined(DRAW_MERGED_RECTS) 
        for (int i=0; i<num; i++) 
            image.DrawRect(&pRc[i].m_rect, 255); 
#endif

#if defined(DRAW_FALSE_NEG_RECTS)
        for (int j=0; j<pInfo->m_nNumObj; j++) 
        {
            bool bFN = true; 
            for (int i=0; i<num; i++) 
            {
                if (pRc[i].m_rect.DetectMatchDetection(pInfo->m_pObjRcs[j])) 
                {
                    bFN = false; 
                    break; 
                }
            }
            if (bFN) 
            {
                image.DrawRect(&pInfo->m_pObjRcs[j], 0); 
                fnCount ++; 
            }
        }
#endif 

#if defined(DRAW_FALSE_POS_RECTS)
        for (int i=0; i<num; i++) 
        {
            bool bFP = true; 
            for (int j=0; j<pInfo->m_nNumObj; j++) 
            {
                if (pRc[i].m_rect.DetectMatchDetection(pInfo->m_pObjRcs[j])) 
                {
                    bFP = false; 
                    break; 
                }
            }
            if (bFP) 
            {
                image.DrawRect(&pRc[i].m_rect, 255); 
                fpCount ++; 
            }
        }
#endif 


        char fname[MAX_PATH]; 
        int len = (int)strlen(pInfo->m_szFileName); 
        strcpy(fname, pInfo->m_szFileName); 
        strcpy(&fname[len-4], "_det.jpg"); 
//        image.Save(fname); 
        count ++; 
        if (count%50 == 0) 
            printf ("."); 
        if (count%1000 == 0) 
            printf ("%d images are processed\r", count); 
    }
    printf ("Totally %d images are processed\n", count); 

    if (bOutputResult)
        fclose(fp); 

#if defined(DRAW_FALSE_NEG_RECTS) && defined(DRAW_FALSE_POS_RECTS)
    printf("Total false negative examples: %d\n", fnCount); 
    printf("Total false positive examples: %d\n", fpCount); 
#endif

#if defined(COUNT_PRUNE_EFFECT)
    __int64 *pPruneCount = detector.GetPruneCount(); 
    __int64 total = 0; 
    for (int i=0; i<detector.GetNumClassifiers(); i++) 
        total += pPruneCount[i]; 
    double avgNode = 0.0; 
    for (int i=0; i<detector.GetNumClassifiers(); i++) 
        avgNode += double(i+1)*pPruneCount[i]/total; 
    printf ("Average number of nodes visited with pruning: %lf\n", avgNode); 
#endif

    ReleaseImgInfoVec(); 
}

int main(int argc, char* argv[])
{
    if (argc != 2 && argc != 3) 
    {
        Usage(); 
        return -1; 
    }

    if (argc == 3) 
    {
        strcpy(szResultFile, argv[2]); 
        bOutputResult = true; 
    }

    LoadTestFile(argv[1]); 

    TestImages(); 

	return 0;
}
