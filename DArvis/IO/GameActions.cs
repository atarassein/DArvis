using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Binarysharp.MemoryManagement;
using DArvis.DTO;
using DArvis.IO.Process;
using DArvis.Models;

namespace DArvis.IO;

public class GameActions
{

    public static void Refresh(Player player)
    {
        var data = new byte[] { 0x38, 0x00, 0x38 };
        var packet = new Packet(data, Packet.PacketSource.Client, player);
        
        PacketManager.InjectPacket(packet);
    }
    
    public static void Assail(Player player)
    {
        var data = new byte[] { 0x13, 0x01 };
        var packet = new Packet(data, Packet.PacketSource.Client, player);
        
        PacketManager.InjectPacket(packet);
    }
    
    public static void UseInventorySlot(Player player, byte slot)
    {
        var data = new byte[] { 0x1C, slot, 0x00 };
        var packet = new Packet(data, Packet.PacketSource.Client, player);
        
        PacketManager.InjectPacket(packet);
    }

    public static void BeginSpell(Player player, byte SpellLines)
    {
        var data = new byte[] { 0x4D, SpellLines, 0x00 };
        var packet = new Packet(data, Packet.PacketSource.Client, player);

        PacketManager.InjectPacket(packet);
    }

    public static void EndSpell(Player player, byte slot)
    {
        var data = new byte[] { 0x0F, slot, 0x00 };
        var packet = new Packet(data, Packet.PacketSource.Client, player);

        PacketManager.InjectPacket(packet);
    }

    public static void SendSpellLines(Player player, string msg)
    {
        var buffer = Encoding.GetEncoding(949).GetBytes(msg);
        var data = new List<byte> { 0x4E, (byte)buffer.Length };
        foreach (var b in buffer)
        {
            data.Add(b);
        }
        data.Add(0x00);
        var byteData = data.ToArray();
        var packet = new Packet(byteData, Packet.PacketSource.Client, player);

        PacketManager.InjectPacket(packet);
    }

    public static async Task Face(Player player, Direction direction)
    {
        var data = new byte[] { 0x11, (byte)direction, 0x00 };
        var packet = new Packet(data, Packet.PacketSource.Client, player);

        PacketManager.InjectPacket(packet);

        player.Location.Direction = direction;
    }

    public static void RequestProfile(Player player)
    {
        var data = new byte[] { 0x2D, 0x00 };
        var packet = new Packet(data, Packet.PacketSource.Client, player);

        PacketManager.InjectPacket(packet);
    }

    #region Walk
    
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }
    
    [DllImport("User32.dll", EntryPoint = "SendMessage")]
    public static extern int SendMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);
    
    public static void InjectWalk(Player player, Direction direction)
    {
        if (!player.IsLoggedIn)
            return;

        const int WM_COPYDATA = 0x004A;
        
        string msg = $"OP.Walk {direction};" + player.PacketId + ";" + player.Location.X + ";" + player.Location.Y;
        var cds = new COPYDATASTRUCT
        {
            dwData = (int)direction,
            cbData = msg.Length + 1,
            lpData = msg
        };
        
        var pId = player.Process.ProcessId;
        var process = System.Diagnostics.Process.GetProcessById(pId);
        var memory = new MemorySharp(process);
        
        SendMessage((int)memory.Windows.MainWindowHandle, WM_COPYDATA, 0, ref cds);
    }
    
    public static async Task WalkAsync(Player player, Direction dir)
    {
        if (dir != player.Location.Direction)
        {
            Console.WriteLine("FACING NEW DIRECTION: " + dir);
            InjectWalk(player, dir);
            player.Location.Direction = dir;
            await Task.Delay(1);
        }
            
        InjectWalk(player, dir);
    }    
    
    public static void Walk(Player player, Direction dir)
    {
        if (dir != player.Location.Direction)
        {
            Face(player, dir);
            player.Location.Direction = dir;
        }
            
        InjectWalk(player, dir);
    }

    #endregion

    public static void PacketWalk(Player player, Direction dir)
    {
        BeginWalk(player, dir);
        Thread.Sleep(15);
        EndWalk(player, dir, 300);
    }

    public static void BeginWalk(Player player, Direction dir)
    {
        player.WalkOrdinal = (player.WalkOrdinal + 1);
        var data = new byte[] { 0x06, (byte)dir, (byte)player.WalkOrdinal, 0x00, 0x06 };
        var packet = new Packet(data, Packet.PacketSource.Client, player);
        PacketManager.InjectPacket(packet);
    }

    public static void EndWalk(Player player, Direction dir, int WalkSpeed = 50)
    {
        short x = (short)player.Location.X;
        short y = (short)player.Location.Y;

        var data = new List<byte> { 0x0C };
        
        var byteBuffer = BitConverter.GetBytes(player.PacketId).ToList();
        if (BitConverter.IsLittleEndian)
        {
            byteBuffer.Reverse();
        }
        foreach (var b in byteBuffer)
        {
            data.Add(b);
        }
        
        byteBuffer = BitConverter.GetBytes(x).ToList();
        if (BitConverter.IsLittleEndian)
        {
            byteBuffer.Reverse();
        }
        foreach (var b in byteBuffer)
        {
            data.Add(b);
        }
        
        byteBuffer = BitConverter.GetBytes(y).ToList();
        if (BitConverter.IsLittleEndian)
        {
            byteBuffer.Reverse();
        }
        foreach (var b in byteBuffer)
        {
            data.Add(b);
        }
        
        data.Add((byte)dir);
        data.Add(0x00);
        
        var packet = new Packet(data.ToArray(), Packet.PacketSource.Client, player);
        PacketManager.InjectPacket(packet);
        Thread.Sleep(WalkSpeed);
    }

}