using System;
using System.Linq;
using DArvis.DTO;
using DArvis.Models;

namespace DArvis.IO.Process.PacketConsumers;

public class MapPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        var objectPacketTypes = new[]
        {
            Packet.PacketType.MapChanged,
            Packet.PacketType.MapData,
        };
        
        return objectPacketTypes.Contains(packet.Type);
    }

    public override void ProcessPacket(Packet packet)
    {
        if (packet.Type == Packet.PacketType.MapData) return;

        if (packet.Type == Packet.PacketType.MapChanged)
        {
            HandleMapChange(packet);
        }
    }
    
    private void HandleMapChange(Packet packet)
    {
        // TODO: Drop a breadcrumb at the door of the previous map if player has followers
        var mapChanged = new MapChanged(packet);
        var mapAttributes = new MapLocationAttributes
        {
            MapName = mapChanged.MapName,
            MapNumber = mapChanged.MapNumber,
            Width = mapChanged.MapWidth,
            Height = mapChanged.MapHeight
        };
        
        packet.Player.Location.Attributes = mapAttributes;
        packet.Handled = true;
    }
}