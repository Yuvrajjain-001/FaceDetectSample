#include "imageinfo.h"
#include "image.h"
extern "C" {
#include "cdjpeg.h"		/* Common decls for cjpeg/djpeg applications */
#include "transupp.h"		/* Support routines for jpegtran */
#include "jversion.h"		/* for version message */
#include "jpeglib.h"
}

#define FLIP_LABELED_IMAGES
//#define MERGE_LABEL_FILES
//#define CONVERT_LABEL_FROM_MSRA_FORMAT

#if defined (FLIP_LABELED_IMAGES) 
void Usage()
{
    char *msg =
        "\n"
        "Tool for flipping labeled images horizontally. This code requires\n"
        "xcopy.exe installed beforehand.\n"
        "\n"
        "hflipimage.exe InDirPath OutDirPath\n"
        "\n"
        "    InDirPath     -- name of the input directory\n"
        "    OutDirPath    -- name of the output directory\n"
        "\n";

    printf("%s\n", msg);
}

void CopyJpegFlip(char *fName, char *fNameFlip)
{   // this code is written based on the example given in jpegtran.c in jpeg-6b 
    // it turns out that for images with odd width, this flip function doesn't flip the
    // image exactly due to boundary issues, hence it is not used 
    struct jpeg_decompress_struct srcinfo; 
    struct jpeg_error_mgr srcerr; 
    jvirt_barray_ptr * src_coef_arrays;
    jvirt_barray_ptr * dst_coef_arrays;

    srcinfo.err = jpeg_std_error(&srcerr); 
    jpeg_create_decompress(&srcinfo); 

    struct jpeg_compress_struct dstinfo;
    struct jpeg_error_mgr dsterr;
    dstinfo.err = jpeg_std_error(&dsterr);
    jpeg_create_compress(&dstinfo);

    FILE *fp = fopen(fName, "rb"); 
    FILE *fpFlip = fopen(fNameFlip, "wb"); 
    jpeg_stdio_src(&srcinfo, fp);

    jcopy_markers_setup(&srcinfo, JCOPYOPT_DEFAULT);
    jpeg_read_header(&srcinfo, TRUE);

    jpeg_transform_info transformoption; 
    transformoption.transform = JXFORM_FLIP_H;
    transformoption.trim = FALSE;
    transformoption.force_grayscale = FALSE;
    jtransform_request_workspace(&srcinfo, &transformoption);

    src_coef_arrays = jpeg_read_coefficients(&srcinfo);
    jpeg_copy_critical_parameters(&srcinfo, &dstinfo);

    dst_coef_arrays = jtransform_adjust_parameters(&srcinfo, &dstinfo,
	  				                               src_coef_arrays,
                                                   &transformoption);

    jpeg_stdio_dest(&dstinfo, fpFlip);
    jpeg_write_coefficients(&dstinfo, dst_coef_arrays);
    jcopy_markers_execute(&srcinfo, &dstinfo, JCOPYOPT_DEFAULT);

    jtransform_execute_transformation(&srcinfo, &dstinfo,
                                      src_coef_arrays,
                                      &transformoption);

    jpeg_finish_compress(&dstinfo);
    jpeg_destroy_compress(&dstinfo);
    (void) jpeg_finish_decompress(&srcinfo);
    jpeg_destroy_decompress(&srcinfo);

    fclose(fp);
    fclose(fpFlip);
}

