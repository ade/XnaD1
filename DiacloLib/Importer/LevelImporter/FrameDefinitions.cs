using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DiacloLib.Importer.LevelComponents
{
    public enum LevelCelFrameType
    {
        Raw = 0,
        Cel = 1,
        LeftLow = 2,
        RightLow = 3,
        LeftHigh = 4,
        RightHigh = 5,
    }
    /// <summary>
    /// .CEL file with special format
    /// </summary>
    public class FrameDefinitions: GenericCEL
    {
        public FrameDefinitions(byte[] fd, Palette p): base(fd, p)
        {

        }
        public override RawBitmap GetFrame(int index)
        {
            EncodedCELFrame frame = this.EncodedFrames[index];
            switch (frame.FrameType)
            {
                case LevelCelFrameType.Cel:
                    return decodeCel(frame);
                case LevelCelFrameType.Raw:
                    return decodeRaw(frame);
                case LevelCelFrameType.LeftHigh:
                case LevelCelFrameType.RightHigh:
                    return decodeHigh(frame, frame.FrameType);
                case LevelCelFrameType.LeftLow:
                case LevelCelFrameType.RightLow:
                    return decodeLow(frame, frame.FrameType);
            }
            return null;
        }

        private RawBitmap decodeLow(EncodedCELFrame frame, LevelCelFrameType type)
        {
            //this tile is shaped like a triangle <| or |> 
            int data_on_line = 4; //every other line contains 2 useless pixels
            int line_step = 1;
            int direction = 1;
            int i = 0;
            MemoryStream result = new MemoryStream(1024); //32x32 px
            while(i < 544) {
                if (type == LevelCelFrameType.LeftLow)
                {
                    //Add transparent pixels in front of data
                    for (int p = 0; p < 32 - data_on_line; p++)
                        result.WriteByte(255);
                }

                //Get pixels from data
                for (int p = 0; p < data_on_line; p++)
                {
                    byte paletteIndex = frame.data[i + p];
                    if (paletteIndex == 0)
                    {
                        //useless pixel! use transparent
                        result.WriteByte(255);
                    }
                    else
                    {
                        result.WriteByte(paletteIndex);
                    }
                }

                if (type == LevelCelFrameType.RightLow)
                {
                    //Add transparent pixels after data
                    for (int p = 0; p < 32 - data_on_line; p++)
                        result.WriteByte(255);
                }

                i += data_on_line;
                if (line_step == 2)
                {
                    if (data_on_line < 32)
                    {
                        data_on_line += 4 * direction;
                        if (data_on_line == 32)
                        {
                            line_step = 0; //3 times with 32!
                        }
                        else
                        {
                            line_step = 1;
                        }
                    }
                    else if (data_on_line == 32)
                    {
                        direction = -1;
                        data_on_line -= 4;
                        line_step = 1;
                    }
                    
                }
                else
                    line_step++;

            }
            //Last line of transparency
            for(int t = 0; t < 32; t++)
                result.WriteByte(255);

            RawBitmap ret = new RawBitmap(result.GetBuffer(), this.Palette);
            ret.Width = 32;
            ret.Height = 32;
            return ret;
        }


        private RawBitmap decodeHigh(EncodedCELFrame frame, LevelCelFrameType type)
        {
            //this tile is shaped like a wedge
            //  ......
            //  ......
            //   .....
            //    ....
            //     ...
            //      ..  or mirrored

            int data_on_line = 4; //every other line contains 2 useless pixels
            int line_step = 1;
            int i = 0;
            MemoryStream result = new MemoryStream(1024); //32x32 px
            while (i < 800)
            {
                if (type == LevelCelFrameType.LeftHigh)
                {
                    //Add transparent pixels in front of data
                    for (int p = 0; p < 32 - data_on_line; p++)
                        result.WriteByte(255);
                }

                //Get pixels from data
                for (int p = 0; p < data_on_line; p++)
                {
                    byte paletteIndex = frame.data[i + p];
                    if (paletteIndex == 0)
                    {
                        //useless pixel! use transparent
                        result.WriteByte(255);
                    }
                    else
                    {
                        result.WriteByte(paletteIndex);
                    }
                }

                if (type == LevelCelFrameType.RightHigh)
                {
                    //Add transparent pixels after data
                    for (int p = 0; p < 32 - data_on_line; p++)
                        result.WriteByte(255);
                }

                i += data_on_line;
                if (line_step == 2)
                {
                    if (data_on_line < 32)
                    {
                        data_on_line += 4;
                        line_step = 1;
                    }
                }
                else
                    line_step++;

            }
           
            RawBitmap ret = new RawBitmap(result.GetBuffer(), this.Palette);
            ret.Width = 32;
            ret.Height = 32;
            return ret;
        }

        private RawBitmap decodeRaw(EncodedCELFrame frame)
        {
            RawBitmap ret = new RawBitmap(frame.data, this.Palette);
            ret.Width = 32;
            ret.Height = 32;
            return ret;
        }

        private RawBitmap decodeCel(EncodedCELFrame frame)
        {
            RawBitmap ret = new RawBitmap(this.decompressCelData(frame.data, 0,null), this.Palette);
            ret.Width = 32;
            ret.Height = 32;
            return ret;
        }
    }
}
