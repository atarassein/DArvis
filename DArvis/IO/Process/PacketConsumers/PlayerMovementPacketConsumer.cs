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
        }
    }

    private void HandlePlayerMovement(Packet packet)
    {
        var playerMoved = new PlayerMoved(packet);
        var player = packet.Player;
        player.Location.X = playerMoved.X;
        player.Location.Y = playerMoved.Y;
        player.Location.Direction = playerMoved.Direction;
        
        Console.WriteLine(player.Name + " moved to (" + player.Location.X + ", " + player.Location.Y + ") facing " + player.Location.Direction);
        packet.Handled = true;
    }

    private void HandlePlayerDirectionChange(Packet packet)
    {
        var playerChangedDirection = new PlayerChangedDirection(packet);
        
        if (packet.Player.PacketId == playerChangedDirection.PacketId)
        {
            packet.Player.Location.Direction = playerChangedDirection.Direction;
            Console.WriteLine("" + packet.Player.Name + " changed direction to " + playerChangedDirection.Direction);
        }
        
        packet.Handled = true;
    }
}