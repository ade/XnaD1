using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib
{
    /// <summary>
    /// The state of a game world
    /// 
    /// </summary>
    public class World
    {
        public Area[] Areas;
        private Dictionary<int,BaseNPC> npcList;
        private int npcID = 0;
        public World()
        {
            npcList = new Dictionary<int, BaseNPC>();
        }
        public BaseNPC GetNPCById(int id)
        {
            BaseNPC ret;
            if (npcList.TryGetValue(id, out ret))
                return ret;
            else
                return null;   
        }
        public void AddNPC(BaseNPC npc)
        {
            this.npcList.Add(npc.ID, npc);
        }
        /// <summary>
        /// Creates and returns the next available monster ID
        /// </summary>
        /// <returns></returns>
        public int NewNpcID()
        {
            return npcID++;
        }
    }
}
