using System;

namespace DArvis.IO;

public delegate void PacketEventHandler(object sender, PacketEventArgs e);

public sealed class PacketEventArgs : EventArgs
{
    public Packet Packet { get; }

    public PacketEventArgs(Packet packet)
    {
        Packet = packet ?? throw new ArgumentNullException(nameof(packet));
    }

    public override string ToString() => "Packet Event: " + Packet.GetType().Name;
}