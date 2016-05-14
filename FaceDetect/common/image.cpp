/******************************************************************************\
*
*   image related stuff
*
\******************************************************************************/

#include "stdafx.h"
#include <windows.h>
#include "image.h"

#ifndef _NO_LIBJPEG
extern "C" {
#include "jpeglib.h"
}
#endif

// this RBG/YUV conversion formular is based on MSDN document 
// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/wceddraw/html/_dxce_converting_between_yuv_and_rgb.asp

//Y = ( (  66 * R + 129 * G +  25 * B + 128) >> 8) +  16
//U = ( ( -38 * R -  74 * G + 112 * B + 128) >> 8) + 128
//V = ( ( 112 * R -  94 * G -  18 * B + 128) >> 8) + 128

//C = Y - 16
//D = U - 128
//E = V - 128
//R = clip(( 298 * C           + 409 * E + 128) >> 8)
//G = clip(( 298 * C - 100 * D - 208 * E + 128) >> 8)
//B = clip(( 298 * C + 516 * D           + 128) >> 8)
void init_color_conv()
{
    using namespace color_conv; 
    if (init) return; 
    for (int i=0; i<256; i++)
        uchar_table[i] = 0; 
    for (int i=256; i<512; i++) 
        uchar_table[i] = i-256; 
    for (int i=512; i<768; i++) 
        uchar_table[i] = 255; 

    for (int i=0; i<256; i++) 
    {
        fast_rgb2yuv_r_y[i] = 66*i; 
        fast_rgb2yuv_g_y[i] = 129*i; 
        fast_rgb2yuv_b_y[i] = 25*i + 128 + 16*(1<<8); 
        fast_rgb2yuv_r_u[i] = -38*i; 
        fast_rgb2yuv_g_u[i] = -74*i; 
        fast_rgb2yuv_b_u[i] = 112*i + 128 + 128*(1<<8);
        fast_rgb2yuv_r_v[i] = 112*i; 
        fast_rgb2yuv_g_v[i] = -94*i; 
        fast_rgb2yuv_b_v[i] = -18*i + 128 + 128*(1<<8); 

        fast_yuv2rgb_y_rgb[i] = 298*(i-16); 
        fast_yuv2rgb_v_r[i] = 409*(i-128) + 128; 
        fast_yuv2rgb_u_g[i] = -100*(i-128); 
        fast_yuv2rgb_v_g[i] = -208*(i-128) + 128; 
        fast_yuv2rgb_u_b[i] = 516*(i-128) + 128; 
    }
    init = 1;   // flag to show that the tables have been initialized }
}

/******************************************************************************\
*
*   public method IMAGE::DrawPixel
*
*   Draws a pixels on this image of the specified opacity (alpha) and
*   luminocity (value). For debugging.
*
\******************************************************************************/

void IMAGE::DrawPixel(int x, int y, float alpha, BYTE value)
{
    if (x >= 0 && x < m_width && y >= 0 && y < m_height)
    {
        const float beta  = 1.0f - alpha;
        const BYTE oldValue = GetValue(x, y);
        const float fNewValue = alpha * value + beta * oldValue;
        const BYTE u8NewValue = (BYTE)(fNewValue + 0.5f);
        SetValue(x, y, u8NewValue);
    }
}

/******************************************************************************\
*
*   public method IMAGE::DrawPixel
*
*   Draws the a rectangle in this monochrome image. For debugging.
*
\******************************************************************************/

void IMAGE::DrawRectAlpha(const IRECT *pRect, float alpha)
{
    const IRECT& irect = *pRect;
    const int xMin = irect.m_ixMin;
    const int xMax = irect.m_ixMax;
    const int yMin = irect.m_iyMin;
    const int yMax = irect.m_iyMax;
    int x, y;

    for (y = yMin; y <= yMax; y++)
    {
        DrawPixel(xMin+0, y, alpha, 0);
        DrawPixel(xMin+1, y, alpha, 255);
        DrawPixel(xMax+0, y, alpha, 255);
        DrawPixel(xMax+1, y, alpha, 0);
    }
    for (x = xMin; x <= xMax; x++)
    {
        DrawPixel(x, yMin+0, alpha, 0);
        DrawPixel(x, yMin+1, alpha, 255);
        DrawPixel(x, yMax+0, alpha, 255);
        DrawPixel(x, yMax+1, alpha, 0);
    }
}

void IMAGE::DrawRect(const IRECT *pRect, BYTE intensity)
{
    const IRECT& irect = *pRect;
    const int xMin = irect.m_ixMin;
    const int xMax = irect.m_ixMax;
    const int yMin = irect.m_iyMin;
    const int yMax = irect.m_iyMax;
    int x, y;

    for (y = yMin; y <= yMax; y++)
    {
        DrawPixel(xMin+0, y, 1.0, intensity);
        DrawPixel(xMin+1, y, 1.0, intensity);
        DrawPixel(xMax+0, y, 1.0, intensity);
        DrawPixel(xMax+1, y, 1.0, intensity);
    }
    for (x = xMin; x <= xMax; x++)
    {
        DrawPixel(x, yMin+0, 1.0, intensity);
        DrawPixel(x, yMin+1, 1.0, intensity);
        DrawPixel(x, yMax+0, 1.0, intensity);
        DrawPixel(x, yMax+1, 1.0, intensity);
    }
}

void IMAGE::LoadBMP(const char *fileName, bool bLoadImageData)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "rb")) == NULL) 
        throw "Open file failed"; 

    BITMAPFILEHEADER fh; 
    fread(&fh, sizeof(BITMAPFILEHEADER), 1, fp); 
    if (fh.bfType != 0x4d42 || fh.bfReserved1 != 0 || fh.bfReserved2 != 0) 
        throw "The file is not a valid BMP file!\n"; 
    
    BITMAPINFOHEADER ih; 
    fread(&ih, sizeof(BITMAPINFOHEADER), 1, fp); 
    if (ih.biBitCount != 8 && ih.biBitCount != 24) 
        throw "bitCount is not 8 or 24!"; 
    if (ih.biCompression != 0) 
        throw "Unsupported compressed BMP format!";

    int width = ih.biWidth; 
    int height = ih.biHeight; 
    int stride = ComputeStride(width); 

    if (!Realloc(width, height)) 
        throw "Out of memory!"; 

    if (!bLoadImageData) 
    {
        fclose(fp); 
        return; 
    }

    if (ih.biBitCount == 8) 
    {
        // skip the color map 
        fseek(fp, 256*4, SEEK_CUR);    // 8 bit color map, 256 of 4-byte RGBQUAD, ignored!

        // now read the data row by row, notice BMP stores pixels bottom up 
        for (int row=height-1; row>=0; row--) 
        {
            unsigned char *p = &m_bData[row*stride]; 
            int readsize = (int)fread((void *)p, 1, (size_t)stride, fp); 
            if (readsize != stride) 
                throw "Reading image data failure!"; 
        }
    }
    else if ( ih.biBitCount == 24 ) // need to do color conversion 
    {
        int strideRaw = ComputeStride(width*3); 
        unsigned char *pData = new unsigned char [strideRaw]; 
        if (pData == NULL) 
            throw "Out of memory"; 
        for (int row=height-1; row>=0; row--) 
        {
            unsigned char *p = &m_bData[row*stride]; 
            int readsize = (int)fread((void *)pData, 1, (size_t)strideRaw, fp); 
            if (readsize != strideRaw) 
                throw "Reading image data failure!"; 
            IMAGE imgDst (width, 1, stride, p); 
            IMAGEC imgSrc (width, 1, IMAGEC::BGR, strideRaw, pData); 
            imgDst.ConvertYFromC(&imgSrc); 
        }
        delete []pData; 
    }
    else
    {
        throw "Unknown image format"; 
    }

    fclose(fp);
}

