using System;
using System.Collections.Concurrent;
using System.Net;
using System.Windows;
using DArvis.Common;
using DArvis.DTO;
using DArvis.IO;

namespace DArvis.Models;

public class Map: UpdatableObject
{
    private readonly object _lock = new();
    private int[,] _terrain;
    // private readonly Dictionary<Point, Player> _players;
    // private readonly Dictionary<Point, Enemy> _enemies;
    // private readonly Dictionary<Point, List<Item>> _items;

    public ConcurrentDictionary<int, MapEntity> Entities;
    
    public MapLocationAttributes Attributes;
    
    private Map(MapLocationAttributes attributes, int[,] terrain)
    {
        Entities = new ConcurrentDictionary<int, MapEntity>();
        Attributes = attributes;
        lock (_lock)
        {
            _terrain = terrain;
        }
    }

    public static Map loadFromAttributes(MapLocationAttributes attributes)
    {
        if (attributes == null)
            throw new ArgumentNullException(nameof(attributes), "MapLocationAttributes cannot be null.");
        
        var grid = MapLoader.LoadMapGridFromAttributes(attributes);
        return new Map(attributes, grid);
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

    public void AddEntity(MapEntity entity)
    {
        if (entity == null || entity.Serial <= 0)
            return;

        bool shouldUpdate = false;
        if (Entities.ContainsKey(entity.Serial))
        {
            Entities[entity.Serial] = entity;
            shouldUpdate = true;
        }
        else
        {
            Entities.TryAdd(entity.Serial, entity);
            shouldUpdate = true;
        }

        if (shouldUpdate)
            Update();
    }
    
    public void AddEntities(MapEntity[] entities)
    {
        bool shouldUpdate = false;
        foreach (var entity in entities)
        {
            if (entity == null || entity.Serial <= 0)
                continue;

            if (Entities.ContainsKey(entity.Serial))
            {
                Entities[entity.Serial] = entity;
                shouldUpdate = true;
            }
            else
            {
                Entities.TryAdd(entity.Serial, entity);
                shouldUpdate = true;
            }
        }
        
        if (shouldUpdate)
            Update();
    }

    public void EntityMoved(int serial, Point oldPoint, Point newPoint, Direction direction)
    {
        if (serial <= 0)
            return;

        bool shouldUpdate = false;
        if (Entities.TryGetValue(serial, out var entity))
        {
            if (entity.Point != newPoint)
            {
                entity.Point = newPoint;
                shouldUpdate = true;
            }

            if (entity.Direction != direction)
            {
                entity.Direction = direction;
                shouldUpdate = true;
            }
            
            if (shouldUpdate)
                Update();
        }
    }
    
    public void RemoveEntityBySerial(int serial)
    {
        if (serial <= 0)
            return;

        if (Entities.TryRemove(serial, out var entity))
        {
            Update();
        }
    }
    
    public override string ToString()
    {
        var result = Attributes.MapName + " (" + Attributes.MapNumber + ") - " + Attributes.Width + "/" + Attributes.Height+ Environment.NewLine;
        for (short i = 0; i < Attributes.Height; i++)
        {
            for (short j = 0; j < Attributes.Width; j++)
            {
                result += IsPassable(j, i) ? " " : "X";
            }
            result += Environment.NewLine;
        }
        return result;
    }

    protected override void OnUpdate()
    {
        // TODO: entities were added, removed, or updated - refresh accordingly
    }
}