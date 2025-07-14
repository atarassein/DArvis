using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;
    using DArvis.Shared;
    using DArvis.Types;
    
    namespace DArvis
    {
        public partial class UnMainWindow : Window
        {
            private const int WM_COPYDATA = 0x004A;
            private int idx, previd;
            public UnMainWindow()
            {
                InitializeComponent();
                SetupCleanupHandlers();
                Loaded += MainWindow_Loaded;
            }
    
            private void MainWindow_Loaded(object sender, RoutedEventArgs e)
            {
                var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                hwndSource?.AddHook(WndProcHook);
            }

            private void InjectButton_Click(object sender, RoutedEventArgs e)
            {
                
            }
            
            private void EjectButton_Click(object sender, RoutedEventArgs e)
            {
                
            }
            
            private void Window_Closed(object sender, EventArgs e)
            {
                Cleanup();
            }
            
            private IntPtr WndProcHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                if (msg == WM_COPYDATA)
                {
                    var ptr = (Copydatastruct)Marshal.PtrToStructure(lParam, typeof(Copydatastruct));
                    if (ptr.CbData <= 0)
                        return IntPtr.Zero;
            
                    var buffer = new byte[ptr.CbData];
                    Marshal.Copy(ptr.LpData, buffer, 0, ptr.CbData); // Copy from unmanaged memory
            
                    var id = CheckTouring(wParam, ref ptr);
            
                    if (!Collections.AttachedClients.ContainsKey(id))
                        return IntPtr.Zero;
            
                    var packet = new Packet
                    {
                        Date = DateTime.Now,
                        Data = buffer,
                        Type = (int)ptr.DwData,
                        Client = Collections.AttachedClients[id]
                    };
            
                    // Console.WriteLine packet
                    Console.WriteLine($"Packet received: {BitConverter.ToString(packet.Data)}");
                    
                    if (packet.Type == 1)
                        Collections.AttachedClients[id].OnPacketRecevied(id, packet);
                    if (packet.Type == 2)
                        Collections.AttachedClients[id].OnPacketSent(id, packet);
            
                    Intercept(ptr, packet, id);
                    handled = true;
                }
            
                return IntPtr.Zero;
            }
            private int CheckTouring(IntPtr wParam, ref Copydatastruct ptr)
            {
                var id = wParam.ToInt32();
                if (id > 0x7FFFF && idx++%2 == 0)
                {
                    if (ptr.DwData == 2)
                        if (Collections.AttachedClients.ContainsKey(previd))
                            Collections.AttachedClients[previd].SendPointer = id;
                    if (ptr.DwData != 1) return id;
                    if (Collections.AttachedClients.ContainsKey(previd))
                        Collections.AttachedClients[previd].RecvPointer = id;
                }
                else
                {
                    previd = id;
                }
                return id;
            }
    
            private static void Intercept(Copydatastruct ptr, Packet packet, int id)
            {
                if (packet.Data.Length <= 0 || packet.Data.Length != ptr.CbData)
                    return;

                var c = Collections.AttachedClients[id];

                if (c.ServerPacketHandler == null)
                    return;
                if (c.ClientPacketHandler == null)
                    return;

                if (packet.Type == 2)
                    c.ClientPacketHandler[packet.Data[0]]?.Invoke(id, packet);
                else if (packet.Type == 1)
                    c.ServerPacketHandler[packet.Data[0]]?.Invoke(id, packet);
                else if (packet.Type == 3) {
                
                }
            }
            
            private void SetupCleanupHandlers()
            {
                Application.Current.Exit += (s, e) => Cleanup();
                AppDomain.CurrentDomain.ProcessExit += (s, e) => Cleanup();
                AppDomain.CurrentDomain.UnhandledException += (s, e) => Cleanup();
            }
    
            private void Cleanup()
            {
                Console.WriteLine("Cleaning up resources...");
            }
            
            [StructLayout(LayoutKind.Sequential)]
            public struct Copydatastruct
            {
                public IntPtr DwData;
                public int CbData;
                public IntPtr LpData; // Changed from string to IntPtr
            }
        }
    }