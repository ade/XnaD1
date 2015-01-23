using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using DiacloLib.Importer;

namespace DiacloLib
{
    public enum AIAction
    {
        Delaying,
        Moving,
        Attacking,
        Stunned,
        Dieing
    }
    public enum AIState
    {
        Inactive, //Server: inactive enemies will not look for targets or otherwise update themselves.
        Active,
        Dead
    }
    public enum NPCType
    {
        Undead,
        Animal,
        Demon
    }
 
    public class BaseNPC: WorldCreature
    {
        public string Name;
        private AIAction action;
        private AIState state;
        protected float walkSpeed; // Duration in seconds to move one tile
        protected float hitRecoverySpeed; //Time to recover after a stunning attack
        protected float attackSpeed; //Full time to make one attack

        //Update intervals (duration in seconds per frame)
        public float AnimationSpeedAttack { get; set; }
        public float AnimationSpeedDeath { get; set; }
        public float AnimationSpeedIdle { get; set; }
        public float AnimationSpeedRecovery { get; set; }
        public float AnimationSpeedWalk { get; set; }

        //Resource locations
        public string AnimationFile { get; set; }
        public string SoundFile { get; set; }
        public string TranslationFile { get; set; }

        //public properties
        public AIAction Action
        {
            get { return this.action; }
        }
        public AIState State
        {
            get { return this.state; }
        }
        public float AttackSpeed
        {
            get { return this.attackSpeed; }
            set { this.attackSpeed = value; }
        }
        public float HitRecoverySpeed
        {
            get { return this.hitRecoverySpeed; }
            set { this.hitRecoverySpeed = value; }
        }
        public float WalkSpeed
        {
            get { return this.walkSpeed; }
            set { this.walkSpeed = value; }
        }

        public override void Update(float secondsPassed)
        {
            //if (this.action == AIAction.Moving && this.TileMoveProgress < 1)
            base.Update(secondsPassed);
        }
        public virtual void SetAction(AIAction a) {
            this.action = a;
        }
        public virtual void SetState(AIState a)
        {
            this.state = a;
        }
        public override void Hurt(int amount, int newHP, WorldCreature offender)
        {
            this.CurrentHP = newHP;
        }
        public virtual void Die()
        {
            this.Area.GetSquare(this.Position).Corpse = this;
            this.Area.GetSquare(this.Position).Creature = null;
        }



    }
}
