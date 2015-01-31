using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using DiacloServer.AITemplates;
using DiacloLib;
using DiacloLib.Importer;

namespace DiacloServer
{


    public struct PlayerThreatFraction {
        public ServerPlayer Player;
        public float Fraction;
        public PlayerThreatFraction(ServerPlayer pl, float percentage) {
            this.Player = pl;
            this.Fraction = percentage;
        }
    }
    /// <summary>
    /// Server class for non-player characters, contains more info than client version
    /// </summary>
    public abstract class ServerNPC: BaseNPC
    {
        protected NPCType type;
        protected Queue<Point> walkQueue;
        protected Player target;
        protected Point targetLastSeen;
        protected bool idle; //Means we need to think and decide what to do
        protected int expValue;
        internal Dictionary<ServerPlayer, int> playerThreat;
        protected int intelligence;
        protected int sightRadius = 10;

        //Parent template
        public MonsterTemplate Template { get; set; }

        //Public fields/properties
        public int ToHit;
        public int Level;
        public int DamageMin;
        public int DamageMax;
        public int ArmorClass;

        //All durations in seconds
        protected float hitSpeed; //Within attackspeed this is the time it takes to reach the frame where the actual hit on a target occurs.
        protected float actionElapsed;
        protected float actionEndTime;
        
        //public TilesetAnimation Animation { get; set; }
        protected Random rnd = new Random();

        public override void Update(float secondsPassed)
        {
            base.Update(secondsPassed);
            if (this.State == AIState.Active)
            {
                this.actionElapsed += secondsPassed;

                if (!this.idle && this.actionElapsed >= this.actionEndTime)
                {
                    if (this.Action == AIAction.Moving)
                    {
                        if (this.TileMoveProgress == 1)
                        {
                            this.idle = true;
                        }
                    }
                    else if (this.Action == AIAction.Dieing)
                    {
                        this.SetState(AIState.Dead);
                    }
                    else if (this.Action == AIAction.Attacking)
                    {
                        if (TargetInMeleeRange())
                        {
                            BattleResult result = BattleServer.MonsterVsPlayer(this, this.target);
                            Server.OnNPCAttack(this, result);
                        }
                        this.idle = true;
                    }
                    else
                    {
                        this.idle = true;
                        this.actionElapsed = 0;
                    }
                }

                this.RefreshEnemy();

                if (this.Action == AIAction.Delaying && !this.idle && this.target != null)
                {
                    FaceEnemy();
                }
            }
        }
        protected void MeleeAttack()
        {
            this.Direction = GameMechanics.AdjacentTileDirection(this.Position, this.target.Position);
            this.StartAction(AIAction.Attacking, this.attackSpeed);
        }
        protected bool TargetInMeleeRange()
        {
            if(this.target != null)
                return GameMechanics.MapTilesAdjacent(this.target.Position, this.Position);
            return false;
        }
        
