using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib
{
    public enum BattleOutcome
    {
        Hit,
        Miss,
        Immune
    }
    public enum BattleTypes
    {
        MonsterVsPlayer,
        PlayerVsMonster
    }
    public class BattleResult
    {
        public BattleTypes Type;
        public BattleOutcome Outcome;
        public int BaseDamage;
        public int AttackerTakeDamage;
        public int TargetTakeDamage;
        public int AttackerNewHP;
        public int TargetNewHP;
        public WorldCreature Attacker;
        public WorldCreature Target;
        public BattleResult()
        {

        }
        public BattleResult(WorldCreature attacker, WorldCreature target)
        {
            this.Attacker = attacker;
            this.Target = target;
            this.AttackerNewHP = attacker.CurrentHP;
            this.TargetNewHP = target.CurrentHP;
        }
        public void Apply()
        {
            if (this.AttackerTakeDamage != 0) 
                this.Attacker.Hurt(this.AttackerTakeDamage, this.AttackerNewHP, this.Target);
            if (this.TargetTakeDamage != 0)
                this.Target.Hurt(this.TargetTakeDamage, this.TargetNewHP, this.Attacker);
        }
    }
}
