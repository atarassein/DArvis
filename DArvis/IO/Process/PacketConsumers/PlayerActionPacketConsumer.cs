using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class PlayerActionPacketConsumer : PacketConsumer
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        var types = new[]
        {
            ServerPacket.PacketType.PlayerAnimation,
        };
        
        return types.Contains(serverPacket.Type);
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        serverPacket.Handled = true;
    }
    
}