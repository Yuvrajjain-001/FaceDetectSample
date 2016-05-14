#include "stdafx.h"
#include "Boost.h" 

int compare_double( const void *arg1, const void *arg2 )
{
    return (*((const double *)arg1) > *((const double *)arg2)) ? 
            1 : ((*((const double *)arg1) < *((const double *)arg2)) ? -1 : 0); 
}

int compare_value(const void *arg1, const void *arg2)
{
    //const float &f1 = ((const VALUEINDEX *)arg1)->m_fVal; 
    //const float &f2 = ((const VALUEINDEX *)arg2)->m_fVal; 
    //return (f1>f2)? 1:((f1<f2)? -1:0); 

    return (((const VALUEINDEX *)arg1)->m_fVal > ((const VALUEINDEX *)arg2)->m_fVal) ? 
            1 : ((((const VALUEINDEX *)arg1)->m_fVal < ((const VALUEINDEX *)arg2)->m_fVal) ? -1 : 0);
}

BOOST::BOOST()
{
    m_fNegRejPercent = 0.0f; 
    m_nMaskFreq = 1000000; 
    m_nWidth = 0; 
    m_nHeight = 0; 
    m_fStepSize = 0.0; 
    m_fStepScale = 1.0; 
    m_ImgInfoVec.clear(); 

    m_nNumBoostFeatures = 0; 

    m_nMaxNumExamples = 0; 
    m_nNumExamples = 0; 
    m_pExamples = NULL;
    m_nNumSampledExamples = 0; 
    m_pSampledExamples = NULL; 
    m_dRandNums = NULL; 

    m_nScoreBufSize = 0; 
    m_pfScoreBuf = NULL; 

    m_nUpdateScoreIdx = 0; 

    for (int i=0; i<MAX_NUM_SCALE; i++) 
        m_RCFeatures[i] = NULL; 

//    m_Rand.Seed(0); 
    m_Rand.Seed((int)time(NULL)); 
}

BOOST::BOOST(const char *szListFile)
{
    m_fNegRejPercent = 0.0; 
    m_nMaskFreq = 1000000; 
    m_nWidth = 0; 
    m_nHeight = 0; 
    m_fStepSize = 0.0; 
    m_fStepScale = 1.0; 
    m_ImgInfoVec.clear(); 

    m_nNumBoostFeatures = 0; 

    m_nMaxNumExamples = 0; 
    m_nNumExamples = 0; 
    m_pExamples = NULL;
    m_nNumSampledExamples = 0; 
    m_pSampledExamples = NULL; 
    m_dRandNums = NULL; 

    m_nScoreBufSize = 0; 
    m_pfScoreBuf = NULL; 

    m_nUpdateScoreIdx = 0; 

    for (int i=0; i<MAX_NUM_SCALE; i++) 
        m_RCFeatures[i] = NULL; 

//    m_Rand.Seed(0); 
    m_Rand.Seed((int)time(NULL)); 

    LoadListFile(szListFile); 
}

BOOST::~BOOST()
{
    ReleaseRCFeatures(); 
    ReleaseImgInfoVec(); 
    ReleaseExamples(); 
    if (m_pfScoreBuf != NULL) 
    {
        delete []m_pfScoreBuf; 
        m_pfScoreBuf = NULL; 
        m_nScoreBufSize = 0; 
    }
}

void BOOST::ReleaseRCFeatures()
{
    for (int i=0; i<MAX_NUM_SCALE; i++) 
    {
        if (m_RCFeatures[i])
            RCFEATURE::DeleteRCFeatureArray(m_RCFeatures[i]); 
        m_RCFeatures[i] = NULL; 
    }
}

void BOOST::ReleaseImgInfoVec()
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

void BOOST::ReleaseExamples()
{
    if (m_pExamples)
    {
        for (int i=0; i<m_nMaxNumExamples; i++) 
        {
            if (m_pExamples[i].m_pIImg)
                delete m_pExamples[i].m_pIImg; 
        }
        delete []m_pExamples; 
        m_pExamples = NULL; 
    }
    m_nMaxNumExamples = 0;
    m_nNumExamples = 0; 

    if (m_pSampledExamples) 
    { 
        for (int i=0; i<m_nNumSampledExamples; i++) 
        {
            if (m_pSampledExamples[i].m_fFeature)
                delete []m_pSampledExamples[i].m_fFeature; 
        }
        delete []m_pSampledExamples; 
        m_pSampledExamples = NULL;     
    }
    m_nNumSampledExamples = 0; 

    if (m_dRandNums) 
    {
        delete []m_dRandNums; 
        m_dRandNums = NULL; 
    }
}

void BOOST::GenerateRCFeatures()
{
    ReleaseRCFeatures(); 
    m_RCFeatures[0] = RCFEATURE::CreateCompleteRCFeatureArray(m_nWidth, m_nHeight, 
        m_nfWidth, m_nfHeight, m_ffStepSize, m_ffStepScale, &m_nNumRCFeatures); 
    for (int i=1; i<MAX_NUM_SCALE; i++) 
    {
        m_RCFeatures[i] = new RCFEATURE [m_nNumRCFeatures]; 
        if (m_RCFeatures[i] == NULL)
            throw "out of memory";
        float scale = pow(m_fStepScale, i); 
        for (int j=0; j<m_nNumRCFeatures; j++) 
            m_RCFeatures[i][j].InitSS(&m_RCFeatures[0][j], scale); 
    }
}

bool BOOST::LoadListFile(const char *szListFile)
{
    ReleaseImgInfoVec(); 

    FILE *fp = fopen (szListFile, "r"); 
    if (fp == NULL) 
    {
        throw "null file";
        return false; 
    }

    fscanf(fp, "numBoostFeatures = %d\n", &m_nNumBoostFeatures); 
    fscanf(fp, "numFeatureTh = %d\n", &m_nNumFeatureTh); 
    if (m_nNumFeatureTh > MAX_NUM_FEATURE_TH) 
    {
        m_nNumFeatureTh = MAX_NUM_FEATURE_TH; 
        printf("WARNING: number of feature thresholds reset to %d\n", MAX_NUM_FEATURE_TH); 
    }
    int maxNumExmaples, numSampledExamples; 
    fscanf(fp, "numExamplesMax = %d\n", &maxNumExmaples); 
    fscanf(fp, "numSampledExamples = %d\n", &numSampledExamples); 

    fscanf(fp, "negRejPercent = %f\n", &m_fNegRejPercent); 
    fscanf(fp, "maskFreq = %d\n", &m_nMaskFreq); 
    fscanf(fp, "baseWidth = %d\nbaseHeight = %d\n", &m_nWidth, &m_nHeight); 
    fscanf(fp, "stepSize = %f\nstepScale = %f\n", &m_fStepSize, &m_fStepScale); 

    fscanf(fp, "featureBaseWidth = %d\nfeatureBaseHeight = %d\n", &m_nfWidth, &m_nfHeight); 
    fscanf(fp, "featureStepSize = %f\nfeatureStepScale = %f\n", &m_ffStepSize, &m_ffStepScale); 

    fscanf(fp, "scoreFilePrefix = %s\nscoreFileSize = %d\n", m_fScoreFilePrefix, &m_nScoreFileSize); 

    GenerateRCFeatures(); 
    m_LoopPara[0].m_fScale = 1.0f; 
    m_LoopPara[0].m_nW = m_nWidth; 
    m_LoopPara[0].m_nH = m_nHeight; 
    m_LoopPara[0].m_nSW = int(m_LoopPara[0].m_nW * m_fStepSize + 0.5); 
    m_LoopPara[0].m_nSH = int(m_LoopPara[0].m_nH * m_fStepSize + 0.5); 
    for (int i=1; i<MAX_NUM_SCALE; i++) 
    {
        float scale = pow(m_fStepScale, i);
        m_LoopPara[i].m_fScale = scale; 
        m_LoopPara[i].m_nW = int(m_nWidth * scale + 0.5); 
        m_LoopPara[i].m_nH = int(m_nHeight * scale + 0.5); 
        m_LoopPara[i].m_nSW = int(m_LoopPara[i].m_nW * m_fStepSize + 0.5); 
        m_LoopPara[i].m_nSH = int(m_LoopPara[i].m_nH * m_fStepSize + 0.5); 
    }

    int numFolders; 
    char szPath[MAX_PATH]; 
    bool bRetVal = true; 
    fscanf(fp, "numFolders = %d\n", &numFolders); 
    int index = 0; 
    for (int i=0; i<numFolders && bRetVal; i++) 
    {
        fscanf(fp, "%s\n", szPath); 
        bRetVal = bRetVal && LoadImageInfo(szPath, &index); 
    }
    fclose(fp); 

#if defined(WRITE_POS_EXAMPLES)
    return false;   // stop the training process 
#endif

    vector<IMGINFO *>::iterator it; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        pInfo->m_pObjRcs = NULL; 
    }

    AllocateExamples(maxNumExmaples, numSampledExamples); 

    // allocate memory for m_pfScoreBuf
    m_nScoreBufSize = m_nScoreFileSize/sizeof(float); 
    m_pfScoreBuf = new float [m_nScoreBufSize]; 
    if (!m_pfScoreBuf) 
    {
        throw "out of memory"; 
        return false; 
    }
    memset(m_pfScoreBuf, 0, m_nScoreBufSize*sizeof(float)); 

    double totalCount = 0; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        totalCount += pInfo->m_nPosCount + pInfo->m_nNegCount; 
    }
    printf("Pre-write score files..."); 
    int num = int((totalCount/m_nScoreBufSize) + 1); 
    for (int i=0; i<num; i++) 
        WriteScoreFile(i);      // initialize score files 
    printf("Done\n"); 

    return true;
}

