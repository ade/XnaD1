using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using DiacloLib;

namespace DiacloServer.AITemplates
{
    public class AISkeleton: ServerNPC
    {
        private bool debugmessages = false;
        public AISkeleton(World w, int areaID, Point pos, int ID): base(w,areaID,pos,ID)
        {
            this.idle = true;
            this.SetState(AIState.Inactive);   
        }
        public override void Update(float secondsPassed)
        {
            if (this.State == AIState.Active)
            {
                base.Update(secondsPassed);

                if (this.idle)
                {
                    //New action required
                    if (this.target != null)
                    {

                        if (this.TargetInMeleeRange())
                        {
                            if (this.Action == AIAction.Delaying) //Last action 
                            {
                                //standing next to the enemy, and was delaying before - attack
                                if(debugmessages) GameConsole.Write("Adjacent, idle, was delaying - attacking", ConsoleMessageTypes.Debug);
                                this.AttackEnemy();
                            }
                            else
                            {   //Attacking or moving was previous action
                                if (rnd.Next(0, 100) < 2 * this.intelligence + 20)
                                {
                                    if (debugmessages) GameConsole.Write("Adjacent, idle, was not delaying but random: Attack", ConsoleMessageTypes.Debug);
                                    this.AttackEnemy();
                                }
                                else
                                {
                                    float delay = (float)(rnd.Next(10, 20) - 2 * this.intelligence) / 20.0f;
                                    if (debugmessages) GameConsole.Write("Adjacent, idle, was not delaying and random: Delay for " + delay.ToString(), ConsoleMessageTypes.Debug);
                                    //do delay for (Rnd[10] + 10 - 2·Intf)/20 seconds
                                    this.Delay(delay);
                                }
                            }
                        }
                        else
                        {
                            //We're not next to the enemy
                            if (this.Action == AIAction.Delaying)
                            {
                                if (debugmessages) GameConsole.Write("Not adjacent, idle, was delaying: Move", ConsoleMessageTypes.Debug);
                                //Last action was delay: move close
                                this.MoveTowardEnemy();
                            }
                            else
                            {
                                //Chance to decide to walk toward target
                                if (rnd.Next(100) < 4 * this.intelligence + 65)
                                {
                                    if (debugmessages) GameConsole.Write("Not adjacent, idle, wasn't delaying: Move (random)", ConsoleMessageTypes.Debug);
                                    this.MoveTowardEnemy();
                                }
                                else
                                {
                                    //Delay..
                                    float delay = (float)(rnd.Next(15, 25) - 2 * this.intelligence) / 20.0f;
                                    if (debugmessages) GameConsole.Write("Not adjacent, idle, wasn't delaying: Delay (random) for " + delay.ToString(), ConsoleMessageTypes.Debug);
                                    this.Delay(delay);
                                }
                            }
                        }
                    }
                    else
                    {
                        //No target! Delay or move toward last seen pos
                        if (this.Action == AIAction.Delaying)
                        {
                            if (debugmessages) GameConsole.Write("Lost target, idle, was delaying: Move", ConsoleMessageTypes.Debug);
                            //Last action was delay: move close
                            this.MoveTowardEnemy();
                        }
                        else
                        {
                            //Chance to decide to walk toward target
                            if (rnd.Next(100) < 4 * this.intelligence + 65)
                            {
                                if (debugmessages) GameConsole.Write("Lost target, idle, wasn't delaying: Move (random)", ConsoleMessageTypes.Debug);
                                this.MoveTowardEnemy();
                            }
                            else
                            {
                                //Delay..
                                float delay = (float)(rnd.Next(15, 25) - 2 * this.intelligence) / 20.0f;
                                if (debugmessages) GameConsole.Write("Lost target, idle, wasn't delaying: Delay (random) for " + delay.ToString(), ConsoleMessageTypes.Debug);
                                this.Delay(delay);
                            }
                        }
                    }
                }
                else
                {

                    
                }

            }
            else
            {
                //State = inactive
            }
            
        }

        private void AttackEnemy()
        {
            this.MeleeAttack();

        }
    }
}
