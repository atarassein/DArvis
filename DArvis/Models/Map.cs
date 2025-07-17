using System;
using DArvis.IO;

namespace DArvis.Models;

public class Map
{
    private readonly object _lock = new();
    
    private int[,] _terrain;
    // private readonly Dictionary<Point, Player> _players;
    // private readonly Dictionary<Point, Enemy> _enemies;
    // private readonly Dictionary<Point, List<Item>> _items;
    
    public int MapNumber, Width, Height;
    
    private Map(int mapNumber, int width, int height, int[,] terrain)
    {
        MapNumber = mapNumber;
        Width = width;
        Height = height;
        lock (_lock)
        {
            _terrain = terrain;
        }
    }
    
    public static Map loadFromFile(int mapNumber, int width, int height)
    {
        var grid = MapLoader.LoadMap(mapNumber, width, height);
        return new Map(mapNumber, width, height, grid);
    } 
    
    public int GetGridValue(int x, int y)
    {
        lock (_lock)
        {
            return _terrain[x, y];
        }
    }
    
    public void SetGridValue(int x, int y, int value)
    {
        lock (_lock)
        {
            _terrain[x, y] = value;
        }
    }
    
    public bool IsPassable(short x, short y)
    {
        return GetGridValue(x, y) == 0;
    }

    public void SetPassable(short x, short y)
    {
        SetGridValue(x, y, 0);
    }
    
    public void SetWall(short x, short y)
    {
        SetGridValue(x, y, 1);
    }

    public override string ToString()
    {
        var result = "";
        for (short i = 0; i < Height; i++)
        {
            for (short j = 0; j < Width; j++)
            {
                result += IsPassable(j, i) ? " " : "X";
            }
            result += Environment.NewLine;
        }
        return result;
    }
}