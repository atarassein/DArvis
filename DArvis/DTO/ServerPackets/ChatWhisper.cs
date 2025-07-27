using System;
using DArvis.IO.Packet;
using DArvis.Models;

namespace DArvis.DTO.ServerPackets;

public class ChatWhisper
{

    public string? SenderName { get; private set; }
    public String Message { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }

    private Player Target;
    
    public ChatWhisper(Packet packet)
    {
        Target = packet.Player;
        var buffer = packet.Buffer;
        var message = buffer.ReadString(4, buffer.Data.Length - 4);
        var senderNameEndIndex = message.IndexOf("\" ", StringComparison.Ordinal);
        if (senderNameEndIndex != -1)
        {
            SenderName = message.Substring(0, senderNameEndIndex);
            Message = message.Substring(senderNameEndIndex + 2);
            Timestamp = DateTime.Now;
        }
    }

    public bool IsWhisper()
    {
        return SenderName != null && Message != string.Empty;
    }
    
    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss}] {SenderName} -> {Target.Name}: {Message}";
    }
}