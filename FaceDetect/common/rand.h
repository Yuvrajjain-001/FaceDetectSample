//+-----------------------------------------------------------------------
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//  Description:
//      Class for generating uniform and Gaussian deviates
//
//  History:
//      2003/11/12-swinder
//			Created
//
//------------------------------------------------------------------------

#pragma once


#define RAND_NTAB 32


////////////////////////////////////////////////////////////////////////
// CRand - Good Random Number Generator
////////////////////////////////////////////////////////////////////////

class CRand
{
public:
	CRand();
	CRand(int iSeed);

	void Seed(int iSeed);
	double DRand();							// [0,1)
	double URand(double min, double max)	// [min,max)
		{ return min + (max-min) * DRand(); }
	int IRand(int iMod)						// 0 to iMod - 1
		{ return (int)(iMod * DRand()); }
	double Gauss();

private:
	int m_iLast;
	int m_iState;
	int m_rgiShuffle[RAND_NTAB];
	bool m_bGaussISet;
	double m_dGaussGSet;
};


////////////////////////////////////////////////////////////////////////
// CRC4 - RC4 encryption/decryption class
////////////////////////////////////////////////////////////////////////

class CRC4
{
public:
	~CRC4();
	HRESULT Init(BYTE *pchKey, int iKeyLen);
	HRESULT Process(BYTE *pchMsg, int iMsgLen);
	void Skip(int iLen);

private:
	int m_i, m_j;
	int m_rgiS[256];
};

