using System;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class ChatPacketConsumer : PacketConsumer
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        return serverPacket.Type == ServerPacket.PacketType.Chat ||
               serverPacket.Type == ServerPacket.PacketType.Message;
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        switch (serverPacket.Type)
        {
            case ServerPacket.PacketType.Chat:
                HandleChatMessage(serverPacket);
                break;
            case ServerPacket.PacketType.Message:
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