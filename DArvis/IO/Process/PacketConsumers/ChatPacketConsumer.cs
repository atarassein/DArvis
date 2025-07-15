using System;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class ChatPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        return packet.Type == Packet.PacketType.Chat ||
               packet.Type == Packet.PacketType.Message;
    }

    public override void ProcessPacket(Packet packet)
    {
        switch (packet.Type)
        {
            case Packet.PacketType.Chat:
                HandleChatMessage(packet);
                break;
            case Packet.PacketType.Message:
                HandleWhisperMessage(packet);
                break;
        }
    }

    private void HandleChatMessage(Packet packet)
    {
        // Console.WriteLine("Processing chat message...");
    }

    private void HandleWhisperMessage(Packet packet)
    {
        // Process whisper message
    }
}