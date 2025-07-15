using System;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class MapLoadedPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        return packet.Type == Packet.PacketType.MapChanged;
    }

    public override void ProcessPacket(Packet packet)
    {
        Console.WriteLine("Processing map change...");
    }
}