bool BOOST::AllocateExamples(int maxNumExamples, int numSampledExamples)
{
    ReleaseExamples(); 
    m_pExamples = new EXAMPLE [maxNumExamples]; 
    if (!m_pExamples) 
    {
        throw "out of memory"; 
        return false; 
    }
    m_nMaxNumExamples = maxNumExamples; 
    m_nNumExamples = 0; 

    for (int i=0; i<maxNumExamples; i++) 
        m_pExamples[i].m_pIImg = NULL; 
    for (int i=0; i<maxNumExamples; i++) 
    {
        m_pExamples[i].m_pIImg = new I_IMAGE(m_nWidth, m_nHeight); 
        if (!m_pExamples[i].m_pIImg) 
        {
            throw "out of memory"; 
            return false; 
        }
    }

    m_pSampledExamples = new SAMPLED_EXAMPLE [numSampledExamples]; 
    if (!m_pSampledExamples) 
    {
        throw "out of memory"; 
        return false; 
    }
    m_nNumSampledExamples = numSampledExamples; 
    for (int i=0; i<m_nNumSampledExamples; i++)
        m_pSampledExamples[i].m_fFeature = NULL; 

    // initialize the sampled examples (allocate memory for features) 
    for (int i=0; i<m_nNumSampledExamples; i++)
    {
        m_pSampledExamples[i].m_fFeature = new float [m_nNumRCFeatures]; 
        if (!m_pSampledExamples[i].m_fFeature)
        {
            throw "out of memory"; 
            return false; 
        }
    }

    // allocate memory for the random number array 
    m_dRandNums = new double [m_nNumSampledExamples]; 
    if (!m_dRandNums) 
    {
        throw "out of memory"; 
        return false; 
    }

    return true; 
}

void BOOST::ComputeAllFeatures(int nStart, int nEnd, int nIdx)
{
    for (int i=nStart; i<=nEnd; i++) 
    {
        EXAMPLE *pE = &m_pExamples[i]; 
        if (pE->m_nSampled < 1) 
            continue;

        I_IMAGE *pIImage = pE->m_pIImg; 
        float norm = pE->m_fNorm; 
        for (int j=0; j<pE->m_nSampled; j++, nIdx++) 
        {
            m_pSampledExamples[nIdx].m_nLabel = pE->m_nLabel; 
            float *pFeature = m_pSampledExamples[nIdx].m_fFeature; 
            if (j==0)
            {
                RCFEATURE *pF = m_RCFeatures[pE->m_nSizeIdx]; 
                for (int k=0; k<m_nNumRCFeatures; k++) 
                    pFeature[k] = pF[k].Eval(pIImage, norm); 
            }
            else 
            {
                memcpy(pFeature, 
                       m_pSampledExamples[nIdx-1].m_fFeature,  
                       m_nNumRCFeatures*sizeof(float)); 
            }
        }
    }
}

DWORD WINAPI ComputeAllFeaturesThreadProc(LPVOID lpParam)
{
    THREADPROC_PARA *pPara = (THREADPROC_PARA *)lpParam; 
    pPara->m_pBoost->ComputeAllFeatures(pPara->m_nStart, pPara->m_nEnd, pPara->m_nIdx);
    return 0; 
}

void BOOST::SampleExamples4FeatureSelection()
{
    printf ("Sampling examples for feature selection..."); 
    EXAMPLE *pE = m_pExamples; 
    double sumWeight = 0.0; 
    for (int i=0; i<m_nNumExamples; i++, pE++) 
    {
        pE->m_nSampled = 0;     // reset the sample flags 
        sumWeight += pE->m_fWeight; 
    }

    for (int i=0; i<m_nNumSampledExamples; i++) 
        m_dRandNums[i] = m_Rand.URand(0.0, sumWeight); 
    qsort(m_dRandNums, m_nNumSampledExamples, sizeof(double), compare_double); 

    THREADPROC_PARA tpPara[NUM_PROC_THREADS]; 
    for (int i=0; i<NUM_PROC_THREADS; i++) 
        tpPara[i].m_pBoost = this; 
    tpPara[0].m_nStart = 0; 
    tpPara[0].m_nIdx = 0; 
    tpPara[NUM_PROC_THREADS-1].m_nEnd = m_nNumExamples-1; 

    int idx = 0; 
    int tpIdx = 0; 
    sumWeight = 0.0; 
    pE = m_pExamples; 
    for (int i=0; i<m_nNumExamples; i++, pE++) 
    {
        sumWeight += pE->m_fWeight; 
        while (idx < m_nNumSampledExamples && m_dRandNums[idx] < sumWeight) 
        {   // take this example
            pE->m_nSampled ++;       // unlike sampling in InitAllExamples, multiple samples on the same 
                                     // example is allowed (true importance sampling). m_nSampled is short, 
                                     // but there shouldn't be an example who is sampled over 2^15 times 
            idx ++; 
        }
        if (tpIdx<NUM_PROC_THREADS-1 && idx > (tpIdx+1)*m_nNumSampledExamples/NUM_PROC_THREADS) 
        {
            tpPara[tpIdx++].m_nEnd = i; 
            tpPara[tpIdx].m_nStart = i+1; 
            tpPara[tpIdx].m_nIdx = idx; 
        }
    }

    HANDLE hThread[NUM_PROC_THREADS]; 
    DWORD dwThreadId[NUM_PROC_THREADS]; 
    for (int i=0; i<NUM_PROC_THREADS; i++) 
    {
        hThread[i] = CreateThread(
            NULL,                           // default security
            0,                              // use default stack size
            ComputeAllFeaturesThreadProc,   // thread function
            &tpPara[i],                     // augument to thread function
            0,                              // use default creation flags
            &dwThreadId[i]);                // returns the thread identifier
        if (hThread[i] == NULL) 
            throw "Thread creation failed"; 
    }

    WaitForMultipleObjects(NUM_PROC_THREADS, hThread, TRUE, INFINITE); 
    for (int i=0; i<NUM_PROC_THREADS; i++) 
        CloseHandle(hThread[i]); 

    printf("Done!\n"); 
}

