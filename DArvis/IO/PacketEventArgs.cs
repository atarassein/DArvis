using System;
using DArvis.DTO;

namespace DArvis.IO;

public delegate void PacketEventHandler(object sender, PacketEventArgs e);

public sealed class PacketEventArgs : EventArgs
{
    public ServerPacket ServerPacket { get; }

    public PacketEventArgs(ServerPacket serverPacket)
    {
        ServerPacket = serverPacket ?? throw new ArgumentNullException(nameof(serverPacket));
    }

    public override string ToString() => "Packet Event: " + ServerPacket.GetType().Name;
}