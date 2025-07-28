using System;
using System.Collections.Concurrent;
using System.Windows;

namespace DArvis.Models;

public class MapEntity
{
    public enum MapEntityType
    {
        Monster = 0,
        Pet     = 1,
        NPC     = 2,
        Player  = 3,
        PassableMonster = 5,
        Aisling = 6,
        Item = 7
    }

    public ushort Sprite;
    public int X;
    public int Y;
    public int PreviousX;
    public int PreviousY;
    public Direction Direction;
    public string Name = "Unknown";
    public int Serial;
    public bool Hidden = false;

    public ConcurrentDictionary<string, DateTime> DebuffExpirationTimes { get; set; } = new();

    public MapEntityType Type { get; set; }

    public bool IsItem => Type == MapEntityType.Item;
    
    public bool IsPassable =>  Type == MapEntityType.Pet 
                               || Type == MapEntityType.Item 
                               || Type == MapEntityType.PassableMonster;
    
    public bool IsHostile => Type == MapEntityType.Monster 
                               || Type == MapEntityType.PassableMonster;
}
