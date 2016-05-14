// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include <iostream>
#include <windows.h>
#include <ASSERT.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <math.h>
//#include <vector>

#ifndef ASSERT
#ifdef DEBUG
#define ASSERT( x ) assert( x )
#else
#define ASSERT( x )
#endif
#endif

#define MAX_NUM_SCALE           32

// TODO: reference additional headers your program requires here
