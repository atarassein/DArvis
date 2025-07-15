using System;
using DArvis.Models;

namespace DArvis.DTO;

public class Packet(byte[] data, Packet.PacketSource source, Player player)
{
    public byte[] Data { get; set; } = data;

    public PacketSource Source { get; set; } = source;

    private PacketType _type;
    public PacketType Type
    {
        get
        {
            if (Data.Length == 0)
                return PacketType.Unknown;

            if (Enum.IsDefined(typeof(PacketType), (int)Data[0]))
                return (PacketType)Data[0];

            return PacketType.Unknown;
        }
        set
        {
            
        }
    }
    
    public Player Player { get; set; } = player;
    
    public override string ToString()
    {
        var player = Player.Name == null ? Player.Process.ProcessId.ToString() : Player.Name;
        return $"[{Source}] ({player}) {BitConverter.ToString(Data)}";
    }
    
    public enum PacketSource    
    {
        Unknown = 0,
        Server = 1,
        Client = 2
    }

    public enum PacketType
    {
        Unknown = -1,
        UnknownPacket00 = 0x00,
        UnknownPacket01 = 0x01,
        UnknownPacket02 = 0x02,
        UnknownPacket03 = 0x03,
        PlayerLocationChanged = 0x04,
        UnknownPacket05 = 0x05,
        UnknownPacket06 = 0x06,
        UnknownPacket07 = 0x07,
        UnknownPacket08 = 0x08,
        UnknownPacket09 = 0x09,
        Message = 0x0A,
        PlayerMoved = 0x0B,
        ObjectMoved = 0x0C,
        Chat = 0x0D,
        ObjectRemoved = 0x0E,
        UnknownPacket0F = 0x0F,
        UnknownPacket10 = 0x10,
        PlayerChangedDirection = 0x11,
        UnknownPacket11 = 0x11,
        UnknownPacket12 = 0x12,
        UnknownPacket13 = 0x13,
        UnknownPacket14 = 0x14,
        MapChanged = 0x15,
        UnknownPacket16 = 0x16,
        UnknownPacket17 = 0x17,
        UnknownPacket18 = 0x18,
        UnknownPacket19 = 0x19,
        UnknownPacket1A = 0x1A,
        UnknownPacket1B = 0x1B,
        UnknownPacket1C = 0x1C,
        UnknownPacket1D = 0x1D,
        UnknownPacket1E = 0x1E,
        UnknownPacket1F = 0x1F,
        UnknownPacket20 = 0x20,
        UnknownPacket21 = 0x21,
        UnknownPacket22 = 0x22,
        UnknownPacket23 = 0x23,
        UnknownPacket24 = 0x24,
        UnknownPacket25 = 0x25,
        UnknownPacket26 = 0x26,
        UnknownPacket27 = 0x27,
        UnknownPacket28 = 0x28,
        UnknownPacket29 = 0x29,
        UnknownPacket2A = 0x2A,
        UnknownPacket2B = 0x2B,
        UnknownPacket2C = 0x2C,
        UnknownPacket2D = 0x2D,
        UnknownPacket2E = 0x2E,
        UnknownPacket2F = 0x2F,
        UnknownPacket30 = 0x30,
        UnknownPacket31 = 0x31,
        UnknownPacket32 = 0x32,
        UnknownPacket33 = 0x33,
        UnknownPacket34 = 0x34,
        UnknownPacket35 = 0x35,
        UnknownPacket36 = 0x36,
        UnknownPacket37 = 0x37,
        UnknownPacket38 = 0x38,
        UnknownPacket39 = 0x39,
        UnknownPacket3A = 0x3A,
        UnknownPacket3B = 0x3B,
        UnknownPacket49 = 0x49,
        UnknownPacket58 = 0x58,
        UnknownPacket60 = 0x60,
        UnknownPacket66 = 0x66,
        UnknownPacket6F = 0x6F,
        UnknownPacket7E = 0x7E,
    }
}