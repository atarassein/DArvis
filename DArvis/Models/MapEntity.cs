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
    public Point Point;
    public Direction Direction;
    public string Name = "Unknown";
    public int Serial;
    public bool Hidden = false;
        
    public MapEntityType Type { get; set; }
}
