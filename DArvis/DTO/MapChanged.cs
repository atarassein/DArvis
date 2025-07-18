namespace DArvis.DTO;

public class MapChanged
{
    
    public int MapNumber { get; set; }
    
    public int MapWidth { get; set; }
    
    public int MapHeight { get; set; }
    
    public string MapName { get; set; }
    
    public MapChanged(Packet packet)
    {
        MapNumber = packet.ReadInt16(1);
        MapWidth = packet.Data[3];
        MapHeight = packet.Data[4];
        MapName = packet.ReadString8(10);
    }
    
    public override string ToString()
    {
        return $"MapChanged: MapNumber={MapNumber}, MapWidth={MapWidth}, MapHeight={MapHeight}, MapName={MapName}";
    }
}