namespace DArvis;

using Shared;
using Components;

using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Windows;

class Program
{
    //60 Frames per second!
    static TimeSpan UpdateSpan { get; set; }

    //Track Frame Updates
    static DateTime LastUpdate { get; set; }

    //List of Updateable components, Add more here if needed.
    static List<UpdateableComponent> _components = new List<UpdateableComponent>();

    static Thread _updatingThread = null;
    static UnMainWindow _mainWindow = null;

    [STAThread]
    static void _Main(string[] args)
    {
        //Set a tick rate of 60 frames per second.
        UpdateSpan = TimeSpan.FromSeconds(1.0 / 60.0);

        //Initialize Components
        SetupComponents();

        //Update Frame
        _updatingThread = new Thread(DoUpdate) {IsBackground = true};
        _updatingThread.Start();

        // Create and run WPF application with MainWindow
        var app = new Application();
        _mainWindow = new UnMainWindow();
        app.Run(_mainWindow);
    }

    //This Function Sets up Components used for this bot.
    private static void SetupComponents()
    {
        var monitor = new ProcessMonitor();
        monitor.Attached += monitor_Attached;
        monitor.Removed += monitor_Removed;
        
        lock (_components)
        {
            _components.Add(monitor);
        }
    }

    //DA process was removed, unload all necessary resources.
    static void monitor_Removed(int pId, EventArgs e)
    {
        var client = Collections.AttachedClients[pId];
        client.CleanUpMememory();
        client.DestroyResources();
        Collections.AttachedClients.Remove(pId);
    }

    //DA Process was attached.
    static void monitor_Attached(int pId, EventArgs e)
    {
        //create a new client class for this DA Process
        var client = new Client(pId);

        //prepare DAvid.dll and inject it into the process.
        client.InitializeMemory(
            System.Diagnostics.Process.GetProcessById(pId),
            Path.Combine(Environment.CurrentDirectory, "DAvid.dll"));

        //Add to our Global collections dictionary.
        Collections.AttachedClients[pId] = client;

        InjectCodeStubs(client);
        
        Console.WriteLine($"Attached to Dark Ages client with PID: {client.ProcessId}");
    }

    public static void InjectCodeStubs(GameClient client)
    {
        if (client == null || client.Memory == null || !client.Memory.IsRunning)
            return;

        var mem = client.Memory;
        var offset = 0x697B;
        var send = mem.Read<int>(0x85C000, false) + offset;
        var payload = new byte[] { 0x13, 0x01 };
        var payload_length_arg =
            mem.Memory.Allocate(2);
        mem.Write(payload_length_arg.BaseAddress, (short)payload.Length, false);
        var payload_arg =
            mem.Memory.Allocate(sizeof(byte) * payload.Length);
        mem.Write(payload_arg.BaseAddress, payload, false);

        var asm = new []
        {
            "mov eax, " + payload_length_arg.BaseAddress,
            "push eax",
            "mov edx, " + payload_arg.BaseAddress,
            "push edx",
            "call " + send,
        };

        mem.Assembly.Inject(asm, 0x006FE000);
    }

    //this is updated 60 frames per second.
    //It's job is to update and elapsed components.
    static void DoUpdate()
    {
        while (true)
        {
            var delta = (DateTime.Now - LastUpdate);
            try
            {
                Update(delta);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
            }
            finally
            {
                LastUpdate = DateTime.Now;
                Thread.Sleep(UpdateSpan);
            }
        }
    }

    static void Update(TimeSpan elapsedTime)
    {
        lock (_components)
        {
            _components.ForEach(i => i.Update(elapsedTime));
        }

        //Update all attached clients in our collections dictionary, this will allow
        //any updateable components inside client to also update accordinaly to the elapsed frame.

        //copy memory here is deliberate!
        List<Client> copy;
        lock (Collections.AttachedClients)
            copy = new List<Client>(Collections.AttachedClients.Values);

        var clients = copy.ToArray();
        foreach (var c in clients)
            c.Update(elapsedTime);
    }
}