using System;
using DArvis.Models;

namespace DArvis.DTO;

public class Packet(byte[] data, Packet.PacketSource source, Player player)
{
    public byte[] Data { get; set; } = data;

    public PacketSource Source { get; set; } = source;

    private PacketType _type;
    public PacketType Type
    {
        get
        {
            if (Data.Length == 0)
                return PacketType.Unknown;

            if (Enum.IsDefined(typeof(PacketType), (int)Data[0]))
                return (PacketType)Data[0];

            return PacketType.Unknown;
        }
        set
        {
            
        }
    }
    
    public Player Player { get; set; } = player;
    
    public override string ToString()
    {
        var player = Player.Name == null ? Player.Process.ProcessId.ToString() : Player.Name;
        return $"[{Source}] ({player}) {BitConverter.ToString(Data)}";
    }
    
    public enum PacketSource    
    {
        Unknown = 0,
        Server = 1,
        Client = 2
    }

    public enum PacketType
    {
        Unknown,
        PlayerChangedDirection = 0x11,
        Message = 0x0A,
        PlayerMoved = 0x0B,
        Chat = 0x0D,
        MapChanged = 0x15,
    }
}