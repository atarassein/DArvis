using System;
using System.Text;
using DArvis.Models;

namespace DArvis.DTO;

public abstract class Packet<TEventType>(byte[] data, Player player) : Packet(data, player)
    where TEventType : Enum
{
    public TEventType EventType
    {
        get
        {
            if (Data.Length == 0)
                return GetUnknownEvent();

            var byteValue = Data[0];
            
            if (Enum.IsDefined(typeof(TEventType), (int)byteValue))
                return (TEventType)Enum.ToObject(typeof(TEventType), byteValue);

            return GetUnknownEvent();
        }
    }

    protected abstract TEventType GetUnknownEvent();
}

public abstract class Packet(byte[] data, Player player)
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
    
    public Player Player { get; set; } = player;

    public byte[] Data { get; set; } = data;

    public PacketBuffer Buffer => new(data);
    
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

}