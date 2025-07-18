namespace DArvis.DTO;

public class EntityRemoved
{
    
    public int Serial { get; set; }
    
    public EntityRemoved(Packet packet)
    {
        Serial = packet.Buffer.ReadInt32();
    }
}