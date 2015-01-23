using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer.LevelComponents
{
    public class SquareInfo
    {
        public int[] Frame;
        public LevelCelFrameType[] Type;
        public SquareInfo(int framesPerSquare)
        {
            this.Frame = new int[framesPerSquare];
            this.Type = new LevelCelFrameType[framesPerSquare];
        }
    }
}
