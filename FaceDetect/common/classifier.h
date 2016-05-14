#pragma once

/******************************************************************************\
*
*   
*
\******************************************************************************/

#include <iostream>
#include "feature.h"

class CLASSIFIER
{
public: 
    CLASSIFIER(); 
    ~CLASSIFIER(); 

public: 
    int         m_nNumTh; 
    float     * m_pfFeatureTh;       // threshold array for the feature 
    float     * m_pfDScore;        // delta score for each segment 
    float       m_fMinPosScoreTh; 
    FEATURE     m_Feature; 

    float * GetFeatureTh() { return m_pfFeatureTh; }; 
    float * GetDScore() {return m_pfDScore;}; 
    float GetMinPosScoreTh() { return m_fMinPosScoreTh; }; 
    void  SetFeatureTh(float *pTh) { for (int i=0; i<m_nNumTh; i++) m_pfFeatureTh[i] = pTh[i]; }; 
    void  SetDScore(float *pScore) { for (int i=0; i<m_nNumTh+1; i++) m_pfDScore[i] = pScore[i]; }; 
    void  SetMinPosScoreTh(float fMinTh) { m_fMinPosScoreTh = fMinTh; }; 

    void Release(); 
    void Init(FILE *file);
    void Write(FILE *file); 

    static void DuplicateClassifier(CLASSIFIER *cfsrc, CLASSIFIER *cfdst, float scale=1.0f); 
    static CLASSIFIER * ReadClassifierFile(int *pCount, int *pBW, int *pBH, int *pNumFTh, float *pTh, const char *fileName);
    static CLASSIFIER * CreateClassifierArray(int *pCount, int *pNumFTh, FILE *file);
    static CLASSIFIER * CreateClassifierArray(int count, int numFTh); 
    static CLASSIFIER * CreateScaledClassifierArray(CLASSIFIER *classifierArray, int nClassifiers, float scale); 
    static void WriteClassifierFile(CLASSIFIER *classifierArray, int nClassifiers, 
        int baseWidth, int baseHeight, int numFTh, float threshold, const char *fileName);
    static void DeleteClassifierArray(CLASSIFIER *classifierArray); 
};