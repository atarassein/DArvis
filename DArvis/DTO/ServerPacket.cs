using System;
using System.Linq;
using System.Text;
using DArvis.Extensions;
using DArvis.Models;
using ConsoleColor = DArvis.Extensions.ConsoleColor;

namespace DArvis.DTO;

public class ServerPacket(byte[] data, ServerPacket.PacketSource source, Player player)
{
    public class PacketBuffer
    {
        public byte[] Data;
        public int bufferIndex = 1; // Start at 1 to skip the packet type byte
        
        public PacketBuffer(byte[] data)
        {
            Data = data;
        }

        public void resetBuffer()
        {
            bufferIndex = 1;
        }
        
        public byte ReadByte()
        {
            if (bufferIndex < Data.Length)
            {
                return Data[bufferIndex++];
            }
            throw new IndexOutOfRangeException("Packet does not contain expected byte");
        }
        
        public short ReadInt16()
        {
            if (bufferIndex + 1 < Data.Length)
            {
                return (short)(Data[bufferIndex++] << 8 | Data[bufferIndex++]);
            }
            throw new IndexOutOfRangeException("Packet does not contain expected Int16");
        }
        
        public ushort ReadUInt16()
        {
            if (bufferIndex + 1 < Data.Length)
            {
                return (ushort)(Data[bufferIndex++] << 8 | Data[bufferIndex++]);
            }
            throw new IndexOutOfRangeException("Packet does not contain expected UInt16");
        }
        
        public int ReadInt32()
        {
            if (bufferIndex + 3 < Data.Length)
            {
                return Data[bufferIndex++] << 24 | Data[bufferIndex++] << 16 | Data[bufferIndex++] << 8 |
                       Data[bufferIndex++];
            }
            throw new IndexOutOfRangeException("Packet does not contain expected Int32");
        }
        
        public string ReadString(int length)
        {
            if (bufferIndex + (length - 1) < Data.Length)
            {
                var buffer = new byte[length];
                System.Buffer.BlockCopy(Data, bufferIndex, buffer, 0, length);
                bufferIndex += length;
                return Encoding.GetEncoding(949).GetString(buffer);
            }
            throw new IndexOutOfRangeException("Packet does not contain expected string of length " + length);
        }
        
        public string ReadString8()
        {
            var length = ReadByte();
            return ReadString(length);
        }
    }
    
    public byte[] Data { get; set; } = data;

    public PacketBuffer Buffer
    {
        get
        {
            return new PacketBuffer(data);
        }
    }

    public PacketSource Source { get; set; } = source;

    private PacketType _type;
    
    public bool Handled = false;
    
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
    
    public int ReadInt16(int start = 0)
    {
        if (start + 1 > Data.Length)
            throw new IndexOutOfRangeException();
        
        return Data[start] << 8 | Data[++start];
    }
    
    public int ReadInt32(int start = 0)
    {
        if (start + 3 > Data.Length)
            throw new IndexOutOfRangeException();
        
        return Data[start] << 24 | Data[++start] << 16 | Data[++start] << 8 | Data[++start];
    }
    
    public string ReadString8(int start = 0)
    {
        var length = Data[start];
        return ReadString(++start, length);
    }
    
    public string ReadString(int start = 0, int length = 0)
    {
        if (start + length > Data.Length)
            throw new IndexOutOfRangeException();
        
        var buffer = new byte[length];
        System.Buffer.BlockCopy(Data, start, buffer, 0, length);
        return Encoding.GetEncoding(949).GetString(buffer);
    }
    
    public override string ToString()
    {
        var player = Player.Name == null ? Player.Process.ProcessId.ToString() : Player.Name;
        var packetIdBytes = BitConverter.GetBytes(Player.PacketId).Reverse().ToArray();
        var playerId = Player.PacketId == 0 ? "N/A" : BitConverter.ToString(packetIdBytes);

        var packetData = BitConverter.ToString(Data);
        var packetString = $"[{Source}] ({player} : {playerId}) {packetData}";
        if (packetData.Contains(playerId))
        {
            packetString = packetString.Replace(playerId, ConsoleOutputExtension.ColorText(playerId, ConsoleColor.Blue));
        }

        return packetString;
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