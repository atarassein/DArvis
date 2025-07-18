using System;
using System.Collections.Concurrent;
using System.Windows;
using DArvis.Common;
using DArvis.IO;

namespace DArvis.Models;

public class Map: UpdatableObject
{
    private readonly object _lock = new();
    private int[,] _terrain;

    public ConcurrentDictionary<int, MapEntity> WallEntities;
    public ConcurrentDictionary<int, MapEntity> PassableEntities;
    public ConcurrentDictionary<int, MapEntity> Items;
    
    public MapLocationAttributes Attributes;
    
    private Map(MapLocationAttributes attributes, int[,] terrain)
    {
        WallEntities = new ConcurrentDictionary<int, MapEntity>();
        PassableEntities = new ConcurrentDictionary<int, MapEntity>();
        Items = new ConcurrentDictionary<int, MapEntity>();
        
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

    public bool AddEntityToDict(MapEntity entity, ConcurrentDictionary<int, MapEntity> entities, bool deferUpdate = false)
    {
        if (entity == null || entity.Serial <= 0)
            return false;

        bool shouldUpdate = false;
        if (entities.ContainsKey(entity.Serial))
        {
            entities[entity.Serial] = entity;
            shouldUpdate = true;
        }
        else
        {
            entities.TryAdd(entity.Serial, entity);
            shouldUpdate = true;
        }

        if (!deferUpdate && shouldUpdate)
            Update();
        
        return shouldUpdate;
    }
    
    public bool AddEntity(MapEntity entity, bool deferUpdate = false)
    {
        bool shouldUpdate;
        if (entity.IsItem)
            shouldUpdate = AddEntityToDict(entity, Items, deferUpdate);
        else if (!entity.IsPassable)
            shouldUpdate = AddEntityToDict(entity, WallEntities, deferUpdate);
        else
            shouldUpdate = AddEntityToDict(entity, PassableEntities, deferUpdate);

        return shouldUpdate;
    }
    
    public void AddEntities(MapEntity[] entities)
    {
        bool shouldUpdate = false;
        foreach (var entity in entities)
            shouldUpdate |= AddEntity(entity, true);
        
        if (shouldUpdate)
            Update();
    }

    public void EntityMoved(int serial, Point oldPoint, Point newPoint, Direction direction)
    {
        if (serial <= 0)
            return;

        bool shouldUpdate = false;
        MapEntity entity;
        if (WallEntities.TryGetValue(serial, out entity) || PassableEntities.TryGetValue(serial, out entity) || Items.TryGetValue(serial, out entity))
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
        }
        
        if (shouldUpdate)
            Update();
    }
    
    public void RemoveEntityBySerial(int serial)
    {
        if (serial <= 0)
            return;

        MapEntity entity;
        if (WallEntities.TryRemove(serial, out entity) || PassableEntities.TryRemove(serial, out entity) || Items.TryRemove(serial, out entity))
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