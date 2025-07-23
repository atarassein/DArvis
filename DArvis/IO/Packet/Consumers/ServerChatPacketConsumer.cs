using System;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DArvis.DTO;
using DArvis.DTO.ServerPackets;

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
        //Console.WriteLine($"{serverPacket}");
        serverPacket.Handled = true;
    }

    private void HandleWhisperMessage(ServerPacket serverPacket)
    {
        //var notificationService = new WindowsToastNotificationService();
        //var _whisperNotificationHandler = new WhisperNotificationHandler(notificationService);
        // Console.WriteLine(serverPacket);
        var whisper = new ChatWhisper(serverPacket);
        if (whisper.IsWhisper())
        {
            Console.WriteLine(whisper);
            
        }
        serverPacket.Handled = true;
    }
}