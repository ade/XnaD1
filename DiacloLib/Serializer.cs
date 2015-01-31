using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
using Lidgren.Network.Xna;
using Microsoft.Xna.Framework;
using DiacloLib.Importer;
namespace DiacloLib
{
    public class Serializer
    {
        public static void WriteCharacter(NetBuffer b, Character c)
        {
            b.Write(c.Name);
            b.Write((int)c.Level);
            b.Write((ushort)c.AttStr);
            b.Write((ushort)c.AttDex);
            b.Write((ushort)c.AttMag);
            b.Write((ushort)c.AttVit);
            b.Write((ushort)c.LevelUpPoints);
            b.Write((uint)c.Experience);
        }
        public static void ReadCharacter(NetBuffer b, Character c)
        {
            c.Name = b.ReadString();
            c.Level = b.ReadInt32();
            c.AttStr = b.ReadUInt16();
            c.AttDex = b.ReadUInt16();
            c.AttMag = b.ReadUInt16();
            c.AttVit = b.ReadUInt16();
            c.LevelUpPoints = b.ReadUInt16();
            c.Experience = b.ReadUInt32();
        }
        public static Character ReadCharacter(NetBuffer b)
        {
            Character p = new Character();
            ReadCharacter(b,p);
            return p;
        }

        public static void WriteWorld(NetBuffer b, World w)
        {
            b.Write((ushort)w.Areas.Length);
            for (int i = 0; i < w.Areas.Length; i++)
            {
                WriteArea(b, w.Areas[i]);
            }
        }

        public static void WriteSquare(NetBuffer b, Square square)
        {
            b.Write(square.PassablePlayer);
            b.Write((byte)square.LevelID);
            for (int i = 0; i < square.Frame.Length; i++) //16 loops
            {
                //one frame is 2 bytes
                b.Write(square.Frame[i]);
            }
        }
        public static void WriteArea(NetBuffer b, Area a)
        {
            b.Write(a.Name);
            b.Write(a.Width); // width (2bytes)
            b.Write(a.Height); //height 2 bytes
            //Now comes the squares, each containing 16*2 bytes of info
            for (int i = 0; i < a.Squares.Length; i++)
            {
                WriteSquare(b, a.Squares[i]);
            }
            

        }
        public static void WritePoint(NetBuffer b, Point m)
        {
            b.Write(m.X);
            b.Write(m.Y);
        }
        public static Point ReadPoint(NetBuffer b)
        {
            Point m = new Point();
            m.X = b.ReadInt32();
            m.Y = b.ReadInt32();
            return m;
        }
        public static void WriteNPC(NetBuffer b, BaseNPC n)
        {
            b.Write((int)n.ID);
            b.Write((int)n.AreaID);
            b.Write((byte)n.Direction);
            b.Write((Point)n.Position);
            b.Write((Point)n.PositionDeviation);
            b.Write((Point)n.TileMoveFrom);
            b.Write((float)n.TileMoveProgress);
            b.Write((Point)n.TileMoveTo);

            b.Write((float)n.AttackSpeed);
            b.Write((float)n.HitRecoverySpeed);
            b.Write((float)n.WalkSpeed);
            b.Write((float)n.AnimationSpeedAttack);
            b.Write((float)n.AnimationSpeedDeath);
            b.Write((float)n.AnimationSpeedIdle);
            b.Write((float)n.AnimationSpeedRecovery);
            b.Write((float)n.AnimationSpeedWalk);
            b.Write((string)n.AnimationFile);
            b.Write((string)n.SoundFile);
            b.Write((string)n.TranslationFile);
            b.Write((byte)n.State);
            b.Write((byte)n.Action);
            
            
        }

        public static void WriteWorldCreature(NetBuffer b, WorldCreature o)
        {
            b.Write((byte)o.Direction);
            b.Write(o.Position);
            b.Write(o.PositionDeviation);
            b.Write(o.TileMoveFrom);
            b.Write(o.TileMoveProgress);
            b.Write(o.TileMoveTo);
            
        }

        public static void WriteNPCUpdate(NetBuffer b, BaseNPC n)
        {
            b.Write(n.ID);
            WriteWorldCreature(b, (WorldCreature)n);
            b.Write((byte)n.Action);
            b.Write((byte)n.State);
        }

