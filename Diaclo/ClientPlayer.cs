using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using DiacloLib.Importer;
using Microsoft.Xna.Framework;
using DiacloLib;

namespace Diaclo
{
    /// <summary>
    /// Extended version of Player to allow for drawing, etc
    /// </summary>
    public class ClientPlayer: Player, IGameDrawable
    {
        private TilesetAnimation Animation;
        private Random rnd = new Random();
        private string[] sounds;
        /// <summary>
        /// Some actions finish before the animation does (e.g. melee attack), 
        /// finish the animation if there are more frames to play. (True=play remaining frames before switch)
        /// </summary>
        private bool IdleAnimationIsQueued = false;
        private enum PlayerSoundFile
        {
            Hurt1,
            Hit1,
            Hit2,
            Death
        }
        private enum PlayerSoundGroups
        {
            Hit,
            Death
        }
        public ClientPlayer(Character c)
        {
            this.Character = c;
            this.RefreshAttributes();
            this.CurrentHP = this.MaxHP;
            this.SetAction(PlayerAction.Idle);
        }
        public override void SetAction(PlayerAction s)
        {
            PlayerAction old = this.Action;
            base.SetAction(s);
            this.UpdateStateAnimation(old, s);

        }
        public virtual void OverrideStatus(PlayerAction a, Direction d, Point position, float actionDuration, float actionEndtime, int currenthp)
        {
            this.SetAction(a);
            this.Direction = d;
            this.Position = position;
            this.actionElapsed = actionDuration;
            this.actionEndTime = actionEndTime;
            this.CurrentHP = currenthp;

        }
        public override void Update(float secondsPassed)
        {
            base.Update(secondsPassed);

            if (this.Animation != null)
            {
                if (this.Animation.CurrentFrame == this.Animation.EndFrame && this.IdleAnimationIsQueued)
                {
                    this.IdleAnimationIsQueued = false;
                    SetAnimation(PlayerGraphicsAction.Idle);
                } else 
                    this.Animation.Update(secondsPassed);
                
            }


        }
        private void UpdateStateAnimation(PlayerAction oldstate, PlayerAction newstate)
        {
            this.GraphicsWeapon = PlayerGraphicsWeapon.Axe;
            if (oldstate != newstate || this.Animation == null)
            {
                switch (newstate)
                {
                    case PlayerAction.Idle:
                        if (oldstate == PlayerAction.Melee)
                        {
                            this.IdleAnimationIsQueued = true;
                            PlayAttackSound();
                        }
                        else
                        {
                            SetAnimation(PlayerGraphicsAction.Idle);
                        }
                        break;
                    case PlayerAction.Walk:
                        SetAnimation(PlayerGraphicsAction.Walk);
                        break;
                    case PlayerAction.Melee:
                        SetAnimation(PlayerGraphicsAction.Attack);
                        break;
                    case PlayerAction.HitRecovery:
                        SetAnimation(PlayerGraphicsAction.TakeHit);
                        break;
                    case PlayerAction.Dead:
                        SetAnimation(PlayerGraphicsAction.Die);
                        break;
                }
            }
        }
        
        private void PlayAttackSound()
        {
            switch (this.GraphicsWeapon)
            {
                case PlayerGraphicsWeapon.Axe:
                    Game.CueSound(Res.SND_WORLD_SWING2);
                    break;
                default:
                    Game.CueSound(Res.SND_WORLD_SWING);
                    break;
            }
        }

        public void SetAnimation(PlayerGraphicsAction gAction) {
            //Get a tileset from memory or disk
            Tileset tileset = GetPlayerTileset(this.Character.Class, this.Direction, this.GraphicsWeapon, gAction, this.GraphicsArmor);
            if(this.Animation == null)
                this.Animation = new TilesetAnimation(tileset, 0, tileset.Count() - 1, 0.1f);

            this.GraphicsAction = gAction;
            this.Animation.Tileset = tileset;
            this.Animation.CurrentFrame = 0;
            this.Animation.EndFrame = this.Animation.Tileset.Count() - 1;
            this.Animation.Loop = true;
            switch (gAction)
            {
                case PlayerGraphicsAction.Idle:
                case PlayerGraphicsAction.IdleTown:
                    this.Animation.UpdateInterval = 0.25f;
                    break;
                case PlayerGraphicsAction.Walk:
                case PlayerGraphicsAction.WalkTown:
                    this.Animation.UpdateInterval = 1.0f / ((this.Animation.Tileset.Count()) / 0.4f);
                    break;
                case PlayerGraphicsAction.Attack:
                    this.Animation.UpdateInterval = GameMechanics.DurationToUpdateInterval(this.AttackSpeed, 11); //11 : hitframe for axe
                    break;
                case PlayerGraphicsAction.TakeHit:
                    this.Animation.UpdateInterval = GameMechanics.DurationToUpdateInterval(this.HitRecoverySpeed, this.Animation.Tileset.Count());
                    break;
                case PlayerGraphicsAction.Die:
                    this.Animation.UpdateInterval = 0.05f;
                    this.Animation.Loop = false;
                    break;
            }
            
        }
        public Tileset GetPlayerTileset(CharacterClass c, Direction d, PlayerGraphicsWeapon w, PlayerGraphicsAction a, PlayerGraphicsArmor ar)
        {
            //If dead player will be uneqipped
            if (a == PlayerGraphicsAction.Die)
            {
                w = PlayerGraphicsWeapon.Unarmed;
                ar = PlayerGraphicsArmor.Light;
            }

            string classname = this.PlayerClassFile(c).ToLower();
            string class_short = classname.Substring(0, 1).ToLower();
            string armor = this.PlayerGraphicsArmorFile(ar);
            string weapon = this.PlayerGraphicsWeaponFile(w);
            string caw = class_short + armor + weapon;
            string tileset_name = caw + PlayerGraphicsActionFile(a);
            string mpqpath = "plrgfx\\" + classname + "\\" + caw + "\\" + tileset_name + ".cl2";
            string direction_suffix = "_" + (int)d;
            string key = tileset_name + direction_suffix;

            Tileset tileset;
            if(!GameContent.PlayerTilesets.TryGetValue(key, out tileset))
            {
                //Build new tileset
                tileset = GameContent.BuildCreatureTileset(mpqpath, d);

                //save tileset in cache
                GameContent.PlayerTilesets.Add(key, tileset);

                //GameConsole.Write("[Player Cache Add] " + key);
            }

            return tileset;
        }

