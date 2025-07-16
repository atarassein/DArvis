#pragma once

static class GameFunction
{
public:
	static int GetSenderID()
	{

		int value;

		__asm
		{
			mov eax, senderOffset
			mov value, eax
		}

		return *(int*)value;
	}

	static int SendToClient(BYTE *packet, int length)
	{
		if (length <= 0) return -1;
		
		LPVOID cave = VirtualAllocEx(GetCurrentProcess(), nullptr, length, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
		if (!cave) return -1;
		
		memcpy(cave, packet, length);

		int Recv = recvPacketin;
		int PacketLength = length;

		__asm
		{
			pushfd
			pushad
			pop ecx
			mov edx, PacketLength
			push edx
			mov eax, cave
			push eax
			call Recv
			lea ecx, [ebp - 00000405]
			push ecx
			popad
			popfd
		}

		VirtualFreeEx(GetCurrentProcess(), cave, 0, MEM_RELEASE);

		return 0;
	}

	static int SendToServer(BYTE *packet, int length)
	{
		if (length <= 0) return -1;
		
		LPVOID cave = VirtualAllocEx(GetCurrentProcess(), nullptr, length, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
		if (!cave) return -1;
		
		memcpy(cave, packet, length);

		int SenderID = GetSenderID();
		int Send = sendOffset;
		int PacketLength = length;
		
		__asm
		{
			pushfd
			pushad
			mov edx, PacketLength
			push edx
			mov eax, cave
			push eax
			mov ecx, [SenderID]
			call Send
			popad
			popfd
		}

		VirtualFreeEx(GetCurrentProcess(), cave, 0, MEM_RELEASE);

		return 0;
	}

};