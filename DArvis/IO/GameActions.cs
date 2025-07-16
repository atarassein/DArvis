using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Binarysharp.MemoryManagement;
using DArvis.Components;
using DArvis.DTO;
using DArvis.IO.Process;
using DArvis.Models;
using DArvis.Types;

namespace DArvis.IO;

public class GameActions
    {

        public static void Refresh(Player player)
        {
            var data = new byte[] { 0x38, 0x00, 0x38 };
            var packet = new DTO.Packet(data, DTO.Packet.PacketSource.Client, player);
            
            PacketManager.InjectPacket(packet);
        }
        
        public static void Assail(Player player)
        {
            var data = new byte[] { 0x13, 0x01 };
            var packet = new DTO.Packet(data, DTO.Packet.PacketSource.Client, player);
            
            PacketManager.InjectPacket(packet);
        }
        
        public static void UseInventorySlot(Player player, byte slot)
        {
            var data = new byte[] { 0x1C, slot, 0x00 };
            var packet = new DTO.Packet(data, DTO.Packet.PacketSource.Client, player);
            
            PacketManager.InjectPacket(packet);
        }

        public static void BeginSpell(Player player, byte SpellLines)
        {
            var data = new byte[] { 0x4D, SpellLines, 0x00 };
            var packet = new DTO.Packet(data, DTO.Packet.PacketSource.Client, player);

            PacketManager.InjectPacket(packet);
        }

        public static void EndSpell(Player player, byte slot)
        {
            var data = new byte[] { 0x0F, slot, 0x00 };
            var packet = new DTO.Packet(data, DTO.Packet.PacketSource.Client, player);

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

        public static void Face(Player player, Direction direction)
        {
            var data = new byte[] { 0x11, (byte)direction, 0x00 };
            var packet = new Packet(data, Packet.PacketSource.Client, player);

            PacketManager.InjectPacket(packet);
         
            // TODO: Not sure if it's wise to set this here, maybe this should only be set when the server responds
            player.Location.Direction = direction;
        }

        public static void RequestProfile(Player player)
        {
            var data = new byte[] { 0x2D, 0x00 };
            var packet = new DTO.Packet(data, DTO.Packet.PacketSource.Client, player);

            PacketManager.InjectPacket(packet);
        }

        private static Random rnd = new Random();

        #region Walk
        
        // TODO: refactor this nonsense, this is a mess
        
        public enum SyncOperation
        {
            [Description("Op.Walk North")]
            WalkNorth = Direction.North,
            [Description("Op.Walk East")]
            WalkEast = Direction.East,
            [Description("Op.Walk South")]
            WalkSouth = Direction.South,
            [Description("Op.Walk West")]
            WalkWest = Direction.West,
        }
        
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            [MarshalAs(UnmanagedType.LPStr)]
            public string lpData;
        }
        
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(int hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);
        
        public static void InjectSyncOperation(Player player, SyncOperation Code)
        {
            if (!player.IsLoggedIn)
                return;

            const int WM_COPYDATA = 0x004A;
            
            string msg = GetEnumDescription((SyncOperation)Code) + ";" + player.PacketId + ";" + player.Location.X + ";" + player.Location.Y;
            var cds = new COPYDATASTRUCT
            {
                dwData = (IntPtr)(int)Code,
                cbData = msg.Length + 1,
                lpData = msg
            };
            
            var pId = player.Process.ProcessId;
            var process = System.Diagnostics.Process.GetProcessById(pId);
            var memory = new MemorySharp(process);
            
            SendMessage((int)memory.Windows.MainWindowHandle, WM_COPYDATA, 0, ref cds);
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
        
        public static void Walk(Player player, Direction dir)
        {
            if ((DateTime.Now - player.LastWalkCommand).TotalMilliseconds > 100)
            {
                if (dir == Direction.Random)
                {
                    var random = (Direction)rnd.Next(0, 3);
                    Walk(player, random);
                }

                if (dir != player.Location.Direction)
                {
                    Face(player, dir);
                    // TODO: maybe we should wait for the server to respond before sending the walk command?
                    // TODO: maybe we should add a delay before the next walk command?
                }

                if (dir == Direction.East)
                    InjectSyncOperation(player, SyncOperation.WalkEast);
                if (dir == Direction.North)
                    InjectSyncOperation(player, SyncOperation.WalkNorth);
                if (dir == Direction.South)
                    InjectSyncOperation(player, SyncOperation.WalkSouth);
                if (dir == Direction.West)
                    InjectSyncOperation(player, SyncOperation.WalkWest);

                player.Location.Direction = dir;
                player.LastWalkCommand = DateTime.Now;
            }
        }

        #endregion

        public static void PacketWalk(GameClient Client, Direction dir)
        {
            BeginWalk(Client, dir);
            Thread.Sleep(15);
            EndWalk(Client, dir, 300);
        }

        public static void BeginWalk(GameClient Client, Direction dir)
        {

            Client.WalkOrdinal = (Client.WalkOrdinal + 1);


            var p = new OldPacket();
            p.WriteByte(0x06);
            p.WriteByte((byte)dir);
            p.WriteByte((byte)(Client.WalkOrdinal));
            p.WriteByte(0x00);
            p.WriteByte(0x06);
            GameClient.InjectPacket<ServerOldPacket>(Client, p);
        }

        public static void EndWalk(GameClient Client, Direction dir, int WalkSpeed = 50)
        {
            short x = 5; //Client.FieldMap.X();
            short y = 5; //Client.FieldMap.Y();

            var p = new OldPacket();
            p.WriteByte(0x0C);
            p.WriteInt32(Client.Attributes.Serial);
            p.WriteInt16(x);
            p.WriteInt16(y);
            p.WriteByte((byte)dir);
            p.WriteByte(0x00);

            GameClient.InjectPacket<ClientOldPacket>(Client, p);
            Thread.Sleep(WalkSpeed);
        }

    }