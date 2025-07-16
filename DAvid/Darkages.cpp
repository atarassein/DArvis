#pragma once

#include "common.h"
#include <thread>
#include <concrt.h>
#include "Constants.h"
#include <ddraw.h>
#include <vector>
#include <chrono>
#include <future>
#include <string>
#include <functional>
#include <cstdio>
#include <cctype>
#include <detours.h>

#include "Logger.h"

Darkages da;
DABase base;

typedef void (_stdcall *OnRecvEvent)(BYTE *data, unsigned int Length); OnRecvEvent Receiver = NULL;
typedef int  (__stdcall *OnSendEvent)(BYTE *data, int arg1, int arg2, char arg3); OnSendEvent Sender = NULL;

typedef int(__stdcall *pWNDPROC)(HWND hWnd, signed int Msg, WPARAM wParam, LPARAM lParam);
pWNDPROC oWndProc = NULL;

BYTE WalkOrdinal = 0;


std::vector<int> split(const std::string &s, char delim) {
	std::vector<int> elems;
	std::stringstream ss(s);
	std::string number;
	while (std::getline(ss, number, delim)) {
		elems.push_back(std::stoi(number));
	}
	return elems;
}

void InjectWalk(byte direction)
{
	int thisptr = *(int*)0x00882E68;
	int Hook = 0x005F0C40;
	void* memory = malloc(sizeof(char));
	memory = (void*)direction;

	__asm
	{
		mov eax, memory
		push eax
		mov ecx, [thisptr]
		call Hook
	}
}

int __stdcall myWndProc(HWND hWnd, signed int Msg, WPARAM wParam, LPARAM lParam)
{
	if (Msg == 0x004A)
	{
		COPYDATASTRUCT* pcds = (COPYDATASTRUCT*)lParam;
		
		if (pcds->dwData == 1)
		{
			InjectWalk(1);
		}
		else if(pcds->dwData == 2)
		{
			InjectWalk(2);
		}
		else if (pcds->dwData == 0)
		{
			InjectWalk(0);
		}
		else if (pcds->dwData == 3)
		{
			InjectWalk(3);
		}
	}

	return oWndProc(hWnd, Msg, wParam, lParam);
}

HDC context = NULL;

HWND hTargetWnd = FindWindow(nullptr, L"DArvis");

void RedirectPacketInformation(byte *packet, int length, int type)
{
	try
	{
		if (!IsWindow(hTargetWnd))
		{
			logger::message("DArvis window not found, trying to find it again.");
			hTargetWnd = FindWindow(nullptr, L"DArvis");
			return;
		}

		if (length <= 0 || packet == nullptr)
		{
			logger::message("Invalid packet data or length.");
			return;
		}
		if (length > 2048)
		{
			logger::message("Packet length exceeds maximum allowed size.");
			return;
		}
		
		COPYDATASTRUCT payload;
		payload.dwData = type;
		payload.cbData = sizeof(BYTE) * length;
		payload.lpData = packet;

		SendMessageTimeout(hTargetWnd, WM_COPYDATA, 
			static_cast<WPARAM>(
		    reinterpret_cast<int>(packet)), 
			reinterpret_cast<LPARAM>(
			static_cast<LPVOID>(&payload)), SMTO_NORMAL, 50,
			nullptr);
		SendMessageTimeout(hTargetWnd, WM_COPYDATA, static_cast<WPARAM>(da.ProcessId) == 0 ? GetCurrentProcessId() : da.ProcessId, (LPARAM)(LPVOID)&payload, SMTO_NORMAL, 50, NULL);
	}
	catch (std::exception)
	{
		return;
	}
}

int __stdcall OnPacketSend(BYTE *data, int arg1, int arg2, char arg3)
{
	__asm
	{
		pushfd
		pushad
	}

	RedirectPacketInformation(data, arg1, 2);

	__asm
	{
		popad
		popfd
	}

	return Sender(data, arg1, arg2, arg3);
}

void __stdcall OnPacketRecv(BYTE *data, unsigned int Length)
{
	if (data[0] == 0x19 && *reinterpret_cast<int*>(OptionA) == 1)
	{
		return;
	}

	if (data[0] == 0x13 && *reinterpret_cast<int*>(OptionB) == 1)
	{
		return;
	}

	if (data[0] == 0x29 && *reinterpret_cast<int*>(OptionC) == 1)
	{
		short animation = (data[9] << 8) | data[10];
		if (animation == 33)
			return;
		if (animation == 245)
			return;
		if (animation == 7)
			return;
		if (animation == 245)
			return;
		if (animation == 239)
			return;
		if (animation == 62)
			return;
	}


	__asm
	{
		pushfd
		pushad
	}

	try
	{
		if (data[0] == 0x07)
		{
			USHORT entity_count = (USHORT)((data[1] << 8) + data[2]);
			int index = 0;

			for (int i = 0; i < entity_count; i++)
			{
				USHORT xcord = (USHORT)((data[index + 3] << 8) + data[index + 4]);
				USHORT ycord = (USHORT)((data[index + 5] << 8) + data[index + 6]);
				USHORT sprite = (USHORT)((data[index + 11] << 8) + data[index + 12]);

				if (sprite > 0x8000 && sprite < 0x9000)
				{
					if (sprite == 0 || sprite == 32000)
					{
						data[index + 11] = 0x93;
						data[index + 12] = 0x00;
					}
					else if (sprite == 32908 && *((int*)OptionD) == 1)
					{
						data[index + 11] = 0x90;
						data[index + 12] = 0x00;
					}

					index += 13;
				}
				else
				{
					if (sprite < 0x8000 && *((int*)OptionE) == 1)
					{
						data[index + 11] = 0x40;
						data[index + 12] = 14;
					}

					int TYPE = (USHORT)((data[index + 18] << 8) + data[index + 19]);

					if (TYPE == 0x0001 || TYPE == 0x0000)
					{
						index += 17;
					}
					else
					{
						int name_length = (BYTE)data[index + 20];
						index += 18 + name_length;
					}
				}
			}
		}
	}
	catch (std::exception)
	{

	}

	RedirectPacketInformation(data, Length, 1);

	__asm
	{
		popad
		popfd
	}

	Receiver(data, Length);
}

bool Darkages::Init(void *hModule)
{
	return true;
};


int CallBack(Darkages game)
{
	da = game;
	da.ProcessId = GetCurrentProcessId();
	da.base = &base;
	return 1;
}

void Darkages::Run()
{
	Receiver = (OnRecvEvent)DetourFunction((PBYTE)recvPacketin, (PBYTE)OnPacketRecv);
	Sender = (OnSendEvent)DetourFunction((PBYTE)sendPacketout, (PBYTE)OnPacketSend);
	oWndProc = (pWNDPROC)DetourFunction((PBYTE)DAPROC, (PBYTE)myWndProc);
	LetsGo(*this, &CallBack);
}

void Darkages::LetsGo(Darkages& obj, Callback cb)
{
	char *name = { 0 };

	__asm
	{
		mov eax, userNameoffset
		mov name, eax
	}

	obj.base = new DABase();
	obj.base->Name = name;
	obj.ProcessId = GetCurrentProcessId();
	cb(obj);
}

void Darkages::CleanUp()
{
	DetourRemove((PBYTE)Receiver, (PBYTE)OnPacketRecv);
	DetourRemove((PBYTE)Sender, (PBYTE)OnPacketSend);
}