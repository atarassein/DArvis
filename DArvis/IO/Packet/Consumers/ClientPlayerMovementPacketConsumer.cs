using System;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Packet.Consumers;

public class ClientPlayerMovementPacketConsumer : PacketConsumer<ClientPacket>
{
    public override bool CanConsume(ClientPacket packet)
    {
        return packet.EventType == ClientPacket.ClientEvent.Turn
            || packet.EventType == ClientPacket.ClientEvent.StepCounter;
    }

    public override void ProcessPacket(ClientPacket packet)
    {
        packet.Handled = true;
    }

}