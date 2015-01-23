using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib.Importer;

namespace DiacloLib
{
    public class GameState
    {
        public Player[] Players { get; set; }
        public World World { get; set; }
        private float timeSinceUpdate;

        public void Update(float secondsPassed)
        {
            if(this.Players != null)
                for (int i = 0; i < this.Players.Length; i++)
                    if(this.Players[i] != null) 
                        this.Players[i].Update(secondsPassed);

            timeSinceUpdate += secondsPassed;
            if (timeSinceUpdate >= 0.016f)
            {
                
                foreach (Area a in World.Areas)
                {
                    if (a.Players.Count > 0)
                    {
                        //If a player is here....
                        foreach (BaseNPC npc in a.npcs)
                        {
                            if (npc.State == AIState.Active)
                                npc.Update(timeSinceUpdate);
                        }
                    }
                }
                timeSinceUpdate = 0;
            }
        }

    }
}
