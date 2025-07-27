using System.Linq;

namespace DArvis.IO.Packet.Consumers;

public class UnknownServerPacketConsumer : PacketConsumer<ServerPacket>
{
    public override bool CanConsume(ServerPacket serverPacket)
    {
        var unknownEvents = new[]
        {
            ServerPacket.ServerEvent.UnknownPacket00,
            ServerPacket.ServerEvent.UnknownPacket01,
            ServerPacket.ServerEvent.UnknownPacket02,
            ServerPacket.ServerEvent.UnknownCharacterData01,
            ServerPacket.ServerEvent.UnknownPacket05,
            ServerPacket.ServerEvent.UnknownPacket06,
            ServerPacket.ServerEvent.UnknownPacket08,
            ServerPacket.ServerEvent.UnknownPacket09,
            ServerPacket.ServerEvent.UnknownPacket10,
            ServerPacket.ServerEvent.UnknownPacket12,
            ServerPacket.ServerEvent.UnknownPacket13,
            ServerPacket.ServerEvent.UnknownPacket14,
            ServerPacket.ServerEvent.UnknownPacket16,
            ServerPacket.ServerEvent.LearnedSpells,
            ServerPacket.ServerEvent.UnknownPacket18,
            ServerPacket.ServerEvent.UnknownPacket19,
            ServerPacket.ServerEvent.UnknownPacket1B,
            ServerPacket.ServerEvent.UnknownPacket1C,
            ServerPacket.ServerEvent.UnknownPacket1D,
            ServerPacket.ServerEvent.UnknownPacket1E,
            ServerPacket.ServerEvent.UnknownPacket1F,
            ServerPacket.ServerEvent.UnknownPacket20,
            ServerPacket.ServerEvent.UnknownPacket21,
            ServerPacket.ServerEvent.UnknownPacket22,
            ServerPacket.ServerEvent.UnknownPacket23,
            ServerPacket.ServerEvent.UnknownPacket24,
            ServerPacket.ServerEvent.UnknownPacket25,
            ServerPacket.ServerEvent.UnknownPacket26,
            ServerPacket.ServerEvent.UnknownPacket27,
            ServerPacket.ServerEvent.UnknownPacket28,
            ServerPacket.ServerEvent.Animation,
            ServerPacket.ServerEvent.UnknownPacket2A,
            ServerPacket.ServerEvent.UnknownPacket2B,
            ServerPacket.ServerEvent.LearnedSkills,
            ServerPacket.ServerEvent.UnknownPacket2D,
            ServerPacket.ServerEvent.UnknownPacket2F,
            ServerPacket.ServerEvent.UnknownPacket30,
            ServerPacket.ServerEvent.UnknownPacket31,
            ServerPacket.ServerEvent.UnknownPacket32,
            ServerPacket.ServerEvent.ProfileData,
            ServerPacket.ServerEvent.UnknownPacket35,
            ServerPacket.ServerEvent.UnknownPacket36,
            ServerPacket.ServerEvent.UnknownPacket38,
            ServerPacket.ServerEvent.ProfileRequested,
            ServerPacket.ServerEvent.UnknownPacket3B,
            ServerPacket.ServerEvent.UnknownPacket3F,
            ServerPacket.ServerEvent.UnknownPacket49,
            ServerPacket.ServerEvent.UnknownPacket4F,
            ServerPacket.ServerEvent.UnknownPacket58,
            ServerPacket.ServerEvent.UnknownPacket60,
            ServerPacket.ServerEvent.GroupRequest,
            ServerPacket.ServerEvent.UnknownPacket66,
            ServerPacket.ServerEvent.UnknownPacket67,
            ServerPacket.ServerEvent.UnknownCharacterData,
            ServerPacket.ServerEvent.UnknownPacket7E,
        };

        return unknownEvents.Contains(serverPacket.EventType);
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
        if (
            serverPacket.EventType == ServerPacket.ServerEvent.LearnedSpells // see notes, this is potentially valuable data that is received upon logging in
            || serverPacket.EventType == ServerPacket.ServerEvent.LearnedSkills // valuable, just no use yet
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownCharacterData // see notes, this is potentially valuable data that is received upon logging in
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownCharacterData01 // contains character name and other data, potentially valuable
            )
        {
            serverPacket.Handled = true;
            return;
        }
        if (
            serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket08 // Unknown
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket1E // Unknown
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket1F // Unknown - map change related
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket19 // Unknown - data just has 01
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket20 // Unknown
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket22 // Unknown - empty data
            || serverPacket.EventType == ServerPacket.ServerEvent.Animation // Seems to be particle animation (fireworks)
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket30 // Unknown - data has 0A-00
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket32 // Seems to be a generic response packet
            || serverPacket.EventType == ServerPacket.ServerEvent.ProfileRequested // TODO: add handler for this packet - tells us someone is viewing our profile
            || serverPacket.EventType == ServerPacket.ServerEvent.ProfileData // Data from viewing someone's profile
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket3B // Seems to be a heartbeat of some type
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket3F // Unknown - contains 7 bytes of data
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket4F // Unknown - possibly jfif
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket58 // Unknown - map change related
            || serverPacket.EventType == ServerPacket.ServerEvent.GroupRequest // Player has requested to join your group
            || serverPacket.EventType == ServerPacket.ServerEvent.UnknownPacket67 // Unknown - map change related
        )
        {
            serverPacket.Handled = true;
        } 
    }
}