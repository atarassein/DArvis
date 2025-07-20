using System;
using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class PlayerMovementPacketConsumer : PacketConsumer
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        var playerMovementPacketTypes = new[]
        {
            ServerPacket.PacketType.PlayerMoved,
            ServerPacket.PacketType.PlayerChangedDirection,
            ServerPacket.PacketType.PlayerLocationChanged
        };
        
        return playerMovementPacketTypes.Contains(serverPacket.Type);
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        switch (serverPacket.Type)
        {
            case ServerPacket.PacketType.PlayerMoved:
                HandlePlayerMovement(serverPacket);
                break;
            case ServerPacket.PacketType.PlayerChangedDirection:
                HandlePlayerDirectionChange(serverPacket);
                break;
            case ServerPacket.PacketType.PlayerLocationChanged:
                HandlePlayerLocationChange(serverPacket);
                break;
        }
    }

    private void HandlePlayerMovement(ServerPacket serverPacket)
    {
        var playerMoved = new PlayerMoved(serverPacket);
        var player = serverPacket.Player;
        // these values will be updated from memory
        //player.Location.X = playerMoved.X;
        //player.Location.Y = playerMoved.Y;
        player.Location.Direction = playerMoved.Direction;
        serverPacket.Handled = true;
    }

    private void HandlePlayerDirectionChange(ServerPacket serverPacket)
    {
        var playerChangedDirection = new PlayerChangedDirection(serverPacket);
        
        if (serverPacket.Player.PacketId == playerChangedDirection.PacketId)
        {
            serverPacket.Player.Location.Direction = playerChangedDirection.Direction;
        }
        
        serverPacket.Handled = true;
    }

    private void HandlePlayerLocationChange(ServerPacket serverPacket)
    {
        //var playerChangedLocation = new PlayerChangedLocation(packet);
        // these values will be updated from memory
        //packet.Player.Location.X = playerChangedLocation.X;
        //packet.Player.Location.Y = playerChangedLocation.Y;
        serverPacket.Handled = true;
    }
}