int main(int argc, char* argv[])
{
    if (argc != 3) 
    {
        Usage(); 
        return -1; 
    }

    // first copy directory structure 
    char command [1024]; 
    sprintf(command, "xcopy %s %s /T /E /I /Y", argv[1], argv[2]); 
    system(command); 

    char inFileName[MAX_PATH], outFileName[MAX_PATH];
    strcpy(inFileName, argv[1]); 
    strcat(inFileName, "\\label.txt"); 
    FILE *fpIn = fopen(inFileName, "r"); 
    strcpy(outFileName, argv[2]); 
    strcat(outFileName, "\\label.txt"); 
    FILE *fpOut = fopen(outFileName, "w"); 

    int numImg; 
    fscanf(fpIn, "%d\n", &numImg); 
    fprintf(fpOut, "%d\n", numImg); 

	IMGINFO info; 
    IMAGE img1, img2; 
    for (int i=0; i<numImg; i++) 
    {
        info.ReadInfo(fpIn, argv[1]); 

		// copy the image first 
		char fileName1[MAX_PATH], fileName2[MAX_PATH]; 
        int len = (int)strlen(argv[1]); 
        sprintf(fileName1, "%s\\%s", argv[1], &info.m_szFileName[len+1]); 
        sprintf(fileName2, "%s\\%s", argv[2], &info.m_szFileName[len+1]); 
        img1.Load(fileName1, true); 
        info.m_nImgWidth = img1.GetWidth(); 
        info.m_nImgHeight = img1.GetHeight(); 
        img1.HFlipToImage(&img2); 
        img2.Save(fileName2, 75); 

        if (info.m_LabelType == NO_FACE) 
			info.m_LabelType = DISCARDED;	// discard negative images as we already have enough negative examples
		if (info.m_LabelType == ALL_LABELED)
			info.m_LabelType = PARTIALLY_LABELED;	// again, make it partially labeled to ignore the negative examples

        for (int j=0; j<info.m_nNumObj; j++)
        {
            FEATUREPTS &fPt = info.m_pObjFPts[j]; 
            FPOINT tmpPt = fPt.leye; 
            fPt.leye.x = info.m_nImgWidth-1-fPt.reye.x; 
            fPt.leye.y = fPt.reye.y; 
            fPt.reye.x = info.m_nImgWidth-1-tmpPt.x; 
            fPt.reye.y = tmpPt.y; 

            fPt.nose.x = info.m_nImgWidth-1-fPt.nose.x; 

            tmpPt = fPt.lmouth; 
            fPt.lmouth.x = info.m_nImgWidth-1-fPt.rmouth.x; 
            fPt.lmouth.y = fPt.rmouth.y; 
            fPt.rmouth.x = info.m_nImgWidth-1-tmpPt.x; 
            fPt.rmouth.y = tmpPt.y; 
        }

        info.WriteInfo(fpOut, argv[1]); 
    }
	
    fclose(fpIn); 
	fclose(fpOut); 
}
#endif


#if defined (MERGE_LABEL_FILES) 
void Usage()
{
    char *msg =
        "\n"
        "Tool for merging multiple label files in the same folder.\n"
        "\n"
        "\n"
        "MiscProcess dirPath fileName\n"
        "\n"
        "    dirPath       -- name of the directory\n"
        "    fileName      -- name of the list file that contains all the subfolder names\n"
        "\n";

    printf("%s\n", msg);
}