void BOOST::InitAllExamples()
{
    printf("Initializing all examples. This may take a few minutes..."); 

    // at the very begining of the training, negative examples are taken randomly, while all positive examples are taken 
    vector<IMGINFO *>::iterator it; 
    double dPosCount=0.0, dNegCount=0.0; 
    int maxNegCount = -1; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        dPosCount += pInfo->m_nPosCount; 
        dNegCount += pInfo->m_nNegCount; 
        if (pInfo->m_nNegCount > maxNegCount) 
            maxNegCount = pInfo->m_nNegCount; 
    }
    m_RemaskPara.m_dTotalPosCount = dPosCount; 
    m_RemaskPara.m_dNegScoreThCount = dNegCount; 
    m_RemaskPara.m_dSampleRatio = (m_nMaxNumExamples-m_RemaskPara.m_dTotalPosCount)/m_RemaskPara.m_dNegScoreThCount; 
    if (m_RemaskPara.m_dSampleRatio > 1.0) 
        m_RemaskPara.m_dSampleRatio = 1.0; 

    char *pMask = new char [maxNegCount]; 
    m_nNumExamples = 0; 
    IMAGE image; 
    IN_IMAGE iimage; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        image.Load(pInfo->m_szFileName); 
        iimage.Init(&image); 

        int numNegExamples = int(m_RemaskPara.m_dSampleRatio*pInfo->m_nNegCount);     
                             // flooring so we never exceed m_nMaxNumExamples
        memset(pMask, 0, pInfo->m_nNegCount); 
        int cnt = 0; 
        while (cnt < numNegExamples) 
        {
            int idx = (int)m_Rand.URand(0, pInfo->m_nNegCount); 
            if (pMask[idx] == 0) 
            {
                pMask[idx] = 1; 
                cnt ++; 
            }
        }

        RLCODEC labelDec(&pInfo->m_LabelVec, RLCODEC::DECODER); 
        char label, mask;
        int negCnt=0; 
        for (int m=0; m<MAX_NUM_SCALE; m++) 
        {
            LOOP_PARA *p = &m_LoopPara[m]; 
            IRECT rect (0, p->m_nW, 0, p->m_nH); 
            while (rect.m_iyMax <= pInfo->m_nImgHeight) 
            {
                while (rect.m_ixMax <= pInfo->m_nImgWidth) 
                {
                    mask = 0; 
                    labelDec.DecodeNext(label); 
                    if (label == 1)     // positive example
                        mask = 1; 
                    else if (label == -1) 
                    {
                        if (pMask[negCnt]) mask = 1; 
                        negCnt ++; 
                    }
                    if (mask == 1) 
                    {
                        m_pExamples[m_nNumExamples].m_nLabel = label; 
                        m_pExamples[m_nNumExamples].m_nSizeIdx = m; 
                        m_pExamples[m_nNumExamples].m_pIImg->InitWithSubSample(&iimage, rect.m_ixMin, rect.m_iyMin, p->m_fScale); 
                        m_pExamples[m_nNumExamples].m_fNorm = iimage.ComputeNorm(&rect); 
                        m_nNumExamples ++; 
                    }
                    rect.m_ixMin += p->m_nSW; 
                    rect.m_ixMax = rect.m_ixMin + p->m_nW; 
                }
                rect.m_iyMin += p->m_nSH; 
                rect.m_iyMax = rect.m_iyMin + p->m_nH; 
                rect.m_ixMin = 0; 
                rect.m_ixMax = p->m_nW; 
            }
        }
    }
    delete []pMask; 

    // set initial score and weights to the masked examples
    m_fInitScore = FindInitScore(); 
    EXAMPLE *pE = m_pExamples; 
    for (int i=0; i<m_nNumExamples; i++, pE++) 
    {
        pE->m_fScore = m_fInitScore; 
        pE->m_fWeight = ComputeWeight(pE->m_nLabel, m_fInitScore); 
    }

    printf("Done!\n"); 
    printf("Total number of positive examples: %lf, negative examples: %lf\n", dPosCount, dNegCount); 
    printf("Total number of masked examples is:%d\n", m_nNumExamples); 
}

void BOOST::ReadScoreFile(int idx)
{
    //clock_t t0 = clock(); 

    char fname[MAX_PATH]; 
    sprintf (fname, "%s%03d.dat", m_fScoreFilePrefix, idx); 

    //FILE *fp = fopen(fname, "rb"); 
    //if (fp == NULL) 
    //    throw "Unable to open file for read!"; 
    //fread(m_pfScoreBuf, sizeof(float), m_nScoreBufSize, fp); 
    //fclose(fp); 

    HANDLE hFile; 
    hFile = CreateFile(fname,                   // file name
                    GENERIC_READ,              // open for write 
                    FILE_SHARE_READ,           // share for write
                    NULL,                       // no security 
                    OPEN_EXISTING,              // always create
                    FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,     // normal file, sequencial scan
                    NULL);                      // no attr. template 

    if (hFile == INVALID_HANDLE_VALUE) 
        throw "Unable to open file for write!"; 

    DWORD numBytesWritten; 
    ReadFile (hFile, m_pfScoreBuf, m_nScoreBufSize*sizeof(float), &numBytesWritten, NULL); 
	CloseHandle(hFile);

    //clock_t t1 = clock(); 
    //printf ("Times taken to read one score file: %f sec\n", 
    //    float(t1-t0)/CLOCKS_PER_SEC); 
}

void BOOST::WriteScoreFile(int idx)
{
    char fname[MAX_PATH]; 
    sprintf (fname, "%s%03d.dat", m_fScoreFilePrefix, idx); 

    //FILE *fp = fopen(fname, "wb"); 
    //if (fp == NULL) 
    //    throw "Unable to open file for write!"; 
    //fwrite(m_pfScoreBuf, sizeof(float), m_nScoreBufSize, fp); 
    //fclose(fp); 

    HANDLE hFile; 
    hFile = CreateFile(fname,                   // file name
                    GENERIC_WRITE,              // open for write 
                    FILE_SHARE_WRITE,           // share for write
                    NULL,                       // no security 
                    CREATE_ALWAYS,              // always create
                    FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,     // normal file, sequencial scan
                    NULL);                      // no attr. template 
     
    if (hFile == INVALID_HANDLE_VALUE) 
        throw "Unable to open file for write!"; 

    DWORD numBytesWritten; 
    WriteFile (hFile, m_pfScoreBuf, m_nScoreBufSize*sizeof(float), &numBytesWritten, NULL); 
	CloseHandle(hFile);
}

// update the scores of ALL rectangles, return minimum and maximum score 
void BOOST::UpdateAllScores(CLASSIFIER *pC, int num)
{
    printf("Updating all scores..."); 

    // duplicate the classifier at differnt scales 
    CLASSIFIER *Classifier[MAX_NUM_SCALE]; 
    for (int m=0; m<MAX_NUM_SCALE; m++) 
    {
        float scale = pow(m_fStepScale, m); 
        Classifier[m] = CLASSIFIER::CreateScaledClassifierArray(pC, num, scale); 
        if (!Classifier[m]) 
            throw "out of memory"; 
    }

    int numStart = m_nUpdateScoreIdx; 
    int numEnd = num; 
    m_nUpdateScoreIdx = num;    // next start 

    double step = (MAX_SCORE-MIN_SCORE)*1.0/(NUM_HIST_BIN-1); 
    double invstep = (NUM_HIST_BIN-1)*1.0/(MAX_SCORE-MIN_SCORE); 
    double *pdNegWeight = new double [NUM_HIST_BIN]; 
    double *pdNegCount = new double [NUM_HIST_BIN]; 
    memset(pdNegWeight, 0, NUM_HIST_BIN*sizeof(double)); 
    memset(pdNegCount, 0, NUM_HIST_BIN*sizeof(double)); 

    int k, scoreFileIdx = 0, scoreIdx = 0; 
    ReadScoreFile(scoreFileIdx); 
    IMAGE image; 
    IN_IMAGE iimage; 
    vector<IMGINFO *>::iterator it; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        image.Load(pInfo->m_szFileName); 
        iimage.Init(&image); 

        RLCODEC labelDec(&pInfo->m_LabelVec, RLCODEC::DECODER); 
        char label; 
        for (int m=0; m<MAX_NUM_SCALE; m++) 
        {
            CLASSIFIER *pC = Classifier[m]; 

            LOOP_PARA *p = &m_LoopPara[m]; 
            IRECT rect (0, p->m_nW, 0, p->m_nH); 

            while (rect.m_iyMax <= pInfo->m_nImgHeight) 
            {
                while (rect.m_ixMax <= pInfo->m_nImgWidth) 
                {
                    labelDec.DecodeNext(label); 
                    if (label != 0)
                    {
                        if (m_pfScoreBuf[scoreIdx] > MIN_SCORE)
                        {
                            float norm = iimage.ComputeNorm(&rect); 
                            for (int j=numStart; j<numEnd; j++)
                            {
                                float fVal = pC[j].m_Feature.Eval((I_IMAGE *)&iimage, norm, rect.m_ixMin, rect.m_iyMin); 
                                float *pTh = pC[j].GetFeatureTh(); 
                                float *pScore = pC[j].GetDScore(); 
                                for (k=0; k<pC[j].m_nNumTh; k++) 
                                {
                                    if (fVal > pTh[k]) 
                                    {
                                        m_pfScoreBuf[scoreIdx] += pScore[k]; 
                                        break;
                                    }
                                }
                                if (k == pC[j].m_nNumTh) m_pfScoreBuf[scoreIdx] += pScore[k]; 
                            }
                            if (label == -1)     // this is a negative rectangle
                            {
                                if (m_pfScoreBuf[scoreIdx] < pC[numEnd-1].GetMinPosScoreTh()) 
                                    m_pfScoreBuf[scoreIdx] = MIN_SCORE;     // directly set it to MIN_SCORE so we will never look it again
                                int idx = int((m_pfScoreBuf[scoreIdx]-MIN_SCORE)*invstep + 0.5); 
                                idx = min(NUM_HIST_BIN-1, idx);  // idx is guaranteed to be greater than or equal to zero
                                //idx = min(NUM_HIST_BIN-1, max(idx,0));
                                pdNegWeight[idx] += ComputeWeight(-1, m_pfScoreBuf[scoreIdx]); 
                                pdNegCount[idx] += 1; 
                            }
                        }
                        scoreIdx ++; 
                        if (scoreIdx == m_nScoreBufSize) 
                        {
                            WriteScoreFile(scoreFileIdx); 
                            scoreFileIdx ++; 
                            ReadScoreFile(scoreFileIdx); 
                            scoreIdx = 0; 
                        }
                    }
                    rect.m_ixMin += p->m_nSW; 
                    rect.m_ixMax = rect.m_ixMin + p->m_nW; 
                }
                rect.m_iyMin += p->m_nSH; 
                rect.m_iyMax = rect.m_iyMin + p->m_nH; 
                rect.m_ixMin = 0; 
                rect.m_ixMax = p->m_nW; 
            }
        }
    }
    if (scoreIdx > 0) 
        WriteScoreFile(scoreFileIdx); 
    printf("Done\n"); 

    double negSum = 0.0, negCount = 0.0; 
    int i; 
    for (i=0; i<NUM_HIST_BIN; i++) 
    {
        negSum += pdNegWeight[i]; 
        negCount += pdNegCount[i]; 
    }
    printf("Number of negative examples stay in training = %lf\n", negCount); 
    m_RemaskPara.m_dNegScoreThCount = negCount; 
    double targetSum = negSum*m_fNegRejPercent; 
    negSum = 0.0, negCount = 0.0; 
    for (i=0; i<NUM_HIST_BIN; i++) 
    {
        negSum += pdNegWeight[i]; 
        negCount += pdNegCount[i]; 
        if (negSum > targetSum || 
            m_RemaskPara.m_dNegScoreThCount-negCount < m_nMaxNumExamples-m_RemaskPara.m_dTotalPosCount)
            break; 
    }
    negCount -= pdNegCount[i]; 
    m_RemaskPara.m_fScoreTh = (float)(MIN_SCORE + max(step*(i-0.5),0)); 
    m_RemaskPara.m_dNegScoreThCount -= negCount; 
    m_RemaskPara.m_dSampleRatio = (m_nMaxNumExamples-m_RemaskPara.m_dTotalPosCount)/m_RemaskPara.m_dNegScoreThCount; 
    if (m_RemaskPara.m_dSampleRatio > 1.0) 
        m_RemaskPara.m_dSampleRatio = 1.0; 
    printf("Threshold = %f, number of negative examples above threshold = %lf\n", 
        m_RemaskPara.m_fScoreTh, m_RemaskPara.m_dNegScoreThCount); 

    delete []pdNegWeight; 
    delete []pdNegCount; 
    for (int m=0; m<MAX_NUM_SCALE; m++) 
        CLASSIFIER::DeleteClassifierArray(Classifier[m]); 

    printf("Done\n"); 
}

