using DArvis.IO.Packet;
using DArvis.Models;

namespace DArvis.DTO.ServerPackets;

public class EntityMoved
{

    public MapEntity Entity;
    
    public EntityMoved(ServerPacket serverPacket)
    {
        var buffer = serverPacket.Buffer;
        var serial = buffer.ReadInt32();
        var previousX = buffer.ReadInt16();
        var previousY = buffer.ReadInt16();
        var direction = (Direction)buffer.ReadByte();
        var x = previousX;
        var y = previousY;
        switch (direction)
        {
            case Direction.South:
                y++;
                break;
            case Direction.East:
                x++;
                break;
            case Direction.West:
                x--;
                break;
            case Direction.North:
                y--;
                break;
        }

        Entity = new MapEntity
        {
            Serial = serial,
            X = x,
            Y = y,
            PreviousX = previousX,
            PreviousY = previousY,
            Direction = direction
        };
    }
}