int main(int argc, char* argv[])
{
    if (argc != 3) 
    {
        Usage(); 
        return -1; 
    }

    char fileName[MAX_PATH], folderName[MAX_PATH], dirName[MAX_PATH];
    strcpy(fileName, argv[1]); 
    strcat(fileName, "\\"); 
    strcat(fileName, argv[2]); 
    FILE *fpList = fopen(fileName, "r"); 
    strcpy(fileName, argv[1]); 
    strcat(fileName, "\\label.txt"); 
    FILE *fpOut = fopen(fileName, "w"); 

    int totalNumImg = 0; 
    while (fgets(folderName, MAX_PATH, fpList) != NULL)
    {
        int len = (int)strlen(folderName); 
        folderName[len-1] = '\0';           // remove the new line character 
        strcpy(fileName, argv[1]); 
        strcat(fileName, "\\"); 
        strcat(fileName, folderName); 
        strcat(fileName, "\\label.txt"); 
        FILE *fp = fopen(fileName, "r"); 
        int numImg=0; 
        fscanf(fp, "%d\n", &numImg); 
        fclose(fp); 
        totalNumImg += numImg; 
    }
    fprintf(fpOut, "%d\n", totalNumImg); 

    fseek(fpList, 0, SEEK_SET); 

    while (fgets(folderName, MAX_PATH, fpList) != NULL)
    {
        int len = (int)strlen(folderName); 
        folderName[len-1] = '\0';           // remove the new line character 
        strcpy(fileName, argv[1]); 
        strcat(fileName, "\\"); 
        strcat(fileName, folderName); 
        strcat(fileName, "\\label.txt"); 
        FILE *fp = fopen(fileName, "r"); 
        int numImg; 
        fscanf(fp, "%d\n", &numImg); 
 
        strcpy(dirName, argv[1]); 
        strcat(dirName, "\\"); 
        strcat(dirName, folderName); 

        IMGINFO info; 
        for (int i=0; i<numImg; i++) 
        {
            info.ReadInfo(fp, dirName); 
            info.WriteInfo(fpOut, argv[1]); 
        }
        fclose(fp); 
    }
    fclose(fpOut); 
    fclose(fpList); 
}

#endif

#if defined (CONVERT_LABEL_FROM_MSRA_FORMAT)
void Usage()
{
    char *msg =
        "\n"
        "Tool for converting labels from MSRA locations.txt to our own format.\n"
        "\n"
        "\n"
        "MiscProcess inFileName outFileName\n"
        "\n"
        "    inFileName    -- name of the MSRA locations.txt file\n"
        "    inFileName    -- name of the output label file (our format)\n" 
        "\n";

    printf("%s\n", msg);
}

int main(int argc, char* argv[])
{
    if (argc != 3) 
    {
        Usage(); 
        return -1; 
    }

    char newLine[1000];
    FILE *fpIn = fopen(argv[1], "r"); 
    FILE *fpOut = fopen(argv[2], "w"); 

    // count how many lines in the old label file 
    int count = 0; 
    while (fgets(newLine, 1000, fpIn) != NULL)
    {
        char fName[MAX_PATH]; 
        float tmp; 
        int retval = sscanf(newLine, 
            "%s {leye %f %f} {reye %f %f} {nose %f %f} {lmouth %f %f} {cmouth %f %f} {rmouth %f %f}\n", 
            fName, &tmp, &tmp, &tmp, &tmp, &tmp, &tmp, &tmp, &tmp, &tmp, &tmp, &tmp, &tmp); 
        if (retval == 13) 
            count ++; 
    }

    fseek(fpIn, 0, SEEK_SET); 
    fprintf(fpOut, "%d\n", count); 
    while (fgets(newLine, 1000, fpIn) != NULL)
    {
        float tmp; 
        IMGINFO info; 
        info.m_szFileName = new char [MAX_PATH]; 
        info.m_LabelType = PARTIALLY_LABELED; 
        info.m_nNumObj = 1; 
        info.m_pObjFPts = new FEATUREPTS [1]; 
        int retval = sscanf(newLine, 
            "%s {leye %f %f} {reye %f %f} {nose %f %f} {lmouth %f %f} {cmouth %f %f} {rmouth %f %f}\n", 
            info.m_szFileName, 
            &info.m_pObjFPts[0].leye.x, &info.m_pObjFPts[0].leye.y, 
            &info.m_pObjFPts[0].reye.x, &info.m_pObjFPts[0].reye.y, 
            &info.m_pObjFPts[0].nose.x, &info.m_pObjFPts[0].nose.y, 
            &info.m_pObjFPts[0].lmouth.x, &info.m_pObjFPts[0].lmouth.y, 
            &tmp, &tmp,
            &info.m_pObjFPts[0].rmouth.x, &info.m_pObjFPts[0].rmouth.y); 
        if (retval == 13) 
        {
            info.WriteInfo(fpOut, NULL); 
        }
    }
    fclose(fpIn); 
    fclose(fpOut); 

	return 0;
}
#endif