void BOOST::ReInitAllExamples(CLASSIFIER *pC, int num)
{
    // this re-initialization process is partially based on the work by J. Friedman et al., "Additive
    // Logistic Regression: a Statistical View of Boosting", Dept. of Statistics, Stanford Univ. 
    // Technical Report, 1998 

    printf("Reinitilizing all examples...\n"); 

    UpdateAllScores(pC, num); 

    printf("Initializing the examples..."); 

    vector<IMGINFO *>::iterator it; 
    int maxTotalCount = 0; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        maxTotalCount = max(maxTotalCount, pInfo->m_nTotalCount); 
    }
    EXAMPLE *pExamples = new EXAMPLE [maxTotalCount]; 
    char *pMask = new char [maxTotalCount]; 
    if (!pExamples || !pMask) 
        throw "Out of memory"; 

    int scoreFileIdx = 0, scoreIdx = 0; 
    ReadScoreFile(scoreFileIdx); 
    m_nNumExamples = 0; 
    IMAGE image; 
    IN_IMAGE iimage; 
    for (it=m_ImgInfoVec.begin(); it!=m_ImgInfoVec.end(); it++) 
    {
        IMGINFO *pInfo = *it; 
        image.Load(pInfo->m_szFileName); 
        iimage.Init(&image); 

        RLCODEC labelDec(&pInfo->m_LabelVec, RLCODEC::DECODER); 
        char label; 
        int negScoreThCount = 0; 
        for (int j=0; j<pInfo->m_nTotalCount; j++) 
        {
            labelDec.DecodeNext(label); 
            pExamples[j].m_nLabel = label; 
            pExamples[j].m_nSampled = 0; 
            if (label == 0) 
                continue;       // we don't care about the skipped examples 
            pExamples[j].m_fScore = m_pfScoreBuf[scoreIdx]; 
            if (pExamples[j].m_fScore > m_RemaskPara.m_fScoreTh && pExamples[j].m_nLabel == -1)
                negScoreThCount ++; 
            scoreIdx ++; 
            if (scoreIdx == m_nScoreBufSize) 
            {
                scoreFileIdx ++; 
                ReadScoreFile(scoreFileIdx); 
                scoreIdx = 0; 
            }
        }

        int numNegExamples = int(m_RemaskPara.m_dSampleRatio*negScoreThCount);     // flooring so we never exceed m_nMaxNumExamples
        char unmask=0, mask=1; 
        if (m_RemaskPara.m_dSampleRatio > 0.5) 
        {   // then we mask the unsampled ones (simple trick to make it faster :)) 
            unmask = 1; 
            mask = 0; 
            numNegExamples = negScoreThCount - numNegExamples; 
        }
        memset(pMask, unmask, negScoreThCount); 
        int cnt = 0; 
        while (cnt < numNegExamples) 
        {
            int idx = (int)m_Rand.URand(0, negScoreThCount); 
            if (pMask[idx] == unmask) 
            {
                pMask[idx] = mask; 
                cnt ++; 
            }
        }

        int negCnt=0; 
        int idx = 0; 
        for (int m=0; m<MAX_NUM_SCALE; m++) 
        {
            LOOP_PARA *p = &m_LoopPara[m]; 
            IRECT rect (0, p->m_nW, 0, p->m_nH); 
            while (rect.m_iyMax <= pInfo->m_nImgHeight) 
            {
                while (rect.m_ixMax <= pInfo->m_nImgWidth) 
                {
                    mask = 0; 
                    if (pExamples[idx].m_nLabel == 1)
                        mask = 1; 
                    else if (pExamples[idx].m_fScore > m_RemaskPara.m_fScoreTh && pExamples[idx].m_nLabel == -1)
                    {
                        if (pMask[negCnt]) 
                            mask = 1; 
                        negCnt ++; 
                    }
                    if (mask == 1) 
                    {
                        m_pExamples[m_nNumExamples].m_nLabel = pExamples[idx].m_nLabel; 
                        m_pExamples[m_nNumExamples].m_nSizeIdx = m; 
                        m_pExamples[m_nNumExamples].m_fScore = pExamples[idx].m_fScore; 
                        m_pExamples[m_nNumExamples].m_fWeight = ComputeWeight(pExamples[idx].m_nLabel, pExamples[idx].m_fScore); 
                        m_pExamples[m_nNumExamples].m_pIImg->InitWithSubSample(&iimage, rect.m_ixMin, rect.m_iyMin, p->m_fScale); 
                        m_pExamples[m_nNumExamples].m_fNorm = iimage.ComputeNorm(&rect); 
                        m_nNumExamples ++; 
                    }
                    rect.m_ixMin += p->m_nSW; 
                    rect.m_ixMax = rect.m_ixMin + p->m_nW; 
                    idx ++; 
                }
                rect.m_iyMin += p->m_nSH; 
                rect.m_iyMax = rect.m_iyMin + p->m_nH; 
                rect.m_ixMin = 0; 
                rect.m_ixMax = p->m_nW; 
            }
        }
    }
    printf("Done\n"); 

#if defined(USE_ROBUST_SAMPLING)
    if (num > START_RS_NUM)
    {
        VALUEINDEX *pVI = new VALUEINDEX[int(m_RemaskPara.m_dTotalPosCount)]; 
        if (pVI == NULL) 
            throw "out of memory"; 
        EXAMPLE *pE = m_pExamples; 
        int j = 0; 
        for (int i=0; i<m_nNumExamples; i++, pE++)
        {
            if (pE->m_nLabel == 1) 
            {
                pVI[j].m_fVal = pE->m_fScore; 
                pVI[j++].m_nIdx = i; 
            }
        }
        qsort(pVI, j, sizeof(VALUEINDEX), compare_value); 
        for (int i=0; i<j*RS_PERCENT; i++) 
            m_pExamples[pVI[i].m_nIdx].m_fWeight = 0.0f ;    // force weight to be zero
        delete []pVI; 
    }