void IMAGE::SaveBMP(const char *fileName)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "wb")) == NULL) 
        throw "Open file failed"; 
    
    int padwidth = ComputeStride(m_width);  // width padded to 4 bytes 
    BITMAPFILEHEADER fh; 
    fh.bfType = 0x4d42; 
    fh.bfSize = 14+40+256*4+padwidth*m_height; 
    fh.bfReserved1 = 0; 
    fh.bfReserved2 = 0; 
    fh.bfOffBits = 14+40+256*4; 
    fwrite(&fh, sizeof(BITMAPFILEHEADER), 1, fp); 

    BITMAPINFOHEADER ih; 
    ih.biSize = 40; 
    ih.biWidth = m_width; 
    ih.biHeight = m_height; 
    ih.biPlanes = 1; 
    ih.biBitCount = 8; 
    ih.biCompression = 0;   // BI_RGB, no compression 
    ih.biSizeImage = 0; 
    ih.biXPelsPerMeter = 0; 
    ih.biYPelsPerMeter = 0; 
    ih.biClrUsed = 0; 
    ih.biClrImportant = 0; 
    fwrite(&ih, sizeof(BITMAPINFOHEADER), 1, fp); 

    // write color map table 
    BYTE rgbquad[4]; 
    rgbquad[3] = 0; 
    for (int i=0; i<256; i++) 
    {
        rgbquad[0] = rgbquad[1] = rgbquad[2] = (BYTE) i; 
        fwrite((void *)rgbquad, 1, 4, fp); 
    }

    // writing data (bottom up)
    for (int row=m_height-1; row>=0; row--) 
    {
        BYTE *p = &m_bData[row*m_stride]; 
        BYTE pad[4] = {0,0,0,0}; 
        fwrite((void *)p, 1, (size_t)m_width, fp); 
        if (padwidth != m_width)   // need padding
            fwrite((void *)pad, 1, padwidth-m_width, fp); 
    }
    fclose(fp);
}

#ifndef _NO_LIBJPEG

void IMAGE::LoadJPG(const char *fileName, bool bLoadImageData)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "rb")) == NULL) 
        throw "Open file failed"; 

    struct jpeg_decompress_struct cinfo; 
    struct jpeg_error_mgr jerr; 
    cinfo.err = jpeg_std_error(&jerr); 
    jpeg_create_decompress(&cinfo); 
    jpeg_stdio_src (&cinfo, fp); 
    jpeg_read_header(&cinfo, TRUE); 

    if (!Realloc (cinfo.image_width, cinfo.image_height)) 
    {
        throw "Out of memory!"; 
        jpeg_destroy_decompress(&cinfo); 
        fclose (fp); 
        return; 
    }

    if (!bLoadImageData) 
    {
        jpeg_destroy_decompress(&cinfo); 
        fclose(fp); 
        return; 
    }

    // force the output format to be grayscale
    cinfo.out_color_space = JCS_GRAYSCALE; 
    // force output scale factor 
    cinfo.scale_denom = 1; 
    
    jpeg_start_decompress(&cinfo); 
    ASSERT (m_width == (int)cinfo.output_width && m_height == (int)cinfo.output_height); 
    BYTE *buffer = m_bData; 
    while (cinfo.output_scanline < cinfo.output_height) 
    {
        jpeg_read_scanlines(&cinfo, &buffer, 1); 
        buffer += m_stride; 
    }
    jpeg_finish_decompress(&cinfo);
    jpeg_destroy_decompress(&cinfo); 
    fclose (fp); 
}

void IMAGE::SaveJPG(const char *fileName, const int quality)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "wb")) == NULL) 
        throw "Open file failed"; 

    struct jpeg_compress_struct cinfo;
    struct jpeg_error_mgr jerr;
    cinfo.err = jpeg_std_error(&jerr);
    jpeg_create_compress(&cinfo);
    jpeg_stdio_dest(&cinfo, fp);
    
    cinfo.image_width = m_width; 
    cinfo.image_height = m_height;
    cinfo.input_components = 1;
    cinfo.in_color_space = JCS_GRAYSCALE; 
    jpeg_set_defaults(&cinfo);

    // set the image quality 
    jpeg_set_quality(&cinfo, quality, TRUE);    // limited to baseline JPEG values 
    jpeg_start_compress(&cinfo, TRUE);
    
    unsigned char *buffer = m_bData; 
    while (cinfo.next_scanline < cinfo.image_height) 
    {
        jpeg_write_scanlines(&cinfo, &buffer, 1); 
        buffer += m_stride; 
    }
    jpeg_finish_compress(&cinfo);
    jpeg_destroy_compress(&cinfo);
    fclose(fp);
}

#endif

void IMAGE::LoadPGM(const char *fileName, bool bLoadImageData)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "rb")) == NULL) 
        throw "Open file failed"; 

    char ch;
    if (fscanf(fp, "P%c\n", &ch) != 1 || ch != '5') 
        throw "The image file is not in PGM raw format!"; 

    // skip all the comments 
    ch = (char) getc(fp);
    while (ch == '#')
    {
        do {
            ch = (char) getc(fp);
        } while (ch != '\n');	// read to the end of the line, safer than fgets 
        ch = (char) getc(fp);
    }

    if (!isdigit(ch))
        throw "Unable to read PGM header information (width and height)!"; 

    ungetc(ch, fp);		// put that digit back 

    // read the width, height, and maximum value for a pixel 
    int width, height, maxval; 
    fscanf(fp, "%d%d%d\n", &width, &height, &maxval); 

    if (maxval != 255)
        throw "Unable to deal with PGM images that are not 8-bit grayscale!"; 

    if (!Realloc(width, height)) 
        throw "Out of memory!"; 

    if (!bLoadImageData) 
    {
        fclose(fp); 
        return; 
    }

    int size = width*height; 
    fseek (fp, -size, SEEK_END);    // this is because while reading the header, fscanf may read extra bytes 
    // now read the data row by row, note PGM file doesn't have row padding 
    for (int row=0; row<height; row++) 
    {
        unsigned char *p = &m_bData[row*m_stride]; 
        int readsize = (int)fread((void *)p, 1, (size_t)width, fp); 
        if (readsize != width) 
            throw "Reading image data failure!"; 
    }
    fclose(fp);
}

void IMAGE::SavePGM(const char *fileName)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "wb")) == NULL) 
        throw "Open file failed"; 

    fprintf(fp, "P5\n%d %d\n%d\n", m_width, m_height, 255); 

    // now write the data row by row, note PGM file doesn't have row padding 
    for (int row=0; row<m_height; row++) 
    {
        unsigned char *p = &m_bData[row*m_stride]; 
        int writesize = (int)fwrite((void *)p, 1, (size_t)m_width, fp); 
        if (writesize != m_width)
            throw "Writing image data failure!"; 
    }
    fclose(fp);
}

/******************************************************************************\
*
*
*
\******************************************************************************/

IMAGE::IMAGE() : 
m_width(0), m_height(0), m_memSize(0), m_bData(NULL)
{
}

/******************************************************************************\
*
*
*
\******************************************************************************/
// Construct an image using an existing memory buffer. Assumed that each colour plane is 
// encoded as a byte. Normally bytePerPixel is 3 or 4
IMAGE::IMAGE(int width, int height, int stride, int bytePerPixel, BYTE *pData) : 
m_memSize(0), m_bData(NULL)
{
    if (pData == NULL || bytePerPixel < 1) 
    {
        Realloc(width, height); 
    }
    else 
    {
        int iPixInc = stride / width;

        if (iPixInc <= 1 || iPixInc < bytePerPixel)
        {
            m_width = width; 
            m_height = height; 
            m_stride = stride;      // if pData is not NULL, stride must be set explicitly 
            m_bData = pData; 
            m_memSize = 0; 
        }
        else
        {
            Realloc(width, height);

            for (int iHeight = 0 ; iHeight < m_height ; ++iHeight)
            {
                BYTE * pLine = pData + iHeight * stride;
                BYTE *pOutData = m_bData + iHeight * m_stride;
                int iOutPix = 0;

                for (int iWidth = 0 ; iWidth < m_width ; ++iWidth)
                {
                    int iSum = 0;
                    for (int iPix = 0 ; iPix < bytePerPixel ; ++iPix)
                    {
                        iSum += pLine[iPix];
                    }
                    pOutData[iOutPix++] = iSum / bytePerPixel;
                    pLine += iPixInc;
                }
            }
        }
    }
}

