#include "StdAfx.h"
#include "LabeledImageCollection.h"
#include <Windows.h>
#include <stdio.h>

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;

namespace DetectionManagedLib 
{
LabeledImageCollection::LabeledImageCollection(String ^strFileName)
{
	const char *pszName = (const char *)(Marshal::StringToHGlobalAnsi(strFileName)).ToPointer();

	char aszName[MAX_PATH],
		*pch;

	// copy the name
	strcpy (aszName, pszName);
	
	Marshal::FreeHGlobal(IntPtr((void*)pszName));

	FILE *fpLabel = fopen(aszName, "r"); 
    if (fpLabel == NULL) 
    {
        throw "invalid filename"; 
    }

	_LabeledImgList = gcnew List<LabeledImg ^> ();

	// determine path
	pch = strrchr (aszName, '\\');
	if (pch != NULL)
	{
		*pch = '\0';
	}

    int nNumImgs; 
    fscanf(fpLabel, "%d\n", &nNumImgs); 

    for (int iImg = 0; iImg < nNumImgs; iImg++) 
    {
        IMGINFO *pInfo = new IMGINFO; 

		pInfo->ReadInfo(fpLabel, pch == NULL ? "." : aszName); 
        
        if (pInfo->m_LabelType == UNANNOTATED || pInfo->m_LabelType == DISCARDED) 
        {
            delete pInfo; 
            continue; 
        }

		_LabeledImgList->Add (gcnew LabeledImg(pInfo));
    }

    fclose(fpLabel); 
}
}