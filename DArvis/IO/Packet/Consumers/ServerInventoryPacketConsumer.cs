using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Packet.Consumers;

public class ServerInventoryPacketConsumer : PacketConsumer<ServerPacket>
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        var types = new[]
        {
            ServerPacket.ServerEvent.InventoryPacket,
            ServerPacket.ServerEvent.EquippedItem
        };
        
        return types.Contains(serverPacket.EventType);
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        serverPacket.Handled = true;
    }
    
}