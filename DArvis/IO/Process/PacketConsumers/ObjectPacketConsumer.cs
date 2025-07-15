using System;
using System.Linq;
using System.Threading;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class ObjectPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        var objectPacketTypes = new[]
        {
            Packet.PacketType.ObjectMoved,
            Packet.PacketType.ObjectRemoved,
        };
        
        return objectPacketTypes.Contains(packet.Type);
    }

    public override void ProcessPacket(Packet packet)
    {
        // TODO: Implement object packet processing logic
        // TODO: This might end up in map packet handling, so we might need to refactor this later.
    }
}