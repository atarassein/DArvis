using System;
using System.Collections.Concurrent;
using System.Windows;
using DArvis.Common;
using DArvis.IO;

namespace DArvis.Models;

[Flags]
public enum TileFlags
{
    None = 0x0000,
    Wall = 0x0001,           // Physical wall or wall entity
    Item = 0x0010,           // Items on ground
    BlockingEntity = 0x0100, // NPC, monster, or player
    PassableEntity = 0x1000  // Passable entities (NPCs, pets, etc.)
}

public class Map: UpdatableObject
{
    private readonly object _lock = new();
    private int[,] _terrain;

    public ConcurrentDictionary<int, MapEntity> BlockingEntities;
    public ConcurrentDictionary<int, MapEntity> PassableEntities;
    public ConcurrentDictionary<int, MapEntity> Items;
    
    public MapLocationAttributes Attributes;
    public Player Owner;
    
    private PathFinder? _pathFinder;
    public PathFinder PathFinder
    {
        get { return _pathFinder ??= new PathFinder(); }
    }
    
    private Map(Player player, MapLocationAttributes attributes, int[,] terrain)
    {
        BlockingEntities = new ConcurrentDictionary<int, MapEntity>();
        PassableEntities = new ConcurrentDictionary<int, MapEntity>();
        Items = new ConcurrentDictionary<int, MapEntity>();
        
        Owner = player;
        Attributes = attributes;
        
        lock (_lock)
        {
            _terrain = terrain;
        }
    }

    public static Map loadFromAttributes(Player player, MapLocationAttributes attributes)
    {
        if (attributes == null)
            throw new ArgumentNullException(nameof(attributes), "MapLocationAttributes cannot be null.");
        
        var grid = MapLoader.LoadMapGridFromAttributes(attributes);
        return new Map(player, attributes, grid);
    }
    
    public int GetGridValue(int x, int y)
    {
        if (x < 0 || x >= Attributes.Width || y < 0 || y >= Attributes.Height)
            throw new ArgumentOutOfRangeException();
        
        lock (_lock)
        {
            return _terrain[x, y];
        }
    }
    
    public void SetGridValue(int x, int y, int value)
    {
        if (x < 0 || x >= Attributes.Width || y < 0 || y >= Attributes.Height)
            throw new ArgumentOutOfRangeException();
        
        lock (_lock)
        {
            _terrain[x, y] = value;
        }
    }
    
    public bool IsPassable(short x, short y)
    {
        var tileValue = GetGridValue(x, y);
        return (tileValue & ((int)TileFlags.Wall | (int)TileFlags.BlockingEntity)) == 0;
    }

    public bool AddEntityToDict(MapEntity entity, ConcurrentDictionary<int, MapEntity> entities, bool deferUpdate = false)
    {
        if (entity == null || entity.Serial <= 0)
            return false;

        // Use AddOrUpdate for atomic operation
        bool wasAdded = false;
        entities.AddOrUpdate(entity.Serial, 
            entity,
            (key, oldValue) =>
            {
                wasAdded = !oldValue.Equals(entity);
                return entity;
            });

        if (!deferUpdate && wasAdded)
            Update();

        return wasAdded;
    }
    
