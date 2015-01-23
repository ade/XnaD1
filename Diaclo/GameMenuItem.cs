using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;

namespace Diaclo
{
    internal class GameMenuItem
    {
        internal string Title;
        internal GameMenu Parent;
        internal object Tag;
        internal int Index;
        public GameMenuItem(string title, object tag, int index, GameMenu parent)
        {
            this.Title = title;
            this.Tag = tag;
            this.Index = index;
            this.Parent = parent;
        }
    }
}
