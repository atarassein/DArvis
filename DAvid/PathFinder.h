#pragma once

#include "common.h"

struct PathFinderNode
{
public:
	short X, Y;
	int H;
	bool IsNull = true;

	PathFinderNode(short x, short y, int h)
		: X(x), Y(y), H(h)
	{
		IsNull = false;
	}

	PathFinderNode *LastNode;
	PathFinderNode *NextNode;
	
	std::string ToString() const
	{
		return "(" + std::to_string(X) + ", " + std::to_string(Y) + ")";
	}
};

struct Point
{
public:
	short X;
	short Y;
};
