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

		try
		{
			if (*reinterpret_cast<int*>(RecvConsumerPacketAvailable) == 1)
			{
				auto target = *reinterpret_cast<int*>(RecvConsumerPacketType);
				auto length = *reinterpret_cast<int*>(RecvConsumerPacketLength);
				auto data = reinterpret_cast<unsigned char*>(RecvConsumerPacketData);

				if (data == 0)
					continue;
				if (length <= 0)
					continue;

				std::vector<byte> packet;
				for (int i = 0; i < length; i++ , data++)
					packet.push_back(*data);

				if (packet.size() == 0 || packet.size() != length)
				{
					packet.clear();
					*reinterpret_cast<int*>(RecvConsumerPacketAvailable) = 0;
				}
				else
				{
					GameFunction::SendToClient(&packet[0], length);
					packet.clear();
					*reinterpret_cast<int*>(RecvConsumerPacketAvailable) = 0;
					*reinterpret_cast<int*>(RecvConsumerPacketData) = 0;
					WriteProcessMemory(GetCurrentProcess(), reinterpret_cast<void*>(RecvConsumerPacketData), 0x00, 2048, NULL);
				}
			}
		}
		catch (const std::exception& ex)
		{
			std::ostringstream oss;
			oss << "Exception: " << ex.what();
			logger::message(oss.str());
		}
		catch (...)
		{
			logger::message("Unknown exception occurred.");
		}
	}
}


DWORD WINAPI PacketConsumer(LPVOID Args)
{
	while (true)
	{
		Sleep(1);

		try
		{
			if (*reinterpret_cast<int*>(SendConsumerPacketAvailable) == 1)
			{
				auto target = *reinterpret_cast<int*>(SendConsumerPacketType);
				auto length = *reinterpret_cast<int*>(SendConsumerPacketLength);
				auto data = reinterpret_cast<unsigned char*>(SendConsumerPacketData);

				if (data == 0)
					continue;
				if (length <= 0)
					continue;

				std::vector<byte> packet;
				for (int i = 0; i < length; i++, data++)
					packet.push_back(*data);

				if (packet.size() == 0 || packet.size() != length)
				{
					packet.clear();
					*reinterpret_cast<int*>(SendConsumerPacketAvailable) = 0;
				}
				else
				{
					GameFunction::SendToServer(&packet[0], length);
					packet.clear();
					*reinterpret_cast<int*>(SendConsumerPacketAvailable) = 0;
					*reinterpret_cast<int*>(SendConsumerPacketData) = 0;
					WriteProcessMemory(GetCurrentProcess(), reinterpret_cast<void*>(SendConsumerPacketData), 0x00, 2048, NULL);
				}
			}
		}
		catch (const std::exception& ex)
		{
			std::ostringstream oss;
			oss << "Exception: " << ex.what();
			logger::message(oss.str());
		}
		catch (...)
		{
			logger::message("Unknown exception occurred.");
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
