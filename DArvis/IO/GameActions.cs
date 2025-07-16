using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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

        public static void Face(GameClient client, Direction dir)
        {
            var packet = new OldPacket(0x11);
            packet.WriteByte((byte)dir);
            GameClient.InjectPacket<ServerOldPacket>(client, packet);

            client.Attributes.ServerPosition.Direction = dir;
            client.LastDirectionTurn = DateTime.Now;
        }

        public static void RequestProfile(Player player)
        {
            var data = new byte[] { 0x2D, 0x00 };
            var packet = new DTO.Packet(data, DTO.Packet.PacketSource.Client, player);

            PacketManager.InjectPacket(packet);
        }

        private static Random rnd = new Random();

        public static void Walk(GameClient Client, Direction dir)
        {
            if ((DateTime.Now - Client.LastMovementUpdate).TotalMilliseconds > 100)
            {
                //Console.WriteLine($"Walking {Client.Attributes.Serial} in direction {dir}");
                if (dir == Direction.Random)
                {
                    var random = (Direction)rnd.Next(0, 3);
                    Walk(Client, random);
                }

                if (dir != Client.Attributes.ServerPosition.Direction)
                {
                    Face(Client, dir);
                }

                if (dir == Direction.East)
                    Client.InjectSyncOperation(SyncOperation.WalkEast);
                if (dir == Direction.North)
                    Client.InjectSyncOperation(SyncOperation.WalkNorth);
                if (dir == Direction.South)
                    Client.InjectSyncOperation(SyncOperation.WalkSouth);
                if (dir == Direction.West)
                    Client.InjectSyncOperation(SyncOperation.WalkWest);

                Client.Attributes.ServerPosition.Direction = dir;

                Client.LastMovementUpdate = DateTime.Now;
            }
        }


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