        protected void Delay(float duration)
        {
            this.StartAction(AIAction.Delaying, duration);
        }
        protected virtual void MoveTowardEnemy()
        {
            if (this.State == AIState.Active && this.idle)
            {
                if (this.walkQueue != null && this.walkQueue.Count != 0)
                {
                    //We have a path (player's position hasn't changed), use it
                }
                else
                {
                    //Need a new path - todo: make a "dumb" navigation function
                    if (this.Position == this.targetLastSeen)
                    {
                        //We're at targets last position. Shut down!
                        this.SetState(AIState.Inactive);
                        this.StartAction(AIAction.Delaying, 0.1f);
                        GameConsole.Write("NPC giving up: went to last position and didnt find anything", ConsoleMessageTypes.Debug);
                    }
                    else
                    {
                        this.walkQueue = PathFinding.Navigate(this.Position, this.targetLastSeen, this.Area);
                    }
                }
                if (this.walkQueue == null)
                {
                    //No path possible

                    if (this.target == null)
                    {
                        //No path, no target: give up
                        this.SetState(AIState.Inactive);
                        this.StartAction(AIAction.Delaying, 0.1f);
                        GameConsole.Write("NPC giving up: couldn't get to last seen position", ConsoleMessageTypes.Debug);
                    } else
                      this.Delay(0.5f); //Wait (todo: move one step closer(dumb))
                }
                else if (this.walkQueue.Count > 0)
                {
                    Point go = this.walkQueue.Dequeue();
                    if (GameMechanics.PositionEnterable(this.Area, go))
                    {
                        if(GameMechanics.MapTilesAdjacent(this.Position, go)) {
                            this.StartMove(go, 1.0f / this.walkSpeed);
                            this.StartAction(AIAction.Moving, this.walkSpeed);
                            
                        } else {
                            this.walkQueue = null;
                            this.StartAction(AIAction.Delaying, 0.1f);
                        }
                    }
                    else
                    {
                        this.StartAction(AIAction.Delaying, 0.1f);
                    }
                    
                    
                }
            }
            else
            {
                GameConsole.Write("NPC was ordered to move toward enemy, but npc is busy. (" + this.State.ToString() + ", " + this.Action.ToString() + ")", ConsoleMessageTypes.Debug);
            }
        
        }
        
        protected void StartAction(AIAction a, float duration)
        {
            if (!this.idle && this.Action == AIAction.Moving && this.TileMoveProgress < 1)
            {
                this.RevertMove();
            }
            this.actionElapsed = 0;
            this.actionEndTime = duration;
            this.idle = false;
            this.SetAction(a);
            Server.OnNPCStatusChange(this);
        }
        
        private void FaceEnemy()
        {
            Direction face = GameMechanics.PointToDirection(this.Position.X, this.Position.Y, this.targetLastSeen.X, this.targetLastSeen.Y);
            if (face != this.Direction)
            {
                this.Direction = face;
                Server.OnNPCStatusChange(this);
            }
        }
        public ServerNPC(World w, int areaid, Point pos, int ID): base(w,areaid,pos,ID)
        {
            this.playerThreat = new Dictionary<ServerPlayer,int>();

        }
        public void MergeTemplate(MonsterTemplate t)
        {
            //Stats
            this.Level = t.MonsterLevel;
            this.MaxHP = rnd.Next(t.MinHP,t.MaxHP);
            this.CurrentHP = this.MaxHP;
            this.type = t.Type;
            this.ToHit = t.ToHit;
            this.sightRadius = 10;
            this.intelligence = t.Intelligence;
            this.hitSpeed = t.BeforeHitDuration();
            this.hitRecoverySpeed = t.RecoveryDuration();
            this.expValue = t.Experience;
            this.DamageMin = t.MinDamage;
            this.DamageMax = t.MaxDamage;
            this.attackSpeed = t.AttackDuration();
            this.ArmorClass = t.ArmorClass;
            this.walkSpeed = t.WalkDuration();

            this.AnimationSpeedAttack = GameMechanics.SpeedToDuration(t.AnimationSpeedAttack);
            this.AnimationSpeedDeath = GameMechanics.SpeedToDuration(t.AnimationSpeedDeath);
            this.AnimationSpeedIdle = GameMechanics.SpeedToDuration(t.AnimationSpeedIdle);
            this.AnimationSpeedRecovery = GameMechanics.SpeedToDuration(t.AnimationSpeedRecover);
            this.AnimationSpeedWalk = GameMechanics.SpeedToDuration(t.AnimationSpeedWalk);

            this.AnimationFile = t.Celfile;
            this.SoundFile = t.SoundFile;
            this.TranslationFile = t.TranslationFile;


        }
        public int calcToHit(int enemyLevel)
        {
            //30 + base + 2·(mlvl - clvl)
            return 30 + this.ToHit + 2 * (this.Level - enemyLevel);
        }
        public void RefreshEnemy()
        {
            if (target != null)
            {
                //Check if this target is still valid
                if (this.targetLastSeen != this.target.Position)
                {
                    //Target has moved, we need a new path
                    this.walkQueue = null;
                }
                if (EnemyValid(this.target))
                {
                    //Target OK, refresh last seen position
                    this.targetLastSeen = this.target.Position;
                }
                else
                {
                    //Enemy not found (out of sight)
                    //GameConsole.Write("lost target!", ConsoleMessageTypes.Debug);
                    this.target = PickNewEnemy();
                    if (this.target != null)
                    {
                        this.targetLastSeen = this.target.Position;
                        GameConsole.Write("found a new target", ConsoleMessageTypes.Debug);
                    }
                }
            }
            else
            {
                this.target = PickNewEnemy();
            }
        }