IMAGE::IMAGE(int width, int height, int stride, BYTE *pData) : 
m_memSize(0), m_bData(NULL)
{
    if (pData == NULL) 
        Realloc(width, height); 
    else 
    {
        m_width = width; 
        m_height = height; 
        m_stride = stride;      // if pData is not NULL, stride must be set explicitly 
        m_bData = pData; 
        m_memSize = 0; 
    }
}

/******************************************************************************\
*
*   public constructor IMAGE::IMAGE
*
\******************************************************************************/

IMAGE::IMAGE(const char *fileName, bool bLoadImageData) : 
    m_height(0), m_width(0), m_memSize(0), m_bData(NULL)
{
    Load(fileName, bLoadImageData);
}

/******************************************************************************\
*
*   public destructor IMAGE::~IMAGE
*
\******************************************************************************/

IMAGE::~IMAGE()
{
    Release(); 
}

bool IMAGE::Realloc(int width, int height)
{
    ASSERT(width > 0);
    ASSERT(height > 0); 

    m_width = width; 
    m_height = height; 
    m_stride = ComputeStride (m_width); 
    int memSize = m_stride * m_height; 
    if (m_memSize < memSize) 
    {   // need to re-alloc the memory 
        if (m_bData && m_memSize>0) _aligned_free(m_bData); 
//        m_bData = new BYTE[memSize];
        m_bData = (BYTE *)_aligned_malloc(memSize, 32);     // 32 byte aligned for IPP 
        if (!m_bData) 
        {
            throw "memory allocation failure"; 
            return false; 
        }
        m_memSize = memSize; 
    }
    return true; 
}

void IMAGE::Release()
{
    if (m_bData && m_memSize>0) { _aligned_free(m_bData); m_bData=NULL; }
    m_height = 0; 
    m_width = 0; 
    m_stride = 0; 
    m_memSize = 0;
}

void IMAGE::Load(const char *fileName, bool bLoadImageData) 
{
    const char *ext = strrchr(fileName, '.') + 1; 
    if (_stricmp(ext, "jpg") == 0) 
    {
#ifndef _NO_LIBJPEG
        LoadJPG(fileName, bLoadImageData); 
#else
        throw "Can't decode JPG file format"; 
#endif
    }
    else if (_stricmp(ext, "bmp") == 0) 
        LoadBMP(fileName, bLoadImageData); 
    else if (_stricmp(ext, "pgm") == 0) 
        LoadPGM(fileName, bLoadImageData); 
    else
        throw "Unknown file format"; 
}

void IMAGE::Save(const char *fileName, const int quality)
{
    const char *ext = strrchr(fileName, '.') + 1; 
    if (_stricmp(ext, "jpg") == 0) 
    {
#ifndef _NO_LIBJPEG
        SaveJPG(fileName, quality); 
#else
        throw "Can't write JPG file format"; 
#endif
    }
    else if (_stricmp(ext, "bmp") == 0) 
        SaveBMP(fileName);  // for bmp, quality is ignored 
    else if (_stricmp(ext, "pgm") == 0) 
        SavePGM(fileName);  // for pgm, quality is ignored 
    else
        throw "Unknown file format"; 
}

void IMAGE::CopyToImage(IMAGE *pImg, bool bVFlip)
{
    if (m_width != pImg->GetWidth() || m_height != pImg->GetHeight())
        pImg->Realloc(m_width, m_height); 
    BYTE *pDst = pImg->GetDataPtr(); 
    int stride = pImg->GetStride(); 
    
    if (!bVFlip)
    {
        for (int i=0; i<m_height; i++) 
        {
            memcpy(pDst, m_bData+i*m_stride, m_width); 
            pDst += stride; 
        }
    }
    else
    {
        for (int i=m_height-1; i>=0; i--) 
        {
            memcpy(pDst, m_bData+i*m_stride, m_width); 
            pDst += stride; 
        }
    }
}

void IMAGE::HFlipToImage(IMAGE *pFImg, bool bVFlip)
{
    if (m_width != pFImg->GetWidth() || m_height != pFImg->GetHeight())
        pFImg->Realloc(m_width, m_height); 

    int nStartRow = 0; 
    int nEndRow = m_height; 
    int nStep = 1; 
    if (bVFlip) 
    {
        nStartRow = m_height-1; 
        nEndRow = -1; 
        nStep = -1; 
    }

    int k = 0; 
    for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
    {
        BYTE *pSrc = m_bData + GetIndex(m_width-1, i); 
        BYTE *pDst = pFImg->GetDataPtr() + k*pFImg->GetStride();  
        for (int j=0; j<m_width; j++)
        {
            *(pDst++) = *(pSrc--); 
        }
    }
}

void IMAGE::ScaleToImage(IMAGE *pSImg, int scaleFactor, bool bVFlip)
{
    int nSWidth = m_width/scaleFactor; 
    int nSHeight = m_height/scaleFactor;
    if (nSWidth != pSImg->GetWidth() || nSHeight != pSImg->GetHeight()) 
        pSImg->Realloc(nSWidth, nSHeight); 

    int nStartRow = 0; 
    int nEndRow = m_height; 
    int nStep = 1; 
    if (bVFlip) 
    {
        nStartRow = m_height-1; 
        nEndRow = -1; 
        nStep = -1; 
    }

    BYTE **pSrc = new BYTE *[scaleFactor]; 
    int rnd = scaleFactor*scaleFactor/2;    // rounding 
    int scale = scaleFactor*scaleFactor;    // scale 

    int k = 0; 
    for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
    {
        BYTE *pDst = pSImg->GetDataPtr() + k*pSImg->GetStride(); 
        for (int m=0; m<scaleFactor; m++) 
            pSrc[m] = m_bData + GetIndex(0, i*scaleFactor+m); 
        for (int j=0; j<nSWidth; j++) 
        {
            int sum = 0; 
            for (int m=0; m<scaleFactor; m++) 
                for (int n=0; n<scaleFactor; n++) 
                    sum += *(pSrc[m]++); 
            *(pDst++) = (sum+rnd)/scale; 
        }
    }

    delete []pSrc; 
}

void IMAGE::CropToImage(IMAGE *pCImg, const IRECT *pRect, bool bVFlip)
{
    IRECT rc = *pRect; 
    rc.Clamp(0, 0, m_width, m_height); 
    int nCWidth = rc.Width(); 
    int nCHeight = rc.Height(); 
    if (nCWidth != pCImg->GetWidth() || nCHeight != pCImg->GetHeight()) 
        pCImg->Realloc(nCWidth, nCHeight); 

    BYTE *pDst = pCImg->GetDataPtr(); 
    int stride = pCImg->GetStride(); 
    if (!bVFlip)
    {
        for (int i=0; i<nCHeight; i++) 
        {
            BYTE *pSrc = GetDataPtr() + GetIndex(rc.m_ixMin, rc.m_iyMin+i); 
            memcpy(pDst, pSrc, nCWidth); 
            pDst += stride; 
        }
    }
    else
    {
        for (int i=nCHeight-1; i>=0; i--) 
        {
            BYTE *pSrc = GetDataPtr() + GetIndex(rc.m_ixMin, rc.m_iyMin+i); 
            memcpy(pDst, pSrc, nCWidth); 
            pDst += stride; 
        }
    }


}

