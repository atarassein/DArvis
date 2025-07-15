using DArvis.DTO;
using DArvis.Services.Logging;

namespace DArvis.IO.Process;

public interface IPacketConsumer
{
    bool CanConsume(Packet packet);
    void ProcessPacket(Packet packet);
}

public abstract class PacketConsumer : IPacketConsumer
{
    protected readonly ILogger logger;

    protected PacketConsumer()
    {
        logger = App.Current.Services.GetService<ILogger>();
        
        // Auto-register with the packet manager
        PacketManager.Instance.RegisterConsumer(this);
    }

    public abstract bool CanConsume(Packet packet);
    public abstract void ProcessPacket(Packet packet);

}