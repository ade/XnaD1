using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Diaclo
{
    public enum NPCSound
    {
        Attack1,
        Attack2,
        Hit1,
        Hit2,
        Death1,
        Death2,
        Special1,
        Special2
    }
    /// <summary>
    /// The ClientNPC adds animation data to a abstract npc.
    /// </summary>
    class ClientNPC: BaseNPC, IGameEntity
    {
        private TilesetAnimation Animation;
        private Direction lastDirection;
        private string[] sounds;
        private Random rnd = new Random();
        public ClientNPC(World w, int areaid, Point position, int ID): base(w,areaid,position,ID)
        {
            this.SetState(AIState.Active); //Client mode: always active
            
        }
        public override void SetAction(AIAction a)
        {
            ActionSound(this.Action, a);
            bool updated = false;
            if (this.Action != a)
                updated = true;

            base.SetAction(a);
            if (updated) this.UpdateAnimation();

            

        }
        public override void SetState(AIState a)
        {
            if (this.State == AIState.Active && a == AIState.Inactive)
            {
                //Can't be set inactive on client
            }
            else base.SetState(a);

            if (this.State == AIState.Dead)
            {
                this.Die();
                this.UpdateAnimation();
            }
            
        }
        /// <summary>
        /// Load a new animation
        /// </summary>
        private Tileset GetAnimation(AIAction a, Direction d)
        {
            int direction_number = (int)d;

            string clfile = this.AnimationFile.Replace("%c", GetActionFile(a));
            string key = clfile + "_" + direction_number;

            Tileset tileset;

            if (!GameContent.MonsterTilesets.TryGetValue(key, out tileset))
            {
                tileset = GameContent.BuildCreatureTileset(clfile, d);
                GameContent.MonsterTilesets.Add(key, tileset);
            }

            return tileset;
        }
        public void UpdateAnimation()
        {
            if (this.Animation == null)
            {
                Tileset t = GetAnimation(this.Action, this.Direction);
                this.Animation = new TilesetAnimation(t, 0, t.Count() - 1, 1);
            }

            if (this.State == AIState.Dead)
            {
                this.Animation.Tileset = GetAnimation(AIAction.Dieing, this.Direction);
                this.Animation.EndFrame = this.Animation.Tileset.Count() - 1;
                this.Animation.CurrentFrame = this.Animation.EndFrame;
                this.Animation.UpdateInterval = -1;
            }
            else
            {
                this.Animation.Tileset = GetAnimation(this.Action, this.Direction);
                this.Animation.CurrentFrame = 0;
                this.Animation.EndFrame = this.Animation.Tileset.Count() - 1;
            }

            switch (this.Action)
            {
                case AIAction.Attacking:
                    this.Animation.UpdateInterval = this.AnimationSpeedAttack; //1.0f / ((this.Animation.Tileset.Count()) / this.attackSpeed);
                    break;
                case AIAction.Moving:
                    this.Animation.UpdateInterval = this.AnimationSpeedWalk; //1.0f / ((this.Animation.Tileset.Count()) / this.walkSpeed);
                    break;
                case AIAction.Delaying:
                    this.Animation.UpdateInterval = this.AnimationSpeedIdle;
                    break;
                case AIAction.Dieing:
                    this.Animation.UpdateInterval = this.AnimationSpeedDeath;
                    break;
                case AIAction.Stunned:
                    this.Animation.UpdateInterval = this.AnimationSpeedRecovery; //GameMechanics.DurationToUpdateInterval(this.HitRecoverySpeed, this.Animation.Tileset.Count());
                    break;
            }
            
        }

        /// <summary>
        /// Get the filename suffix for an action
        /// </summary>
        private string GetActionFile(AIAction a)
        {
            string ret;
            switch(a) {
                case AIAction.Attacking:
                    ret = "a";
                    break;
                case AIAction.Delaying:
                    ret = "n";
                    break;
                case AIAction.Dieing:
                    ret = "d";
                    break;
                case AIAction.Moving:
                    ret = "w";
                    break;
                case AIAction.Stunned:
                    ret = "h";
                    break;
                default://special
                    ret = "s";
                    break;
            }
            return ret;
        }

        /// <summary>
        /// Load all relevant tilesets to game cache
        /// </summary>
        /// <returns></returns>
        public void CacheTilesets()
        {
            for(int d = 0; d < 8; d++)
                GetAnimation(AIAction.Attacking, (Direction)d);
            for (int d = 0; d < 8; d++)
                GetAnimation(AIAction.Delaying, (Direction)d);
            for (int d = 0; d < 8; d++)
                GetAnimation(AIAction.Dieing, (Direction)d);
            for (int d = 0; d < 8; d++)
                GetAnimation(AIAction.Moving, (Direction)d);
            for (int d = 0; d < 8; d++)
                GetAnimation(AIAction.Stunned, (Direction)d);
        }

        public string[] CacheSounds()
        {
            this.sounds = new string[8];
            this.sounds[(int)NPCSound.Attack1] = this.SoundFile.Replace("%c%i", "a1");
            this.sounds[(int)NPCSound.Attack2] = this.SoundFile.Replace("%c%i", "a2");
            this.sounds[(int)NPCSound.Death1] = this.SoundFile.Replace("%c%i", "d1");
            this.sounds[(int)NPCSound.Death2] = this.SoundFile.Replace("%c%i", "d2");
            this.sounds[(int)NPCSound.Hit1] = this.SoundFile.Replace("%c%i", "h1");
            this.sounds[(int)NPCSound.Hit2] = this.SoundFile.Replace("%c%i", "h2");
            this.sounds[(int)NPCSound.Special1] = this.SoundFile.Replace("%c%i", "s1");
            this.sounds[(int)NPCSound.Special2] = this.SoundFile.Replace("%c%i", "s2");

            return this.sounds;
        }

        public override void Die()
        {
            base.Die();
            
        }
        public override void Hurt(int amount, int newHP, WorldCreature offender)
        {
            base.Hurt(amount, newHP, offender);
            if (rnd.Next(2) == 0)
                Game.CueSound(this.sounds[(int)NPCSound.Hit1]);
            else
                Game.CueSound(this.sounds[(int)NPCSound.Hit2]);
        }
        private void ActionSound(AIAction old, AIAction a)
        {
            switch(a) {
                case AIAction.Dieing:
                    if (rnd.Next(2) == 0)
                        Game.CueSound(this.sounds[(int)NPCSound.Death1]);
                    else
                        Game.CueSound(this.sounds[(int)NPCSound.Death2]);
                    break;
                
            }
            if (old == AIAction.Attacking)
            {
                if (rnd.Next(2) == 0)
                    Game.CueSound(this.sounds[(int)NPCSound.Attack1]);
                else
                    Game.CueSound(this.sounds[(int)NPCSound.Attack2]);
            }
        }

        #region IGameDrawable Members

        public void Draw(SpriteBatch spriteBatch, int x, int y)
        {
            this.Animation.Draw(spriteBatch, x + this.PositionDeviation.X, y + this.PositionDeviation.Y);
        }

        #endregion
        #region IGameUpdateable Members

        public override void Update(float secondsPassed)
        {
            base.Update(secondsPassed);

            if (this.Action == AIAction.Moving && this.TileMoveProgress == 1)
            {
                this.SetAction(AIAction.Delaying);
            }

            //Check animation update
            if (this.Direction != lastDirection)
            {
                UpdateAnimation();
            }
            this.lastDirection = Direction;

            if (this.Action == AIAction.Dieing)
            {
                if (this.Animation.CurrentFrame < this.Animation.EndFrame)
                    this.Animation.Update(secondsPassed);
                else
                    this.SetState(AIState.Dead);
            }
            else
                this.Animation.Update(secondsPassed);
            
        }

        #endregion
    }
}
