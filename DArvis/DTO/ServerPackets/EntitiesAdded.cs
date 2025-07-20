using DArvis.IO.Packet;
using DArvis.Models;

namespace DArvis.DTO.ServerPackets;

public class EntitiesAdded
{

    public MapEntity[] Entities { get; set; }
    
    public EntitiesAdded(ServerPacket serverPacket)
    {
        var buffer = serverPacket.Buffer;
        var count = buffer.ReadUInt16();
        Entities = new MapEntity[count];

        int dontTrackLeaders = -1;
        if (serverPacket.Player.Leader != null)
            dontTrackLeaders = serverPacket.Player.Leader.PacketId;
        
        for (int i = 0; i < count; i++)
        {
            var entity = new MapEntity();
            
            entity.X = buffer.ReadInt16();
            entity.Y = buffer.ReadInt16();
            
            var serial = buffer.ReadInt32();
            // Don't track the leader's entity, the leader will track itself for us.
            if (serial == dontTrackLeaders)
                continue;
            
            entity.Serial = serial;
            
            var sprite = buffer.ReadUInt16();
      
            if (sprite < 0x8000) // Non-NPC sprites begin at 0x8000
            {
                // NPC
                sprite -= 0x4000; // Remove the NPC offset
                buffer.ReadByte();
                buffer.ReadByte();
                buffer.ReadByte();
                buffer.ReadByte();
                
                var direction = buffer.ReadByte();
                // the data says it's facing south but it's facing east
                // the data says it's facing east but it's facing south
                entity.Direction = (Direction)direction;
                
                buffer.ReadByte();
                var type = buffer.ReadByte();
                entity.Type = (MapEntity.MapEntityType)type;
                if (entity.Type == MapEntity.MapEntityType.NPC)
                {
                    var nameLength = buffer.ReadByte();
                    var name = buffer.ReadString(nameLength);
                    entity.Name = name;
                    
                    // Fix the direction for NPCs. As far as I can tell they can only face East or South.
                    if (entity.Direction == Direction.South)
                        entity.Direction = Direction.East;
                    else if (entity.Direction == Direction.East)
                        entity.Direction = Direction.South;
                }
            }
            else
            {
                sprite -= 0x8000; // Non-NPC sprites begin at 0x8000... remove the offset
                buffer.ReadByte(); // This appears to be empty data
                buffer.ReadByte(); // This appears to be empty data
                buffer.ReadByte(); // This appears to be empty data
                entity.Type = MapEntity.MapEntityType.Item;
            }
            entity.Sprite = sprite;
            
            Entities[i] = entity;
        }
        
    }
}