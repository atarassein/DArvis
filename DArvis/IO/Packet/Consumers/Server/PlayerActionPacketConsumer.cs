using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Packet.Consumers.Server;

public class PlayerActionPacketConsumer : PacketConsumer
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        var types = new[]
        {
            ServerPacket.ServerEvent.PlayerAnimation,
        };
        
        return types.Contains(serverPacket.EventType);
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        serverPacket.Handled = true;
    }
    
}