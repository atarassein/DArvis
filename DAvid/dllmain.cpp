#pragma once

#include "common.h"
#include "Constants.h"
#include "GameFunctions.h"
#include "Logger.h"

Darkages* da;


void RenderCalls(void);


DWORD WINAPI Setup(LPVOID Args)
{
	if (da->Init(Args))
	{
		if (da != nullptr)
			da->Run();

		return 1;
	}
}

DWORD WINAPI PacketRecvConsumer(LPVOID Args)
{
	while (true)
	{
		Sleep(1);
        
		if (*reinterpret_cast<int*>(RecvConsumerPacketAvailable) == 1)
		{
			auto length = *reinterpret_cast<int*>(RecvConsumerPacketLength);
			auto data = reinterpret_cast<unsigned char*>(RecvConsumerPacketData);
            
			if (data && length > 0)
			{
				GameFunction::SendToClient(data, length);
				*reinterpret_cast<int*>(RecvConsumerPacketAvailable) = 0;
			}
		}
	}
}

DWORD WINAPI PacketConsumer(LPVOID Args)
{
    while (true)
    {
        Sleep(1);
        
        if (*reinterpret_cast<int*>(SendConsumerPacketAvailable) == 1)
        {
            auto length = *reinterpret_cast<int*>(SendConsumerPacketLength);
            auto data = reinterpret_cast<unsigned char*>(SendConsumerPacketData);
            
            if (data && length > 0)
            {
                GameFunction::SendToServer(data, length);
                *reinterpret_cast<int*>(SendConsumerPacketAvailable) = 0;
            }
        }
    }
}

HANDLE a, b, c;

BOOL APIENTRY DllMain(HANDLE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
		case DLL_PROCESS_ATTACH:
			{
				DisableThreadLibraryCalls(NULL);
				da = new Darkages;
				a = CreateThread(NULL, 0, Setup, 0, 0, &da->ProcessId);
				b = CreateThread(NULL, 0, PacketConsumer, 0, 0, &da->ProcessId);
				c = CreateThread(NULL, 0, PacketRecvConsumer, 0, 0, &da->ProcessId);
			}
			break;
		case DLL_PROCESS_DETACH:
			if (da->GameHandle != nullptr)
			{
				CloseHandle(a);
				CloseHandle(b);
				CloseHandle(c);
				da->CleanUp();
			}
			break;
		default:
			break;
	}
	return TRUE;
}
