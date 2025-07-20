namespace DArvis.DTO;

public class MapChanged
{
    
    public int MapNumber { get; set; }
    
    public int MapWidth { get; set; }
    
    public int MapHeight { get; set; }
    
    public string MapName { get; set; }
    
    public MapChanged(ServerPacket serverPacket)
    {
        MapNumber = serverPacket.ReadInt16(1);
        MapWidth = serverPacket.Data[3];
        MapHeight = serverPacket.Data[4];
        MapName = serverPacket.ReadString8(10);
    }
    
    public override string ToString()
    {
        return $"MapChanged: MapNumber={MapNumber}, MapWidth={MapWidth}, MapHeight={MapHeight}, MapName={MapName}";
    }
}