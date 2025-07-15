using System;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class PlayerMovementPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        return packet.Type == Packet.PacketType.PlayerMoved ||
               packet.Type == Packet.PacketType.PlayerChangedDirection;
    }

    public override void ProcessPacket(Packet packet)
    {
        switch (packet.Type)
        {
            case Packet.PacketType.PlayerMoved:
                HandlePlayerMovement(packet);
                break;
            case Packet.PacketType.PlayerChangedDirection:
                HandlePlayerDirectionChange(packet);
                break;
        }
    }

    private void HandlePlayerMovement(Packet packet)
    {
        Console.WriteLine("Processing player movement...");
    }

    private void HandlePlayerDirectionChange(Packet packet)
    {
        Console.WriteLine("Processing player direction change...");
    }
}