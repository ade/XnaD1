using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;
using Lidgren.Network;
using Lidgren.Network.Xna;
using Microsoft.Xna.Framework;
using DiacloLib.Importer;

namespace Diaclo
{
    public static class ClientSerializer
    {
        public static World ReadWorld(NetBuffer b)
        {
            World w = new World();
            w.Areas = new Area[b.ReadUInt16()];

            for (ushort i = 0; i < w.Areas.Length; i++)
            {
                ReadArea(b, w, i);
            }
            return w;
        }
        public static Area ReadArea(NetBuffer b, World w, ushort id)
        {
            String name = b.ReadString();
            UInt16 width = b.ReadUInt16(); // width (2bytes)
            UInt16 height = b.ReadUInt16(); // height (2bytes)
            Area a = new Area(width, height);
            a.Name = name;
            a.ID = id;
            w.Areas[id] = a;
            //Now comes the squares, each containing 16*2 bytes of info
            for (int i = 0; i < a.Squares.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                a.Squares[i] = ReadSquare(b);
                a.Squares[i].Position = new Point(x, y);
            }

            
            return a;
        }
        public static void ReadAreaNPCList(NetBuffer b, World w)
        {
            //Read npcs
            ushort areaid = b.ReadUInt16();
            Area a = w.Areas[areaid];
            int npcs = b.ReadInt32();
            a.npcs.Clear();
            for (int i = 0; i < npcs; i++)
            {
                a.npcs.Add(ReadNPC(b, w));
            }
        }
        public static BaseNPC ReadNPC(NetBuffer b, World w)
        {
            int id = b.ReadInt32();
            int areaid = b.ReadInt32();
            Direction direction = (Direction)b.ReadByte();
            Point position = b.ReadPoint();
            Point PositionDeviation = b.ReadPoint();
            Point TileMoveFrom = b.ReadPoint();
            float TileMoveProgress = b.ReadFloat();
            Point TileMoveTo = b.ReadPoint();

            ClientNPC n;
            n = (ClientNPC)w.GetNPCById(id);
            if (n == null)
                n = new ClientNPC(w, areaid, position, id);
          
            n.AttackSpeed = b.ReadFloat();
            n.HitRecoverySpeed = b.ReadFloat();
            n.WalkSpeed = b.ReadFloat();
            n.AnimationSpeedAttack = b.ReadFloat();
            n.AnimationSpeedDeath = b.ReadFloat();
            n.AnimationSpeedIdle = b.ReadFloat();
            n.AnimationSpeedRecovery = b.ReadFloat();
            n.AnimationSpeedWalk = b.ReadFloat();
            n.AnimationFile = b.ReadString();
            n.SoundFile = b.ReadString();
            n.TranslationFile = b.ReadString();
            n.SetState((AIState)b.ReadByte());
            n.SetAction((AIAction)b.ReadByte());
            n.UpdateAnimation();
            
            return (BaseNPC)n;
        }
        public static Square ReadSquare(NetBuffer b)
        {
            Square square = new Square();
            square.PassablePlayer = b.ReadBoolean();
            square.LevelID = b.ReadByte();
            for (int i = 0; i < square.Frame.Length; i++)
            {
                //one frame is 4 bytes
                square.Frame[i] = b.ReadUInt16();
            }
            return square;
        }
        public static void UpdateWorldCreature(NetBuffer b, WorldCreature o)
        {
            o.Direction = (Direction)b.ReadByte();
            o.SetLocation(o.AreaID, b.ReadPoint());
            o.PositionDeviation = b.ReadPoint();
            o.TileMoveFrom = b.ReadPoint();
            o.TileMoveProgress = b.ReadFloat();
            o.TileMoveTo = b.ReadPoint();
        }
        public static void UpdateNPC(NetBuffer b, World w)
        {
            int id = b.ReadInt32();
            
            BaseNPC n = w.GetNPCById(id);
            if (n != null)
            {
                UpdateWorldCreature(b, (WorldCreature)n);
                n.SetAction((AIAction)b.ReadByte());
                n.SetState((AIState)b.ReadByte());
            }
            else
            {
                GameConsole.Write("ClientSerializer.UpdateNPC: ID not found: " + id, ConsoleMessageTypes.Debug);
            }
        }

        internal static BattleResult ReadBattleResult(NetBuffer b, GameState gameState)
        {
            BattleResult result = new BattleResult();
            result.Type = (BattleTypes)b.ReadByte();
            result.Outcome = (BattleOutcome)b.ReadByte();
            switch (result.Type)
            {
                case BattleTypes.MonsterVsPlayer:
                    result.Attacker = gameState.World.GetNPCById(b.ReadInt32());
                    result.Target = gameState.Players[b.ReadInt32()];
                    break;
                case BattleTypes.PlayerVsMonster:
                    result.Attacker = gameState.Players[b.ReadInt32()];
                    result.Target = gameState.World.GetNPCById(b.ReadInt32());
                    break;
            }
            result.AttackerTakeDamage = b.ReadInt32();
            result.TargetTakeDamage = b.ReadInt32();
            result.TargetNewHP = b.ReadInt32();
            result.AttackerNewHP = b.ReadInt32();
            return result;
        }
        internal static void UpdatePlayerStatus(NetBuffer b, GameState gameState)
        {
            byte id = b.ReadByte();
            ClientPlayer p = (ClientPlayer)gameState.Players[id];
            p.OverrideStatus((PlayerAction)b.ReadByte(), (Direction)b.ReadByte(), b.ReadPoint(), b.ReadFloat(), b.ReadFloat(), b.ReadInt32());
        }
        internal static void ReadCharacterUpdate(NetBuffer b, Player p)
        {
            Character c = p.Character;
            c.AttDex = b.ReadUInt16();
            c.AttMag = b.ReadUInt16();
            c.AttStr = b.ReadUInt16();
            c.AttVit = b.ReadUInt16();
            c.Experience = b.ReadUInt32();
            c.Level = b.ReadUInt16();
            c.LevelUpPoints = b.ReadUInt16();
            p.RefreshAttributes();
        }
        
    }
}
