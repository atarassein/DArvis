using System;
using System.Linq;
using System.Text;
using DArvis.Extensions;
using DArvis.Models;
using ConsoleColor = DArvis.Extensions.ConsoleColor;

namespace DArvis.IO.Packet;

public class ServerPacket(byte[] data, Player player) : Packet<ServerPacket.ServerEvent>(data, player)
{
    
    protected override ServerEvent GetUnknownEvent() => ServerEvent.Unknown;

    public override string ToString()
    {
        var player = Player.Name == null ? Player.Process.ProcessId.ToString() : Player.Name;
        var packetIdBytes = BitConverter.GetBytes(Player.PacketId).Reverse().ToArray();
        var playerId = Player.PacketId == 0 ? "00-00-00-00" : BitConverter.ToString(packetIdBytes);

        var packetData = BitConverter.ToString(Data);
        var packetString = $"<<s [{player,-12}][{playerId}] {packetData}";
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
        UnknownCharacterData01 = 0x03,
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
        InventoryPacket = 0x0F,
        UnknownPacket10 = 0x10,
        PlayerChangedDirection = 0x11,
        UnknownPacket12 = 0x12,
        UnknownPacket13 = 0x13,
        UnknownPacket14 = 0x14,
        MapChanged = 0x15,
        UnknownPacket16 = 0x16,
        LearnedSpells = 0x17,
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
        LearnedSkills = 0x2C,
        UnknownPacket2D = 0x2D,
        MapSelection = 0x2E, // ie 2E-08-66-69-65-6C-64-30-30-31-08-00-01-33-00-4D-04-41-62-65-6C-00-00-0B-C6-00-0E-00-0E-01-03-00-57-06-4D-69-6C-65-74-68-00-00-0C-07-00-01-00-01-01-01-00-74-0D-45-61-73-74-20-57-6F-6F-64-6C-61-6E-64-00-00-0C-08-00-05-00-01-00-DC-00-64-0D-57-65-73-74-20-57-6F-6F-64-6C-61-6E-64-00-00-01-C1-00-31-00-1A-01-17-00-8A-0B-50-72-61-76-61-74-20-43-61-76-65-00-00-0B-EC-00-1A-00-18-02-00-01-45-11-42-6C-61-63-6B-73-74-61-72-20-56-69-6C-6C-61-67-65-00-00-0C-8A-00-33-00-52-01-1D-00-64-0E-4D-69-6C-65-74-68-20-43-6F-6C-6C-65-67-65-00-00-0B-DD-00-05-00-05-00-F7-00-92-0E-4B-61-73-6D-61-6E-69-75-6D-20-4D-69-6E-65-00-00-02-94-00-19-00-2E
        UnknownPacket2F = 0x2F,
        UnknownPacket30 = 0x30,
        UnknownPacket31 = 0x31,
        UnknownPacket32 = 0x32, // Seems to be a generic response packet
        AislingAdded = 0x33,
        ProfileData = 0x34,
        UnknownPacket35 = 0x35,
        UnknownPacket36 = 0x36,
        EquippedItem = 0x37,
        UnknownPacket38 = 0x38,
        ProfileRequested = 0x39,
        SpellBuffExpiration = 0x3A, // Spell buff expiration
        UnknownPacket3B = 0x3B, // Seems to be a heartbeat of some type
        UnknownPacket3F = 0x3F,
        MapData = 0x3C,
        UnknownPacket49 = 0x49,
        UnknownPacket4F = 0x4F, // Unknown - possibly jfif
        UnknownPacket58 = 0x58, // Unknown - map change related
        UnknownPacket60 = 0x60,
        GroupRequest = 0x63,
        UnknownPacket66 = 0x66,
        UnknownPacket67 = 0x67, // Unknown - map change related
        UnknownCharacterData = 0x6F,
        UnknownPacket7E = 0x7E,
    }
}