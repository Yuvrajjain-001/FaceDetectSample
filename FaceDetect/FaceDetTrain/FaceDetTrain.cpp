// HSAVDetTrain.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "boost.h"

void Usage()
{
    char *msg =
        "\n"
        "Tool for face detection training.\n"
        "\n"
        "\n"
        "FaceDetTrain fileName \n"
        "\n"
        "    fileName      -- name of a train configuration file\n"
        "\n";

    printf("%s\n", msg);
}

int main(int argc, char* argv[])
{
    if (argc != 2) 
    {
        Usage(); 
        return -1; 
    }

    clock_t tStart, t0, t1, t2, t3, t4, tEnd; 
    tStart = clock(); 

    BOOST boost; 
    bool bRet = boost.LoadListFile(argv[1]); 
    if (!bRet) return -1; 

    boost.InitAllExamples(); 
    
    int nNumBoostFeatures = boost.GetNumBoostFeatures(); 
    int nNumFTh = boost.GetNumFeatureTh(); 
    CLASSIFIER *pClassifier = CLASSIFIER::CreateClassifierArray(nNumBoostFeatures, nNumFTh);

    int nMaskFreq = 2; 
    int fidx = -1; 
    for (int nSFIdx = 0; nSFIdx<nNumBoostFeatures; nSFIdx++) 
    {
        int nSFNum = nSFIdx + 1; 
        t0 = clock(); 
        if (nSFIdx == 0)    // force the first feature as the norm feature, it's free in computation :) 
        {
            t1 = t0; 
            boost.SelectNormFeature(&pClassifier[nSFIdx]); 
            t2 = clock(); 
        }
        else
        {
            boost.SampleExamples4FeatureSelection(); 
            t1 = clock(); 
            fidx = boost.SelectOneFeature(&pClassifier[nSFIdx]); 
            t2 = clock(); 
        }
        boost.UpdateExampleWeights(pClassifier, nSFNum, fidx); 
        t3 = clock(); 
        printf ("Times taken to sample examples: %f sec, to select a feature: %f sec, to update weights: %f sec\n", 
            float(t1-t0)/CLOCKS_PER_SEC, float(t2-t1)/CLOCKS_PER_SEC, float(t3-t2)/CLOCKS_PER_SEC); 
        char fName[MAX_PATH]; 
        sprintf(fName, "classifier%04d.txt", nSFNum); 
        CLASSIFIER::WriteClassifierFile(pClassifier, nSFNum, 
                                        boost.GetWidth(), boost.GetHeight(), 
                                        boost.GetNumFeatureTh(), 
                                        pClassifier[nSFIdx].GetMinPosScoreTh(), 
                                        fName); 

        if (nSFNum%nMaskFreq == 0)   // remask all examples every several features
        {
            boost.ReInitAllExamples(pClassifier, nSFNum); 
            t4 = clock(); 
            printf ("Times taken to remask all examples: %f sec\n", float(t4-t3)/CLOCKS_PER_SEC);
            nMaskFreq = nMaskFreq * 2; 
            if (nMaskFreq > boost.GetMaskFreq())
                nMaskFreq = boost.GetMaskFreq(); 
        }
    }

    tEnd = clock(); 
    printf ("Times taken to train the classifier: %f sec\n", float(tEnd-tStart)/CLOCKS_PER_SEC); 

    CLASSIFIER::DeleteClassifierArray(pClassifier); 
	return 0;
}

