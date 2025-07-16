using System;
using System.Linq;
using DArvis.DTO;

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
        packet.Player.Location.MapNumber = mapChanged.MapNumber;
        packet.Player.Location.MapName = mapChanged.MapName;
        
        // TODO: If player is a follower then process new map for pathfinding purposes
        
        packet.Handled = true;
    }
}