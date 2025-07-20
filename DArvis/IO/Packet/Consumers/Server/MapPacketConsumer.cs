using System.Linq;
using DArvis.DTO;
using DArvis.DTO.ServerPackets;
using DArvis.Models;

namespace DArvis.IO.Packet.Consumers.Server;

public class MapPacketConsumer : PacketConsumer<ServerPacket>
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        var objectEvents = new[]
        {
            ServerPacket.ServerEvent.MapChanged,
            ServerPacket.ServerEvent.MapData,
            ServerPacket.ServerEvent.AislingAdded,
            ServerPacket.ServerEvent.EntitiesAdded,
            ServerPacket.ServerEvent.EntityMoved,
            ServerPacket.ServerEvent.EntityRemoved,
        };
        
        return objectEvents.Contains(serverPacket.EventType);
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        if (serverPacket.EventType == ServerPacket.ServerEvent.MapData)
        {
            // We don't need to do anything with MapData packets since we get map data from files
            serverPacket.Handled = true;
            return;
        }

        if (serverPacket.EventType == ServerPacket.ServerEvent.MapChanged)
        {
            HandleMapChange(serverPacket);
        }

        if (!serverPacket.Player.NeedsMapData())
        {
            serverPacket.Handled = true;
            return;
        }
        
        if (serverPacket.EventType == ServerPacket.ServerEvent.AislingAdded)
        {
            HandleAislingAdded(serverPacket);
        }

        if (serverPacket.EventType == ServerPacket.ServerEvent.EntitiesAdded)
        {
            HandleEntitiesAdded(serverPacket);
        }
        
        if (serverPacket.EventType == ServerPacket.ServerEvent.EntityMoved)
        {
            HandleEntityMoved(serverPacket);
        }

        if (serverPacket.EventType == ServerPacket.ServerEvent.EntityRemoved)
        {
            HandleEntityRemoved(serverPacket);
        }
    }
    
    private void HandleMapChange(ServerPacket serverPacket)
    {
        // TODO: Drop a breadcrumb at the door of the previous map if player has followers
        var mapChanged = new MapChanged(serverPacket);
        var mapAttributes = new MapLocationAttributes
        {
            MapName = mapChanged.MapName,
            MapNumber = mapChanged.MapNumber,
            Width = mapChanged.MapWidth,
            Height = mapChanged.MapHeight
        };
        
        serverPacket.Player.Location.Attributes = mapAttributes;
        serverPacket.Handled = true;
    }
    
    private void HandleAislingAdded(ServerPacket serverPacket)
    {
        var aisling = new AislingEntityAdded(serverPacket);
        if (serverPacket.Player.Leader != null && aisling.Entity.Serial == serverPacket.Player.Leader.PacketId)
        {
            // The leader will handle its own tracking for us.
            serverPacket.Handled = true;
            return;
        }
        
        if (aisling.Entity.Serial == serverPacket.Player.PacketId)
        {
            serverPacket.Handled = true;
            return; // We don't really need to see when the player is added to their own map
        }
        
        // var added = ConsoleOutputExtension.ColorText("AISLING ADDED", ConsoleColor.Green);
        // Console.WriteLine($"{added}   " + packet);
        serverPacket.Player.Location.CurrentMap.AddEntity(aisling.Entity);
        serverPacket.Handled = true;
    }

    private void HandleEntitiesAdded(ServerPacket serverPacket)
    {
        // var added = ConsoleOutputExtension.ColorText("ENTITY ADDED", ConsoleColor.Green);
        // Console.WriteLine($"{added}    " + packet);
        var entities = new EntitiesAdded(serverPacket);
        
        serverPacket.Player.Location.CurrentMap.AddEntities(entities.Entities);
        serverPacket.Handled = true;
    }
    
    private void HandleEntityMoved(ServerPacket serverPacket)
    {
        var entityMoved = new EntityMoved(serverPacket);
        if (serverPacket.Player.Leader != null && entityMoved.Serial == serverPacket.Player.Leader.PacketId)
        {
            // The leader will handle its own tracking for us.
            serverPacket.Handled = true;
            return;
        }
        
        var prevX = entityMoved.PreviousX;
        var prevY = entityMoved.PreviousY;
        var newX = entityMoved.X;
        var newY = entityMoved.Y;
        var direction = entityMoved.Direction;
        // Console.WriteLine($"ENTITY MOVED: {entityMoved.Serial} ({newX}, {newY})");
        // var moved = ConsoleOutputExtension.ColorText("ENTITY MOVED", ConsoleColor.Yellow);
        // Console.WriteLine($"{moved}    " + packet);
        
        serverPacket.Player.Location.CurrentMap.EntityMoved(entityMoved.Serial, prevX, prevY, newX, newY, direction);
        serverPacket.Handled = true;
    }
    
    private void HandleEntityRemoved(ServerPacket serverPacket)
    {
        var entityRemoved = new EntityRemoved(serverPacket);
        if (serverPacket.Player.Leader != null && entityRemoved.Serial == serverPacket.Player.Leader.PacketId)
        {
            // The leader will handle its own tracking for us.
            serverPacket.Handled = true;
            return;
        }
        
        serverPacket.Player.Location.CurrentMap.RemoveEntityBySerial(entityRemoved.Serial);
        // var removed = ConsoleOutputExtension.ColorText("ENTITY REMOVED", ConsoleColor.Red);
        // Console.WriteLine($"{removed}  " + packet);
        serverPacket.Handled = true;
    }
}