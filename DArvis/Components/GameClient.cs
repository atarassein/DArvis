using DArvis.IO;

namespace DArvis.Components;
using Shared;
using States;
using Types;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Binarysharp.MemoryManagement;

[Serializable]
public abstract class GameClient : UpdateableComponent
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate void ProgressCallback(int value);

    public enum MovementState : byte
    {
        [Description("Movement is Locked, and you cannot sent walk packets.")] Locked = 0x74,
        [Description("Movement is Free, and you can send walk packets.")] Free = 0x75
    }
    
    public Collection<UpdateableComponent> InstalledComponents = [];

    protected GameClient()
    {
        Timer = new UpdateTimer(TimeSpan.FromMilliseconds(1.0));
        PrepareComponents();

        ShouldUpdate = true;
    }

    public int ProcessId { get; internal set; }
    
    internal static string Hack { get; set; }
    public int WalkOrdinal { get; internal set; }

    [DllImport("DAvid.dll")]
    public static extern void OnAction([MarshalAs(UnmanagedType.FunctionPtr)] ProgressCallback callbackPointer);

    public void InitializeMemory(Process p, string dllPath)
    {
        Memory = new MemorySharp(p);
        CleanUpMememory();

        var injected = Memory.Read<byte>((IntPtr)DAStaticPointers.DAvid, false);
        if (injected == 85)
        {
            try
            {
                Memory.Modules.Inject(dllPath);
                Console.Beep();
                Console.WriteLine("Injected ETDA.dll into process {0} ({1})", p.ProcessName, p.Id);
                Hack = dllPath;
            } catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
            }
        }
    }

    public virtual void OnAttached()
    {
        Task.Run(() => ProcessOutQueue(this));
        Task.Run(() => ProcessInQueue(this));
    }

    internal byte this[int Address]
    {
        get
        {
            if (IsInGame())
                return _memory.Read<byte>((IntPtr)Address, false);

            return byte.MinValue;
        }
        set
        {
            if (IsInGame())
                _memory.Write((IntPtr)Address, value, false);
        }
    }

    internal void AddClientHandler(byte action, EventHandler<OldPacket> data)
    {
        ClientPacketHandler[action] = data;
    }


    internal void AddServerHandler(byte action, EventHandler<OldPacket> data)
    {
        ServerPacketHandler[action] = data;
    }

    public bool IsCasting()
    {
        if (!_memory.IsRunning || !IsInGame())
            return false;

        var stateptr = _memory.Read<int>((IntPtr)DAStaticPointers.IsCasting, false);
        stateptr += 0x8C94;
        var state = _memory.Read<int>((IntPtr)stateptr, false);

        return state == 1;
    }

    public void ApplyMovementLock()
    {
        if (!_memory.IsRunning || !IsInGame())
            return;
        var state = (MovementState)_memory.Read<byte>((IntPtr)DAStaticPointers.Movement, false);
        if (state == MovementState.Free)
            _memory.Write((IntPtr)DAStaticPointers.Movement, (byte)MovementState.Locked, false);
    }

    public MovementState GetMoveState()
    {
        if (!_memory.IsRunning || !IsInGame())
            throw new Exception("Error, Memory not ready.");

        var state = (MovementState)_memory.Read<byte>((IntPtr)DAStaticPointers.Movement, false);
        return state;
    }

    public void ReleaseMovementLock()
    {
        if (!_memory.IsRunning || !IsInGame())
            return;
        var state = (MovementState) _memory.Read<byte>((IntPtr)DAStaticPointers.Movement, false);
        if (state == MovementState.Locked)
            _memory.Write((IntPtr)DAStaticPointers.Movement, (byte) MovementState.Free, false);
    }
    
    public void LoadStates(string assemblyPath)
    {
        if (string.IsNullOrEmpty(assemblyPath))
            return;
        if (!File.Exists(assemblyPath))
            return;
        
        try
        {
            var asm = Assembly.LoadFrom(assemblyPath);
            var types = asm.GetTypes();

            foreach (var type in types)
            {
                if (type.IsClass && type.IsSubclassOf(typeof(GameState)))
                {
                    var tempState = (GameState) Activator.CreateInstance(type);
                    tempState.Client = this;
                    // This handles user interface settings
                    //tempState.SettingsInterface = new StateSettings(tempState) {Dock = DockStyle.Fill};
                    //tempState.SettingsInterface.OnSettingsUpdated += SettingsInterface_OnSettingsUpdated;
                    tempState.InitState();

                    if (!StateMachine.States.Contains(tempState))
                        StateMachine.States.Add(tempState);
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
        }
    }

    private void SettingsInterface_OnSettingsUpdated(GameState state)
    {
        //refresh client anytime settings are changed.
        if (Client.IsInGame())
            GameActions._Refresh(Client);
    }

    public void CleanUpMememory()
    {
        if (_memory.IsRunning)
        {
            _memory.Write((IntPtr)DAStaticPointers.SendBuffer, 0, false);
            _memory.Write((IntPtr)DAStaticPointers.RecvBuffer, 0, false);
        }

        InjectToClientQueue = new ConcurrentQueue<byte[]>();
        InjectToServerQueue = new ConcurrentQueue<byte[]>();

        GC.Collect();
    }

    public void DestroyResources()
    {
        ShouldUpdate = false;

        lock (InstalledComponents)
        {
            foreach (var component in InstalledComponents)
                component.Dispose();

            InstalledComponents.Clear();
        }
        
        ServerPacketHandler = new EventHandler<OldPacket>[256];
        ClientPacketHandler = new EventHandler<OldPacket>[256];
        GC.Collect();
    }

    private void PrepareComponents()
    {
        Memory = new MemorySharp(Process.GetCurrentProcess()); // Example initialization
        StateMachine = new GameStateEngine(this);
        RunningState = null; // Assuming it can be null
        SpellBar = new List<short>();
        MapName = string.Empty;
        EquippedWeapon = string.Empty;
        LocalWorldUsers = new List<string>();
        
        //core components, endabled by default
        // InstalledComponents.Add(new Inventory {Client = this, Enabled = true});
        // InstalledComponents.Add(new PlayerAttributes {Client = this, Enabled = true});
        // InstalledComponents.Add(new Magic {Client = this, Enabled = true});
        // InstalledComponents.Add(new GameEquipment {Client = this, Enabled = true});
        // InstalledComponents.Add(new Activebar {Client = this, Enabled = true});
        // InstalledComponents.Add(new TargetFinder {Client = this, Enabled = true});
        //
        // //disabled by default components
        // InstalledComponents.Add(new StressTest {Client = this, Enabled = false});

        //mandatory components
        // FieldMap = new Map();
        // FieldMap.Enabled = true;
        // FieldMap.Client = this;
        // FieldMap.Init(0, 0, 0);
        // InstalledComponents.Add(FieldMap);

        //init state machine.
        StateMachine = new GameStateEngine(this);
        StateMachine.Client = this;
        StateMachine.Enabled = true;

        InstalledComponents.Add(StateMachine);
    }

    public override void Update(TimeSpan tick)
    {
        Timer.Update(tick);

        if (Timer.Elapsed)
        {
            try
            {
                UpdateComponents(tick);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
            }
            finally
            {
                Timer.Reset();
            }
        }
    }

    private void UpdateComponents(TimeSpan tick)
    {
        if (Client.ShouldUpdate)
        {
            var copy = default(List<UpdateableComponent>);
            lock (InstalledComponents)
            {
                copy = new List<UpdateableComponent>(InstalledComponents);
            }

            foreach (var component in copy)
                if (component.Enabled)
                    component.Update(tick);
        }
    }

    public abstract class RepeatableTimer : UpdateableComponent
    {
    }

    #region GameClient Properties

    internal bool ShouldUpdate;

    public EventHandler<OldPacket> OnPacketRecevied = delegate { };

    public EventHandler<OldPacket> OnPacketSent = delegate { };

    public int SendPointer { get; set; }
    public int RecvPointer { get; set; }
    public MemorySharp Memory { get; set; }

    public GameStateEngine StateMachine { get; set; }

    #endregion

    #region Internal GameClient Properties

    public bool IsCursed { get; internal set; }
    public bool ShouldRemoveDebuffs { get; internal set; }
    public bool Paused { get; internal set; }
    public GameState RunningState { get; internal set; }
    public DateTime LastUseInvetorySlot { get; internal set; }
    public DateTime LastEquipmentUpdate { get; internal set; }
    public DateTime LastRefreshed { get; internal set; }
    public DateTime LastCastStarted { get; internal set; }
    public DateTime WhenLastCasted { get; internal set; }
    public DateTime LastMovementUpdate { get; internal set; }
    public DateTime LastDirectionTurn { get; internal set; }
    public List<short> SpellBar { get; set; }
    public byte EquippedWeaponId { get; set; }

    public bool ClientReady = true;
    public bool MapLoaded = false;
    public bool MapLoading { get; set; }
    
    public string MapName { get; set; }
    public short MapId { get; set; }
    
    public string EquippedWeapon { get; set; }
    public int Steps { get; set; }
    public List<string> LocalWorldUsers { get; set; }
    public int LastCastLines = -1;

    public bool IsRefreshing
    {
        get { return DateTime.Now - LastRefreshed < new TimeSpan(0, 0, 0, 0, 500); }
    }

    public PlayerAttributes Attributes
    {
        get
        {
            List<UpdateableComponent> copy;
            lock (InstalledComponents)
            {
                copy = new List<UpdateableComponent>(InstalledComponents);
            }
            var obj = copy.OfType<PlayerAttributes>().FirstOrDefault();
            if (obj != null)
                return obj;
            throw new Exception("Error, Component PlayerAttributes is not installed.");
        }
    }
    
    #endregion

    #region Packet Hooks

    public EventHandler<OldPacket>[] ClientPacketHandler = new EventHandler<OldPacket>[256];
    public EventHandler<OldPacket>[] ServerPacketHandler = new EventHandler<OldPacket>[256];
    internal ConcurrentQueue<byte[]> InjectToServerQueue = new ConcurrentQueue<byte[]>();
    internal ConcurrentQueue<byte[]> InjectToClientQueue = new ConcurrentQueue<byte[]>();
    internal static int _Total;

    private static ushort LastCRC = ushort.MaxValue;

    private static ushort Crc16(byte[] bytes)
    {
        const ushort poly = 4129;
        ushort[] table = new ushort[256];
        ushort initialValue = 0xffff;
        ushort temp, a;
        ushort crc = initialValue;
        for (int i = 0; i < table.Length; ++i)
        {
            temp = 0;
            a = (ushort)(i << 8);
            for (int j = 0; j < 8; ++j)
            {
                if (((temp ^ a) & 0x8000) != 0)
                    temp = (ushort)((temp << 1) ^ poly);
                else
                    temp <<= 1;
                a <<= 1;
            }
            table[i] = temp;
        }
        for (int i = 0; i < bytes.Length; ++i)
        {
            crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
        }
        return crc;
    }

    #region Inject op Commands - Syncronized
    public const Int32 WM_COPYDATA = 0x4A;

    [DllImport("User32.dll", EntryPoint = "SendMessage")]
    public static extern int SendMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);

    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }


    public void InjectSyncOperation(SyncOperation Code)
    {
        if (!IsInGame())
            return;

        if (Memory != null && Memory.IsRunning)
        {
            string msg = GetEnumDescription((SyncOperation)Code) + ";" + Attributes.Serial + ";" + Attributes.ServerPosition.X + ";" + Attributes.ServerPosition.Y;
            var cds = new COPYDATASTRUCT
            {
                dwData = (IntPtr)(int)Code,
                cbData = msg.Length + 1,
                lpData = msg
            };
            SendMessage((int)Memory.Windows.MainWindowHandle, WM_COPYDATA, 0, ref cds);
        }
    }
    public static string GetEnumDescription(Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());

        DescriptionAttribute[] attributes =
            (DescriptionAttribute[])fi.GetCustomAttributes(
            typeof(DescriptionAttribute),
            false);

        if (attributes != null &&
            attributes.Length > 0)
            return attributes[0].Description;
        else
            return value.ToString();
    }

    #endregion

    public static void InjectPacket<T>(GameClient client, OldPacket oldPacket, bool force = false) where T : OldPacket
    {
        if (client == null)
            return;

        if (force)
        {
            if (typeof(T) == typeof(ClientOldPacket))
                client.InjectToClientQueue.Enqueue(oldPacket.Data);
            else if (typeof(T) == typeof(ServerOldPacket))
                client.InjectToServerQueue.Enqueue(oldPacket.Data);

            return;
        }

        var a = Crc16(oldPacket.Data);
        var b = LastCRC;

        if (a != b)
        {
            if (typeof(T) == typeof(ClientOldPacket))
            {
                lock (client.InjectToClientQueue)
                {
                    client.InjectToClientQueue.Enqueue(oldPacket.Data);
                }
            }
            else if (typeof(T) == typeof(ServerOldPacket))
            {
                lock (client.InjectToServerQueue)
                {
                    client.InjectToServerQueue.Enqueue(oldPacket.Data);
                }
            }
            LastCRC = a;
            b = a;
        }
        else
        {
            client.CleanUpMememory();
        }
    }

    #endregion

    #region Packet Consumers

    internal static void ProcessInQueue(GameClient client)
    {
        while (true)
        {
            Thread.Sleep(1);

            if (client == null)
                continue;
            if (client.Memory == null)
                continue;
            if (!client.Memory.IsRunning || !client.IsInGame())
                continue;


            byte[] activeBuffer;
            while (client.InjectToClientQueue.TryDequeue(out activeBuffer))
            {
                Interlocked.Add(ref _Total, 1);
                while (client.Memory.Read<byte>((IntPtr)DAStaticPointers.RecvBuffer, false) == 1)
                {
                    if (!client.Memory.IsRunning)
                        break;
                    Thread.Sleep(1);
                }

                client.Memory.Write((IntPtr)DAStaticPointers.RecvBuffer, 1, false);
                client.Memory.Write((IntPtr)DAStaticPointers.RecvBuffer + 0x04, 0, false);
                client.Memory.Write((IntPtr)DAStaticPointers.RecvBuffer + 0x08, activeBuffer.Length, false);
                client.Memory.Write((IntPtr)DAStaticPointers.RecvBuffer + 0x12, activeBuffer, false);
            }
        }
    }

    internal static void ProcessOutQueue(GameClient client)
    {
        while (true)
        {
            Thread.Sleep(1);

            if (client == null)
                continue;
            if (client.Memory == null)
                continue;
            if (!client.Memory.IsRunning || !client.IsInGame())
                continue;


            byte[] activeBuffer;
            while (client.InjectToServerQueue.TryDequeue(out activeBuffer))
            {
                Interlocked.Add(ref _Total, 1);

                while (client.Memory.Read<byte>((IntPtr)DAStaticPointers.SendBuffer, false) == 1)
                {
                    if (!client.Memory.IsRunning)
                        break;
                    Thread.Sleep(1);
                }
                client.Memory.Write((IntPtr)DAStaticPointers.SendBuffer, 1, false);
                client.Memory.Write((IntPtr)DAStaticPointers.SendBuffer + 0x04, 1, false);
                client.Memory.Write((IntPtr)DAStaticPointers.SendBuffer + 0x08, activeBuffer.Length, false);
                client.Memory.Write((IntPtr)DAStaticPointers.SendBuffer + 0x12, activeBuffer, false);
            }

        }
    }

    #endregion
}