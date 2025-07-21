using System;
using System.Linq;
using DArvis.Extensions;
using DArvis.Models;
using ConsoleColor = DArvis.Extensions.ConsoleColor;

namespace DArvis.IO.Packet;

public class ClientPacket(byte[] data, Player player) : Packet<ClientPacket.ClientEvent>(data, player)
{
    protected override ClientEvent GetUnknownEvent() => ClientEvent.Unknown;
    
    public override string ToString()
    {
        var player = Player.Name == null ? Player.Process.ProcessId.ToString() : Player.Name;
        var packetIdBytes = BitConverter.GetBytes(Player.PacketId).Reverse().ToArray();
        var defaultPlayerId = "00-00-00-00";
        var playerId = Player.PacketId == 0 ? defaultPlayerId : BitConverter.ToString(packetIdBytes);

        var packetData = BitConverter.ToString(Data);
        var packetString = $"c>> [{player,-12}][{playerId}] {packetData}";
        if (packetData.Contains(playerId) && playerId != defaultPlayerId)
        {
            packetString = packetString.Replace(playerId, ConsoleOutputExtension.ColorText(playerId, ConsoleColor.Blue));
        }

        return packetString;
    }
    
    public enum ClientEvent
    {
        Unknown = -1,
        StepCounter = 0x06,
        Unknown0C = 0x0C,
        Say = 0x0E,
        Turn = 0x11,
        Unknown13 = 0x13,
        Chat = 0x19,
        Unknown2D = 0x38,
        Unknown38 = 0x38,
        Click = 0x43,
        Unknown45 = 0x45,
    }
}