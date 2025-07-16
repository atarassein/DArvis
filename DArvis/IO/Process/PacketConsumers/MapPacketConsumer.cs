using System;
using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class MapPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        var objectPacketTypes = new[]
        {
            Packet.PacketType.MapChanged,
            Packet.PacketType.MapData,
        };
        
        return objectPacketTypes.Contains(packet.Type);
    }

    public override void ProcessPacket(Packet packet)
    {
        if (packet.Type == Packet.PacketType.MapData) return;
        
        Console.WriteLine($">>>[{packet}]");
        packet.Handled = true;
        
    }
}