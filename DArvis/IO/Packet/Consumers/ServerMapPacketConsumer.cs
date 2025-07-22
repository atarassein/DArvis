using System;
using System.Linq;
using DArvis.DTO;
using DArvis.DTO.ServerPackets;
using DArvis.Extensions;
using DArvis.Models;
using ConsoleColor = DArvis.Extensions.ConsoleColor;

namespace DArvis.IO.Packet.Consumers;

public class ServerMapPacketConsumer : PacketConsumer<ServerPacket>
{
    public override bool CanConsume(ServerPacket packet)
    {
        var objectEvents = new[]
        {
            ServerPacket.ServerEvent.MapChanged,
            ServerPacket.ServerEvent.MapSelection,
            ServerPacket.ServerEvent.MapData,
            ServerPacket.ServerEvent.AislingAdded,
            ServerPacket.ServerEvent.EntitiesAdded,
            ServerPacket.ServerEvent.EntityMoved,
            ServerPacket.ServerEvent.EntityRemoved,
        };
        
        return objectEvents.Contains(packet.EventType);
    }

    public override void ProcessPacket(ServerPacket packet)
    {
        if (packet.EventType == ServerPacket.ServerEvent.MapData)
        {
            // We don't need to do anything with MapData packets since we get map data from files
            packet.Handled = true;
            return;
        }

        if (packet.EventType == ServerPacket.ServerEvent.MapSelection)
        {
            HandleMapSelection(packet);
        }

        if (packet.EventType == ServerPacket.ServerEvent.MapChanged)
        {
            HandleMapChange(packet);
        }

        if (packet.Player.Location.CurrentMap == null)
        {
            // Console.WriteLine("no map loaded, cannot process map packet: " + packet.EventType);
            packet.Handled = true;
            return;
        }
        
        if (packet.EventType == ServerPacket.ServerEvent.AislingAdded)
        {
            HandleAislingAdded(packet);
        }

        if (packet.EventType == ServerPacket.ServerEvent.EntitiesAdded)
        {
            HandleEntitiesAdded(packet);
        }
        
        if (packet.EventType == ServerPacket.ServerEvent.EntityMoved)
        {
            HandleEntityMoved(packet);
        }

        if (packet.EventType == ServerPacket.ServerEvent.EntityRemoved)
        {
            HandleEntityRemoved(packet);
        }
    }
    
    private void HandleMapSelection(ServerPacket packet)
    {
        var mapSelection = new MapSelection(packet);
        
        packet.Handled = true;
    }
    
    private void HandleMapChange(ServerPacket packet)
    {
        var mapChanged = new MapChanged(packet);
        var mapAttributes = new MapLocationAttributes
        {
            MapName = mapChanged.MapName,
            MapNumber = mapChanged.MapNumber,
            Width = mapChanged.MapWidth,
            Height = mapChanged.MapHeight
        };
        packet.Player.AislingManager.HideEveryoneForRefresh();
        packet.Player.Location.Attributes = mapAttributes;
        packet.Handled = true;
    }
    
    private void HandleAislingAdded(ServerPacket packet)
    {
        var aisling = new AislingEntityAdded(packet);
        
        // Self-tracking and leader tracking are valid use-cases for AislingManager
        packet.Player.AislingManager.AddAisling(aisling.Entity);
        
        if (packet.Player.Leader != null && aisling.Entity.Serial == packet.Player.Leader.PacketId)
        {
            // The leader will handle its own tracking for us.
            packet.Handled = true;
            return;
        }
        
        if (aisling.Entity.Serial == packet.Player.PacketId)
        {
            packet.Handled = true;
            return; // We don't really need to see when the player is added to their own map
        }
        
        var added = ConsoleOutputExtension.ColorText("AISLING ADDED", ConsoleColor.Green);
        Console.WriteLine($"{added}   " + packet);
        packet.Player.Location.CurrentMap.AddEntity(aisling.Entity);
        packet.Handled = true;
    }

    private void HandleEntitiesAdded(ServerPacket packet)
    {
        // var added = ConsoleOutputExtension.ColorText("ENTITY ADDED", ConsoleColor.Green);
        // Console.WriteLine($"{added}    " + packet);
        var entities = new EntitiesAdded(packet);
        
        packet.Player.Location.CurrentMap.AddEntities(entities.Entities);
        packet.Handled = true;
    }
    
    private void HandleEntityMoved(ServerPacket packet)
    {
        var entityMoved = new EntityMoved(packet);
        var entity = entityMoved.Entity;
        if (packet.Player.Leader != null && entity.Serial == packet.Player.Leader.PacketId)
        {
            // The leader will handle its own tracking for us.
            packet.Handled = true;
            return;
        }

        if (!packet.Player.AislingManager.UpdateAisling(entity))
        {
            // This is an entity... likely a monster.
            // TODO: Handle entity tracking for monsters
        }
        
        var serial = entity.Serial;
        var prevX = entity.PreviousX;
        var prevY = entity.PreviousY;
        var newX = entity.X;
        var newY = entity.Y;
        var direction = entity.Direction;
        // Console.WriteLine($"ENTITY MOVED: {entityMoved.Serial} ({newX}, {newY})");
        // var moved = ConsoleOutputExtension.ColorText("ENTITY MOVED", ConsoleColor.Yellow);
        // Console.WriteLine($"{moved}    " + packet);
        
        packet.Player.Location.CurrentMap.EntityMoved(serial, prevX, prevY, newX, newY, direction);
        packet.Handled = true;
    }
    
    private void HandleEntityRemoved(ServerPacket packet)
    {
        var entityRemoved = new EntityRemoved(packet);
        
        packet.Player.AislingManager.HideAisling(entityRemoved.Serial);
        
        if (packet.Player.Leader != null && entityRemoved.Serial == packet.Player.Leader.PacketId)
        {
            // The leader will handle its own tracking for us.
            packet.Handled = true;
            return;
        }
        
        packet.Player.Location.CurrentMap.RemoveEntityBySerial(entityRemoved.Serial);
        // var removed = ConsoleOutputExtension.ColorText("ENTITY REMOVED", ConsoleColor.Red);
        // Console.WriteLine($"{removed}  " + packet);
        packet.Handled = true;
    }
}