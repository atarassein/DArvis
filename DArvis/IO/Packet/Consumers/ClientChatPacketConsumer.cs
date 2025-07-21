using System;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Packet.Consumers;

public class ClientChatPacketConsumer : PacketConsumer<ClientPacket>
{
    public override bool CanConsume(ClientPacket packet)
    {
        return packet.EventType == ClientPacket.ClientEvent.Chat
            || packet.EventType == ClientPacket.ClientEvent.Say;
    }

    public override void ProcessPacket(ClientPacket packet)
    {
        packet.Handled = true;
    }

}