#endif

    delete []pMask; 
    delete []pExamples; 
}

void BOOST::SetFeature(int idx, FEATURE *pF)
{
    if (idx >= 0) 
    {
        pF->m_nType = FEATURE::RECTFEATURE; 
        pF->m_pF.pRCF = new RCFEATURE(&m_RCFeatures[0][idx]); 
    }
    else
        pF->m_nType = FEATURE::NORMFEATURE; 
}

float BOOST::FindInitScore() 
{
    int nPosCount=0, nNegCount=0; 
    for (int i=0; i<m_nNumExamples; i++) 
    {
        if (m_pExamples[i].m_nLabel == 1) 
            nPosCount ++; 
        else    // m_pExamples[i].m_nLabel == -1
            nNegCount ++; 
    }

    // we want nPosCount * 1 / (1+exp(score)) == nNegCount * 1 / (1+exp(-score)) 
    // or, nNegCount - nPosCount = nPosCount*exp(-score) - nNegCount*exp(score) 
    // we solved it with binary search
	// When score -> infinity, dRHS goes to -infinity
	// When score -> -infinity, dRHS goes to infinity
    double score = 0.0; 
    double dLHS = nNegCount - nPosCount; 
    double dHiScore = 10.0, dLowScore = -10.0; 
    while (dHiScore - dLowScore > 1e-6) 
    {
        score = (dHiScore + dLowScore)/2.0; 
        double dRHS = nPosCount*exp(-score) - nNegCount*exp(score); 
        if (dRHS < dLHS) 
            dHiScore = score; 
        else
            dLowScore = score; 
    }
    return (float)score; 
}

bool BOOST::LoadImageInfo(const char *szPath, int *index)
{
    char szName[MAX_PATH]; 
    sprintf(szName, "%s\\label.txt", szPath); 
    FILE *fpLabel = fopen(szName, "r"); 
    if (fpLabel == NULL) 
    {
        throw "null file"; 
        return false; 
    }

    int startsize = (int)m_ImgInfoVec.size(); 
    int nNumImgs; 
    fscanf(fpLabel, "%d\n", &nNumImgs); 
    IMAGE image; 
#if defined(WRITE_POS_EXAMPLES)
    IMAGE subimage; 
    static int count_subimg = 0; 
#endif
    for (int i=0; i<nNumImgs; i++) 
    {
        IMGINFO *pInfo = new IMGINFO; 
        pInfo->ReadInfo(fpLabel, szPath); 
        pInfo->CheckInfo(); 
        if (pInfo->m_LabelType == UNANNOTATED || pInfo->m_LabelType == DISCARDED) 
        {
            delete pInfo; 
            continue; 
        }

        if (pInfo->m_nNumObj > 0) 
        {
            pInfo->m_nNumMatchRect = new int [pInfo->m_nNumObj]; 
            for (int j=0; j<pInfo->m_nNumObj; j++) 
                pInfo->m_nNumMatchRect[j] = 0; 
        }

        // count how many rectangles we have in this image, this is used to determine the initial weights 
        pInfo->m_nTotalCount = 0; 
        pInfo->m_nPosCount = 0; 
        pInfo->m_nNegCount = 0; 
        RUNLABEL runLabel = {0, -1}; 
#if !defined(WRITE_POS_EXAMPLES)
        image.Load(pInfo->m_szFileName, false); 
#else
        image.Load(pInfo->m_szFileName, true); 
#endif
        pInfo->m_nImgWidth = image.GetWidth(); 
        pInfo->m_nImgHeight = image.GetHeight(); 
        RLCODEC labelEnc(&pInfo->m_LabelVec, RLCODEC::ENCODER); 
        for (int m=0; m<MAX_NUM_SCALE; m++) 
        {
            LOOP_PARA *p = &m_LoopPara[m]; 
            IRECT rect (0, p->m_nW, 0, p->m_nH); 
            while (rect.m_iyMax <= pInfo->m_nImgHeight) 
            {
                while (rect.m_ixMax <= pInfo->m_nImgWidth) 
                {
                    int label = -1; 
                    for (int i=0; i<pInfo->m_nNumObj; i++) 
                    {
                        if (rect.DetectMatchTight(pInfo->m_pObjRcs[i], m_fStepSize, m_fStepScale))
                        {
                            label = 1; 
                            pInfo->m_nNumMatchRect[i] += 1; 
                            break; 
                        }
                        else if (rect.DetectMatchLoose(pInfo->m_pObjRcs[i]))
                        {
                            // loosely match, temporary set bPos so it won't be counted as negative examples
                            label = 0;
                        }
                    }
                    if (label == -1 && pInfo->m_LabelType == PARTIALLY_LABELED) 
                        label = 0;      // for partially labeled images, we never use negative examples 

                    labelEnc.Encode(label); 
                    pInfo->m_nTotalCount ++; 
                    if (label == 1)
                    {
                        pInfo->m_nPosCount ++; 
#if defined(WRITE_POS_EXAMPLES)
                        image.CropImage(&subimage, &rect); 
                        sprintf(szName, "D:\\Temp\\%06d.jpg", count_subimg); 
                        subimage.Save(szName); 
                        count_subimg ++; 
#endif
                    }
                    else if (label == -1)
                        pInfo->m_nNegCount ++; 
                    rect.m_ixMin += p->m_nSW; 
                    rect.m_ixMax = rect.m_ixMin + p->m_nW; 
                }
                rect.m_iyMin += p->m_nSH; 
                rect.m_iyMax = rect.m_iyMin + p->m_nH; 
                rect.m_ixMin = 0; 
                rect.m_ixMax = p->m_nW; 
            }
        }

        if (pInfo->m_nNumObj > 0) 
        {
            bool bDiscard = false; 
            for (int j=0; j<pInfo->m_nNumObj; j++) 
            {
                if (pInfo->m_nNumMatchRect[j] == 0)
                    bDiscard = true;    // discard the image if postive example are not scanned 
            }
            if (bDiscard) 
            {
                delete pInfo; 
                continue; 
            }
        }

        labelEnc.EncodeDone(); 
        *index += 1; 
        m_ImgInfoVec.push_back(pInfo); 

        int size = (int)m_ImgInfoVec.size(); 
        if (size!=startsize && (size-startsize)%500 == 0)
            printf ("%d images have been loaded from %s!\r", size-startsize, szPath); 
    }

    int size = (int)m_ImgInfoVec.size(); 
    printf ("%d images have been loaded from %s!\n", size-startsize, szPath); 
    
    fclose(fpLabel); 

    return true; 
}

DWORD WINAPI SelectOneFeatureThreadProc(LPVOID lpParam)
{
    THREADPROC_PARA *pPara = (THREADPROC_PARA *)lpParam; 
    pPara->m_pBoost->SelectOneFeatureProc(pPara->m_nStart, pPara->m_nEnd, pPara->m_pVI);
    return 0; 
}

