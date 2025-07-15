using System;
using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class UnknownPacketConsumer : PacketConsumer
{
    public override bool CanConsume(Packet packet)
    {
        var unknownPacketTypes = new[]
        {
            Packet.PacketType.UnknownPacket00,
            Packet.PacketType.UnknownPacket01,
            Packet.PacketType.UnknownPacket02,
            Packet.PacketType.UnknownPacket03,
            Packet.PacketType.UnknownPacket05,
            Packet.PacketType.UnknownPacket06,
            Packet.PacketType.UnknownPacket07,
            Packet.PacketType.UnknownPacket08,
            Packet.PacketType.UnknownPacket09,
            Packet.PacketType.UnknownPacket0F,
            Packet.PacketType.UnknownPacket10,
            Packet.PacketType.UnknownPacket11,
            Packet.PacketType.UnknownPacket12,
            Packet.PacketType.UnknownPacket13,
            Packet.PacketType.UnknownPacket14,
            Packet.PacketType.UnknownPacket16,
            Packet.PacketType.UnknownPacket17,
            Packet.PacketType.UnknownPacket18,
            Packet.PacketType.UnknownPacket19,
            Packet.PacketType.UnknownPacket1A,
            Packet.PacketType.UnknownPacket1B,
            Packet.PacketType.UnknownPacket1C,
            Packet.PacketType.UnknownPacket1D,
            Packet.PacketType.UnknownPacket1E,
            Packet.PacketType.UnknownPacket1F,
            Packet.PacketType.UnknownPacket20,
            Packet.PacketType.UnknownPacket21,
            Packet.PacketType.UnknownPacket22,
            Packet.PacketType.UnknownPacket23,
            Packet.PacketType.UnknownPacket24,
            Packet.PacketType.UnknownPacket25,
            Packet.PacketType.UnknownPacket26,
            Packet.PacketType.UnknownPacket27,
            Packet.PacketType.UnknownPacket28,
            Packet.PacketType.UnknownPacket29,
            Packet.PacketType.UnknownPacket2A,
            Packet.PacketType.UnknownPacket2B,
            Packet.PacketType.UnknownPacket2C,
            Packet.PacketType.UnknownPacket2D,
            Packet.PacketType.UnknownPacket2E,
            Packet.PacketType.UnknownPacket2F,
            Packet.PacketType.UnknownPacket30,
            Packet.PacketType.UnknownPacket31,
            Packet.PacketType.UnknownPacket32,
            Packet.PacketType.UnknownPacket33,
            Packet.PacketType.UnknownPacket34,
            Packet.PacketType.UnknownPacket35,
            Packet.PacketType.UnknownPacket36,
            Packet.PacketType.UnknownPacket37,
            Packet.PacketType.UnknownPacket38,
            Packet.PacketType.UnknownPacket39,
            Packet.PacketType.UnknownPacket3A,
            Packet.PacketType.UnknownPacket3B,
            Packet.PacketType.UnknownPacket49,
            Packet.PacketType.UnknownPacket58,
            Packet.PacketType.UnknownPacket60,
            Packet.PacketType.UnknownPacket66,
            Packet.PacketType.UnknownPacket6F,
            Packet.PacketType.UnknownPacket7E,
        };

        return unknownPacketTypes.Contains(packet.Type);
    }

    public override void ProcessPacket(Packet packet)
    {
        Console.WriteLine($"???[{packet.Type}]: {packet}");
    }
}