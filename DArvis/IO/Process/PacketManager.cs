using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Binarysharp.MemoryManagement;
using DArvis.DTO;
using DArvis.IO.Process.PacketConsumers;
using DArvis.Models;
using DArvis.Services.Logging;
using DArvis.Shared;

namespace DArvis.IO.Process
{
    public sealed class PacketManager
    {
        private static readonly PacketManager instance = new PacketManager();
        public static PacketManager Instance => instance;

        private readonly ILogger logger;
        
        private readonly List<IPacketConsumer> consumers = new();
        
        private readonly ConcurrentQueue<DTO.Packet> ServerPacketQueue = new();
        private static readonly object DispatcherLock = new();
        private static bool IsDispatching = false;
        
        private readonly ConcurrentQueue<Packet> ServerPacketInjectionQueue = new();
        private static readonly object ServerInjectionLock = new();
        private static bool IsInjectingToServer = false;
        
        private PacketManager()
        {
            logger = App.Current.Services.GetService<ILogger>();
            
            // Packet data contains Koren encoding, which is not supported by default in .NET Core.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
        
        public static void RegisterConsumers()
        {
            Instance.RegisterConsumer(new UnknownPacketConsumer());
            Instance.RegisterConsumer(new ObjectPacketConsumer());
            Instance.RegisterConsumer(new ChatPacketConsumer());
            Instance.RegisterConsumer(new PlayerMovementPacketConsumer());
            Instance.RegisterConsumer(new PlayerActionPacketConsumer());
            Instance.RegisterConsumer(new MapPacketConsumer());
        }
        
        public void RegisterConsumer(IPacketConsumer consumer)
        {
            consumers.Add(consumer);
        }

        public void UnregisterConsumer(IPacketConsumer consumer)
        {
            consumers.Remove(consumer);
        }

        
        /// <summary>
        /// Waits for new processes to be added and injects DAvid.dll into them.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnPlayerAdded(object sender, PlayerEventArgs e)
        {
            
            var dllInjectionWorker = new BackgroundWorker();
            dllInjectionWorker.DoWork += DoInjectCodeStubs;
            // send f5 after injection
            dllInjectionWorker.RunWorkerCompleted += (s, args) =>
            {
                if (e.Player.IsLoggedIn)
                {
                    GameActions.Refresh(e.Player);
                    GameActions.Refresh(e.Player); // Refresh twice to ensure all data is loaded
                }
            };
            dllInjectionWorker.RunWorkerAsync(e);
        }
        
        /// <summary>
        /// Injects DAvid.dll into the process of the player.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoInjectCodeStubs(object? sender, DoWorkEventArgs e)
        {
            var playerEventArgs = (PlayerEventArgs?)e.Argument;
            if (playerEventArgs == null)
                return;
            
            var pId = playerEventArgs.Player.Process.ProcessId;
            InjectDAvid(pId);
        }

        public static void InjectDAvid(int pId)
        {
            var process = System.Diagnostics.Process.GetProcessById(pId);
            var memory = new MemorySharp(process);
            
            memory.Write((IntPtr)DAStaticPointers.SendBuffer, 0, false);
            memory.Write((IntPtr)DAStaticPointers.RecvBuffer, 0, false);
            GC.Collect();
            
            var injected = memory.Read<byte>((IntPtr)DAStaticPointers.DAvid, false);
            if (injected != 0x55)
                return;
            
            try
            {
                var dllPath = Path.Combine(Environment.CurrentDirectory, "DAvid.dll");
                memory.Modules.Inject(dllPath);
                Instance.logger.LogInfo($"Injected DAvid.dll into process {process.ProcessName} ({process.Id})");
                Console.Beep();
            } catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Not 100% sure what this does, I think it skips the cutscene.
        /// If packet interception works both ways then this could be removed
        /// </summary>
        /// <param name="memory"></param>
        private void SkipCutscene(MemorySharp memory)
        {
            var targetFunctionAddr = memory.Read<int>((IntPtr)0x85C000, false) + 0x697B;
            
            var payload = new byte[] { 0x13, 0x01 };
            
            var payloadLengthArg = memory.Memory.Allocate(2);
            var payloadLengthAddr = payloadLengthArg.BaseAddress;
            memory.Write(payloadLengthAddr, (short)payload.Length, false);
            
            var payloadArg = memory.Memory.Allocate(sizeof(byte) * payload.Length);
            var payloadAddr = payloadArg.BaseAddress;
            memory.Write(payloadAddr, payload, false);

            var asm = new []
            {
                "mov eax, " + payloadLengthAddr,
                "push eax",
                "mov edx, " + payloadAddr,
                "push edx",
                "call " + targetFunctionAddr,
            };

            memory.Assembly.Inject(asm, (IntPtr)0x006FE000);
        }

        public IntPtr InterceptPacket(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {   
            const int WM_COPYDATA = 0x004A;
            
            if (msg != WM_COPYDATA)
                return IntPtr.Zero;

            var ptr = (Copydatastruct)Marshal.PtrToStructure(lParam, typeof(Copydatastruct));
            if (ptr.CbData <= 0)
                return IntPtr.Zero;
    
            var data = new byte[ptr.CbData];
            var typeData = (int)ptr.DwData;
            var source = Packet.PacketSource.Unknown;
            var id = wParam.ToInt32();
            
            Marshal.Copy(ptr.LpData, data, 0, ptr.CbData); // Copy from unmanaged memory
            var player = PlayerManager.Instance.GetPlayer(id);
            if (player == null)
            {
                return IntPtr.Zero;
            }
            
            if (Enum.IsDefined(typeof(Packet.PacketSource), typeData))
            {
                source = (Packet.PacketSource)typeData;
            }
            
            var packet = new Packet(data, source, player);
            if (packet.Source == Packet.PacketSource.Server)
            {
                ServerPacketQueue.Enqueue(packet);
            }

            if (packet.Source == Packet.PacketSource.Client)
            {
                Console.WriteLine(packet);
            }
            DispatchPackets();

            handled = true;
            return IntPtr.Zero;
        }

        public static void InjectPacket(DTO.Packet packet)
        {
            if (packet.Source == DTO.Packet.PacketSource.Client)
            {
                Instance.ServerPacketInjectionQueue.Enqueue(packet);
            }

            Instance.ProcessOutgoingPackets();
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct Copydatastruct
        {
            public IntPtr DwData;
            public int CbData;
            public IntPtr LpData;
        }
        
        public void DispatchPackets()
        {
            lock (DispatcherLock)
            {
                if (IsDispatching)
                    return;

                if (!ServerPacketQueue.TryPeek(out var packet))
                    return;

                IsDispatching = true;
            }

            var packetDispatcher = new BackgroundWorker();
            packetDispatcher.DoWork += (sender, e) =>
            {
                while (!ServerPacketQueue.IsEmpty)
                {
                    ServerPacketQueue.TryDequeue(out var packet);
                    var ableConsumers = consumers.FindAll(c => c.CanConsume(packet));
                    foreach (var consumer in ableConsumers)
                    {
                        consumer.ProcessPacket(packet);
                    }
                    
                    if (!packet.Handled)
                    {
                        logger.LogWarn($"No consumer found for packet type: {packet.Type}");
                        Console.WriteLine($"???[0x{packet.Data[0]:X2}]: {packet}");
                    }
                }
            };
            
            packetDispatcher.RunWorkerCompleted += (sender, e) =>
            {
                lock (DispatcherLock)
                {
                    IsDispatching = false;
                }
            };
            
            packetDispatcher.RunWorkerAsync();
            
        }
        
        public void ProcessOutgoingPackets()
        {
            lock (ServerInjectionLock)
            {
                if (IsInjectingToServer)
                    return;

                if (!ServerPacketInjectionQueue.TryPeek(out var packet))
                    return;

                IsInjectingToServer = true;
            }

            var packetInjector = new BackgroundWorker();
            packetInjector.DoWork += (sender, e) =>
            {
                while (!ServerPacketInjectionQueue.IsEmpty)
                {
                    ServerPacketInjectionQueue.TryDequeue(out var packet);
                    
                    var pId = packet.Player.Process.ProcessId;
                    var process = System.Diagnostics.Process.GetProcessById(pId);
                    var memory = new MemorySharp(process);
                    
                    while (memory.Read<byte>((IntPtr)DAStaticPointers.SendBuffer, false) == 1)
                    {
                        if (!memory.IsRunning)
                            break;
                        Thread.Sleep(1);
                    }
                    memory.Write((IntPtr)DAStaticPointers.SendBuffer, 1, false);
                    memory.Write((IntPtr)DAStaticPointers.SendBuffer + 0x04, 1, false);
                    memory.Write((IntPtr)DAStaticPointers.SendBuffer + 0x08, packet.Data.Length, false);
                    memory.Write((IntPtr)DAStaticPointers.SendBuffer + 0x12, packet.Data, false);
                }
            };
            
            packetInjector.RunWorkerCompleted += (sender, e) =>
            {
                lock (ServerInjectionLock)
                {
                    IsInjectingToServer = false;
                }
            };
            
            packetInjector.RunWorkerAsync();
        }

    }
}