void IMAGE::ConvertYFromC(IMAGEC *pImgC, bool bVFlip)
{
	using namespace color_conv; 
    if (!init)  // the tables haven't been initialized 
        init_color_conv();

    int width = pImgC->GetWidth(); 
    int height = pImgC->GetHeight(); 
    if (m_width != width || m_height != height)
        Realloc(width, height); 

    int nStartRow = 0; 
    int nEndRow = m_height; 
    int nStep = 1; 
    if (bVFlip) 
    {
        nStartRow = m_height-1; 
        nEndRow = -1; 
        nStep = -1; 
    }

    int k=0; 
    BYTE *pSrcData = pImgC->GetDataPtr(); 
    IMAGEC::COLORSPACE color = pImgC->GetColorSpace(); 
    switch (color) 
    {
    case IMAGEC::BGR: 
        for (int i=nStartRow; i!=nEndRow; i+=nStep,k++) 
        {
            BYTE *pSrc = pSrcData + i*pImgC->GetStride(); 
            BYTE *pDst = m_bData + k*m_stride; 
            for (int j=0; j<m_width; j++) 
            {
                *(pDst++) = (fast_rgb2yuv_b_y[pSrc[0]]+fast_rgb2yuv_g_y[pSrc[1]]+fast_rgb2yuv_r_y[pSrc[2]])>>8; 
                pSrc += 3; 
            }
        }
        break; 
    case IMAGEC::RGB: 
        for (int i=nStartRow; i!=nEndRow; i+=nStep,k++) 
        {
            BYTE *pSrc = pSrcData + i*pImgC->GetStride(); 
            BYTE *pDst = m_bData + i*m_stride; 
            for (int j=0; j<m_width; j++) 
            {
                *(pDst++) = (fast_rgb2yuv_r_y[pSrc[0]]+fast_rgb2yuv_g_y[pSrc[1]]+fast_rgb2yuv_b_y[pSrc[2]])>>8; 
                pSrc += 3; 
            }
        }
        break; 
    case IMAGEC::YUV: 
        for (int i=nStartRow; i!=nEndRow; i+=nStep,k++) 
        {
            BYTE *pSrc = pSrcData + i*pImgC->GetStride(); 
            BYTE *pDst = m_bData + i*m_stride; 
            for (int j=0; j<m_width; j++) 
            {
                *(pDst++) = pSrc[0]; 
                pSrc += 3; 
            }
        }
        break; 
    default: 
        throw "Unknown source color format"; 
        break; 
    }
}

/*******************************************************************************
*
* Functions for I_IMAGE
*
********************************************************************************/


I_IMAGE::I_IMAGE() :
m_width(0), m_height(0), m_memSize(0), m_iData(NULL)
{
}

/******************************************************************************\
*
*
*
\******************************************************************************/

I_IMAGE::I_IMAGE(int width, int height) :
m_memSize(0), m_iData(NULL)
{
    Realloc(width, height); 
}

/******************************************************************************\
*
*
*
\******************************************************************************/

I_IMAGE::~I_IMAGE()
{
    Release(); 
}

bool I_IMAGE::Realloc(int width, int height)
{
    ASSERT(width > 0);
    ASSERT(height > 0); 

    m_width = width+1; 
    m_height = height+1; 
    int memSize = m_width * m_height; 
    if (m_memSize < memSize) 
    {   // need to re-alloc the memory 
        if (m_iData) delete []m_iData; 
        m_iData = new unsigned int [memSize];
        if (!m_iData) 
        {
            throw "memory allocation failure"; 
            return false; 
        }
        m_memSize = memSize; 
    }
    return true; 
}

void I_IMAGE::Release()
{
    if (m_iData) { delete []m_iData; m_iData=NULL; }
    m_height = 0; 
    m_width = 0; 
    m_memSize = 0;
}

/******************************************************************************\
*
*   public method I_IMAGE::Init(IMAGE*)
*
*   Initialize an integral image from a monochrome image. The value of
*   at the location x,y is equal to
*
*   iimage(x,y) = sum(0 <= x' < x) sum(0 <= y' < y) image(x',y')
*
*   Notice that the right hand side of the ranges are exclusive.
*
\******************************************************************************/

void I_IMAGE::Init(const IMAGE* pImage)
{
    const IMAGE& image = *pImage;
    int width0 = image.GetWidth(); 
    int height0 = image.GetHeight();
    if (m_width != width0+1 || m_height != height0+1)
        Realloc(width0, height0); 

    BYTE *pImgData = image.GetDataPtr(); 
    unsigned int *pIImgData = m_iData; 

    // set first row to be zero 
    for (int iX = 0; iX < m_width; iX++) 
        *(pIImgData++) = 0; 

    for (int iY = 0; iY < height0; iY++)
    {
        *(pIImgData++) = 0;         // skip first column 
        unsigned int rowSum = 0;
        for (int iX = 0; iX < width0; iX++, pIImgData++)
        {
            rowSum += pImgData[iX];
            *pIImgData = rowSum + *(pIImgData-m_width);
        }
        pImgData += image.GetStride(); 
    }
}

/******************************************************************************\
*
*   public method I_IMAGE::InitWithSubSample(I_IMAGE*, int x, int y, float scale)
*
*   Intialize the integral image from another integral image, but subsampling
*   is required during this process. 
*   This function is written for the training code 
*
\******************************************************************************/
void I_IMAGE::InitWithSubSample(const IN_IMAGE* pIImage, int x, int y, float scale)
{
    const I_IMAGE& iimage = *pIImage;
    unsigned int *pIImgData = m_iData; 
    for (int iY=0; iY<m_height; iY++)
    {
        int iYMap = int(iY * scale + 0.5); 
        for (int iX=0; iX<m_width; iX++) 
        {
            int iXMap = int(iX * scale + 0.5); 
            *(pIImgData++) = iimage.GetValue(x+iXMap, y+iYMap); 
        }
    }
}

///******************************************************************************\
//*
//*   public method I_IMAGE::Init(IMAGE*, IMAGE*)
//*
//*   Intialize the integral image from the difference of two monochrome
//*   images. We create a virtual monochrome image that who's pixel values
//*   are equal to the absolute value of the difference of the two monochrome
//*   pixels. We then from an integral image from this virtual image.
//*
//\******************************************************************************/
//
//void I_IMAGE::Init(const IMAGE* pImageA, const IMAGE* pImageB)
//{
//    const IMAGE& imageA = *pImageA;
//    const IMAGE& imageB = *pImageB;
//
//    ASSERT(imageA.GetWidth() == imageB.GetWidth());
//    ASSERT(imageA.GetHeight() == imageB.GetHeight());
//    int width0 = imageA.GetWidth(); 
//    int height0 = imageA.GetHeight();
//    if (m_width != width0+1 || m_height != height0+1)
//        Realloc(width0, height0); 
//
//    BYTE *pImgDataA = imageA.GetDataPtr(); 
//    BYTE *pImgDataB = imageB.GetDataPtr(); 
//    unsigned int *pIImgData = m_iData; 
//
//    // set first row to be zero 
//    for (int iX = 0; iX < m_width; iX++) 
//        *(pIImgData++) = 0; 
//
//    for (int iY = 0; iY < height0; iY++)
//    {
//        *(pIImgData++) = 0;         // skip first column 
//        unsigned int rowSum = 0;
//        for (int iX = 0; iX < width0; iX++, pIImgData++)
//        {
//            rowSum += abs((int)(pImgDataA[iX]) - (int)(pImgDataB[iX]));
//            *pIImgData = rowSum + *(pIImgData-m_width);
//        }
//        pImgDataA += imageA.GetStride(); 
//        pImgDataB += imageB.GetStride(); 
//    }
//}

unsigned int I_IMAGE::GetValue(int x, int y) const
{
    const int index = GetIndex(x,y);
    return m_iData[index];
}

/*******************************************************************************
*
* Functions for IN_IMAGE
*
********************************************************************************/

