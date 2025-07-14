using System;

namespace DArvis.Settings
{
    public delegate void ClientVersionEventHandler(object sender, ClientVersionEventArgs e);

    public sealed class ClientVersionEventArgs : EventArgs
    {
        public ClientVersion Version { get; }

        public ClientVersionEventArgs(ClientVersion version)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }
    }
}
