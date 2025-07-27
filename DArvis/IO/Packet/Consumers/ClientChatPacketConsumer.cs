using System;

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
        // Console.WriteLine(packet);
        packet.Handled = true;
    }

}