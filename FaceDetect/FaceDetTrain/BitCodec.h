#pragma once

class BITCODEC
{
private: 
    BYTE * m_pArr; 
    BYTE * m_pCurByte; 
    BYTE   m_nByte; 
    BYTE   m_nShift; 

public: 

    enum CODECTYPE
    {
        ENCODER, 
        DECODER, 
    } m_nType;  

    BITCODEC (BYTE *pArr, CODECTYPE nType)
    {   // for simplicity, we assume the memory of pArr is allocated and deallocated outside this codec 
        m_pArr = pArr; 
        m_nType = nType; 
        if (nType == ENCODER) 
        {
            m_pCurByte = m_pArr;
            m_nByte = 0; 
            m_nShift = 0; 
        }
        else // nType == DECODER
        {
            m_pCurByte = m_pArr; 
            m_nByte = *(m_pCurByte++); 
            m_nShift = 0; 
        }
    }; 
    ~BITCODEC () {}; 

    void Encode(char val)
    {   // val is either 1 or 0 
        ASSERT(m_nType == ENCODER); 
        m_nByte += (val & 1)<<m_nShift; 
        m_nShift ++; 
        if (m_nShift == 8)
        {
            *(m_pCurByte++) = m_nByte; 
            m_nByte = 0; 
            m_nShift = 0; 
        }
    };
    void EncodeDone()       // IMPORTANT: must call EncodeDone to wrap up the encoding 
    {
        if (m_nShift != 0)
            *m_pCurByte = m_nByte; 
    };

    bool DecodeNext(char &val)
    {
        ASSERT(m_nType = DECODER); 
        val = (m_nByte & (1<<m_nShift))>>m_nShift; 
        m_nShift ++; 
        if (m_nShift == 8) 
        {
            m_nByte = *(m_pCurByte++); 
            m_nShift = 0; 
        }
        return true;    // sorry, we never check here whether there is buffer overflow, so we always return true
    }; 
}; 