IN_IMAGE::IN_IMAGE() :
m_llData(NULL)
{
}

/******************************************************************************\
*
*
*
\******************************************************************************/

IN_IMAGE::IN_IMAGE(int width, int height) :
m_llData(NULL)
{
    Realloc(width, height); 
}

/******************************************************************************\
*
*
*
\******************************************************************************/

IN_IMAGE::~IN_IMAGE()
{
    Release(); 
}

bool IN_IMAGE::Realloc(int width, int height)
{
    ASSERT(width > 0);
    ASSERT(height > 0); 

    m_width = width+1; 
    m_height = height+1; 
    int memSize = m_width * m_height; 
    if (m_memSize < memSize) 
    {   // need to re-alloc the memory 
        if (m_iData) delete []m_iData; 
        if (m_llData) delete []m_llData; 
        m_iData = new unsigned int [memSize];
        m_llData = new I2TYPE [memSize];
        if (!m_iData || !m_llData) 
        {
            throw "memory allocation failure"; 
            return false; 
        }
        m_memSize = memSize; 
    }
    return true; 
}

void IN_IMAGE::Release()
{
    if (m_iData) { delete []m_iData; m_iData=NULL; }
    if (m_llData) { delete []m_llData; m_llData=NULL; }
    m_height = 0; 
    m_width = 0; 
    m_memSize = 0;
}

void IN_IMAGE::Init(const IMAGE* pImage)
{
    const IMAGE& image = *pImage;
    int width0 = image.GetWidth(); 
    int height0 = image.GetHeight();
    if (m_width != width0+1 || m_height != height0+1)
        Realloc(width0, height0); 

    BYTE *pImgData = image.GetDataPtr(); 
    unsigned int *pIImgData = m_iData; 
    I2TYPE *pI2ImgData = m_llData; 

    // set first row to be zero 
    for (int iX = 0; iX < m_width; iX++) 
    {
        *(pIImgData++) = 0; 
        *(pI2ImgData++) = 0; 
    }

    for (int iY = 0; iY < height0; iY++)
    {
        *(pIImgData++) = 0;         // skip first column 
        *(pI2ImgData++) = 0; 
        unsigned int rowSum = 0;
        I2TYPE rowSum2 = 0;
        for (int iX = 0; iX < width0; iX++, pIImgData++, pI2ImgData++)
        {
            rowSum += pImgData[iX];
            rowSum2 += pImgData[iX]*pImgData[iX];
            *pIImgData = rowSum + *(pIImgData-m_width);
            *pI2ImgData = rowSum2 + *(pI2ImgData-m_width);
        }
        pImgData += image.GetStride(); 
    }
}

I2TYPE IN_IMAGE::GetValue2(int x, int y) const
{
    const int index = GetIndex(x,y);
    return m_llData[index];
}

float IN_IMAGE::ComputeNorm(IRECT *rc)
{
    const int idx00 = rc->m_iyMin*m_width+rc->m_ixMin; 
    const int idx01 = rc->m_iyMax*m_width+rc->m_ixMin; 
    const int idx10 = rc->m_iyMin*m_width+rc->m_ixMax; 
    const int idx11 = rc->m_iyMax*m_width+rc->m_ixMax; 

    const unsigned int v00 = m_iData[idx00]; 
    const unsigned int v01 = m_iData[idx01];
    const unsigned int v10 = m_iData[idx10];
    const unsigned int v11 = m_iData[idx11];
    const double sum = (double)((v11 - v01) - (v10 - v00));

    const I2TYPE s00 = m_llData[idx00]; 
    const I2TYPE s01 = m_llData[idx01];
    const I2TYPE s10 = m_llData[idx10];
    const I2TYPE s11 = m_llData[idx11];
    const double sum2 = (double)((s11 - s01) - (s10 - s00));

    double area = rc->Area(); 
    double var = sqrt(sum2*area-sum*sum); 
    // what we return is the inverse of the variance so later we only do multiplication instead of division
    // if variance is less than 1.0, we return 1.0 here.
    if (var <= area) 
        return 1.0f; 
    else
        return (float)(area/var); 
}

/******************************************************************************\
*
*   IMAGEC class 
*
\******************************************************************************/

IMAGEC::IMAGEC() : 
m_width(0), m_height(0), m_stride(0), m_memSize(0), m_colorSpace(UNSPECIFIED), m_bData(NULL)
{
}

IMAGEC::IMAGEC(int width, int height, COLORSPACE colorspace, int stride, BYTE *pData) :
m_memSize(0), m_colorSpace(UNSPECIFIED), m_bData(NULL)
{
    m_colorSpace = colorspace; 
    if (pData == NULL) 
        Realloc(width, height); 
    else 
    {
        m_width = width; 
        m_height = height; 
        m_stride = stride;      // if pData is not NULL, stride must be set explicitly 
        m_bData = pData; 
        m_memSize = 0; 
    }
}


IMAGEC::IMAGEC(const char *fileName, bool bLoadImageData) : 
m_width(0), m_height(0), m_stride(0), m_memSize(0), m_colorSpace(UNSPECIFIED), m_bData(NULL)
{
    Load(fileName, bLoadImageData);
}

IMAGEC::~IMAGEC()
{
    Release(); 
}

bool IMAGEC::Realloc(int width, int height)
{
    ASSERT(width > 0);
    ASSERT(height > 0); 

    m_width = width; 
    m_height = height; 
    m_stride = ComputeStride(m_width); 
    int memSize = m_stride * m_height; 
    if (m_memSize < memSize) 
    {   // need to re-alloc the memory 
        if (m_bData && m_memSize>0) _aligned_free(m_bData); 
//        m_bData = new BYTE[memSize];
        m_bData = (BYTE *)_aligned_malloc(memSize, 32);
        if (!m_bData) 
        {
            throw "memory allocation failure"; 
            return false; 
        }
        m_memSize = memSize; 
    }
    return true; 
}

void IMAGEC::Release()
{
    if (m_bData && m_memSize>0) { _aligned_free(m_bData); m_bData = NULL; }
    m_width = 0; 
    m_stride = 0; 
    m_height = 0; 
    m_memSize = 0; 
}

void IMAGEC::LoadBMP(const char *fileName, bool bLoadImageData)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "rb")) == NULL) 
        throw "Open file failed"; 

    BITMAPFILEHEADER fh; 
    fread(&fh, sizeof(BITMAPFILEHEADER), 1, fp); 
    if (fh.bfType != 0x4d42 || fh.bfReserved1 != 0 || fh.bfReserved2 != 0) 
        throw "The file is not a valid BMP file!\n"; 
    
    BITMAPINFOHEADER ih; 
    fread(&ih, sizeof(BITMAPINFOHEADER), 1, fp); 
    if (ih.biCompression != 0) 
        throw "Unsupported compressed BMP format!";

    int width = ih.biWidth;
    int height = ih.biHeight; 
    int stride = ComputeStride(width); 
    if (!Realloc(width, height)) 
        throw "Out of memory!"; 
    m_colorSpace = BGR; 

    if (!bLoadImageData) 
    {
        fclose(fp); 
        return; 
    }

    if (ih.biBitCount == 8)     // need to do color conversion 
    {
        // skip the color map 
        fseek(fp, 256*4, SEEK_CUR);    // 8 bit color map, 256 of 4-byte RGBQUAD, ignored!

        int strideRaw = ((width+3)/4)*4; 
        BYTE *pData = new BYTE [strideRaw]; 
        if (pData == NULL) 
            throw "Out of memory"; 

        // now read the data row by row, notice BMP stores pixels bottom up 
        for (int row=height-1; row>=0; row--) 
        {
            BYTE *p = &m_bData[row*stride]; 
            int readsize = (int)fread((void *)pData, 1, (size_t)strideRaw, fp); 
            if (readsize != stride) 
                throw "Reading image data failure!"; 
            IMAGEC imgDst (width, 1, BGR, stride, p); 
            IMAGE imgSrc (width, 1, strideRaw, pData); 
            imgDst.ConvertCFromY (&imgSrc, BGR); 
        }
        delete []pData; 
    }
    else if (ih.biBitCount == 24)
    {
        for (int row=height-1; row>=0; row--) 
        {
            unsigned char *p = &m_bData[row*stride]; 
            int readsize = (int)fread((void *)p, 1, (size_t)stride, fp); 
            if (readsize != stride) 
                throw "Reading image data failure!"; 
        }
    }
    else 
    {
        throw "Unknown image format"; 
    }
    fclose(fp);
}

