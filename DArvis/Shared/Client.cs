using DArvis.Components;
using DArvis.IO;

namespace DArvis.Shared;

using System.Collections.Generic;
using DataHandlers;
using System;

[Serializable]
public class Client : GameClient
{
    public static event Action<Client> ClientAttached;
    
    public Client(int pId)
    {
        Client = this;

        //Add any Client Extenders here.
        Client.SpellBar = new List<short>();
        Client.LocalWorldUsers = new List<string>();
        Client.ProcessId = pId;
    }

    public new Client OnAttached()
    {
        //Initialize Map from Cache
        //Incoming.InitializeMapLoad(this);

        Console.WriteLine("Client.OnAttached()");
        AddServerHandlers();
        AddClientHandlers();
        PreparePrelims();

        base.OnAttached();
        return this;
    }

    private void AddClientHandlers()
    {
        AddClientHandler(0x03, Outgoing.Stub);
        AddClientHandler(0x0B, Outgoing.Stub);
        AddClientHandler(0x1C, Outgoing.Stub);
        AddClientHandler(0x0F, Outgoing.Stub);
        AddClientHandler(0x4D, Outgoing.Stub);
    }

    private void AddServerHandlers()
    {
        AddServerHandler(0x15, Incoming.Stub);
        AddServerHandler(0x04, Incoming.Stub);
        AddServerHandler(0x0B, Incoming.Stub);
        AddServerHandler(0x07, Incoming.Stub);
        AddServerHandler(0x0A, Incoming.Stub);
        AddServerHandler(0x0C, Incoming.Stub);
        AddServerHandler(0x0D, Incoming.Stub);
        AddServerHandler(0x0E, Incoming.Stub);
        AddServerHandler(0x11, Incoming.Stub);
        AddServerHandler(0x19, Incoming.Stub);
        AddServerHandler(0x1A, Incoming.Stub);
        AddServerHandler(0x29, Incoming.Stub);
        AddServerHandler(0x33, Incoming.Stub);
        AddServerHandler(0x3A, Incoming.Stub);
        AddServerHandler(0x39, Incoming.Stub);
        AddServerHandler(0x05, Incoming.Stub);
        AddServerHandler(0x06, Incoming.Stub);
        AddServerHandler(0x37, Incoming.Stub);
        AddServerHandler(0x08, Incoming.Stub);
        AddServerHandler(0x36, Incoming.Stub);
    }

    private void PreparePrelims()
    {
        ClientAttached?.Invoke(this);
    }

    //This is used to manage Auto Logging-In (If Enabled).
    internal void OnClientStateUpdated(bool transit)
    {
        if (ClientReady && !transit)
        {
            //Client.Active?.HardReset();
            Client.CleanUpMememory();
        }

        //Console.WriteLine("Client is ready.");
        ClientReady = transit;
    }
}