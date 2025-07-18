using System.Windows;
using DArvis.Models;

namespace DArvis.DTO;

public class EntityMoved
{
    public int Serial;
    public Point PreviousPoint;
    public Point Point;
    public Direction Direction;
    
    public EntityMoved(Packet packet)
    {
        var buffer = packet.Buffer;
        Serial = buffer.ReadInt32();
        var x = buffer.ReadInt16();
        var y = buffer.ReadInt16();
        PreviousPoint = new Point(x, y);
        Direction = (Direction)buffer.ReadByte();
        switch (Direction)
        {
            case Direction.South:
                y++;
                break;
            case Direction.East:
                x++;
                break;
            case Direction.West:
                x--;
                break;
            case Direction.North:
                y--;
                break;
        }
        
        Point = new Point(x, y);
    }
}