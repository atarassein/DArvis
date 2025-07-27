using DArvis.Services.Logging;

namespace DArvis.IO.Packet;

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
    }

    public abstract bool CanConsume(Packet packet);
    public abstract void ProcessPacket(Packet packet);
}

public abstract class PacketConsumer<T> : PacketConsumer where T : Packet
{
    public override bool CanConsume(Packet packet)
    {
        return packet is T typedPacket && CanConsume(typedPacket);
    }

    public override void ProcessPacket(Packet packet)
    {
        if (packet is T typedPacket)
        {
            ProcessPacket(typedPacket);
        }
    }

    public abstract bool CanConsume(T packet);
    public abstract void ProcessPacket(T packet);
}