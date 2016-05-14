// ScanImages.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "ScanImages.h"
#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// The one and only application object

CWinApp theApp;

using namespace std;

void ScanFolder4Images(CString strFolder, CStringArray *pArray)
{
    CFileFind finder;

    // build a string with wildcards
    CString strWildcard = strFolder;
    strWildcard += _T("\\*.*");

    // start working for files
    BOOL bWorking = finder.FindFile(strWildcard);

    while (bWorking)
    {
        bWorking = finder.FindNextFile();

        // skip . and .. files; otherwise, we'd
        // recur infinitely!

        if (finder.IsDots())
            continue;

        // if it's a directory, recursively search it

        if (finder.IsDirectory())
        {
            CString str = finder.GetFilePath();
            cout << (LPCTSTR) str << endl;
            ScanFolder4Images(str, pArray);
        }
        
        CString str = finder.GetFileName(); 
        CString strlower = str.MakeLower(); 
        if (strlower.Right(3) == "jpg" || strlower.Right(3) == "bmp" || strlower.Right(3) == "pgm") 
            pArray->Add(finder.GetFilePath()); 
    }

    finder.Close();
}

int main(int argc, char* argv[], char* envp[])
{
	// initialize MFC and print and error on failure
	if (!AfxWinInit(::GetModuleHandle(NULL), NULL, ::GetCommandLine(), 0))
	{
		// TODO: change error code to suit your needs
		printf(_T("Fatal Error: MFC initialization failed\n"));
		return -1;
	}
	else
	{
		// TODO: code your application's behavior here.
        if (argc != 3)
        {
            printf ("Usage: ScanImages.exe foldername outputfilename"); 
            return -1; 
        }
        else
        {
            CStringArray fileNameArray; 
            CString strFolder (argv[1]); 
            strFolder.TrimRight(_T(" \\/")); 
            ScanFolder4Images(strFolder, &fileNameArray); 
            FILE *file = fopen(argv[2], "w"); 
            if (file == NULL) 
            {
                printf ("Unable to open file to write!\n"); 
                return -1;  
            }
//            fprintf (file, "%s\n%d\n", strFolder.GetBuffer(), fileNameArray.GetCount());
            fprintf (file, "%d\n", fileNameArray.GetCount());
            for (int i=0; i<fileNameArray.GetCount(); i++) 
            {
                CString str = fileNameArray.ElementAt(i); 
                CString strr = str.Right(str.GetLength() - (strFolder.GetLength()+1)); 
                fprintf (file, "%s\n", strr.GetBuffer()); 
                fprintf (file, "0\n"); 
//                fprintf (file, "1\n0\t0\t19\t19\n"); 
            }

            fclose(file); 
        }
	}

	return 0;
}
