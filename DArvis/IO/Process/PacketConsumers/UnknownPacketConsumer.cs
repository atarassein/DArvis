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
            Packet.PacketType.UnknownPacket08,
            Packet.PacketType.UnknownPacket09,
            Packet.PacketType.UnknownPacket0F,
            Packet.PacketType.UnknownPacket10,
            Packet.PacketType.UnknownPacket12,
            Packet.PacketType.UnknownPacket13,
            Packet.PacketType.UnknownPacket14,
            Packet.PacketType.UnknownPacket16,
            Packet.PacketType.UnknownPacket17,
            Packet.PacketType.UnknownPacket18,
            Packet.PacketType.UnknownPacket19,
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
            Packet.PacketType.Animation,
            Packet.PacketType.UnknownPacket2A,
            Packet.PacketType.UnknownPacket2B,
            Packet.PacketType.UnknownPacket2C,
            Packet.PacketType.UnknownPacket2D,
            Packet.PacketType.UnknownPacket2E,
            Packet.PacketType.UnknownPacket2F,
            Packet.PacketType.UnknownPacket30,
            Packet.PacketType.UnknownPacket31,
            Packet.PacketType.UnknownPacket32,
            Packet.PacketType.UnknownPacket34,
            Packet.PacketType.UnknownPacket35,
            Packet.PacketType.UnknownPacket36,
            Packet.PacketType.UnknownPacket37,
            Packet.PacketType.UnknownPacket38,
            Packet.PacketType.ProfileRequested,
            Packet.PacketType.UnknownPacket3A,
            Packet.PacketType.UnknownPacket3B,
            Packet.PacketType.UnknownPacket49,
            Packet.PacketType.UnknownPacket58,
            Packet.PacketType.UnknownPacket60,
            Packet.PacketType.UnknownPacket66,
            Packet.PacketType.UnknownPacket67,
            Packet.PacketType.UnknownPacket6F,
            Packet.PacketType.UnknownPacket7E,
        };

        return unknownPacketTypes.Contains(packet.Type);
    }

    public override void ProcessPacket(Packet packet)
    {
        if (
            packet.Type == Packet.PacketType.UnknownPacket08 // Unknown
            || packet.Type == Packet.PacketType.UnknownPacket1E // Unknown
            || packet.Type == Packet.PacketType.UnknownPacket1F // Unknown - map change related
            || packet.Type == Packet.PacketType.UnknownPacket19 // Unknown - data just has 01
            || packet.Type == Packet.PacketType.UnknownPacket20 // Unknown
            || packet.Type == Packet.PacketType.UnknownPacket22 // Unknown - empty data
            || packet.Type == Packet.PacketType.Animation // Seems to be particle animation (fireworks)
            || packet.Type == Packet.PacketType.UnknownPacket32 // Seems to be a generic response packet
            || packet.Type == Packet.PacketType.ProfileRequested // TODO: add handler for this packet
            || packet.Type == Packet.PacketType.UnknownPacket3B // Seems to be a heartbeat of some type
            || packet.Type == Packet.PacketType.UnknownPacket58 // Unknown - map change related
            || packet.Type == Packet.PacketType.UnknownPacket67 // Unknown - map change related
        )
        {
            packet.Handled = true;
        } 
    }
}