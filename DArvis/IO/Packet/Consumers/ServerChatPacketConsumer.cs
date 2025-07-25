using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DArvis.DTO;
using DArvis.DTO.ServerPackets;
using DArvis.Services.SideQuest;

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
            var sideQuest = App.Current.Services.GetService<ISideQuest>();
            var toast = new ToastMessage
            {
                Type = "whisper",
                Title = whisper.SenderName + " > " + serverPacket.Player.Name,
                Content = whisper.Message,
                ClientPipe = "ToastReplyListener42"
            };
            sideQuest.ShowBackgroundToast(serverPacket.Player, toast);
            Console.WriteLine(whisper);
        }
        serverPacket.Handled = true;
    }
}