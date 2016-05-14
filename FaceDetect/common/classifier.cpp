/******************************************************************************\
*
*   
*
\******************************************************************************/

#include "stdafx.h"
#include "classifier.h"

CLASSIFIER::CLASSIFIER()
{
    m_nNumTh = 0; 
    m_pfFeatureTh = NULL; 
    m_pfDScore = NULL; 
}

CLASSIFIER::~CLASSIFIER()
{
    Release(); 
}

void CLASSIFIER::Release()
{
    m_nNumTh = 0; 
    if (m_pfFeatureTh != NULL) { delete []m_pfFeatureTh; m_pfFeatureTh = NULL; }
    if (m_pfDScore != NULL) { delete []m_pfDScore; m_pfDScore = NULL; }
}

void CLASSIFIER::DuplicateClassifier(CLASSIFIER *cfsrc, CLASSIFIER *cfdst, float scale)
{
    cfdst->Release(); 
    cfdst->m_nNumTh = cfsrc->m_nNumTh; 
    cfdst->m_pfFeatureTh = new float [cfdst->m_nNumTh]; 
    cfdst->m_pfDScore = new float [cfdst->m_nNumTh+1]; 
    if (cfdst->m_pfFeatureTh == NULL || cfdst->m_pfDScore == NULL)
        throw "out of memory"; 

    cfdst->SetFeatureTh(cfsrc->GetFeatureTh()); 
    cfdst->SetDScore(cfsrc->GetDScore()); 
    cfdst->SetMinPosScoreTh(cfsrc->GetMinPosScoreTh()); 
    cfdst->m_Feature.Init(&cfsrc->m_Feature, scale); 
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

CLASSIFIER* CLASSIFIER::ReadClassifierFile(int *pCount, int *pBW, int *pBH, int *pNumFTh, float *pTh, const char *fileName)
{
    FILE *file = fopen(fileName, "r");
    if (file == NULL) throw "fopen";

    if (fscanf(file, "%d %d", pBW, pBH) != 2)   // base width and height 
        throw "base width and height"; 

    CLASSIFIER *classifierArray = 0;

    try
    {
        classifierArray = CreateClassifierArray(pCount, pNumFTh, file);
    }
    catch (...)
    {
        if (fclose(file) != 0)
            throw "fclose";
        throw;
    }

    if (fscanf(file, "%f", pTh) != 1)   // final decision threshold 
        throw "decision threshold"; 

    if (fclose(file) != 0)
        throw "fclose";

    return classifierArray;
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

CLASSIFIER* CLASSIFIER::CreateClassifierArray(int count, int numFTh)
{
    CLASSIFIER *classifierArray = new CLASSIFIER[count];
    if (classifierArray == NULL)
        throw "out of memory";

    for (int i=0; i<count; i++) 
    {
        classifierArray[i].m_nNumTh = numFTh; 
        classifierArray[i].m_pfFeatureTh = new float [numFTh]; 
        classifierArray[i].m_pfDScore = new float [numFTh+1]; 
        if (classifierArray[i].m_pfFeatureTh == NULL || 
            classifierArray[i].m_pfDScore == NULL)
            throw "out of memory"; 
    }
    return classifierArray; 
}

CLASSIFIER* CLASSIFIER::CreateClassifierArray(int *pCount, int *pNumFTh, FILE *file)
{
    int nClassifiers = 0, nNumTh;
    if (fscanf(file, "%d %d", &nClassifiers, &nNumTh) != 2)
        throw "nClassifiers & nNumTh";

    CLASSIFIER *classifierArray = new CLASSIFIER[nClassifiers];
    if (classifierArray == NULL)
        throw "out of memory";

    for (int i = 0; i < nClassifiers; i++)
    {
        classifierArray[i].m_nNumTh = nNumTh; 
        classifierArray[i].m_pfFeatureTh = new float [nNumTh]; 
        classifierArray[i].m_pfDScore = new float [nNumTh+1]; 
        if (classifierArray[i].m_pfFeatureTh == NULL || 
            classifierArray[i].m_pfDScore == NULL)
            throw "out of memory"; 
        classifierArray[i].Init(file);
    }

    *pCount = nClassifiers;
    *pNumFTh = nNumTh; 
    return classifierArray;
}

CLASSIFIER * CLASSIFIER::CreateScaledClassifierArray(CLASSIFIER *classifierArray, int nClassifiers, float scale)
{
    CLASSIFIER *scaledClassifierArray = new CLASSIFIER [nClassifiers]; 
    if (!scaledClassifierArray) 
        throw "out of memory"; 
    for (int i=0; i<nClassifiers; i++) 
        CLASSIFIER::DuplicateClassifier(&classifierArray[i], &scaledClassifierArray[i], scale); 

    return scaledClassifierArray; 
}


/******************************************************************************\
*
*   
*
\******************************************************************************/

void CLASSIFIER::Init(FILE *file)
{
    for (int i=0; i<m_nNumTh; i++) 
        fscanf(file, "%f %f", &m_pfFeatureTh[i], &m_pfDScore[i]); 
    fscanf(file, "%f %f", &m_pfDScore[m_nNumTh], &m_fMinPosScoreTh); 
    m_Feature.Init(file); 
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

void CLASSIFIER::Write(FILE *file)
{
    for (int i=0; i<m_nNumTh; i++) 
        fprintf(file, "%f %f\n", m_pfFeatureTh[i], m_pfDScore[i]); 
    fprintf (file, "%f %f\n", m_pfDScore[m_nNumTh], m_fMinPosScoreTh); 
    m_Feature.Write(file); 
}

/******************************************************************************\
*
*   
*
\******************************************************************************/

void CLASSIFIER::WriteClassifierFile(CLASSIFIER *classifierArray, int nClassifiers, 
                                     int baseWidth, int baseHeight, int numFTh, float threshold, const char *fileName)
{
    FILE *file = fopen (fileName, "w"); 
    if (file == NULL) 
        throw "open"; 

    fprintf (file, "%d %d\n%d %d\n", baseWidth, baseHeight, nClassifiers, numFTh); 
    for (int i=0; i<nClassifiers; i++) 
        classifierArray[i].Write(file); 
    fprintf (file, "%f\n", threshold); 
    fclose(file); 
}

void CLASSIFIER::DeleteClassifierArray(CLASSIFIER *classifierArray)
{
    if (classifierArray)
        delete []classifierArray; 
    classifierArray = NULL; 
}
