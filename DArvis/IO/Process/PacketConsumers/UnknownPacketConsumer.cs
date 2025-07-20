using System;
using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class UnknownPacketConsumer : PacketConsumer
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        var unknownPacketTypes = new[]
        {
            ServerPacket.PacketType.UnknownPacket00,
            ServerPacket.PacketType.UnknownPacket01,
            ServerPacket.PacketType.UnknownPacket02,
            ServerPacket.PacketType.UnknownPacket03,
            ServerPacket.PacketType.UnknownPacket05,
            ServerPacket.PacketType.UnknownPacket06,
            ServerPacket.PacketType.UnknownPacket08,
            ServerPacket.PacketType.UnknownPacket09,
            ServerPacket.PacketType.UnknownPacket0F,
            ServerPacket.PacketType.UnknownPacket10,
            ServerPacket.PacketType.UnknownPacket12,
            ServerPacket.PacketType.UnknownPacket13,
            ServerPacket.PacketType.UnknownPacket14,
            ServerPacket.PacketType.UnknownPacket16,
            ServerPacket.PacketType.UnknownPacket17,
            ServerPacket.PacketType.UnknownPacket18,
            ServerPacket.PacketType.UnknownPacket19,
            ServerPacket.PacketType.UnknownPacket1B,
            ServerPacket.PacketType.UnknownPacket1C,
            ServerPacket.PacketType.UnknownPacket1D,
            ServerPacket.PacketType.UnknownPacket1E,
            ServerPacket.PacketType.UnknownPacket1F,
            ServerPacket.PacketType.UnknownPacket20,
            ServerPacket.PacketType.UnknownPacket21,
            ServerPacket.PacketType.UnknownPacket22,
            ServerPacket.PacketType.UnknownPacket23,
            ServerPacket.PacketType.UnknownPacket24,
            ServerPacket.PacketType.UnknownPacket25,
            ServerPacket.PacketType.UnknownPacket26,
            ServerPacket.PacketType.UnknownPacket27,
            ServerPacket.PacketType.UnknownPacket28,
            ServerPacket.PacketType.Animation,
            ServerPacket.PacketType.UnknownPacket2A,
            ServerPacket.PacketType.UnknownPacket2B,
            ServerPacket.PacketType.UnknownPacket2C,
            ServerPacket.PacketType.UnknownPacket2D,
            ServerPacket.PacketType.UnknownPacket2E,
            ServerPacket.PacketType.UnknownPacket2F,
            ServerPacket.PacketType.UnknownPacket30,
            ServerPacket.PacketType.UnknownPacket31,
            ServerPacket.PacketType.UnknownPacket32,
            ServerPacket.PacketType.ProfileData,
            ServerPacket.PacketType.UnknownPacket35,
            ServerPacket.PacketType.UnknownPacket36,
            ServerPacket.PacketType.UnknownPacket37,
            ServerPacket.PacketType.UnknownPacket38,
            ServerPacket.PacketType.ProfileRequested,
            ServerPacket.PacketType.UnknownPacket3B,
            ServerPacket.PacketType.UnknownPacket3F,
            ServerPacket.PacketType.UnknownPacket49,
            ServerPacket.PacketType.UnknownPacket58,
            ServerPacket.PacketType.UnknownPacket60,
            ServerPacket.PacketType.GroupRequest,
            ServerPacket.PacketType.UnknownPacket66,
            ServerPacket.PacketType.UnknownPacket67,
            ServerPacket.PacketType.UnknownPacket6F,
            ServerPacket.PacketType.UnknownPacket7E,
        };

        return unknownPacketTypes.Contains(serverPacket.Type);
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        if (
            serverPacket.Type == ServerPacket.PacketType.UnknownPacket08 // Unknown
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket1E // Unknown
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket1F // Unknown - map change related
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket19 // Unknown - data just has 01
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket20 // Unknown
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket22 // Unknown - empty data
            || serverPacket.Type == ServerPacket.PacketType.Animation // Seems to be particle animation (fireworks)
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket30 // Unknown - data has 0A-00
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket32 // Seems to be a generic response packet
            || serverPacket.Type == ServerPacket.PacketType.ProfileRequested // TODO: add handler for this packet - tells us someone is viewing our profile
            || serverPacket.Type == ServerPacket.PacketType.ProfileData // Data from viewing someone's profile
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket3B // Seems to be a heartbeat of some type
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket3F // Unknown - contains 7 bytes of data
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket58 // Unknown - map change related
            || serverPacket.Type == ServerPacket.PacketType.GroupRequest // Player has requested to join your group
            || serverPacket.Type == ServerPacket.PacketType.UnknownPacket67 // Unknown - map change related
        )
        {
            serverPacket.Handled = true;
        } 
    }
}