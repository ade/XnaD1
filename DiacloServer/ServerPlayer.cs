using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;
using Microsoft.Xna.Framework;
using Lidgren.Network;

namespace DiacloServer
{
    public enum PlayerServerStatus
    {
        Connecting = 0, //Waiting for client to send it's info (it's connection was accepted)
        Playing = 1 //Info was sent and player is now entering playing state
    }
    public class ServerPlayer: Player
    {
        public NetConnection NetConnection;
        public PlayerServerStatus ServerStatus;
        public ServerPlayer(NetConnection c, byte ID): base()
        {
            this.NetConnection = c;
            this.ID = ID;
        }
        public override void ActionSuccessful(PlayerAction a)
        {
            base.ActionSuccessful(a);

            if (a == PlayerAction.Melee)
            {
                //Make a hit roll
                Square sq = this.Area.GetSquare(this.actionTarget); 
                if (sq != null && sq.getNPC() != null)
                {
                    ServerNPC target = (ServerNPC)this.Area.GetSquare(this.actionTarget).getNPC();
                    BattleResult r = BattleServer.PlayerMeleeVsMonster(this, target);
                    Server.OnPlayerMeleeFinished(this, r);
                }
            }

        }
        public override void ActionFailed(PlayerAction a)
        {
            base.ActionFailed(a);

            Server.OnPlayerActionFailed(this, a);
        }

        public void AddStat(AttributeType stat)
        {
            this.Character.LevelUpPoints--;
            switch (stat)
            {
                case AttributeType.Strength:
                    this.Character.AttStr++;
                    break;
                case AttributeType.Magic:
                    this.Character.AttMag++;
                    break;
                case AttributeType.Dexterity:
                    this.Character.AttDex++;
                    break;
                case AttributeType.Vitality:
                    this.Character.AttVit++;
                    break;
            }

        }
        public uint GainExp(int base_exp, int mlvl)
        {
            //todo: experience cap for one kill

            uint exp = (uint)(base_exp * (1.0 + 0.1 * (mlvl * this.Character.Level)));
            this.Character.Experience += exp;
            if (this.Character.Level < 50 && this.Character.Experience >= LevelExperience[this.Character.Level])
            {
                //Level up
                this.Character.Level++;
                this.Character.LevelUpPoints += 5;
                this.CurrentHP = this.MaxHP;
                Server.OnPlayerStatusChanged(this);
                switch (this.Character.Class)
                {
                    case CharacterClass.Warrior:
                    default:

                        break;
                }
            }
            return exp;
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
            if (origin.getPlayer() == this || (origin.getNPC() == null && origin.getPlayer() == null))
            {
                this.Position = this.TileMoveFrom;
            }
            this.StopMove();
        }
        protected override void StartAction(PlayerAction playerAction, float duration)
        {
            PlayerAction old = this.Action;
            base.StartAction(playerAction, duration);
            if (this.Action != PlayerAction.Idle && old == PlayerAction.Walk)
            {
                this.StopMove();
                this.ActionFailed(PlayerAction.Walk);
            }
        }
        /// <summary>
        /// Change a player's status to idle.
        /// </summary>
        /// <param name="sendToClient">Send updated status to client?</param>
        public void SetIdle(bool sendToClient)
        {
            base.SetIdle();
            if(sendToClient)
                Server.OnPlayerStatusChanged(this);
        }
        /// <summary>
        /// Change a player's status to idle. Automatically notifies client.
        /// </summary>
        public override void SetIdle()
        {
            this.SetIdle(true);
        }
    }
}
