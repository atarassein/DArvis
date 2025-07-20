using System;
using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class PlayerMovementPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        var playerMovementPacketTypes = new[]
        {
            Packet.PacketType.PlayerMoved,
            Packet.PacketType.PlayerChangedDirection,
            Packet.PacketType.PlayerLocationChanged
        };
        
        return playerMovementPacketTypes.Contains(packet.Type);
    }

    public override void ProcessPacket(Packet packet)
    {
        switch (packet.Type)
        {
            case Packet.PacketType.PlayerMoved:
                HandlePlayerMovement(packet);
                break;
            case Packet.PacketType.PlayerChangedDirection:
                HandlePlayerDirectionChange(packet);
                break;
            case Packet.PacketType.PlayerLocationChanged:
                HandlePlayerLocationChange(packet);
                break;
        }
    }

    private void HandlePlayerMovement(Packet packet)
    {
        var playerMoved = new PlayerMoved(packet);
        var player = packet.Player;
        //player.Location.X = playerMoved.X;
        //player.Location.Y = playerMoved.Y;
        player.Location.Direction = playerMoved.Direction;
        packet.Handled = true;
    }

    private void HandlePlayerDirectionChange(Packet packet)
    {
        var playerChangedDirection = new PlayerChangedDirection(packet);
        
        if (packet.Player.PacketId == playerChangedDirection.PacketId)
        {
            packet.Player.Location.Direction = playerChangedDirection.Direction;
        }
        
        packet.Handled = true;
    }

    private void HandlePlayerLocationChange(Packet packet)
    {
        var playerChangedLocation = new PlayerChangedLocation(packet);
        //packet.Player.Location.X = playerChangedLocation.X;
        //packet.Player.Location.Y = playerChangedLocation.Y;
        packet.Handled = true;
    }
}