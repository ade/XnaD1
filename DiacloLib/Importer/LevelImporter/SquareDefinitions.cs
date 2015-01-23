using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer.LevelComponents
{

    /// <summary>
    /// .MIN file
    /// </summary>
    public class SquareDefinitions
    {
        public byte[] data;
        public int FramesPerSquare;
        public SquareInfo[] Squares;
        
        public SquareDefinitions(byte[] data, int framesPerSquare)
        {
            this.data = data;
            this.FramesPerSquare = framesPerSquare;
            IntelStream stream = new IntelStream(this.data);

            int squares = this.data.Length / 2;
            this.Squares = new SquareInfo[squares];
            
            for (int i = 0; i < squares / 2; i++)
            {
                this.Squares[i] = new SquareInfo(framesPerSquare);
                for (int f = 0; f < framesPerSquare; f++)
                {
                    ushort d = stream.ReadWord();
                    this.Squares[i].Frame[f] = (int)d % 4096;
                    this.Squares[i].Type[f] = (LevelCelFrameType)(d / 4096);
                }
            }
        }
    }
}
