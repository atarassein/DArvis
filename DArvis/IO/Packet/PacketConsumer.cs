using DArvis.DTO;
using DArvis.Services.Logging;

namespace DArvis.IO.Packet;

public interface IPacketConsumer
{
    bool CanConsume(ServerPacket serverPacket);
    void ProcessPacket(ServerPacket serverPacket);
}

public abstract class PacketConsumer : IPacketConsumer
{
    protected readonly ILogger logger;

    protected PacketConsumer()
    {
        logger = App.Current.Services.GetService<ILogger>();
    }

    public abstract bool CanConsume(ServerPacket serverPacket);
    public abstract void ProcessPacket(ServerPacket serverPacket);

}