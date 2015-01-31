using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DiacloLib
{
    public enum PlayerAction
    {
        Idle,
        Walk,
        Melee,
        HitRecovery,
        Dead
    }
    public class Player: WorldCreature, IGameUpdateable
    {
        public static uint[] LevelExperience = new uint[] //Experience required for level up .. todo: read real values from source
        {
            //Level 1   2           3           4           5           6           7           8           9           10
            0,          2000,       4000,       8000,       12000,      18000,      25000,      35000,      47000,      63000,      //10
            83000,      110000,     140000,     180000,     230000,     310000,     420000,     570000,     760000,     1000000,    //20
            1300000,    1800000,    2000000,    3000000,    4000000,    5500000,    7000000,    10000000,   12000000,   15000000,   //30
            20000000,   25000000,   32000000,   42000000,   53000000,   67000000,   85000000,   100000000,  135000000,  170000000,  //40
            210000000,  260000000,  320000000,  400000000,  500000000,  600000000,  730000000,  900000000,  1100000000, 1300000000  //50
        };

        //Character contains primary (persistent) attributes
        public Character Character { get; set; }
        
        //Secondary attributes (derivative of primaries - recalculated when primaries are changed/items equipped etc)
        public int LightRadius = 10; //aggro range.
        private int _mana;
        private int _tohit;
        private int _armorclass;
        private int _blockchance;
        private int _dmgmin;
        private int _dmgmax;
        private float _attackspeed;
        private float _hitRecoverySpeed;

        protected float actionEndTime;
        protected float actionElapsed;
        protected Point actionTarget;

        public float ActionEndTime
        {
            get { return actionEndTime; }
        }
        public float ActionElapsed
        {
            get { return actionElapsed; }
        }
        public float HitRecoverySpeed
        {
            get { return _hitRecoverySpeed; }
        }
        public int Mana {
            get { return this._mana; }
        }
        public int ToHit { //displayed value only!!
            get { return this._tohit; }
        }
        public int ArmorClass {
            get { return this._armorclass; }
        }
        public int BlockChance {
            get { return this._blockchance; }
        }
        public int DamageMin {
            get { return this._dmgmin; }
        }
        public int DamageMax {
            get { return this._dmgmax; }
        }
        public float AttackSpeed
        {
            get { return this._attackspeed; }
        }
        
        protected PlayerAction action;
        public PlayerAction Action
        {
            get
            {
                return action;
            }
        }

        //Animation
        protected PlayerGraphicsAction GraphicsAction { get; set; }
        protected PlayerGraphicsArmor GraphicsArmor { get; set; }
        protected PlayerGraphicsWeapon GraphicsWeapon { get; set; }

        public Player()
        {

        }

        public void RefreshAttributes() {
            this.GraphicsArmor = PlayerGraphicsArmor.Light; //to be replaced.

            switch (this.Character.Class) {
                case CharacterClass.Warrior:
                default:
                    this.MaxHP = 2*this.Character.AttVit + 2*this.Character.Level + 18;
                    this._mana = this.Character.AttMag + this.Character.Level - 1;
                    this._tohit = 50 + this.Character.AttDex / 2; //displayed value. not used in hit roll
                    this._dmgmin = (int)Math.Ceiling((double)(this.Character.AttStr * this.Character.Level) / 200) + 2;
                    this._dmgmax = this._dmgmin; //without equipping a weapon, dmg will not be variable
                    //All classes
                    this._armorclass = this.Character.AttDex / 5;
                    this._blockchance = 0; //No shield
                    this._attackspeed = 0.50f;
                    this._hitRecoverySpeed = 0.3f;
                    break;
            }
        }

        public bool Walk(GameState gameState, Point request)
        {
            if (this.Action == PlayerAction.Idle && GameMechanics.MapTilesAdjacent(this.Position, request) && this.Position != request && GameMechanics.PositionEnterable(this.Area, request))
            {
                float walkspeed = 0.4f;
                this.StartMove(request, GameMechanics.DurationToDurationsPerSecond(walkspeed));
                this.StartAction(PlayerAction.Walk, walkspeed);
                return true;
            }
            return false;
        }
        public float GetTimeUntilIdle()
        {
            float ret = actionEndTime - actionElapsed;
            if (ret < 0) ret = 0;
            return ret;
        }

        public bool MeleeAttack(Point position)
        {
            bool result = false;
            //Check distance
            if (GameMechanics.MapTilesAdjacent(this.Position, position))
            {
                if (this.Action == PlayerAction.Idle)
                {
                    this.Direction = GameMechanics.AdjacentTileDirection(this.Position, position);
                    this.actionTarget = position;
                    this.StartAction(PlayerAction.Melee, this.AttackSpeed);
                    result = true;
                }
            }
            return result;
        }
        public virtual void ActionSuccessful(PlayerAction a)
        {
        }
        public virtual void ActionFailed(PlayerAction a)
        {
        }
        protected virtual void StartAction(PlayerAction playerAction, float duration)
        {
            PlayerAction old = this.Action;

            this.actionEndTime = duration;
            this.actionElapsed = 0;
            this.action = playerAction;
        }



        public override void Hurt(int amount, int newHP, WorldCreature offender)
        {
            this.CurrentHP = newHP;
            if (this.CurrentHP < 1 && this.Action != PlayerAction.Dead)
            {
                this.StartAction(PlayerAction.Dead, 0);
            } 
            else if (amount >= this.Character.Level)
            {
                this.StartAction(PlayerAction.HitRecovery, this.HitRecoverySpeed);
            }

        }
        public virtual void SetIdle()
        {
            this.StartAction(PlayerAction.Idle, 0);
            this.StopMove();
        }

        #region IGameUpdateable Members

        public override void Update(float secondsPassed)
        {
            base.Update(secondsPassed);

            this.actionElapsed += secondsPassed;

            //Check if current action has expired
            if (this.Action != PlayerAction.Idle && this.Action != PlayerAction.Dead && this.actionElapsed >= this.actionEndTime)
            {
                this.ActionSuccessful(this.action);
                this.StartAction(PlayerAction.Idle, 0);
                
            }
        }

        #endregion
    }
}
