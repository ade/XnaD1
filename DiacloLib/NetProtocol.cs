using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using Lidgren.Network.Xna;
using Microsoft.Xna.Framework;
using DiacloLib.Importer;

namespace DiacloLib
{
    public enum ProtocolClientToServer : byte
    {
        PlayerInfo,
        Handshake,
        Walk,
        MeleeAttack,
        RequestStatPoint,
        RequestRespawn
    };
    public enum ProtocolServerToClient : byte
    {
        UserConnected,
        UserDisconnected,
        UserList,
        ServerFull,
        HandshakeOK,
        MapInfo,
        Walk,
        SpawnPlayer,
        SpawnMonster,
        NPCUpdate,
        BattleResult,
        PlayerStatusUpdate,
        PlayerStatsUpdate,
        PlayerDeath,
        MonsterTemplates,
        AreaNPCList
    };
    public enum KillerType
    {
        NPC,
        Player,
        Object
    }

    public static class NetProtocol
    {
        /*
         * Client -> server
         * 
         */ 
        #region Client to server
        public static void C2S_Handshake(NetClient client)
        {
            NetBuffer b = client.CreateBuffer(256);             
            b.Write((UInt16)ProtocolClientToServer.Handshake); //Command bytes (2)
            b.Write((UInt16)Settings.CLIENT_VERSION);           //Version bytes (2)
            client.SendMessage(b, NetChannel.ReliableInOrder1);
        }
        public static void C2S_PlayerInfo(NetClient netClient, Player p)
        {
            NetBuffer b = netClient.CreateBuffer();
            b.Write((UInt16)ProtocolClientToServer.PlayerInfo);
            Serializer.WriteCharacter(b, p.Character);
            netClient.SendMessage(b, NetChannel.ReliableInOrder1);

        }
        public static void C2S_Walk(NetClient netClient, Point pos)
        {
            NetBuffer b = netClient.CreateBuffer();
            b.Write((ushort)ProtocolClientToServer.Walk);
            b.Write(pos);
            netClient.SendMessage(b, NetChannel.ReliableInOrder1);
        }
        public static void C2S_MeleeAttack(NetClient netClient, Point pos)
        {
            NetBuffer b = netClient.CreateBuffer();
            b.Write((ushort)ProtocolClientToServer.MeleeAttack);
            b.Write(pos);
            netClient.SendMessage(b, NetChannel.ReliableInOrder1);
        }
        public static void C2S_RequestStat(NetClient netClient, AttributeType stat)
        {
            NetBuffer b = netClient.CreateBuffer();
            b.Write((ushort)ProtocolClientToServer.RequestStatPoint);
            b.Write((byte)stat);
            netClient.SendMessage(b, NetChannel.ReliableInOrder1);
        }
        public static NetBuffer C2S_RequestRespawn(NetClient netClient)
        {
            NetBuffer b = netClient.CreateBuffer();
            b.Write((ushort)ProtocolClientToServer.RequestRespawn);
            return b;
        }
        #endregion



        /*
         * Server -> client
         * 
         */
        #region Server to client
        public static void S2C_HandshakeOK(NetServer s, NetConnection client, int pid)
        {
            NetBuffer b = s.CreateBuffer();
            b.Write((UInt16)ProtocolServerToClient.HandshakeOK);
            b.Write((ushort)Settings.SERVER_VERSION);
            b.Write((byte)pid); //player's id
            client.SendMessage(b, NetChannel.ReliableInOrder1);
        }

        public static NetBuffer S2C_MapInfo(NetServer s, World w)
        {
            //make buffer size approximation
            int areaBytes = 0;
            for (int i = 0; i < w.Areas.Length; i++)
            {
                areaBytes += w.Areas[i].Squares.Length * 16 * 4;
                areaBytes += w.Areas[i].npcs.Count * 70;
            }
            NetBuffer b = s.CreateBuffer(areaBytes);
            b.Write((UInt16)ProtocolServerToClient.MapInfo);
            Serializer.WriteWorld(b, w);
            return b;       
        }
        public static NetBuffer S2C_Spawn(NetServer s, ushort area_id, Point pos)
        {
            NetBuffer b = s.CreateBuffer();
            b.Write((UInt16)ProtocolServerToClient.SpawnPlayer);
            b.Write(area_id);
            b.Write(pos);
            return b;
        }
        public static void S2C_Walk(NetServer s, NetConnection client, Point request, Point correction, bool approved)
        {
            NetBuffer b = s.CreateBuffer();
            b.Write((UInt16)ProtocolServerToClient.Walk);
            b.Write(request);
            b.Write(correction);
            b.Write(approved);
            client.SendMessage(b, NetChannel.ReliableInOrder1);

        }
        public static void S2C_SpawnMonster(NetServer s, NetConnection c, BaseNPC n)
        {
            NetBuffer b = s.CreateBuffer();
            b.Write((ushort)ProtocolServerToClient.SpawnMonster);
            Serializer.WriteNPC(b, n);
            c.SendMessage(b, NetChannel.ReliableInOrder1);
        }
        public static NetBuffer S2C_NPCUpdate(NetServer s, BaseNPC n)
        {
            NetBuffer b = s.CreateBuffer();
            b.Write((UInt16)ProtocolServerToClient.NPCUpdate);
            Serializer.WriteNPCUpdate(b, n);
            return b;
        }
        public static NetBuffer S2C_BattleResult(NetServer server, BattleResult battle)
        {
            NetBuffer b = server.CreateBuffer();
            b.Write((UInt16)ProtocolServerToClient.BattleResult);
            Serializer.WriteBattleResult(b, battle);
            return b;
        }
        public static NetBuffer S2C_PlayerStatus(NetServer server, Player p)
        {
            NetBuffer b = server.CreateBuffer();
            b.Write((UInt16)ProtocolServerToClient.PlayerStatusUpdate);
            Serializer.WritePlayerStatus(b, p);
            return b;
        }
        public static NetBuffer S2C_PlayerStatsUpdate(NetServer netServer, Player p)
        {
            NetBuffer b = netServer.CreateBuffer();
            b.Write((UInt16)ProtocolServerToClient.PlayerStatsUpdate);
            Serializer.WriteCharacterUpdate(b, p);
            return b;
        }
        /*
        public static NetBuffer S2C_MonsterTemplates(NetServer server, MonsterTemplate[] templates)
        {
            NetBuffer b = server.CreateBuffer();
            b.Write((ushort)ProtocolServerToClient.MonsterTemplates);
            Serializer.WriteMonsterTemplates(b, templates);
            return b;
        }
         */ 
        public static NetBuffer S2C_AreaNPCList(NetServer server, Area a)
        {
            int capacity = a.npcs.Count * 130; //approximation of space needed to avoid array copying
            NetBuffer b = server.CreateBuffer(capacity);
            b.Write((ushort)ProtocolServerToClient.AreaNPCList);
            Serializer.WriteAreaNPCList(b, a);
            return b;
        }
        #endregion Server to client









    }
}
