using System.Collections.Generic;
using DArvis.IO.Packet;

namespace DArvis.DTO.ServerPackets;

public class MapSelection
{
    public string Name;
    public Dictionary<string, byte[]> Maps;
    
    public MapSelection(Packet packet)
    {
        var buffer = packet.Buffer;
        var nameLen = buffer.ReadByte();
        Name = buffer.ReadString(nameLen);
        var mapCount = buffer.ReadByte();
        buffer.ReadByte(); // unknown
        for (int i = 0; i < mapCount; i++)
        {
            buffer.ReadByte(); // unknown
            buffer.ReadByte(); // unknown
            buffer.ReadByte(); // unknown
            buffer.ReadByte(); // unknown
            var mapNameLen = buffer.ReadByte();
            var mapName = buffer.ReadString(mapNameLen);
            var mapSerial = new byte[8];
            for (int j = 0; j < 8; j++)
            {
                mapSerial[j] = buffer.ReadByte(); // map serial next bytes
            }
            Maps[mapName] = mapSerial;
        }
    }
}