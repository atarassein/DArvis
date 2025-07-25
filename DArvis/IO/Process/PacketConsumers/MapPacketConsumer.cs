﻿using System;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using DArvis.DTO;
using DArvis.Extensions;
using DArvis.Models;
using ConsoleColor = DArvis.Extensions.ConsoleColor;

namespace DArvis.IO.Process.PacketConsumers;

public class MapPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        var objectPacketTypes = new[]
        {
            Packet.PacketType.MapChanged,
            Packet.PacketType.MapData,
            Packet.PacketType.AislingAdded,
            Packet.PacketType.EntitiesAdded,
            Packet.PacketType.EntityMoved,
            Packet.PacketType.EntityRemoved,
        };
        
        return objectPacketTypes.Contains(packet.Type);
    }

    public override void ProcessPacket(Packet packet)
    {
        if (packet.Type == Packet.PacketType.MapData)
        {
            // We don't need to do anything with MapData packets since we get map data from files
            packet.Handled = true;
            return;
        }

        if (packet.Type == Packet.PacketType.MapChanged)
        {
            HandleMapChange(packet);
        }

        if (!packet.Player.NeedsMapData())
        {
            packet.Handled = true;
            return;
        }
        
        if (packet.Type == Packet.PacketType.AislingAdded)
        {
            HandleAislingAdded(packet);
        }

        if (packet.Type == Packet.PacketType.EntitiesAdded)
        {
            HandleEntitiesAdded(packet);
        }
        
        if (packet.Type == Packet.PacketType.EntityMoved)
        {
            HandleEntityMoved(packet);
        }

        if (packet.Type == Packet.PacketType.EntityRemoved)
        {
            HandleEntityRemoved(packet);
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
    
    private void HandleAislingAdded(Packet packet)
    {
        var aisling = new AislingEntityAdded(packet);
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
        
        // var added = ConsoleOutputExtension.ColorText("AISLING ADDED", ConsoleColor.Green);
        // Console.WriteLine($"{added}   " + packet);
        packet.Player.Location.CurrentMap.AddEntity(aisling.Entity);
        packet.Handled = true;
    }

    private void HandleEntitiesAdded(Packet packet)
    {
        // var added = ConsoleOutputExtension.ColorText("ENTITY ADDED", ConsoleColor.Green);
        // Console.WriteLine($"{added}    " + packet);
        var entities = new EntitiesAdded(packet);
        
        packet.Player.Location.CurrentMap.AddEntities(entities.Entities);
        packet.Handled = true;
    }
    
    private void HandleEntityMoved(Packet packet)
    {
        var entityMoved = new EntityMoved(packet);
        if (packet.Player.Leader != null && entityMoved.Serial == packet.Player.Leader.PacketId)
        {
            // The leader will handle its own tracking for us.
            packet.Handled = true;
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
        
        packet.Player.Location.CurrentMap.EntityMoved(entityMoved.Serial, prevX, prevY, newX, newY, direction);
        packet.Handled = true;
    }
    
    private void HandleEntityRemoved(Packet packet)
    {
        var entityRemoved = new EntityRemoved(packet);
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