void BOOST::SelectOneFeatureProc(int nStart, int nEnd, VALUEINDEX *pVI)
{
    // we will use the sampled example set to find the top NUM_TOP_FEATURES number of features 
    VALUEINDEX tmpVI = {-1, pVI[0].m_fVal}; 
    vector<VALUEINDEX> topFeatureVec; 
    vector<VALUEINDEX>::iterator it; 
    topFeatureVec.assign(NUM_TOP_FEATURES, tmpVI); 
    double *pdPosWeight = new double [NUM_HIST_BIN]; 
    double *pdNegWeight = new double [NUM_HIST_BIN]; 
    float fFTh[MAX_NUM_FEATURE_TH]; 
    float fDScore[MAX_NUM_FEATURE_TH+1];

    // loop through all the features 
    for (int j=nStart; j<=nEnd; j++) 
    {
        SAMPLED_EXAMPLE *pSE = m_pSampledExamples; 
        float maxFVal = -1e6, minFVal = 1e6; 
        for (int i=0; i<m_nNumSampledExamples; i++,pSE++) 
        {
            maxFVal = max(maxFVal, pSE->m_fFeature[j]); 
            minFVal = min(minFVal, pSE->m_fFeature[j]); 
        }
        float step = (maxFVal-minFVal)/(NUM_HIST_BIN-1); 
        float invstep = (NUM_HIST_BIN-1)/(maxFVal-minFVal); 
        memset(pdPosWeight, 0, NUM_HIST_BIN*sizeof(double)); 
        memset(pdNegWeight, 0, NUM_HIST_BIN*sizeof(double)); 
        pSE = m_pSampledExamples;
        // since it's too expansive to sort all examples, we do binning here 
        for (int i=0; i<m_nNumSampledExamples; i++,pSE++) 
        {
            int idx = int((pSE->m_fFeature[j]-minFVal)*invstep+0.5); 
            if (pSE->m_nLabel == 1) 
                pdPosWeight[idx] += 1.0; 
            else 
                pdNegWeight[idx] += 1.0; 
        }

        float zMinScore = FindFeatureThresholds(pdPosWeight, pdNegWeight, NUM_HIST_BIN, 
                                                minFVal, step, m_nNumFeatureTh, 
                                                fFTh, fDScore); 
        tmpVI = topFeatureVec.back(); 
        if (zMinScore < tmpVI.m_fVal) 
        {
            tmpVI.m_fVal = zMinScore; 
            tmpVI.m_nIdx = j; 
            for (it=topFeatureVec.begin(); it!=topFeatureVec.end(); it++)
            {
                if ((*it).m_fVal > zMinScore)
                {
                    topFeatureVec.insert(it, tmpVI); 
                    break; 
                }
            }
            topFeatureVec.pop_back(); 
        }
    }

    VALUEINDEX *pTmpVI = pVI; 
    for (it=topFeatureVec.begin(); it!=topFeatureVec.end(); it++,pTmpVI++)
        *pTmpVI = *it; 

    delete []pdPosWeight; 
    delete []pdNegWeight; 
}

int BOOST::SelectNormFeature(CLASSIFIER *pC)
{
    // now use the whole example set to select the top feature
    printf("Compute for the first norm feature..."); 
    double posSum = 0.0, negSum = 0.0; 
    for (int i=0; i<m_nNumExamples; i++)
    {
        if (m_pExamples[i].m_nLabel == 1) 
            posSum += m_pExamples[i].m_fWeight; 
        else if (m_pExamples[i].m_nLabel == -1) 
            negSum += m_pExamples[i].m_fWeight; 
        else
            throw "Invalid example"; 
    }
    int nFeatureIdx = -1; 
    float zMinScore = (float)sqrt(posSum*negSum); 

    float fFTh[MAX_NUM_FEATURE_TH]; 
    float fDScore[MAX_NUM_FEATURE_TH+1];
    double *pdPosWeight = new double [NUM_HIST_BIN]; 
    double *pdNegWeight = new double [NUM_HIST_BIN]; 

    EXAMPLE *pE = m_pExamples; 
    float maxFVal = -1e6, minFVal = 1e6; 
    for (int i=0; i<m_nNumExamples; i++, pE++) 
    {
        pE->m_fFVal = pE->m_fNorm; 
        maxFVal = max(maxFVal, pE->m_fFVal); 
        minFVal = min(minFVal, pE->m_fFVal); 
    }

    float step = (maxFVal-minFVal)/(NUM_HIST_BIN-1); 
    float invstep = (NUM_HIST_BIN-1)/(maxFVal-minFVal); 
    memset(pdPosWeight, 0, NUM_HIST_BIN*sizeof(double)); 
    memset(pdNegWeight, 0, NUM_HIST_BIN*sizeof(double)); 
    pE = m_pExamples; 
    for (int i=0; i<m_nNumExamples; i++,pE++) 
    {
        int idx = int((pE->m_fFVal-minFVal)*invstep+0.5); 
        if (pE->m_nLabel == 1) 
            pdPosWeight[idx] += pE->m_fWeight; 
        else 
            pdNegWeight[idx] += pE->m_fWeight; 
    }
    float zScore = FindFeatureThresholds(pdPosWeight, pdNegWeight, NUM_HIST_BIN, 
                                         minFVal, step, m_nNumFeatureTh, 
                                         fFTh, fDScore); 

    pC->SetFeatureTh(fFTh); 
    pC->SetDScore(fDScore); 
    pC->SetMinPosScoreTh(-1000000);       // temporary set to a very small number 
    SetFeature(-1, &pC->m_Feature); 
    printf ("Done!\n"); 

    delete []pdPosWeight; 
    delete []pdNegWeight; 

    return -1; 
}

int BOOST::SelectOneFeature(CLASSIFIER *pC)
{
    ASSERT (m_pExamples && m_pSampledExamples); 

    // compute the total weights of positive and negative examples 
    double posSum = 0.0, negSum = 0.0; 
    for (int i=0; i<m_nNumSampledExamples; i++)
    {
        if (m_pSampledExamples[i].m_nLabel == 1) 
            posSum += 1.0; 
        else if (m_pSampledExamples[i].m_nLabel == -1) 
            negSum += 1.0; 
        else
            throw "Invalid example"; 
    }
    printf ("In the sampled example set, there are %d positive examples and %d negative examples\n", 
        (int)posSum, int(negSum)); 

#if defined(INCLUDE_RANDOM_RCFEATURES)
    GenerateRCFeatures(); 
#endif

    float fFTh[MAX_NUM_FEATURE_TH], fMinFTh[MAX_NUM_FEATURE_TH]; 
    float fDScore[MAX_NUM_FEATURE_TH+1], fMinDScore[MAX_NUM_FEATURE_TH+1];
    double *pdPosWeight = new double [NUM_HIST_BIN]; 
    double *pdNegWeight = new double [NUM_HIST_BIN]; 
    int nNumFeatures = m_nNumRCFeatures; 
    printf ("A total of %d features will be processed. This may take a few minutes...", nNumFeatures); 

    VALUEINDEX pVI[NUM_PROC_THREADS][NUM_TOP_FEATURES]; 
    for (int i=0; i<NUM_PROC_THREADS; i++) 
        pVI[i][0].m_fVal = (float)sqrt(posSum*negSum); 

    THREADPROC_PARA tpPara[NUM_PROC_THREADS]; 
    for (int i=0; i<NUM_PROC_THREADS; i++) 
    {
        tpPara[i].m_pBoost = this; 
        tpPara[i].m_nStart = i*nNumFeatures/NUM_PROC_THREADS; 
        tpPara[i].m_nEnd = (i+1)*nNumFeatures/NUM_PROC_THREADS-1; 
        tpPara[i].m_pVI = pVI[i];
    }
    tpPara[NUM_PROC_THREADS-1].m_nEnd = nNumFeatures-1; 

    HANDLE hThread[NUM_PROC_THREADS]; 
    DWORD dwThreadId[NUM_PROC_THREADS]; 
    for (int i=0; i<NUM_PROC_THREADS; i++) 
    {
        hThread[i] = CreateThread(
            NULL,                           // default security
            0,                              // use default stack size
            SelectOneFeatureThreadProc,     // thread function
            &tpPara[i],                     // augument to thread function
            0,                              // use default creation flags
            &dwThreadId[i]);                // returns the thread identifier
        if (hThread[i] == NULL) 
            throw "Thread creation failed"; 
    }

    WaitForMultipleObjects(NUM_PROC_THREADS, hThread, TRUE, INFINITE); 
    for (int i=0; i<NUM_PROC_THREADS; i++) 
        CloseHandle(hThread[i]); 
    printf ("Done!\n"); 

    VALUEINDEX tmpVI = {-1, (float)sqrt(posSum*negSum)}; 
    vector<VALUEINDEX> topFeatureVec; 
    vector<VALUEINDEX>::iterator it; 
    topFeatureVec.assign(NUM_TOP_FEATURES, tmpVI); 
    for (int i=0; i<NUM_PROC_THREADS; i++) 
        for (int j=0; j<NUM_TOP_FEATURES; j++)
        {
            tmpVI = topFeatureVec.back(); 
            if (pVI[i][j].m_fVal < tmpVI.m_fVal) 
            {
                tmpVI.m_fVal = pVI[i][j].m_fVal; 
                tmpVI.m_nIdx = pVI[i][j].m_nIdx; 
                for (it=topFeatureVec.begin(); it!=topFeatureVec.end(); it++)
                {
                    if ((*it).m_fVal > pVI[i][j].m_fVal)
                    {
                        topFeatureVec.insert(it, tmpVI); 
                        break; 
                    }
                }
                topFeatureVec.pop_back(); 
            }
        }

    // now use the whole example set to select the top feature
    printf("Select among the top features..."); 
    posSum = 0.0, negSum = 0.0; 
    for (int i=0; i<m_nNumExamples; i++)
    {
        if (m_pExamples[i].m_nLabel == 1) 
            posSum += m_pExamples[i].m_fWeight; 
        else if (m_pExamples[i].m_nLabel == -1) 
            negSum += m_pExamples[i].m_fWeight; 
        else
            throw "Invalid example"; 
    }
    int nFeatureIdx = -1; 
    float zMinScore = (float)sqrt(posSum*negSum); 
    for (it=topFeatureVec.begin(); it!=topFeatureVec.end(); it++)
    {
        //printf("%f\n", (*it).m_fVal); 
        EXAMPLE *pE = m_pExamples; 
        int fidx = (*it).m_nIdx; 
        float maxFVal = -1e6, minFVal = 1e6; 
        for (int i=0; i<m_nNumExamples; i++, pE++) 
        {
            pE->m_fFVal = m_RCFeatures[pE->m_nSizeIdx][fidx].Eval(pE->m_pIImg, pE->m_fNorm); 
            maxFVal = max(maxFVal, pE->m_fFVal); 
            minFVal = min(minFVal, pE->m_fFVal); 
        }

        float step = (maxFVal-minFVal)/(NUM_HIST_BIN-1); 
        float invstep = (NUM_HIST_BIN-1)/(maxFVal-minFVal); 
        memset(pdPosWeight, 0, NUM_HIST_BIN*sizeof(double)); 
        memset(pdNegWeight, 0, NUM_HIST_BIN*sizeof(double)); 
        pE = m_pExamples; 
        for (int i=0; i<m_nNumExamples; i++,pE++) 
        {
            int idx = int((pE->m_fFVal-minFVal)*invstep+0.5); 
            if (pE->m_nLabel == 1) 
                pdPosWeight[idx] += pE->m_fWeight; 
            else 
                pdNegWeight[idx] += pE->m_fWeight; 
        }
        float zScore = FindFeatureThresholds(pdPosWeight, pdNegWeight, NUM_HIST_BIN, 
                                             minFVal, step, m_nNumFeatureTh, 
                                             fFTh, fDScore); 
        if (zScore < zMinScore) 
        {
            zMinScore = zScore; 
            nFeatureIdx = fidx; 
            memcpy(fMinFTh, fFTh, m_nNumFeatureTh*sizeof(float)); 
            memcpy(fMinDScore, fDScore, (m_nNumFeatureTh+1)*sizeof(float)); 
        }
    }

    pC->SetFeatureTh(fMinFTh); 
    pC->SetDScore(fMinDScore); 
    pC->SetMinPosScoreTh(-1000000);       // temporary set to a very small number 
    SetFeature(nFeatureIdx, &pC->m_Feature); 
    printf ("Done!\n"); 

    delete []pdPosWeight; 
    delete []pdNegWeight; 
    return nFeatureIdx; 
}

