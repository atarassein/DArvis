using System.Linq;
using DArvis.DTO;

namespace DArvis.IO.Process.PacketConsumers;

public class SpellBuffPacketConsumer : PacketConsumer
{
    //Packet.Event.SpellBuffExpiration;
    public override bool CanConsume(ServerPacket serverPacket)
    {
        var objectEvents = new[]
        {
            ServerPacket.ServerEvent.SpellBuffExpiration,
        };
        
        return objectEvents.Contains(serverPacket.EventType);
    }

    public override void ProcessPacket(ServerPacket serverPacket)
    {
     
    }
    
    /// expiring packets
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-03-05-00
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-03-04-00
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-03-03-00
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-03-02-00
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-03-01-00
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-03-00-00

    /// begin spell
    ///[Client] (AmorFati : 00-0A-5C-D9) 4D-02-00-4D

    /// ???
    ///[Client] (AmorFati : 00-0A-5C-D9) 4E-07-61-72-6D-61-63-68-64-00-4E

    /// end spell
    ///[Client] (AmorFati : 00-0A-5C-D9) 0F-10-00-0B-EE-5D-00-03-00-09-00-0F

    /// buff timer
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-5E-06-00
    ///[Client] (Aternal : 00-0B-EE-5D) 45-35-35-00-45
    ///[Client] (AmorFati : 00-0A-5C-D9) 45-12-8E-00-45
    ///[Client] (Aternal : 00-0B-EE-5D) 45-4D-52-00-45
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-5E-05-00
    ///[Client] (AmorFati : 00-0A-5C-D9) 45-0E-F6-00-45
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-5E-04-00
    ///[Client] (Aternal : 00-0B-EE-5D) 45-7F-01-00-45
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-5E-03-00
    ///[Client] (AmorFati : 00-0A-5C-D9) 45-A6-C6-00-45
    ///[Client] (Aternal : 00-0B-EE-5D) 45-07-2F-00-45
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-5E-02-00
    ///[Client] (AmorFati : 00-0A-5C-D9) 45-19-50-00-45
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-5E-01-00
    ///[Client] (Aternal : 00-0B-EE-5D) 45-32-B2-00-45
    ///???[0x3A]: [Server] (Aternal : 00-0B-EE-5D) 3A-00-5E-00-00
    ///[Client] (AmorFati : 00-0A-5C-D9) 45-24-BF-00-45
    
}