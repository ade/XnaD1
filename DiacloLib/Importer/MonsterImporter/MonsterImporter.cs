using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer.MonsterImporter
{
    public static class MonsterImporter
    {
        /// <summary>
        /// Import monster data from EXE dump (from Game EXE via Jarulf's DView). Currently replaced by some placeholders.
        /// </summary>
        /// <returns></returns>
        public static MonsterTemplate[] GetMonsters()
        {
            MonsterTemplate[] ret = new MonsterTemplate[3];
            
            //This part is intended to be replaced by a predefined database. Demo code follows.

            //Skeleton
            MonsterTemplate skeleton = new MonsterTemplate();
            skeleton.Index = 0;
            skeleton.AIType = MonsterAITypes.Skeleton;
            skeleton.AnimationSpeedAttack = 0;
            skeleton.AnimationSpeedDeath = 0;
            skeleton.AnimationSpeedIdle = 5;
            skeleton.AnimationSpeedRecover = 0;
            skeleton.AnimationSpeedWalk = 0;
            skeleton.FramesIdle = 12;
            skeleton.FramesWalk = 8;
            skeleton.FramesAttack = 13;
            skeleton.FramesRecovery = 6;
            skeleton.FramesDeath = 17;
            skeleton.Animsize = 128;
            skeleton.ArmorClass = 0;
            skeleton.Celfile = @"Monsters\SkelAxe\SklAx%c.CL2";
            skeleton.Experience = 60;
            skeleton.HasTranslation = true;
            skeleton.HitFrame = 8;
            skeleton.Intelligence = 0;
            skeleton.MaxDamage = 4;
            skeleton.MinDamage = 2;
            skeleton.MinDungeonLevel = 1;
            skeleton.MaxDungeonLevel = 2;
            skeleton.MinHP = 5;
            skeleton.MaxHP = 10;
            skeleton.MonsterLevel = 1;
            skeleton.Name = "Skeleton";
            skeleton.SoundFile = @"Monsters\SkelAxe\SklAx%c%i.WAV";
            skeleton.ToHit = 20;
            skeleton.TranslationFile = @"Monsters\SkelAxe\White.TRN";
            skeleton.Type = NPCType.Undead;

            //zombie
            MonsterTemplate zombie = new MonsterTemplate();
            zombie.Index = 1;
            zombie.AIType = MonsterAITypes.Zombie;
            zombie.AnimationSpeedAttack = 0;
            zombie.AnimationSpeedDeath = 0;
            zombie.AnimationSpeedIdle = 4;
            zombie.AnimationSpeedRecover = 0;
            zombie.AnimationSpeedWalk = 0;
            zombie.FramesIdle = 11;
            zombie.FramesAttack = 12;
            zombie.FramesWalk = 24;
            zombie.FramesRecovery = 6;
            zombie.FramesDeath = 16;
            zombie.Animsize = 128;
            zombie.ArmorClass = 0;
            zombie.Celfile = @"Monsters\Zombie\Zombie%c.CL2";
            zombie.Experience = 120;
            zombie.HitFrame = 8;
            zombie.Intelligence = 0;
            zombie.MaxDamage = 8;
            zombie.MinDamage = 4;
            zombie.MinDungeonLevel = 1;
            zombie.MaxDungeonLevel = 2;
            zombie.MinHP = 10;
            zombie.MaxHP = 20;
            zombie.MonsterLevel = 2;
            zombie.Name = "Zombie";
            zombie.SoundFile = @"Monsters\Zombie\Zombie%c%i.WAV";
            zombie.ToHit = 30;
            zombie.Type = NPCType.Undead;

            //fallen one
            MonsterTemplate falspear = new MonsterTemplate();
            falspear.Index = 2;
            falspear.AIType = MonsterAITypes.Skeleton;
            falspear.AnimationSpeedAttack = 0;
            falspear.AnimationSpeedDeath = 0;
            falspear.AnimationSpeedIdle = 3;
            falspear.AnimationSpeedRecover = 0;
            falspear.AnimationSpeedWalk = 0;
            falspear.FramesIdle = 11;
            falspear.FramesWalk = 11;
            falspear.FramesAttack = 13;
            falspear.FramesRecovery = 11;
            falspear.FramesDeath = 18;
            falspear.Animsize = 128;
            falspear.ArmorClass = 0;
            falspear.Celfile = @"Monsters\FalSpear\Phall%c.CL2";
            falspear.Experience = 60;
            falspear.HasTranslation = true;
            falspear.HitFrame = 7;
            falspear.Intelligence = 0;
            falspear.MaxDamage = 15;
            falspear.MinDamage = 10;
            falspear.MinDungeonLevel = 1;
            falspear.MaxDungeonLevel = 2;
            falspear.MinHP = 15;
            falspear.MaxHP = 25;
            falspear.MonsterLevel = 4;
            falspear.Name = "Fallen one";
            falspear.SoundFile = @"Monsters\FalSpear\Phall%c%i.WAV";
            falspear.ToHit = 30;
            falspear.TranslationFile = @"Monsters\FalSpear\FallenT.TRN";
            falspear.Type = NPCType.Demon;

            ret[0] = skeleton;
            ret[1] = zombie;
            ret[2] = falspear;
            return ret;
        }
    }
}