        protected Player PickNewEnemy()
        {
            
            foreach (Player p in this.Area.Players)
            {
                if(EnemyValid(p)) {
                    //GameConsole.Write("Found player " + p.ID, ConsoleMessageTypes.Debug);
                    return p;
                }
            }
            
            return null;
        }
        public bool EnemyValid(Player p)
        {
            if(PathFinding.FlightDistance(this.Position,p.Position) > p.LightRadius) 
                return false;
            else
                return true;

        }
        public override void Hurt(int amount, int newHP, WorldCreature offender)
        {
            base.Hurt(amount, newHP, offender); //subtract hp

            //Add damager to eligble for xp if nonexistant, or add damage caused
            if(offender is ServerPlayer) {
                ServerPlayer p = (ServerPlayer)offender;
                if (!this.playerThreat.ContainsKey(p))
                {
                    this.playerThreat.Add(p, amount);
                }
                else
                {
                    this.playerThreat[p] = this.playerThreat[p] + amount;
                }
            }

            if (this.CurrentHP <= 0)
            {
                this.StartAction(AIAction.Dieing, 1.0f);
                this.Die();

                //Award XP
                PlayerThreatFraction[] ptf = this.GetThreat();
                for (int i = 0; i < ptf.Length; i++)
                {
                    uint xp = (uint)Math.Floor(ptf[i].Fraction * this.expValue);
                    xp = ptf[i].Player.GainExp((int)xp, this.Level);

                    Server.OnPlayerGainXP(ptf[i].Player);
                }
            }
            else
            {
                if (amount > this.Level * 3)
                {
                    this.StartAction(AIAction.Stunned, this.HitRecoverySpeed);
                }
            }

        }
        public PlayerThreatFraction[] GetThreat()
        {
            int count = this.playerThreat.Count;
            int total = this.MaxHP;
            PlayerThreatFraction[] ret = new PlayerThreatFraction[count];
            int index = 0;
            foreach (KeyValuePair<ServerPlayer, int> kp in this.playerThreat)
            {
                float damageCaused = (float)kp.Value;
                if (damageCaused > total) damageCaused = total; //can't get more than 100% of damage done

                ret[index] = new PlayerThreatFraction(kp.Key, (float)((float)damageCaused / (float)total));
                index++;
            }
            return ret;
        }
        public static ServerNPC FromTemplate(World w, int areaID, Point position, MonsterTemplate t, int ID)
        {
            ServerNPC ret;
            switch (t.AIType)
            {
                case MonsterAITypes.Skeleton:
                default:
                    ret = new AISkeleton(w, areaID, position, ID);
                    break;
            }

            ret.MergeTemplate(t);
            ret.Template = t;

            return ret;
        }
        /// <summary>
        /// Complete current tile move
        /// </summary>
        public virtual void FinishMove()
        {
            this.Position = TileMoveTo;
            this.StopMove();
        }
        /// <summary>
        /// Cancel current tile move and return to origin
        /// </summary>
        public virtual void RevertMove()
        {
            Square origin = this.Area.GetSquare(this.TileMoveFrom);
            if (origin.getNPC() == this || (origin.getNPC() == null && origin.getPlayer() == null))
            {
                this.Position = this.TileMoveFrom;
            }
            this.StopMove();
        }
    }
}
