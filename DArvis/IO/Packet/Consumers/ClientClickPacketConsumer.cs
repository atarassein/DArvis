using System;

namespace DArvis.IO.Packet.Consumers;

public class ClientClickPacketConsumer : PacketConsumer<ClientPacket>
{
    public override bool CanConsume(ClientPacket packet)
    {
        return packet.EventType == ClientPacket.ClientEvent.Click; // not sure what this is, it fires off when clicking around the character
    }

    public override void ProcessPacket(ClientPacket packet)
    {
        var buffer = packet.Buffer;
        var unknown = buffer.ReadByte();
        var x = buffer.ReadInt16();
        var y = buffer.ReadInt16();
        // Console.WriteLine($"click: {unknown}? ({x},{y}) {buffer.ReadByte()}?");
        
        packet.Handled = true;
    }

}