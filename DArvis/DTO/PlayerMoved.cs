using System;
using System.Buffers.Binary;
using DArvis.Models;

namespace DArvis.DTO;

public class PlayerMoved
{
    
    public Direction Direction { get; set; }
    
    public int X { get; set; }
    
    public int Y { get; set; }
    
    public PlayerMoved(ServerPacket serverPacket)
    {
        var direction = (int)serverPacket.Data[1];
        var x = BinaryPrimitives.ReadInt16BigEndian(serverPacket.Data.AsSpan(2, 2));
        var y = BinaryPrimitives.ReadInt16BigEndian(serverPacket.Data.AsSpan(4, 2));
        
        if (serverPacket.Data.Length < 5)
            throw new ArgumentException("Packet data is too short to contain player movement information.");

        if (Enum.IsDefined(typeof(Direction), direction))
        {
            Direction = (Direction)direction;
        }
        else
        {
            throw new ArgumentException("Invalid direction value in packet data.");
        }
        
        switch (Direction)
        {
            case Direction.South:
                y++;
                break;
            case Direction.West:
                x--;
                break;
            case Direction.East:
                x++;
                break;
            case Direction.North:
                y--;
                break;
        }

        X = x;
        Y = y;
    }
}