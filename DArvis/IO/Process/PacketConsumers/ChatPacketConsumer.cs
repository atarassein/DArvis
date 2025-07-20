using System;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class ChatPacketConsumer : PacketConsumer
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        
        return serverPacket.EventType == ServerPacket.ServerEvent.Chat ||
               serverPacket.EventType == ServerPacket.ServerEvent.Message;
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        switch (serverPacket.EventType)
        {
            case ServerPacket.ServerEvent.Chat:
                HandleChatMessage(serverPacket);
                break;
            case ServerPacket.ServerEvent.Message:
                HandleWhisperMessage(serverPacket);
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