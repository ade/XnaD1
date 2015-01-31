using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace DiacloLib
{
    public enum SquareEntityType
    {
        Trigger_Teleport
    }
    public class SquareEntity
    {
        public SquareEntityType Type { get; set; }
        public Point TargetPos { get; set; }
        public int TargetArea { get; set; }
    }
}
