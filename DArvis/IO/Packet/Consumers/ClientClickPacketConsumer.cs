namespace DArvis.IO.Packet.Consumers;

public class ClientClickPacketConsumer : PacketConsumer<ClientPacket>
{
    public override bool CanConsume(ClientPacket packet)
    {
        return packet.EventType == ClientPacket.ClientEvent.Click; // not sure what this is, it fires off when clicking around the character
    }

    public override void ProcessPacket(ClientPacket packet)
    {
        packet.Handled = true;
    }

}