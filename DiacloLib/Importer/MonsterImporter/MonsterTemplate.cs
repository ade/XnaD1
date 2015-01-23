using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer
{
    public enum MonsterAITypes
    {
        Zombie,
        Skeleton,
        Scavenger,
        Hidden,
        FallenOne,
        SkeletonArcher,
        GoatArcher,
        WingedFiend,
        Butcher,
        MagmaDemon,
        Incinerator,
        Balrog,
        Succubi,
        Gargoyle,
        LightningDemon,
        Mage,
        Viper,
        HornedDemon,
        Overlord,
        Spit,
        SkeletonKing,
        GoatMan,
    }
    /// <summary>
    /// Imported monster data 
    /// </summary>
    public class MonsterTemplate
    {
        //Format example (rows: index, description, value)
        //Fields with value such as "{type}" means there is either a dash (-) or a value of the specified type in the input file
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //0                     1               2                   3                               4                   5                               6               7                   8                   9               
        //name	                animsize	    seedsize	        celfile	                        2ndattack	        soundfile	                    specialsound	translation	        translationfile	    idleframeset	
        //Zombie	            128	            799	                Monsters\Zombie\Zombie%c.CL2	{bool}              Monsters\Zombie\Zombie%c%i.WAV	{bool}          {bool}     --->>    {string}            11	            
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //10                    11              12                  13                              14                  15                              16              17                  18                  19                             
        //walkframeset	        attackframeset	recoveryframeset	deathframeset	                2nd_attack_frameset	idle_speed	                    walk_speed	    attack_speed	    recovery_speed	    death_speed 	
        //24	                12	            6	                16	                            0	                4	                            0	            0	                0	                0	        	                
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //20                    21              22                  23                              24                  25                              26              27                  28                  29                  
        //2nd_attack_speed      min_dlvl	    max_dlvl	        mlvl	                        min_hp	            max_hp	                        attack_type	    special_abilities1	special_abilities2	special_abilities3	
        //0                     1	            2	                1	                            4	                7	                            Zombie	        0	                0	                0	                
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //30                    31              32                  33                              34                  35                              36              37                  38                  39          
        //special_abilities4	intelligence    tohit%	            tohit_frame	                    mindam	            maxdam	                        2nd_tohit%	    2nd_tohit_frame	    2nd_mindam	        2nd_maxdam		
        //0	                    0	            10	                8	                            2	                5	                            0	            0	                0	                0	        	
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //40                    41              42                  43                              44                  45                              46
        //ac                    type	        norm_resist	        hell_resist	                    special_drop	    selection_outline	            experience
        //5                     undead	        I--	                I--	                            0	                3	                            54		
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// ID/Index of monstertype
        /// </summary>
        public int Index { get; set; }
        
        /// <summary>
        /// 0 name (string): Name as presented to player
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 1 animsize (number): Width in pixels of the animation (CL2 file in index 3)
        /// </summary>
        public byte Animsize { get; set; }    

        /// <summary>
        /// 2 seedsize (number): Used in random level generation; see jarulf's guide.
        /// </summary>
        public int Seedsize { get; set; }

        /// <summary>
        /// 3 celfile (string): Animation filenames in MPQ
        /// </summary>
        public string Celfile { get; set; }

        /// <summary>
        /// 4 2ndattack (bool): Does mob have second attack swing? The secondary attack can either be in the same animation as the primary (suffix 'A'), or it can be in the "special animation file" (suffix 'S').
        /// </summary>
        public bool HasSecondAttack { get; set; }

        /// <summary>
        /// 5 soundfile (string): Sound filenames in mpq
        /// </summary>
        public string SoundFile { get; set; }

        /// <summary>
        /// 6 specialsound (bool): Has a special sound?
        /// </summary>
        public bool HasSpecialSound { get; set; }

        /// <summary>
        /// 7 translation (bool): Use a color transformation file?
        /// </summary>
        public bool HasTranslation { get; set; }

        /// <summary>
        /// 8 translationfile (string): Name of translation file to apply if specified true in previous entry
        /// </summary>
        public string TranslationFile { get; set; }

        /// <summary>
        /// 9 idleframeset (number): Amount of frames in the idle animation (per direction)
        /// </summary>
        public byte FramesIdle { get; set; }
        /// <summary>
        /// 10 walkframeset (number): Amount of frames in the walk animation (per direction)
        /// </summary>
        public byte FramesWalk { get; set; }
        /// <summary>
        /// 11 attackframeset (number): Amount of frames in the attack animation (per direction)
        /// </summary>
        public byte FramesAttack { get; set; }
        /// <summary>
        /// 12 recoveryframeset (number): Amount of frames in the recovery animation (per direction)
        /// </summary>
        public byte FramesRecovery { get; set; }
        /// <summary>
        /// 13 deathframeset (number): Amount of frames in the death animation (per direction)
        /// </summary>
        public byte FramesDeath { get; set; }
        /// <summary>
        /// 14 2ndattack_frameset (number): Amount of frames in the secondary attack animation (per direction)
        /// </summary>
        public byte FramesSecondaryAttack { get; set; }

        /// <summary>
        /// 15 idle_speed (speed): Playback rate of idle anim
        /// </summary>
        public byte AnimationSpeedIdle { get; set; }

        /// <summary>
        /// 16 walk_speed (speed): Playback rate of walk anim
        /// </summary>
        public byte AnimationSpeedWalk { get; set; }

        /// <summary>
        /// 17 attack_speed (speed): Playback rate of attack anim
        /// </summary>
        public byte AnimationSpeedAttack { get; set; }

        /// <summary>
        /// 18 recovery_speed (speed): Playback rate of recovery anim
        /// </summary>
        public byte AnimationSpeedRecover { get; set; }

        /// <summary>
        /// 19 death_speed (speed): Playback rate of death anim
        /// </summary>
        public byte AnimationSpeedDeath { get; set; }

        /// <summary>
        /// 20 2nd_attack_speed (speed): Playback rate of 2nd attack anim
        /// </summary>
        public byte AnimationSpeedSecondaryAttack { get; set; }

        /// <summary>
        /// 21 min_dlvl (dungeon level number): Minimum level to spawn on
        /// </summary>
        public byte MinDungeonLevel { get; set; }

        /// <summary>
        /// 22 max_dlvl (dungeon level number): Maximum level to spawn on
        /// </summary>
        public byte MaxDungeonLevel { get; set; }

        /// <summary>
        /// 23 mlvl (level number): Level of monster
        /// </summary>
        public int MonsterLevel { get; set; }

        /// <summary>
        /// 24 min_hp (number): Minimum health
        /// </summary>
        public int MinHP { get; set; }

        /// <summary>
        /// 25 max_hp (number): Maximum health
        /// </summary>
        public int MaxHP { get; set; }

        /// <summary>
        /// 26 attack_type (string): AI Type
        /// </summary>
        public MonsterAITypes AIType { get; set; }

        //27-30 special abilities (number): unknown

        /// <summary>
        /// 31 intelligence (number): intelligence factor (used by ai script)
        /// </summary>
        public byte Intelligence { get; set; }

        /// <summary>
        /// 32 tohit% (number): base chance to hit
        /// </summary>
        public int ToHit { get; set; }

        /// <summary>
        /// 33 tohit_frame (number): The frame number of the attack animation that the actual attack occurs
        /// </summary>
        public byte HitFrame { get; set; }

        /// <summary>
        /// 34 mindam (number): Minimum damage (primary attack)
        /// </summary>
        public int MinDamage { get; set; }

        /// <summary>
        /// 35 maxdam (number): Maximum damage (primary attack)
        /// </summary>
        public int MaxDamage { get; set; }

        /// <summary>
        /// 36 2nd_tohit (number): Secondary attack hit chance
        /// </summary>
        public int ToHitSecondary { get; set; }

        /// <summary>
        /// 37 2nd_tohit_frame (number): Frame number of the secondary attack animation where the attack occurs
        /// </summary>
        public byte HitFrameSecondary { get; set; }

        /// <summary>
        /// 38 2nd_mindam (number): Minimum damage of secondary attack
        /// </summary>
        public int MinDamageSecondary { get; set; }

        /// <summary>
        /// 39 2nd_maxdam (number): Maximum damage of secondary attack
        /// </summary>
        public int MaxDamageSecondary { get; set; }

        /// <summary>
        /// 40 ac (number) Armor class.
        /// </summary>
        public int ArmorClass { get; set; }

        /// <summary>
        /// 41 type (string): Monster type
        /// </summary>
        public NPCType Type { get; set; }

        /// <summary>
        /// 42 resistancesnormal (string): R= resist I= immune, order is Magic, fire, lightning. In byte = nothing,nothing,immunemagic,immunefire,immunelightning,resistmagic,resistfire,resistlightning
        /// </summary>
        public byte NormalResistances { get; set; }

        /// <summary>
        /// 43 Hell diffulty resistances (string): R= resist I= immune, order is Magic, fire, lightning. In byte = nothing,nothing,immunemagic,immunefire,immunelightning,resistmagic,resistfire,resistlightning
        /// </summary>
        public byte HellResistances { get; set; }

        // 44 special drop, unknown
        // 45 selection outline, unknown

        /// <summary>
        /// 46 experience (number): Base experience value
        /// </summary>
        public int Experience { get; set; }


        public const byte RESIST_LIGHTNING = 1;
        public const byte RESIST_FIRE = 2;
        public const byte RESIST_MAGIC = 4;
        public const byte IMMUNE_LIGHTNING = 8;
        public const byte IMMUNE_FIRE = 16;
        public const byte IMMUNE_MAGIC = 32;

        public bool ResistsMagicNormal()
        {
            return (this.NormalResistances & RESIST_MAGIC) > 0;
        }
        public bool ResistsFireNormal()
        {
            return (this.NormalResistances & RESIST_FIRE) > 0;
        }
        public bool ResistsLightningNormal()
        {
            return (this.NormalResistances & RESIST_LIGHTNING) > 0;
        }
        public bool ImmuneMagicNormal()
        {
            return (this.NormalResistances & IMMUNE_MAGIC) > 0;
        }
        public bool ImmuneFireNormal()
        {
            return (this.NormalResistances & IMMUNE_FIRE) > 0;
        }
        public bool ImmuneLightningNormal()
        {
            return (this.NormalResistances & IMMUNE_LIGHTNING) > 0;
        }
        public bool ResistsMagicHell()
        {
            return (this.HellResistances & RESIST_MAGIC) > 0;
        }
        public bool ResistsFireHell()
        {
            return (this.HellResistances & RESIST_FIRE) > 0;
        }
        public bool ResistsLightningHell()
        {
            return (this.HellResistances & RESIST_LIGHTNING) > 0;
        }
        public bool ImmuneMagicHell()
        {
            return (this.HellResistances & IMMUNE_MAGIC) > 0;
        }
        public bool ImmuneFireHell()
        {
            return (this.HellResistances & IMMUNE_FIRE) > 0;
        }
        public bool ImmuneLightningHell()
        {
            return (this.HellResistances & IMMUNE_LIGHTNING) > 0;
        }
        public float AttackDuration()
        {
            return GameMechanics.SpeedToDuration(this.AnimationSpeedAttack) * this.FramesAttack;
        }
        public float RecoveryDuration()
        {
            return GameMechanics.SpeedToDuration(this.AnimationSpeedRecover) * this.FramesRecovery;
        }
        public float WalkDuration()
        {
            return GameMechanics.SpeedToDuration(this.AnimationSpeedWalk) * this.FramesWalk;
        }
        public float DeathDuration()
        {
            return GameMechanics.SpeedToDuration(this.AnimationSpeedDeath) * this.FramesDeath;
        }
        public float SecondaryAttackDuration()
        {
            return GameMechanics.SpeedToDuration(this.AnimationSpeedSecondaryAttack) * this.FramesSecondaryAttack;
        }
        public float BeforeHitDuration()
        {
            return GameMechanics.SpeedToDuration(this.AnimationSpeedAttack) * this.HitFrame;
        }
    }
}
