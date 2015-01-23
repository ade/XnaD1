using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib.Importer.LevelComponents;

namespace DiacloLib.Importer
{
    public class EncodedCELFrame
    {
        public byte[] data;
        public int DetectedWidth = 0;
        public LevelCelFrameType FrameType;
        public EncodedCELFrame(int length)
        {
            this.data = new byte[length];
        }
        public bool HasHeader()
        {
            return (this.data[0] + this.data[1] * 256) == 10;
        }
    }
}
