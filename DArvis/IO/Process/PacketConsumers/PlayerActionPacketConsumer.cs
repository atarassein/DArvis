using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class PlayerActionPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        var types = new[]
        {
            Packet.PacketType.PlayerAnimation,
        };
        
        return types.Contains(packet.Type);
    }

    public override void ProcessPacket(Packet packet)
    {
        packet.Handled = true;
    }
    
}