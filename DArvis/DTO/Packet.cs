using System;
using DArvis.Models;

namespace DArvis.DTO;

public class Packet(byte[] data, Packet.PacketType type, Player player)
{
    public byte[] Data { get; set; } = data;

    public PacketType Type { get; set; } = type;

    public Player Player { get; set; } = player;
    
    public override string ToString()
    {
        var player = Player.Name == null ? Player.Process.ProcessId.ToString() : Player.Name;
        return $"[{Type}] ({player}) {BitConverter.ToString(Data)}";
    }
    
    public enum PacketType    
    {
        Unknown = 0,
        Server = 1,
        Client = 2
    }
    
}