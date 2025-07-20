using DArvis.IO.Packet;
using DArvis.Models;

namespace DArvis.DTO.ServerPackets;

public class EntityMoved
{
    public int Serial;
    public int PreviousX;
    public int PreviousY;
    public int X;
    public int Y;
    public Direction Direction;
    
    public EntityMoved(ServerPacket serverPacket)
    {
        var buffer = serverPacket.Buffer;
        Serial = buffer.ReadInt32();
        X = PreviousX = buffer.ReadInt16();
        Y = PreviousY = buffer.ReadInt16();
        Direction = (Direction)buffer.ReadByte();
        switch (Direction)
        {
            case Direction.South:
                Y++;
                break;
            case Direction.East:
                X++;
                break;
            case Direction.West:
                X--;
                break;
            case Direction.North:
                Y--;
                break;
        }
    }
}