using System;
using System.Threading;
using DArvis.Components;
using DArvis.Models;
using DArvis.Types;

namespace DArvis.Actions;

public class GameActions
    {
          
        //Base Functions Must be defined in here.
        //The callback is optional, But must be specified as final constructor.

        public static void Assail(GameClient client, Func<GameClient, Packet, bool> callback = null)
        {
            var p = new Packet();
            p.Write(new byte[] { 0x13, 0x01 });

            GameClient.InjectPacket<ServerPacket>(client, p);
            callback?.Invoke(client, p);
        }


        public static void Refresh(GameClient client, bool force = false, Func<GameClient, Packet, bool> callback = null)
        {
            var p = new Packet();
            p.Write(new byte[] { 0x38, 0x00, 0x38 });
            GameClient.InjectPacket<ServerPacket>(client, p, force);
            GameClient.InjectPacket<ServerPacket>(client, p, force);

            client.LastRefreshed = DateTime.Now;
            callback?.Invoke(client, p);
        }
        
        public static void UseInventorySlot(GameClient client, byte slot)
        {
            var packet = new Packet();
            packet.WriteByte(0x1C);
            packet.WriteByte(slot);
            packet.WriteByte(0x00);

            GameClient.InjectPacket<ServerPacket>(client, packet);
        }

        public static void BeginSpell(GameClient client, byte SpellLines, 
            Func<GameClient, Packet, bool> callback = null)
        {
            var packet = new Packet();
            packet.WriteByte(0x4D);
            packet.WriteByte(SpellLines);
            packet.WriteByte(0x00);

            GameClient.InjectPacket<ServerPacket>(client, packet);
            callback?.Invoke(client, packet);
        }

        public static void EndSpell(GameClient client, byte slot,
            Func<GameClient, Packet, bool> callback = null)
        {
            var packet = new Packet();
            packet.WriteByte(0x0F);
            packet.WriteByte(slot);
            packet.WriteByte(0x00);

            GameClient.InjectPacket<ServerPacket>(client, packet);
            callback?.Invoke(client, packet);
        }

        public static void SendSpellLines(GameClient client, string msg,
            Func<GameClient, Packet, bool> callback = null)
        {
            var packet = new Packet();
            packet.WriteByte(0x4E);
            packet.WriteString8(msg);
            packet.WriteByte(0x00);

            GameClient.InjectPacket<ServerPacket>(client, packet);
            callback?.Invoke(client, packet);
        }

        public static void Face(GameClient client, Direction dir)
        {
            var packet = new Packet(0x11);
            packet.WriteByte((byte)dir);
            GameClient.InjectPacket<ServerPacket>(client, packet);

            client.Attributes.ServerPosition.Direction = dir;
            client.LastDirectionTurn = DateTime.Now;
        }

        public static void RequestProfile(GameClient client,
            Func<GameClient, Packet, bool> callback = null)
        {
            var packet = new Packet();
            packet.WriteByte(0x2D);
            packet.WriteByte(0x00);

            GameClient.InjectPacket<ServerPacket>(client, packet);
            callback?.Invoke(client, packet);
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


            var p = new Packet();
            p.WriteByte(0x06);
            p.WriteByte((byte)dir);
            p.WriteByte((byte)(Client.WalkOrdinal));
            p.WriteByte(0x00);
            p.WriteByte(0x06);
            GameClient.InjectPacket<ServerPacket>(Client, p);
        }

        public static void EndWalk(GameClient Client, Direction dir, int WalkSpeed = 50)
        {
            short x = 5; //Client.FieldMap.X();
            short y = 5; //Client.FieldMap.Y();

            var p = new Packet();
            p.WriteByte(0x0C);
            p.WriteInt32(Client.Attributes.Serial);
            p.WriteInt16(x);
            p.WriteInt16(y);
            p.WriteByte((byte)dir);
            p.WriteByte(0x00);

            GameClient.InjectPacket<ClientPacket>(Client, p);
            Thread.Sleep(WalkSpeed);
        }

    }