void IMAGEC::SaveBMP(const char *fileName)
{
    IMAGEC imgSave; 
    BYTE *pData = m_bData; 
    int stride = m_stride; 
    switch (m_colorSpace) 
    {
    case UNSPECIFIED: 
        throw "Unknown color space!"; 
        break; 
    case BGR: 
        break; 
    case RGB: 
    case YUV: 
        ConvertCToC(&imgSave, BGR); 
        pData = imgSave.GetDataPtr(); 
        stride = imgSave.GetStride(); 
        break; 
    }

    FILE *fp; 
    if ((fp = fopen (fileName, "wb")) == NULL) 
        throw "Open file failed"; 
    
    int padwidth = ComputeStride(m_width);  // width padded to 4 bytes, as BMP requires
    BITMAPFILEHEADER fh; 
    fh.bfType = 0x4d42; 
    fh.bfSize = 14+40+padwidth*m_height; 
    fh.bfReserved1 = 0; 
    fh.bfReserved2 = 0; 
    fh.bfOffBits = 14+40; 
    fwrite(&fh, sizeof(BITMAPFILEHEADER), 1, fp); 

    BITMAPINFOHEADER ih; 
    ih.biSize = 40; 
    ih.biWidth = m_width; 
    ih.biHeight = m_height; 
    ih.biPlanes = 1; 
    ih.biBitCount = 24; 
    ih.biCompression = 0;   // BI_RGB, no compression 
    ih.biSizeImage = 0; 
    ih.biXPelsPerMeter = 0; 
    ih.biYPelsPerMeter = 0; 
    ih.biClrUsed = 0; 
    ih.biClrImportant = 0; 
    fwrite(&ih, sizeof(BITMAPINFOHEADER), 1, fp); 

    // writing data (bottom up)
    for (int row=m_height-1; row>=0; row--) 
    {
        BYTE *p = &pData[row*stride]; 
        BYTE pad[4] = {0,0,0,0}; 
        fwrite((void *)p, 1, (size_t)m_width*3, fp); 
        if (padwidth != m_width*3)   // need padding
            fwrite((void *)pad, 1, padwidth-m_width*3, fp); 
    }
    fclose(fp);
}

#ifndef _NO_LIBJPEG

void IMAGEC::LoadJPG(const char *fileName, bool bLoadImageData)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "rb")) == NULL) 
        throw "Open file failed"; 

    struct jpeg_decompress_struct cinfo; 
    struct jpeg_error_mgr jerr; 
    cinfo.err = jpeg_std_error(&jerr); 
    jpeg_create_decompress(&cinfo); 
    jpeg_stdio_src (&cinfo, fp); 
    jpeg_read_header(&cinfo, TRUE); 

    if (!Realloc (cinfo.image_width, cinfo.image_height)) 
    {
        throw "Out of memory!"; 
        jpeg_destroy_decompress(&cinfo); 
        fclose (fp); 
        return; 
    }
    m_colorSpace = RGB; 

    if (!bLoadImageData) 
    {
        jpeg_destroy_decompress(&cinfo); 
        fclose(fp); 
        return; 
    }

    // force the output format to be grayscale
    cinfo.out_color_space = JCS_RGB; 
    // force output scale factor 
    cinfo.scale_denom = 1; 
    
    jpeg_start_decompress(&cinfo); 
    ASSERT (m_width == (int)cinfo.output_width && m_height == (int)cinfo.output_height); 
    BYTE *buffer = m_bData; 
    while (cinfo.output_scanline < cinfo.output_height) 
    {
        jpeg_read_scanlines(&cinfo, &buffer, 1); 
        buffer += m_stride; 
    }
    jpeg_finish_decompress(&cinfo);
    jpeg_destroy_decompress(&cinfo); 
    fclose (fp); 
}

void IMAGEC::SaveJPG(const char *fileName, const int quality)
{
    IMAGEC imgSave; 
    BYTE *pData = m_bData; 
    int stride = m_stride; 
    switch (m_colorSpace) 
    {
    case UNSPECIFIED: 
        throw "Unknown color space!"; 
        break; 
    case RGB: 
        break; 
    case BGR: 
    case YUV: 
        ConvertCToC(&imgSave, RGB); 
        pData = imgSave.GetDataPtr(); 
        stride = imgSave.GetStride(); 
        break; 
    }

    FILE *fp; 
    if ((fp = fopen (fileName, "wb")) == NULL) 
        throw "Open file failed"; 

    struct jpeg_compress_struct cinfo;
    struct jpeg_error_mgr jerr;
    cinfo.err = jpeg_std_error(&jerr);
    jpeg_create_compress(&cinfo);
    jpeg_stdio_dest(&cinfo, fp);
    
    cinfo.image_width = m_width; 
    cinfo.image_height = m_height;
    cinfo.input_components = 3;
    cinfo.in_color_space = JCS_RGB; 
    jpeg_set_defaults(&cinfo);

    // set the image quality 
    jpeg_set_quality(&cinfo, quality, TRUE);    // limited to baseline JPEG values 
    jpeg_start_compress(&cinfo, TRUE);
    
    unsigned char *buffer = pData; 
    while (cinfo.next_scanline < cinfo.image_height) 
    {
        jpeg_write_scanlines(&cinfo, &buffer, 1); 
        buffer += stride; 
    }
    jpeg_finish_compress(&cinfo);
    jpeg_destroy_compress(&cinfo);
    fclose(fp);
}

#endif

// load PPM color image 
void IMAGEC::LoadPPM(const char *fileName, bool bLoadImageData)
{
    FILE *fp; 
    if ((fp = fopen (fileName, "rb")) == NULL) 
        throw "Open file failed"; 

    char ch;
    if (fscanf(fp, "P%c\n", &ch) != 1 || ch != '6') 
        throw "The image file is not in PPM raw format!"; 

    // skip all the comments 
    ch = (char) getc(fp);
    while (ch == '#')
    {
        do {
            ch = (char) getc(fp);
        } while (ch != '\n');	// read to the end of the line, safer than fgets 
        ch = (char) getc(fp);
    }

    if (!isdigit(ch))
        throw "Unable to read PGM header information (width and height)!"; 

    ungetc(ch, fp);		// put that digit back 

    // read the width, height, and maximum value for a pixel 
    int width, height, maxval; 
    fscanf(fp, "%d%d%d\n", &width, &height, &maxval); 

    if (maxval != 255)
        throw "Unable to deal with PGM images that are not 8-bit grayscale!"; 

    if (!Realloc(width, height)) 
        throw "Out of memory!"; 

    if (!bLoadImageData) 
    {
        fclose(fp); 
        return; 
    }

    int size = width*height*3; 
    fseek (fp, -size, SEEK_END);    // this is because while reading the header, fscanf may read extra bytes 
    // now read the data row by row, note PGM file doesn't have row padding 
    for (int row=0; row<height; row++) 
    {
        unsigned char *p = &m_bData[row*m_stride]; 
        int readsize = (int)fread((void *)p, 1, (size_t)width*3, fp); 
        if (readsize != width*3) 
            throw "Reading image data failure!"; 
    }
    fclose(fp);
}

