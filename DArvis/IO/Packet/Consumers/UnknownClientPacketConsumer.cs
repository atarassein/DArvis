using System.Linq;

namespace DArvis.IO.Packet.Consumers;

public class UnknownClientPacketConsumer : PacketConsumer<ClientPacket>
{
    public override bool CanConsume(ClientPacket clientPacket)
    {
        var unknownEvents = new[]
        {
            ClientPacket.ClientEvent.Unknown0C,
            ClientPacket.ClientEvent.Unknown13,
            ClientPacket.ClientEvent.Unknown2D,
            ClientPacket.ClientEvent.Unknown38,
            ClientPacket.ClientEvent.Unknown45,
        };

        return unknownEvents.Contains(clientPacket.EventType);
    }

    public override void ProcessPacket(ClientPacket clientPacket)
    {
        if (
            clientPacket.EventType == ClientPacket.ClientEvent.Unknown0C // Unknown data - 0C-00-00-00-00-00-0C
            || clientPacket.EventType == ClientPacket.ClientEvent.Unknown13 // Unknown - 13-00-13 or 13-00-21-1B-D5-02-51-01-00
            || clientPacket.EventType == ClientPacket.ClientEvent.Unknown2D // Unknown - 2D-00
            || clientPacket.EventType == ClientPacket.ClientEvent.Unknown38 // Unknown data - happens during F5
            || clientPacket.EventType == ClientPacket.ClientEvent.Unknown45 // Unknown data - might be a  keep-alive packet
        )
        {
            clientPacket.Handled = true;
        } 
    }
}