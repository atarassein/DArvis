#pragma once

class DABase
{
public:
	std::string Name;
	UINT Serial;

	int FindPointer(int offset, HANDLE pHandle, int baseaddr, int offsets[])
	{
		int Address = baseaddr;
		int total = offset;
		for (int i = 0; i < total; i++)
		{
			ReadProcessMemory(pHandle, (LPCVOID)Address, &Address, 4, NULL);
			Address += offsets[i];
		}
		return Address;
	}

	bool CanInjectClient()
	{
		int offsets[] = { 491744 };
		int ptr = FindPointer(1, GetCurrentProcess(), 0x0075F8D8, offsets);
		return ptr == 0;
	}

};