float BOOST::FindOneFeatureThreshold(double *pdPosWeight, double *pdNegWeight, int numBin, int &th, float zscore[2])
{
    double posSum = 0.0, negSum = 0.0; 
    for (int i=0; i<numBin; i++) 
    {
        posSum += pdPosWeight[i];
        negSum += pdNegWeight[i];
    }
    double partialPosSum = pdPosWeight[0];
    double partialNegSum = pdNegWeight[0]; 
    double remainPosSum = posSum - partialPosSum; 
    double remainNegSum = negSum - partialNegSum; 
    th = 0; 
    zscore[0] = (float)sqrt(remainPosSum*remainNegSum); 
    zscore[1] = (float)sqrt(partialPosSum*partialNegSum); 
    float zMinScore = zscore[0] + zscore[1]; 
    for (int i=1; i<numBin; i++)
    {
        partialPosSum += pdPosWeight[i]; 
        partialNegSum += pdNegWeight[i]; 
        remainPosSum = posSum - partialPosSum; 
        remainNegSum = negSum - partialNegSum; 
        float s1 = (float)sqrt(remainPosSum*remainNegSum); 
        float s2 = (float)sqrt(partialPosSum*partialNegSum); 
        float zScore = s1 + s2; 
        if (zScore < zMinScore) 
        {
            th = i; 
            zscore[0] = s1; 
            zscore[1] = s2; 
            zMinScore = zScore; 
        }
    }
    return zMinScore; 
}

