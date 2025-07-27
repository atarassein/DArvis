using DArvis.IO.Packet;

namespace DArvis.DTO.ServerPackets;

public class EntityRemoved
{
    
    public int Serial { get; set; }
    
    public EntityRemoved(ServerPacket serverPacket)
    {
        Serial = serverPacket.Buffer.ReadInt32();
    }
}