using System;
using System.Buffers.Binary;
using DArvis.Models;

namespace DArvis.DTO;

public class PlayerChangedDirection
{
    
    public Direction Direction { get; set; }
    
    public int PacketId { get; set; }
    
    public PlayerChangedDirection(ServerPacket serverPacket)
    {
        if (serverPacket.Data.Length < 6)
            throw new ArgumentException("Packet data is too short to contain player movement information.");

        PacketId = serverPacket.ReadInt32(1);
        var direction = (int)serverPacket.Data[5];
        
        if (Enum.IsDefined(typeof(Direction), direction))
        {
            Direction = (Direction)direction;
        }
        else
        {
            throw new ArgumentException("Invalid direction value in packet data.");
        }
        
    }
}