using DArvis.Models;
namespace DArvis.IO.Packet;

public class ClientPacket(byte[] data, Player player) : Packet<ClientPacket.ClientEvent>(data, player)
{
    protected override ClientEvent GetUnknownEvent() => ClientEvent.Unknown;
    
    public enum ClientEvent
    {
        Unknown = -1,
    }
}