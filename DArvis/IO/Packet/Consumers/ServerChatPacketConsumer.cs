using System;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Packet.Consumers;

public class ServerChatPacketConsumer : PacketConsumer<ServerPacket>
{
    public override bool CanConsume(ServerPacket packet)
    {
        var serverPacket = (ServerPacket)packet;
        return serverPacket.EventType == ServerPacket.ServerEvent.Chat ||
               serverPacket.EventType == ServerPacket.ServerEvent.Message;
    }

    public override void ProcessPacket(ServerPacket packet)
    {
        switch (packet.EventType)
        {
            case ServerPacket.ServerEvent.Chat:
                HandleChatMessage(packet);
                break;
            case ServerPacket.ServerEvent.Message:
                HandleWhisperMessage(packet);
                break;
        }
    }

    private void HandleChatMessage(ServerPacket serverPacket)
    {
        // Console.WriteLine("Processing chat message...");
        serverPacket.Handled = true;
    }

    private void HandleWhisperMessage(ServerPacket serverPacket)
    {
        // Process whisper message
        serverPacket.Handled = true;
    }
}