    public bool AddEntity(MapEntity entity, bool deferUpdate = false)
    {
        bool shouldUpdate;
        if (entity.IsItem)
            shouldUpdate = AddEntityToDict(entity, Items, deferUpdate);
        else if (!entity.IsPassable)
            shouldUpdate = AddEntityToDict(entity, BlockingEntities, deferUpdate);
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

    public void EntityMoved(int serial, int prevX, int prevY, int newX, int newY, Direction direction)
    {
        if (serial <= 0)
            return;

        bool shouldUpdate = false;
        MapEntity entity;
        if (BlockingEntities.TryGetValue(serial, out entity) || PassableEntities.TryGetValue(serial, out entity) || Items.TryGetValue(serial, out entity))
        {
            if (entity.X != newX)
            {
                entity.X = newX;
                shouldUpdate = true;
            }
            
            if (entity.Y != newY)
            {
                entity.Y = newY;
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
        if (BlockingEntities.TryRemove(serial, out entity) || PassableEntities.TryRemove(serial, out entity) || Items.TryRemove(serial, out entity))
        {
            Update();
        }
    }
    
    public override string ToString()
    {
        var start = new PointVector
        {
            Position = new Point(Owner.Location.X, Owner.Location.Y),
            Direction = Owner.Location.Direction
        };
        var end = new PointVector
        {
            Position = Owner.Leader.Breadcrumb ?? new Point(Owner.Leader.Location.X, Owner.Leader.Location.Y),
            Direction = Owner.Leader.Location.Direction
        };
        var pathToLeader = PathFinder.FindPath(start, end, _terrain);
        
        int[,] grid = new int[Attributes.Width, Attributes.Height];
        foreach (var step in pathToLeader)
        {
            grid[(int)step.Position.X, (int)step.Position.Y] = 1;
        }
        
        var result = Attributes.MapName + " (" + Attributes.MapNumber + ") - " + Attributes.Width + "/" + Attributes.Height+ Environment.NewLine;
        for (short i = 0; i < Attributes.Height; i++)
        {
            for (short j = 0; j < Attributes.Width; j++)
            {
                
                if (Owner.Location.X == j && Owner.Location.Y == i)
                {
                    result += "O";
                    continue;
                }

                if (Owner.IsOnSameMapAs(Owner.Leader) && Owner.Leader.Location.X == j && Owner.Leader.Location.Y == i)
                {
                    result += "L";
                    continue;
                }
                
                if (grid[j, i] == 1)
                {
                    result += ".";
                    continue;
                }
                
                if (HasPassableEntity(j, i))
                {
                    result += "~";
                    continue;
                }
                
                if (HasEntity(j, i))
                {
                    result += "M";
                    continue;
                }

                if (HasItem(j, i))
                {
                    result += "$";
                    continue;
                }
                result += IsPassable(j, i) ? " " : "X";
            }
            result += Environment.NewLine;
        }
        return result;
    }

    private bool IsValidCoordinate(double x, double y)
    {
        return x >= 0 && x < Attributes.Width && y >= 0 && y < Attributes.Height;
    }
    
    public bool IsPassableForPathfinding(int x, int y)
    {
        if (!IsValidCoordinate(x, y))
            return false;
    
        var tileValue = GetGridValue(x, y);
        // Passable if no walls or entities (items don't block movement)
        return (tileValue & ((int)TileFlags.Wall | (int)TileFlags.BlockingEntity)) == 0;
    }
    
    public bool HasWall(int x, int y)
    {
        return (GetGridValue(x, y) & (int)TileFlags.Wall) != 0;
    }
    
    public bool HasPassableEntity(int x, int y)
    {
        return (GetGridValue(x, y) & (int)TileFlags.PassableEntity) != 0;
    }
    
    public bool HasEntity(int x, int y)
    {
        return (GetGridValue(x, y) & (int)TileFlags.BlockingEntity) != 0;
    }
    
    public bool HasItem(int x, int y)
    {
        return (GetGridValue(x, y) & (int)TileFlags.Item) != 0;
    }


    
    private bool IsUpdating = false;
    protected override void OnUpdate()
    {
        if (IsUpdating)
            return;
        
        IsUpdating = true;
        
        var width = Attributes.Width;
        var height = Attributes.Height;
        lock (_lock)
        {
            // Reset all tiles to their base terrain values (preserve original walls)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Keep original walls (value 1), clear everything else to 0
                    var hasOriginalWall = (_terrain[x, y] & (int)TileFlags.Wall) != 0;
                    _terrain[x, y] = hasOriginalWall ? (int)TileFlags.Wall : 0;
                }
            }

            // Add wall entities
            foreach (var entity in BlockingEntities.Values)
            {
                if (IsValidCoordinate(entity.X, entity.Y))
                {
                    _terrain[entity.X, entity.Y] |= (int)TileFlags.BlockingEntity;
                }
            }

            // Add passable entities (NPCs, monsters, players)
            foreach (var entity in PassableEntities.Values)
            {
                if (IsValidCoordinate(entity.X, entity.Y))
                {
                    _terrain[entity.X, entity.Y] |= (int)TileFlags.PassableEntity;
                }
            }

            // Add items
            foreach (var entity in Items.Values)
            {
                if (IsValidCoordinate(entity.X, entity.Y))
                {
                    _terrain[entity.X, entity.Y] |= (int)TileFlags.Item;
                }
            }

            if (Owner.Leader != null)
            {
                //Console.WriteLine(this);
            }
            IsUpdating = false;
        }
    }
}