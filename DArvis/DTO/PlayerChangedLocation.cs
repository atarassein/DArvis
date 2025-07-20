using System;
using System.Buffers.Binary;
using DArvis.Models;

namespace DArvis.DTO;

public class PlayerChangedLocation
{
    
    public int X { get; set; }
    
    public int Y { get; set; }
    
    public PlayerChangedLocation(ServerPacket serverPacket)
    {
        if (serverPacket.Data.Length < 5)
            throw new ArgumentException("Packet data is too short to contain player location information.");

        X = serverPacket.ReadInt16(1);
        Y = serverPacket.ReadInt16(3);
        
    }
}