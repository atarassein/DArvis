#pragma once

#include <Windows.h>
#include <iostream>
#include <fstream>
#include <string>
#include <sstream>
#include <iomanip>
#include <stdio.h>
#include <detours.h>
#pragma comment(lib, "detours.lib") 
#include "DA.h"

#include <objidl.h>
#include <gdiplus.h>
using namespace Gdiplus;
#pragma comment (lib,"Gdiplus.lib")

#define l(...) {\
    char str[100];\
    sprintf(str, __VA_ARGS__);\
    std::cout << "[" << __FILE__ << "][" << __FUNCTION__ << "][Line " << __LINE__ << "] " << str << std::endl;\
	    }

class Executor
{
public:

	void* HookFunction(DWORD address, DWORD jumpto);

	Executor();
	virtual ~Executor();
};

void RedirectIOToConsole(void);