using DArvis.Models;

namespace DArvis.DTO;

public class AislingEntityAdded
{
    
    public MapEntity Entity;
    
    public AislingEntityAdded(Packet packet)
    {
        Entity = new MapEntity();
        Entity.Type = MapEntity.MapEntityType.Aisling;
        
        var buffer = packet.Buffer;
        Entity.X = buffer.ReadInt16();
        Entity.Y = buffer.ReadInt16();
        Entity.Direction = (Direction)buffer.ReadByte();
        Entity.Serial = buffer.ReadInt32();
        
        // The length of name bytes is stored at index 41
        if (packet.Data.Length < 42)
            return;
        
        var nameLength = packet.Data[41];
        if (nameLength == 0)
            Entity.Hidden = true;
        
        // If there isn't enough data for the name then the server sent us bad data, bail out
        if (packet.Data.Length < 41 + nameLength)
            return;

        Entity.Name = packet.ReadString(42, nameLength);
    }
}