float BOOST::FindFeatureThresholds(double *pdPosWeight,  // array of positive weights
                                   double *pdNegWeight,  // array of negative weights
                                   int numBin,           // number of bins in the above weight arrays
                                   float minFVal,        // minimum feature value 
                                   float step,           // step size of the bins
                                   int numFTh,           // number of feature thresholds
                                   float *pfFTh,         // feature threshold array (output)
                                   float *pfDScore)      // dscore array (output)
{
    int nTh[MAX_NUM_FEATURE_TH], th, tmpNum;
    bool bThMoved[MAX_NUM_FEATURE_TH]; 
    float fZScore[MAX_NUM_FEATURE_TH+1], zscore[2]; 
    float bCanSplit[MAX_NUM_FEATURE_TH+1]; 
    double *pdTmpPosW, *pdTmpNegW; 

    ASSERT (numFTh >= 1 && numFTh < NUM_HIST_BIN); 
    if (numFTh == 1) 
    {
        FindOneFeatureThreshold(pdPosWeight, pdNegWeight, numBin, th, zscore); 
        nTh[0] = th; 
        fZScore[0] = zscore[0], fZScore[1] = zscore[1]; 
    }
    else
    {
/*      // initialize with uniformly distributed thresholds. Can be used to test Lao's F&G2004 paper if MAX_ITER
        // is set to 0. In my test this does not work as well as the splitting approach below 
        for (int i=0; i<numFTh; i++)
            nTh[i] = (NUM_HIST_BIN*(numFTh-i))/(numFTh+1);  // uniformly spread the initial values
        double posSum = 0.0, negSum = 0.0; 
        for (int i=numBin-1; i>nTh[0]; i--) 
        {
            posSum += pdPosWeight[i];
            negSum += pdNegWeight[i];
        }
        fZScore[0] = (float)sqrt(posSum*negSum); 
        for (int i=1; i<numFTh; i++) 
        {
            posSum = 0, negSum = 0;  
            for (int j=nTh[i-1]; j>nTh[i]; j--)
            {
                posSum += pdPosWeight[j];
                negSum += pdNegWeight[j];
            }
            fZScore[i] = (float)sqrt(posSum*negSum); 
        }
        posSum = 0, negSum = 0;  
        for (int i=nTh[numFTh-1]; i>=0; i--)
        {
            posSum += pdPosWeight[i];
            negSum += pdNegWeight[i];
        }
        fZScore[numFTh] = (float)sqrt(posSum*negSum); 
*/

        FindOneFeatureThreshold(pdPosWeight, pdNegWeight, numBin, th, zscore); 
        nTh[0] = th; 
        fZScore[0] = zscore[0], fZScore[1] = zscore[1]; 
        bCanSplit[0] = (th < numBin-2); 
        bCanSplit[1] = (th > 0); 
        int num = 1; 
        while (num < numFTh) 
        {
            // pick the segment with the largest z score and split
            float maxZScore; 
            int maxZScoreIdx; 
            for (int i=0; i<=num; i++) 
            {
                if (bCanSplit[i])
                {
                    maxZScore = fZScore[i]; 
                    maxZScoreIdx = i; 
                    break; 
                }
            }
            for (int i=maxZScoreIdx+1; i<=num; i++) 
            {
                if (fZScore[i] > maxZScore && bCanSplit[i])
                {
                    maxZScore = fZScore[i]; 
                    maxZScoreIdx = i; 
                }
            }
            if (maxZScoreIdx == 0) 
            {
                pdTmpPosW = &pdPosWeight[nTh[0]+1]; 
                pdTmpNegW = &pdNegWeight[nTh[0]+1]; 
                tmpNum = numBin-nTh[0]-1; 
            }
            else if (maxZScoreIdx == num) 
            {
                pdTmpPosW = pdPosWeight; 
                pdTmpNegW = pdNegWeight; 
                tmpNum = nTh[num-1]+1; 
            }
            else
            {
                pdTmpPosW = &pdPosWeight[nTh[maxZScoreIdx]+1]; 
                pdTmpNegW = &pdNegWeight[nTh[maxZScoreIdx]+1]; 
                tmpNum = nTh[maxZScoreIdx-1]-nTh[maxZScoreIdx]; 
            }
            FindOneFeatureThreshold(pdTmpPosW, pdTmpNegW, tmpNum, th, zscore); 
            if (maxZScoreIdx == num) 
            {
                nTh[num] = th; 
                fZScore[num] = zscore[0]; 
                fZScore[num+1] = zscore[1]; 
                bCanSplit[num] = (th < tmpNum-2); 
                bCanSplit[num+1] = (th > 0); 
            }
            else
            {
                fZScore[num+1] = fZScore[num]; 
                bCanSplit[num+1] = bCanSplit[num]; 
                for (int i=num; i>maxZScoreIdx; i--)
                {
                    nTh[i] = nTh[i-1]; 
                    fZScore[i] = fZScore[i-1]; 
                    bCanSplit[i] = bCanSplit[i-1]; 
                }
                nTh[maxZScoreIdx] = nTh[maxZScoreIdx+1]+1+th; 
                fZScore[maxZScoreIdx] = zscore[0]; 
                fZScore[maxZScoreIdx+1] = zscore[1]; 
                bCanSplit[maxZScoreIdx] = (th < tmpNum-2);
                bCanSplit[maxZScoreIdx+1] = (th > 0); 
            }
            num ++; 
        }

        // iterate and refine the thresholds 
        for (int i=0; i<numFTh; i++) 
            bThMoved[i] = true;  
        bool bAnyThMoved = true; 
        int numIter = 0; 
        while (numIter < MAX_ITER && bAnyThMoved)
        {
            // first threshold 
            if (bThMoved[1] || numIter == 0)
            {
                pdTmpPosW = &pdPosWeight[nTh[1]+1]; 
                pdTmpNegW = &pdNegWeight[nTh[1]+1]; 
                tmpNum = numBin-nTh[1]-1; 
                FindOneFeatureThreshold(pdTmpPosW, pdTmpNegW, tmpNum, th, zscore); 
                if (nTh[0] != nTh[1]+1+th)
                {
                    nTh[0] = nTh[1]+1+th; 
                    fZScore[0] = zscore[0]; 
                    fZScore[1] = zscore[1]; 
                    bThMoved[0] = true; 
                }
                else
                    bThMoved[0] = false; 
            }
            else
                bThMoved[0] = false; 
            // the middle ones
            for (int i=1; i<numFTh-1; i++) 
            {
                if (bThMoved[i+1] || bThMoved[i-1] || numIter == 0) 
                {
                    pdTmpPosW = &pdPosWeight[nTh[i+1]+1]; 
                    pdTmpNegW = &pdNegWeight[nTh[i+1]+1]; 
                    tmpNum = nTh[i-1]-nTh[i+1]; 
                    FindOneFeatureThreshold(pdTmpPosW, pdTmpNegW, tmpNum, th, zscore); 
                    if (nTh[i] != nTh[i+1]+1+th)
                    {
                        nTh[i] = nTh[i+1]+1+th; 
                        fZScore[i] = zscore[0]; 
                        fZScore[i+1] = zscore[1]; 
                        bThMoved[i] = true; 
                    }
                    else
                        bThMoved[i] = false; 
                }
                else
                    bThMoved[i] = false; 
            }
            // the last threshold 
            if (bThMoved[numFTh-2] || numIter == 0)
            {
                pdTmpPosW = pdPosWeight; 
                pdTmpNegW = pdNegWeight; 
                tmpNum = nTh[numFTh-2]+1; 
                FindOneFeatureThreshold(pdTmpPosW, pdTmpNegW, tmpNum, th, zscore); 
                if (nTh[numFTh-1] != th)
                {
                    nTh[numFTh-1] = th; 
                    fZScore[numFTh-1] = zscore[0]; 
                    fZScore[numFTh] = zscore[1]; 
                    bThMoved[numFTh-1] = true; 
                }
                else
                    bThMoved[numFTh-1] = false; 
            }
            else
                bThMoved[numFTh-1] = false; 
            
            bAnyThMoved = false; 
            for (int i=0; i<numFTh; i++) 
                bAnyThMoved |= bThMoved[i];  
            numIter ++; 
        }
    }

    float zMinScore = 0.0f; 
    double posSum = 0.0, negSum = 0.0; 
    for (int i=0; i<numBin; i++) 
    {
        posSum += pdPosWeight[i];
        negSum += pdNegWeight[i];
    }
    double smoothing = max((posSum + negSum)/m_nNumExamples, 1e-10); 
    pfFTh[0] = float(minFVal + step*(nTh[0]+0.5)); 
    posSum = 0.0, negSum = 0.0;
    for (int i=numBin-1; i>nTh[0]; i--) 
    {
        posSum += pdPosWeight[i];
        negSum += pdNegWeight[i];
    }
    pfDScore[0] = float(0.5*log((posSum+smoothing)/(negSum+smoothing))); 
    zMinScore += (float)sqrt(posSum*negSum); 
    for (int i=1; i<numFTh; i++) 
    {
        pfFTh[i] = float(minFVal + step*(nTh[i]+0.5)); 
        posSum = 0, negSum = 0;  
        for (int j=nTh[i-1]; j>nTh[i]; j--)
        {
            posSum += pdPosWeight[j];
            negSum += pdNegWeight[j];
        }
        pfDScore[i] = float(0.5*log((posSum+smoothing)/(negSum+smoothing))); 
        zMinScore += (float)sqrt(posSum*negSum); 
    }
    posSum = 0, negSum = 0;  
    for (int i=nTh[numFTh-1]; i>=0; i--)
    {
        posSum += pdPosWeight[i];
        negSum += pdNegWeight[i];
    }
    pfDScore[numFTh] = float(0.5*log((posSum+smoothing)/(negSum+smoothing))); 
    zMinScore += (float)sqrt(posSum*negSum); 
    return zMinScore; 
}

void BOOST::UpdateExampleWeights(CLASSIFIER *pC, int num, int fidx)
{
    printf ("Update example weights..."); 

    // take the selected feature, compute the feature value for all examples 
    EXAMPLE *pE = m_pExamples; 
    if (num > 1) 
    {
        for (int i=0; i<m_nNumExamples; i++, pE++) 
            pE->m_fFVal = m_RCFeatures[pE->m_nSizeIdx][fidx].Eval(pE->m_pIImg, pE->m_fNorm); 
    }
    else 
    {
        for (int i=0; i<m_nNumExamples; i++, pE++) 
            pE->m_fFVal = pE->m_fNorm; 
    }

    int j; 
    float minScore = 1e6; 
    float *threshold = pC[num-1].GetFeatureTh(); 
    float *dscore = pC[num-1].GetDScore(); 
    pE = m_pExamples; 
    for (int i=0; i<m_nNumExamples; i++,pE++) 
    {
        for (j=0; j<m_nNumFeatureTh; j++) 
        {
            if (pE->m_fFVal > threshold[j]) 
            {
                pE->m_fScore += dscore[j]; 
                break;
            }
        }
        if (j == m_nNumFeatureTh) pE->m_fScore += dscore[j]; 
        if (pE->m_nLabel == 1 && pE->m_fScore < minScore)
            minScore = pE->m_fScore; 
        pE->m_fWeight = ComputeWeight(pE->m_nLabel, pE->m_fScore); 
    }
    printf("Done!\n"); 

#if defined(USE_ROBUST_SAMPLING)
    if (num > START_RS_NUM)
    {
        VALUEINDEX *pVI = new VALUEINDEX[int(m_RemaskPara.m_dTotalPosCount)]; 
        if (pVI == NULL) 
            throw "out of memory"; 
        pE = m_pExamples; 
        j = 0; 
        for (int i=0; i<m_nNumExamples; i++, pE++)
        {
            if (pE->m_nLabel == 1) 
            {
                pVI[j].m_fVal = pE->m_fScore; 
                pVI[j++].m_nIdx = i; 
            }
        }
        qsort(pVI, j, sizeof(VALUEINDEX), compare_value); 
        for (int i=0; i<j*RS_PERCENT; i++) 
            m_pExamples[pVI[i].m_nIdx].m_fWeight = 0.0f ;    // force weight to be zero
        delete []pVI; 
    }
#endif

    pE = m_pExamples; 
    double posSum = 0.0, negSum = 0.0; 
    for (int i=0; i<m_nNumExamples; i++,pE++) 
    {
        if (pE->m_nLabel == 1) 
            posSum += pE->m_fWeight; 
        else 
            negSum += pE->m_fWeight; 
    }
    printf("positive weight sum = %lf, negative weight sum = %lf\n", posSum, negSum); 

    if (num == 1) 
    {
        for (int i=0; i<m_nNumFeatureTh+1; i++) 
            dscore[i] += m_fInitScore; 
    }
    pC[num-1].SetDScore(dscore); 
    pC[num-1].SetMinPosScoreTh(minScore); 
}