// write PPM image 
void IMAGEC::SavePPM(const char *fileName)
{
    IMAGEC imgSave; 
    BYTE *pData = m_bData; 
    int stride = m_stride; 
    switch (m_colorSpace) 
    {
    case UNSPECIFIED: 
        throw "Unknown color space!"; 
        break; 
    case RGB: 
        break; 
    case BGR: 
    case YUV: 
        ConvertCToC(&imgSave, RGB); 
        pData = imgSave.GetDataPtr(); 
        stride = imgSave.GetStride(); 
        break; 
    }

    FILE *fp; 
    if ((fp = fopen (fileName, "wb")) == NULL) 
        throw "Open file failed"; 

    fprintf(fp, "P6\n%d %d\n%d\n", m_width, m_height, 255); 

    // now write the data row by row, note PGM file doesn't have row padding 
    for (int row=0; row<m_height; row++) 
    {
        BYTE *p = &pData[row*stride]; 
        int writesize = (int)fwrite((void *)p, 1, (size_t)m_width*3, fp); 
        if (writesize != m_width*3)
            throw "Writing image data failure!"; 
    }
    fclose(fp);
}

void IMAGEC::Load(const char *fileName, bool bLoadImageData) 
{
    const char *ext = strrchr(fileName, '.') + 1; 
    if (_stricmp(ext, "jpg") == 0) 
    {
#ifndef _NO_LIBJPEG
        LoadJPG(fileName, bLoadImageData); 
#else
        throw "Can't decode JPG file format"; 
#endif
    }
    else if (_stricmp(ext, "bmp") == 0) 
        LoadBMP(fileName, bLoadImageData); 
    else if (_stricmp(ext, "ppm") == 0) 
        LoadPPM(fileName, bLoadImageData); 
    else
        throw "Unknown file format"; 
}

void IMAGEC::Save(const char *fileName, const int quality)
{
    const char *ext = strrchr(fileName, '.') + 1; 
    if (_stricmp(ext, "jpg") == 0) 
    {
#ifndef _NO_LIBJPEG
        SaveJPG(fileName, quality); 
#else
        throw "Can't write JPG file format"; 
#endif
    }
    else if (_stricmp(ext, "bmp") == 0) 
        SaveBMP(fileName);  // for bmp, quality is ignored 
    else if (_stricmp(ext, "ppm") == 0) 
        SavePPM(fileName);  // for ppm, quality is ignored 
    else
        throw "Unknown file format"; 
}

void IMAGEC::ConvertCFromY(IMAGE *pImg, COLORSPACE clrDst, bool bVFlip)
{
    int width = pImg->GetWidth(); 
    int height = pImg->GetHeight(); 
    if (m_width != width || m_height != height)
        Realloc(width, height); 

    int nStartRow = 0; 
    int nEndRow = m_height; 
    int nStep = 1; 
    if (bVFlip) 
    {
        nStartRow = m_height-1; 
        nEndRow = -1; 
        nStep = -1; 
    }

    int k = 0; 
    BYTE *pSrcData = pImg->GetDataPtr(); 
    BYTE *pSrc, *pDst; 
    switch (clrDst) 
    {
    case RGB:
    case BGR: 
        for (int i=nStartRow; i!=nEndRow; i+=nStep,k++) 
        {
            pSrc = pSrcData + i*pImg->GetStride(); 
            pDst = m_bData + k*m_stride;  
            for (int j=0; j<m_width; j++) 
            {
                pDst[0] = pDst[1] = pDst[2] = *(pSrc++); 
                pDst += 3; 
            }
        }
        break; 
    case YUV: 
        for (int i=nStartRow; i!=nEndRow; i+=nStep,k++) 
        {
            pSrc = pSrcData + i*pImg->GetStride(); 
            pDst = m_bData + k*m_stride;  
            for (int j=0; j<m_width; j++) 
            {
                pDst[0] = *(pSrc++); 
                pDst[1] = pDst[2] = 128; 
                pDst += 3; 
            }
        }
        break; 
    default: 
        throw "Unknown color space!"; 
        break; 
    }

    m_colorSpace = clrDst; 
}

void IMAGEC::ConvertCToC(IMAGEC *pImg, COLORSPACE clrDst, bool bVFlip)
{
    using namespace color_conv; 
    if (!init)  // the tables haven't been initialized 
        init_color_conv();

    COLORSPACE clrSrc = GetColorSpace(); 
    if (clrSrc == clrDst) 
    {
        CopyToImage(pImg, bVFlip);
        return;
    }

    int width = pImg->GetWidth(); 
    int height = pImg->GetHeight(); 
    if (m_width != width || m_height != height)
        pImg->Realloc(m_width, m_height); 

    int nStartRow = 0; 
    int nEndRow = m_height; 
    int nStep = 1; 
    if (bVFlip) 
    {
        nStartRow = m_height-1; 
        nEndRow = -1; 
        nStep = -1; 
    }

    int k=0; 
    BYTE *pSrc, *pDst; 
    if (clrSrc == BGR && clrDst == YUV) 
    {
        for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
        {
            pSrc = m_bData + i*m_stride; 
            pDst = pImg->GetDataPtr() + k*pImg->GetStride();  
            for (int j=0; j<m_width; j++) 
            {
                pDst[0] = (fast_rgb2yuv_b_y[pSrc[0]]+fast_rgb2yuv_g_y[pSrc[1]]+fast_rgb2yuv_r_y[pSrc[2]])>>8; 
                pDst[1] = (fast_rgb2yuv_b_u[pSrc[0]]+fast_rgb2yuv_g_u[pSrc[1]]+fast_rgb2yuv_r_u[pSrc[2]])>>8; 
                pDst[2] = (fast_rgb2yuv_b_v[pSrc[0]]+fast_rgb2yuv_g_v[pSrc[1]]+fast_rgb2yuv_r_v[pSrc[2]])>>8; 
                pSrc += 3; 
                pDst += 3; 
            }
        }
    }
    else if (clrSrc == RGB && clrDst == YUV) 
    {
        for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
        {
            pSrc = m_bData + i*m_stride; 
            pDst = pImg->GetDataPtr() + k*pImg->GetStride();  
            for (int j=0; j<m_width; j++) 
            {
                pDst[0] = (fast_rgb2yuv_r_y[pSrc[0]]+fast_rgb2yuv_g_y[pSrc[1]]+fast_rgb2yuv_b_y[pSrc[2]])>>8; 
                pDst[1] = (fast_rgb2yuv_r_u[pSrc[0]]+fast_rgb2yuv_g_u[pSrc[1]]+fast_rgb2yuv_b_u[pSrc[2]])>>8; 
                pDst[2] = (fast_rgb2yuv_r_v[pSrc[0]]+fast_rgb2yuv_g_v[pSrc[1]]+fast_rgb2yuv_b_v[pSrc[2]])>>8; 
                pSrc += 3; 
                pDst += 3; 
            }
        }
    }
    else if (clrSrc == YUV && clrDst == BGR) 
    {
        for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
        {
            pSrc = m_bData + i*m_stride; 
            pDst = pImg->GetDataPtr() + k*pImg->GetStride();  
            for (int j=0; j<m_width; j++) 
            {
                pDst[0] = fast_uchar[(fast_yuv2rgb_y_rgb[pSrc[0]]+fast_yuv2rgb_u_b[pSrc[1]])>>8]; 
                pDst[1] = fast_uchar[(fast_yuv2rgb_y_rgb[pSrc[0]]+fast_yuv2rgb_u_g[pSrc[1]]+fast_yuv2rgb_v_g[pSrc[2]])>>8]; 
                pDst[2] = fast_uchar[(fast_yuv2rgb_y_rgb[pSrc[0]]+fast_yuv2rgb_v_r[pSrc[2]])>>8]; 
                pSrc += 3; 
                pDst += 3; 
            }
        }
    }
    else if (clrSrc == YUV && clrDst == RGB) 
    {
        for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
        {
            pSrc = m_bData + i*m_stride; 
            pDst = pImg->GetDataPtr() + k*pImg->GetStride();  
            for (int j=0; j<m_width; j++) 
            {
                pDst[0] = fast_uchar[(fast_yuv2rgb_y_rgb[pSrc[0]]+fast_yuv2rgb_v_r[pSrc[2]])>>8]; 
                pDst[1] = fast_uchar[(fast_yuv2rgb_y_rgb[pSrc[0]]+fast_yuv2rgb_u_g[pSrc[1]]+fast_yuv2rgb_v_g[pSrc[2]])>>8]; 
                pDst[2] = fast_uchar[(fast_yuv2rgb_y_rgb[pSrc[0]]+fast_yuv2rgb_u_b[pSrc[1]])>>8]; 
                pSrc += 3; 
                pDst += 3; 
            }
        }
    }
    else  // clrSrc == RGB && clrDst == BGR || clrSrc == BGR && clrDst == RGB
    {   // just need to swap B and R
        for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
        {
            pSrc = m_bData + i*m_stride; 
            pDst = pImg->GetDataPtr() + k*pImg->GetStride();  
            for (int j=0; j<m_width; j++) 
            {
                pDst[0] = pSrc[2]; 
                pDst[1] = pSrc[1]; 
                pDst[2] = pSrc[0]; 
                pSrc += 3; 
                pDst += 3; 
            }
        }
    }
    pImg->SetColorSpace(clrDst); 
}

