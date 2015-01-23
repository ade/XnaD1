using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace DiacloLib.Importer
{
    public class GenericCEL
    {

        public EncodedCELFrame[] EncodedFrames;
        private RawBitmap[] decodedFrames;
        public Palette Palette;
        public int Frames;
        private int KnownWidth = 0;
        public GenericCEL(byte[] filedata, Palette p)
        {
            this.Palette = p;
            this.ReadFile(filedata);
        }
        public GenericCEL(byte[] filedata, Palette p, int known_width)
        {
            this.Palette = p;
            this.ReadFile(filedata);
            this.KnownWidth = known_width;
        }
        public void ReadFile(byte[] data)
        {
            IntelStream stream = new IntelStream(data);
            uint frames = stream.ReadDWord();
            this.Frames = (int)frames;

            uint[] offsets = new uint[frames+1];

            //Get offsets for each frame
            for (int i = 0; i < frames+1; i++)
            {
                offsets[i] = stream.ReadDWord();
            }

            //Check if this is a grouped CEL by checking if the first dword matches with offset for end of headers
            if (!(offsets[0] == (frames + 2) * 4)) // 2 extra dwords. one for frame count and one for last byte/filelength pointer
            {
                throw new NotImplementedException();
            }

            //Extract encoded frames
            this.EncodedFrames = new EncodedCELFrame[frames];
            this.decodedFrames = new RawBitmap[frames];

            for (int i = 0; i < frames; i++)
            {
                int frameLength = (int)(offsets[i + 1] - offsets[i]);
                EncodedCELFrame frame = new EncodedCELFrame(frameLength);
                stream.Read(frame.data, 0, frameLength);
                this.EncodedFrames[i] = frame;
            }

        }
        protected byte[] decompressCelData(byte[] data, int offset, EncodedCELFrame setWidth)
        {
            MemoryStream stream = new MemoryStream(1024);
            if (setWidth != null && this.KnownWidth != 0)
            {
                setWidth.DetectedWidth = this.KnownWidth;
                setWidth = null;
            }
            
            //While decoding, try to find the width of the image.
            //Non-transparency method
            int width = 0;
            bool nonMaxFound = false;
            bool hasTransparency = false;

            //Line break method
            int[] lineBreakPositions = new int[10];
            int lineBreakCount = 0;
            int lastLength = 0;
            int lastType = 0;
            bool lastMaxed = false;
            int minWidth = 0;
            int currentWidth = 0;
            
            
            //Decompress data
            for (int i = offset; i < data.Length; i++)
            {
                byte b = data[i];
                int type = 0;
                bool maxedCode = false;
                if (b < 128) //Palette indexes
                {
                    //Write b bytes of plain palette indexes
                    stream.Write(data, i + 1, b);
                    i += b; //Skip to next code byte

                    if (b < 127)
                    {
                        //Check longest concatenation found
                        if (currentWidth + b > minWidth)
                            minWidth = currentWidth + b;
                        currentWidth = 0;

                        maxedCode = false;
                        //Non maximum value indicates that this could be a line break. If it's the first one, save the width.
                        if (!nonMaxFound)
                        {
                            width = (int)stream.Length;
                            nonMaxFound = true;
                        }
                    }
                    else
                    {
                        maxedCode = true;
                        currentWidth += 127;
                    }
                    type = 1;
                }
                else //empty pixels
                {
                    if (b == 128)
                    {
                        maxedCode = true;
                        currentWidth += 128;
                    }
                    else
                    {
                        maxedCode = false;
                        //Check longest concatenation found
                        if (currentWidth + 256 - b > minWidth)
                            minWidth = currentWidth + 256 - b;
                        currentWidth = 0;
                    }

                    //Write 256-b bytes of transparent pixels
                    hasTransparency = true;
                    for (int j = 0; j < 256 - b; j++)
                    {
                        stream.WriteByte(255);
                    }
                    type = 2;
                }

                //Check for linebreak
                if(type == lastType && lastMaxed == false) {
                    //Line break detected
                    if (lineBreakCount < 10)
                    {
                        lineBreakPositions[lineBreakCount] = lastLength;
                        lineBreakCount++;
                    }
                }
                lastType = type;
                lastMaxed = maxedCode;
                lastLength = (int)stream.Length;
            }
            byte[] ret = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(ret, 0, (int)stream.Length);

            //Write width to frame if we didn't encounter any transparency
            if (!hasTransparency && setWidth != null)
            {
                setWidth.DetectedWidth = width;
            }
            else if (lineBreakCount > 1 && setWidth != null)
            {
                for (int tryWidth = minWidth; tryWidth < 1024; tryWidth++)
                {
                    bool match = true;
                    for(int i = 0; i < lineBreakCount; i++) {
                        if (lineBreakPositions[i] % tryWidth != 0)
                            match = false;
                    }
                    if (match)
                    {
                        setWidth.DetectedWidth = tryWidth;
                        break;
                    }
                }
            }

            return ret;

        }

        private RawBitmap decodeFrame(int index) {
            EncodedCELFrame encoded = this.EncodedFrames[index];
            RawBitmap ret;
            if (this.EncodedFrames[index].HasHeader())
            {
                ret = new RawBitmap(decompressCelData(encoded.data, 10, encoded), this.Palette);
            } else  {
                ret = new RawBitmap(decompressCelData(encoded.data, 0, encoded), this.Palette);
            }

            //check if we detected a width during decompression
            if (encoded.DetectedWidth > 0)
            {
                ret.Width = encoded.DetectedWidth;
            }
            else
            {
                //none detected, attempt to find using header
                if (encoded.HasHeader())
                    ret.Width = findWidthByHeader(index);
                else
                    throw new Exception("CEL decompression: no width found!");
            }
            ret.Height = ret.data.Length / ret.Width;
            return ret;
        }

        private int findWidthByHeader(int index)
        {
            byte[] data = this.EncodedFrames[index].data;
            //Attempt to find width by using offsets from frame header
            int offset_to_32y = data[2] + data[3] * 256; //the second WORD.
            byte[] first_32_lines = new byte[offset_to_32y];
            Array.Copy(data, first_32_lines, offset_to_32y);
            byte[] decompressed_first_32_lines = decompressCelData(first_32_lines, 10, null); //skip 10: header
            return decompressed_first_32_lines.Length / 32;
        }
        public virtual RawBitmap GetFrame(int index)
        {
            if (this.decodedFrames[index] == null)
                this.decodedFrames[index] = this.decodeFrame(index);

            return this.decodedFrames[index];

        }
        public EncodedCELFrame getEncodedFrame(int index)
        {
            return this.EncodedFrames[index];
        }

        public RawBitmap[] GetFrames()
        {
            RawBitmap[] ret = new RawBitmap[this.decodedFrames.Length];
            for (int i = 0; i < this.decodedFrames.Length; i++)
            {
                if (this.decodedFrames[i] == null)
                    this.decodedFrames[i] = this.decodeFrame(i);
                ret[i] = this.decodedFrames[i];
            }
            return ret;
        }
    }
}