        public static void WriteBattleResult(NetBuffer b, BattleResult battle)
        {
            b.Write((byte)battle.Type);
            b.Write((byte)battle.Outcome);
            b.Write((int)battle.Attacker.ID);
            b.Write((int)battle.Target.ID);
            b.Write((int)battle.AttackerTakeDamage);
            b.Write((int)battle.TargetTakeDamage);
            b.Write((int)battle.TargetNewHP);
            b.Write((int)battle.AttackerNewHP);
        }


        public static void WritePlayerStatus(NetBuffer b, Player p)
        {
            b.Write((byte)p.ID);
            b.Write((byte)p.Action);
            b.Write((byte)p.Direction);
            b.Write((Point)p.Position);
            b.Write((float)p.ActionElapsed);
            b.Write((float)p.ActionEndTime);
            b.Write((int)p.CurrentHP);
        }
        public static void WriteCharacterUpdate(NetBuffer b, Player p)
        {
            Character c = p.Character;
            b.Write((ushort)c.AttDex);
            b.Write((ushort)c.AttMag);
            b.Write((ushort)c.AttStr);
            b.Write((ushort)c.AttVit);
            b.Write((uint)c.Experience);
            b.Write((ushort)c.Level);
            b.Write((ushort)c.LevelUpPoints);
            
        }

        /*
        public static void WriteMonsterTemplate(NetBuffer b, MonsterTemplate t)
        {
            b.Write((ushort)t.Index);
            b.Write((string)t.Name);
            b.Write((string)t.SoundFile);
            b.Write((string)t.TranslationFile);
            b.Write((string)t.Celfile);
          
            b.Write((bool)t.HasSecondAttack);
            b.Write((bool)t.HasSpecialSound);
            b.Write((bool)t.HasTranslation);
            
            b.Write((byte)t.Animsize);

            b.Write((byte)t.FramesAttack);
            b.Write((byte)t.FramesDeath);
            b.Write((byte)t.FramesIdle);
            b.Write((byte)t.FramesRecovery);
            b.Write((byte)t.FramesSecondaryAttack);
            b.Write((byte)t.FramesWalk);

            b.Write((byte)t.AnimationSpeedAttack);
            b.Write((byte)t.AnimationSpeedDeath);
            b.Write((byte)t.AnimationSpeedIdle);
            b.Write((byte)t.AnimationSpeedRecover);
            b.Write((byte)t.AnimationSpeedSecondaryAttack);
            b.Write((byte)t.AnimationSpeedWalk);
            
        }
        public static MonsterTemplate ReadMonsterTemplate(NetBuffer b)
        {
            MonsterTemplate t = new MonsterTemplate();
            t.Index = b.ReadUInt16();
            t.Name = b.ReadString();
            t.SoundFile = b.ReadString();
            t.TranslationFile = b.ReadString();
            t.Celfile = b.ReadString();

            t.HasSecondAttack = b.ReadBoolean();
            t.HasSpecialSound = b.ReadBoolean();
            t.HasTranslation = b.ReadBoolean();

            t.Animsize = b.ReadByte();

            t.FramesAttack = b.ReadByte();
            t.FramesDeath = b.ReadByte();
            t.FramesIdle = b.ReadByte();
            t.FramesRecovery = b.ReadByte();
            t.FramesSecondaryAttack = b.ReadByte();
            t.FramesWalk = b.ReadByte();

            t.AnimationSpeedAttack = b.ReadByte();
            t.AnimationSpeedDeath = b.ReadByte();
            t.AnimationSpeedIdle = b.ReadByte();
            t.AnimationSpeedRecover = b.ReadByte();
            t.AnimationSpeedSecondaryAttack = b.ReadByte();
            t.AnimationSpeedWalk = b.ReadByte();

            return t;
        }
        public static void WriteMonsterTemplates(NetBuffer b, MonsterTemplate[] templates)
        {
            ushort count = (ushort)templates.Length;
            b.Write((ushort)count);

            for (int i = 0; i < count; i++)
            {
                WriteMonsterTemplate(b, templates[i]);
            }
        }
        public static MonsterTemplate[] ReadMonsterTemplates(NetBuffer b)
        {
            ushort count = (ushort)b.ReadUInt16();
            MonsterTemplate[] ret = new MonsterTemplate[count];

            for (int i = 0; i < count; i++)
            {
                ret[i] = ReadMonsterTemplate(b);
            }
            return ret;
        }
        */
        internal static void WriteAreaNPCList(NetBuffer b, Area a)
        {
            b.Write((ushort)a.ID);
            b.Write(a.npcs.Count);
            foreach (BaseNPC n in a.npcs)
            {
                WriteNPC(b, n);
            }
        }
    }
}
