using DArvis.IO.Packet;
using DArvis.Models;

namespace DArvis.DTO.ServerPackets;

public class AislingEntityAdded
{
    
    public MapEntity Entity;
    
    public AislingEntityAdded(ServerPacket serverPacket)
    {
        Entity = new MapEntity();
        Entity.Type = MapEntity.MapEntityType.Aisling;
        
        var buffer = serverPacket.Buffer;
        Entity.X = buffer.ReadInt16();
        Entity.Y = buffer.ReadInt16();
        Entity.Direction = (Direction)buffer.ReadByte();
        Entity.Serial = buffer.ReadInt32();
        
        // The length of name bytes is stored at index 41
        if (serverPacket.Data.Length < 42)
            return;
        
        var nameLength = serverPacket.Data[41];
        if (nameLength == 0)
            Entity.Hidden = true;
        
        // If there isn't enough data for the name then the server sent us bad data, bail out
        if (serverPacket.Data.Length < 41 + nameLength)
            return;

        Entity.Name = serverPacket.ReadString(42, nameLength);
    }
}