void IMAGEC::DrawPixel(int x, int y, float alpha, BYTE color[3])
{
    if (x >= 0 && x < m_width && y >= 0 && y < m_height)
    {
        const float beta  = 1.0f - alpha;
        BYTE oldColor[3], newColor[3]; 
        GetValue(x, y, oldColor); 
        for (int i=0; i<3; i++) 
            newColor[i] = (BYTE)(alpha * color[i] + beta * oldColor[i] + 0.5f); 
        SetValue(x, y, newColor); 
    }
}

void IMAGEC::DrawRect(const IRECT *pRect, BYTE color[3])
{
    const IRECT& irect = *pRect;
    const int xMin = irect.m_ixMin;
    const int xMax = irect.m_ixMax;
    const int yMin = irect.m_iyMin;
    const int yMax = irect.m_iyMax;
    int x, y;

    for (y = yMin; y <= yMax; y++)
    {
        DrawPixel(xMin+0, y, 1.0, color);
        DrawPixel(xMin+1, y, 1.0, color);
        DrawPixel(xMax+0, y, 1.0, color);
        DrawPixel(xMax+1, y, 1.0, color);
    }
    for (x = xMin; x <= xMax; x++)
    {
        DrawPixel(x, yMin+0, 1.0, color);
        DrawPixel(x, yMin+1, 1.0, color);
        DrawPixel(x, yMax+0, 1.0, color);
        DrawPixel(x, yMax+1, 1.0, color);
    }
}

void IMAGEC::CopyToImage(IMAGEC *pImg, bool bVFlip)
{
    if (m_width != pImg->GetWidth() || m_height != pImg->GetHeight())
        pImg->Realloc(m_width, m_height); 
    pImg->SetColorSpace(m_colorSpace); 
    BYTE *pDst = pImg->GetDataPtr(); 
    int stride = pImg->GetStride(); 

    if (!bVFlip)
    {
        for (int i=0; i<m_height; i++) 
        {
            memcpy(pDst, m_bData+i*m_stride, m_width*3); 
            pDst += stride; 
        }
    }
    else
    {
        for (int i=m_height-1; i>=0; i--) 
        {
            memcpy(pDst, m_bData+i*m_stride, m_width*3); 
            pDst += stride; 
        }
    }
}

void IMAGEC::HFlipToImage(IMAGEC *pFImg, bool bVFlip)
{
    if (m_width != pFImg->GetWidth() || m_height != pFImg->GetHeight())
        pFImg->Realloc(m_width, m_height); 
    pFImg->SetColorSpace(m_colorSpace); 

    int nStartRow = 0; 
    int nEndRow = m_height; 
    int nStep = 1; 
    if (bVFlip) 
    {
        nStartRow = m_height-1; 
        nEndRow = -1; 
        nStep = -1; 
    }

    int k = 0; 
    for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
    {
        BYTE *pSrc = m_bData + GetIndex(m_width-1, i); 
        BYTE *pDst = pFImg->GetDataPtr() + k*pFImg->GetStride();  
        for (int j=0; j<m_width; j++)
        {
            pDst[0] = pSrc[0]; 
            pDst[1] = pSrc[1]; 
            pDst[2] = pSrc[2]; 
            pDst += 3; 
            pSrc -= 3; 
        }
    }
}

void IMAGEC::ScaleToImage(IMAGEC *pSImg, int scaleFactor, bool bVFlip)
{
    int nSWidth = m_width/scaleFactor; 
    int nSHeight = m_height/scaleFactor;
    if (nSWidth != pSImg->GetWidth() || nSHeight != pSImg->GetHeight()) 
        pSImg->Realloc(nSWidth, nSHeight); 
    pSImg->SetColorSpace(m_colorSpace); 

    int nStartRow = 0; 
    int nEndRow = nSHeight; 
    int nStep = 1; 
    if (bVFlip) 
    {
        nStartRow = nSHeight-1; 
        nEndRow = -1; 
        nStep = -1; 
    }

    BYTE **pSrc = new BYTE *[scaleFactor]; 
    int rnd = scaleFactor*scaleFactor/2;    // rounding 
    int scale = scaleFactor*scaleFactor;    // scale 

    int k = 0; 
    for (int i=nStartRow; i!=nEndRow; i+=nStep, k++) 
    {
        BYTE *pDst = pSImg->GetDataPtr() + k*pSImg->GetStride(); 
        for (int m=0; m<scaleFactor; m++) 
            pSrc[m] = m_bData + GetIndex(0, i*scaleFactor+m); 
        for (int j=0; j<nSWidth; j++) 
        {
            int sum[3] = {0, 0, 0}; 
            for (int m=0; m<scaleFactor; m++) 
                for (int n=0; n<scaleFactor; n++) 
                {
                    sum[0] += pSrc[m][0]; 
                    sum[1] += pSrc[m][1]; 
                    sum[2] += pSrc[m][2]; 
                    pSrc[m] += 3; 
                }
            pDst[0] = (sum[0]+rnd)/scale; 
            pDst[1] = (sum[1]+rnd)/scale; 
            pDst[2] = (sum[2]+rnd)/scale; 
            pDst += 3; 
        }
    }
    delete []pSrc; 
}

void IMAGEC::CropToImage(IMAGEC *pCImg, const IRECT *pRect, bool bVFlip)
{
    IRECT rc = *pRect; 
    rc.Clamp(0, 0, m_width, m_height); 
    int nCWidth = rc.Width(); 
    int nCHeight = rc.Height(); 
    if (nCWidth != pCImg->GetWidth() || nCHeight != pCImg->GetHeight()) 
        pCImg->Realloc(nCWidth, nCHeight); 

    pCImg->SetColorSpace(m_colorSpace); 
    BYTE *pDst = pCImg->GetDataPtr(); 
    int stride = pCImg->GetStride(); 
    if (!bVFlip)
    {
        for (int i=0; i<nCHeight; i++) 
        {
            BYTE *pSrc = GetDataPtr() + GetIndex(rc.m_ixMin, rc.m_iyMin+i); 
            memcpy(pDst, pSrc, nCWidth*3); 
            pDst += stride; 
        }
    }
    else
    {
        for (int i=nCHeight-1; i>=0; i--) 
        {
            BYTE *pSrc = GetDataPtr() + GetIndex(rc.m_ixMin, rc.m_iyMin+i); 
            memcpy(pDst, pSrc, nCWidth*3); 
            pDst += stride; 
        }
    }
}