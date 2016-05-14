#pragma once

#include <vector>
#include <assert.h>

using namespace std; 

struct RUNLABEL
{
    int     m_nRun; 
    char    m_nLabel; 
};

class RLCODEC
{
private: 
    vector<RUNLABEL> *m_pRLVec; 
    vector<RUNLABEL>::iterator m_It; 
    RUNLABEL    m_RL; 

public: 

    enum CODECTYPE
    {
        ENCODER, 
        DECODER, 
    } m_nType;  

    RLCODEC (vector<RUNLABEL> *pRLVec, CODECTYPE nType)
    {
        m_pRLVec = pRLVec; 
        m_nType = nType; 
        if (nType == ENCODER) 
        {
            m_pRLVec->clear(); 
            m_RL.m_nLabel = -1; 
            m_RL.m_nRun = 0; 
        }
        else // nType == DECODER
        {
            m_It = m_pRLVec->begin(); 
            m_RL = *m_It; 
        }
    }; 
    ~RLCODEC () {}; 

    void Encode(char label)
    {
        assert(m_nType == ENCODER); 
        if (m_RL.m_nLabel != label) 
        {
            if (m_RL.m_nRun != 0)
                m_pRLVec->push_back(m_RL); 
            m_RL.m_nLabel = label, m_RL.m_nRun = 1; 
        }
        else
            m_RL.m_nRun ++; 
    };
    void EncodeDone()       // IMPORTANT: must call EncodeDone to wrap up the encoding 
    {
        if (m_RL.m_nRun != 0)
            m_pRLVec->push_back(m_RL); 
    };

    bool DecodeNext(char &label)
    {
        assert(m_nType = DECODER); 
        label = m_RL.m_nLabel; 
        m_RL.m_nRun --; 
        if (m_RL.m_nRun < 1) 
        {
            m_It ++; 
            if (m_It != m_pRLVec->end()) 
                m_RL = *m_It; 
            else
                return false;   // the decoder will return false when reaching the end of the code 
        }
        return true; 
    }
}; 