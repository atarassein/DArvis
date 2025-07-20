using System;
using System.Linq;
using System.Text;
using DArvis.Extensions;
using DArvis.Models;
using ConsoleColor = DArvis.Extensions.ConsoleColor;

namespace DArvis.DTO;

public class ServerPacket(byte[] data, Player player) : Packet<ServerPacket.ServerEvent>(data, player)
{
    
    public bool Handled = false;
    
    
    public Player Player { get; set; } = player;
    protected override ServerEvent GetUnknownEvent() => ServerEvent.Unknown;

    public override string ToString()
    {
        var player = Player.Name == null ? Player.Process.ProcessId.ToString() : Player.Name;
        var packetIdBytes = BitConverter.GetBytes(Player.PacketId).Reverse().ToArray();
        var playerId = Player.PacketId == 0 ? "00-00-00-00" : BitConverter.ToString(packetIdBytes);

        var packetData = BitConverter.ToString(Data);
        var packetString = $" ←s [{player,-12}][{playerId}] {packetData}";
        if (packetData.Contains(playerId))
        {
            packetString = packetString.Replace(playerId, ConsoleOutputExtension.ColorText(playerId, ConsoleColor.Blue));
        }

        return packetString;
    }
    
    public enum ServerEvent
    {
        Unknown = -1,
        UnknownPacket00 = 0x00,
        UnknownPacket01 = 0x01,
        UnknownPacket02 = 0x02,
        UnknownPacket03 = 0x03,
        PlayerLocationChanged = 0x04,
        UnknownPacket05 = 0x05,
        UnknownPacket06 = 0x06,
        EntitiesAdded = 0x07,
        UnknownPacket08 = 0x08, // Unknown
        UnknownPacket09 = 0x09,
        Message = 0x0A,
        PlayerMoved = 0x0B,
        EntityMoved = 0x0C,
        Chat = 0x0D,
        EntityRemoved = 0x0E,
        UnknownPacket0F = 0x0F,
        UnknownPacket10 = 0x10,
        PlayerChangedDirection = 0x11,
        UnknownPacket12 = 0x12,
        UnknownPacket13 = 0x13,
        UnknownPacket14 = 0x14,
        MapChanged = 0x15,
        UnknownPacket16 = 0x16,
        UnknownPacket17 = 0x17,
        UnknownPacket18 = 0x18,
        UnknownPacket19 = 0x19,
        PlayerAnimation = 0x1A,
        UnknownPacket1B = 0x1B,
        UnknownPacket1C = 0x1C,
        UnknownPacket1D = 0x1D,
        UnknownPacket1E = 0x1E, // Unknown
        UnknownPacket1F = 0x1F, // Unknown - map change related
        UnknownPacket20 = 0x20, // Unknown
        UnknownPacket21 = 0x21,
        UnknownPacket22 = 0x22,
        UnknownPacket23 = 0x23,
        UnknownPacket24 = 0x24,
        UnknownPacket25 = 0x25,
        UnknownPacket26 = 0x26,
        UnknownPacket27 = 0x27,
        UnknownPacket28 = 0x28,
        Animation = 0x29,
        UnknownPacket2A = 0x2A,
        UnknownPacket2B = 0x2B,
        UnknownPacket2C = 0x2C,
        UnknownPacket2D = 0x2D,
        UnknownPacket2E = 0x2E,
        UnknownPacket2F = 0x2F,
        UnknownPacket30 = 0x30,
        UnknownPacket31 = 0x31,
        UnknownPacket32 = 0x32, // Seems to be a generic response packet
        AislingAdded = 0x33,
        ProfileData = 0x34,
        UnknownPacket35 = 0x35,
        UnknownPacket36 = 0x36,
        UnknownPacket37 = 0x37,
        UnknownPacket38 = 0x38,
        ProfileRequested = 0x39,
        SpellBuffExpiration = 0x3A, // Spell buff expiration
        UnknownPacket3B = 0x3B, // Seems to be a heartbeat of some type
        UnknownPacket3F = 0x3F,
        MapData = 0x3C,
        UnknownPacket49 = 0x49,
        UnknownPacket58 = 0x58, // Unknown - map change related
        UnknownPacket60 = 0x60,
        GroupRequest = 0x63,
        UnknownPacket66 = 0x66,
        UnknownPacket67 = 0x67, // Unknown - map change related
        UnknownPacket6F = 0x6F,
        UnknownPacket7E = 0x7E,
    }
}