        public void Draw(SpriteBatch spriteBatch, int x, int y)
        {
            if (this.Animation != null)
                this.Animation.Draw(spriteBatch, x + this.PositionDeviation.X, y + this.PositionDeviation.Y);

        }
        private String PlayerGraphicsWeaponFile(PlayerGraphicsWeapon w)
        {
            switch (w)
            {
                case PlayerGraphicsWeapon.Axe: return "a";
                case PlayerGraphicsWeapon.Bow: return "b";
                case PlayerGraphicsWeapon.Mace: return "m";
                case PlayerGraphicsWeapon.SwordShield: return "d";
                case PlayerGraphicsWeapon.MaceShield: return "h";
                case PlayerGraphicsWeapon.Unarmed: return "n";
                case PlayerGraphicsWeapon.Sword: return "s";
                case PlayerGraphicsWeapon.Staff: return "t";
                case PlayerGraphicsWeapon.Shield: return "u";
            }
            return "";

        }
        private string PlayerGraphicsArmorFile(PlayerGraphicsArmor a)
        {
            switch (a)
            {
                case PlayerGraphicsArmor.Light: return "l";
                case PlayerGraphicsArmor.Medium: return "m";
                case PlayerGraphicsArmor.Heavy: return "h";
            }
            return "";
        }

        private String PlayerGraphicsActionFile(PlayerGraphicsAction a)
        {
            switch (a)
            {
                case PlayerGraphicsAction.Attack: return "at";
                case PlayerGraphicsAction.CastFire: return "fm";
                case PlayerGraphicsAction.CastLightning: return "lm";
                case PlayerGraphicsAction.CastOther: return "qm";
                case PlayerGraphicsAction.Die: return "dt";
                case PlayerGraphicsAction.Idle: return "as";
                case PlayerGraphicsAction.IdleTown: return "st";
                case PlayerGraphicsAction.TakeHit: return "ht";
                case PlayerGraphicsAction.Walk: return "aw";
                case PlayerGraphicsAction.WalkTown: return "wl";
            }
            return "";
        }
        private string PlayerClassFile(CharacterClass c)
        {
            switch (c)
            {
                case CharacterClass.Rogue: return "Rogue";
                case CharacterClass.Sorceror: return "Sorceror";
                case CharacterClass.Warrior: return "Warrior";
            }
            return "";

        }

        internal void Die()
        {
            
            this.StartAction(PlayerAction.Dead, 0);
            

            
        }

        internal void CacheAnimations()
        {
            for (int d = 0; d < 8; d++)
                GetPlayerTileset(this.Character.Class, (Direction)d, this.GraphicsWeapon, PlayerGraphicsAction.Attack, this.GraphicsArmor);
            for (int d = 0; d < 8; d++)
                GetPlayerTileset(this.Character.Class, (Direction)d, this.GraphicsWeapon, PlayerGraphicsAction.Die, this.GraphicsArmor);
            for (int d = 0; d < 8; d++)
                GetPlayerTileset(this.Character.Class, (Direction)d, this.GraphicsWeapon, PlayerGraphicsAction.Idle, this.GraphicsArmor);
            for (int d = 0; d < 8; d++)
                GetPlayerTileset(this.Character.Class, (Direction)d, this.GraphicsWeapon, PlayerGraphicsAction.TakeHit, this.GraphicsArmor);
            for (int d = 0; d < 8; d++)
                GetPlayerTileset(this.Character.Class, (Direction)d, this.GraphicsWeapon, PlayerGraphicsAction.Walk, this.GraphicsArmor);
        }

        internal string[] CacheSounds()
        {
            this.sounds = new string[4];
            this.sounds[(int)PlayerSoundFile.Hurt1] = Res.SND_PLAYER_HURT;
            this.sounds[(int)PlayerSoundFile.Hit1] = Res.SND_PLAYER_TAKEHIT;
            this.sounds[(int)PlayerSoundFile.Hit2] = Res.SND_PLAYER_TAKEHIT2;
            this.sounds[(int)PlayerSoundFile.Death] = Res.SND_PLAYER_DEATH;

            return this.sounds;
        }
        public override void Hurt(int amount, int newHP, WorldCreature offender)
        {
            base.Hurt(amount, newHP, offender);
            if(newHP > 0)
                CueSound(PlayerSoundGroups.Hit);
            else
                CueSound(PlayerSoundGroups.Death);
        }
        private void CueSound(PlayerSoundGroups s)
        {
            switch (s)
            {
                case PlayerSoundGroups.Death:
                    Game.CueSound(this.sounds[(int)PlayerSoundFile.Death]);
                    break;
                case PlayerSoundGroups.Hit:
                    if (rnd.Next(2) == 0) 
                        Game.CueSound(this.sounds[(int)PlayerSoundFile.Hit1]);
                    else
                        Game.CueSound(this.sounds[(int)PlayerSoundFile.Hit2]);
                    break;
            }
        }
    }
}
