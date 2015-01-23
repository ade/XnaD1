using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace DiacloLib.Importer
{

    public class CL2Container
    {
        /// <summary>
        /// Raw frame, with RLE compression, etc. Not a bitmap!
        /// </summary>
        private class EncodedCL2Frame
        {
            public byte[] data;
            public EncodedCL2Frame(byte[] data)
            {
                this.data = data;
            }
            public EncodedCL2Frame(int length)
            {
                this.data = new byte[length];
            }
        }

        private List<EncodedCL2Frame> Frames;
        private RawBitmap[] DecodedFrames;
        public Palette Palette { get; set; }
        public int Groups;
        public CL2Container(byte[] filedata, Palette p)
        {
            this.Frames = new List<EncodedCL2Frame>();
            ReadFile(filedata);
            this.Palette = p;
        }
        public void ReadFile(byte[] data)
        {
            Stopwatch sw = Stopwatch.StartNew();
            this.Frames.Clear();
            IntelStream stream = new IntelStream(data);
            this.Groups = 0;
            int[] groupFrames;

            //Attempt to find amount of groups
            uint[] groupHeader = new uint[8];
            for (int i = 0; i < 8; i++)
            {
                groupHeader[i] = stream.ReadDWord();
            }

            //Since group sizes are the same, we can check if we have the same relative value between pointers
            if ((groupHeader[1] - groupHeader[0]) == (groupHeader[2] - groupHeader[1]))
            {
                //When there are groups, the amount of groups will be calculated from the length of the first pointer (4: length of a dword)
                //In diabdat.mpq, this will always be equal to 8, the number of directions.
                this.Groups = (int)(groupHeader[0] / 4);
            }
            else
            {
                this.Groups = 1;
            }

            //For every group there are an amount of frames..
            groupFrames = new int[this.Groups];
            uint[][] frameOffset = new uint[this.Groups][];

            //Read all clip headers
            for (int g = 0; g < this.Groups; g++)
            {
                //The group pointer is relative from it's own position in the file, so we must add it's location to the offset from start of file
                uint groupOffset = (uint)(groupHeader[g]); //(if there's no group, data starts at 0)
                stream.Seek(groupOffset, System.IO.SeekOrigin.Begin);
                
                //First, the amount of frames in this group/"clip"
                uint frames = stream.ReadDWord();
                frameOffset[g] = new uint[frames+1]; //Last is the end of last frame
                
                //For every frame, there's a pointer to where that frame starts
                for (int i = 0; i <= frames; i++)
                {
                    //The values are the relative position of the frame from the clip/group header start position, thus,
                    //The absolute byte position of a frame is (relative offset) + groupOffset
                    frameOffset[g][i] = stream.ReadDWord() + groupOffset;
                }               
            }

            //Now read all frames.
            for (int g = 0; g < Groups; g++)
            {
                for (int i = 0; i < frameOffset[g].Length-1; i++)
                {
                    //There is no gap between frames in the file, thus
                    //by comparing two offsets, we can determine the frame size.
                    int framesize = (int)(frameOffset[g][i + 1] - frameOffset[g][i]);
                    stream.Seek(frameOffset[g][i], System.IO.SeekOrigin.Begin);

                    //Read frame
                    EncodedCL2Frame frame = new EncodedCL2Frame(framesize);
                    stream.Read(frame.data, 0, framesize);
                    this.Frames.Add(frame);
                }
            }


            //Create a new buffer for decoded frames
            this.DecodedFrames = new RawBitmap[this.Frames.Count];
            GameConsole.ReportPerformance(PerformanceCategory.GraphicImporting, sw.ElapsedTicks);
            
        }
        /// <summary>
        /// Get a specific frame from a group
        /// </summary>
        /// <param name="group">The group id, zero based</param>
        /// <param name="subframe">Frame id, zero based</param>
        /// <param name="known_width">If the frame header is known to be faulty, a width can be specified manually.</param>
        /// <returns></returns>
        public RawBitmap GetDecodedFrame(int group, int subframe, int known_width)
        {
            return GetDecodedFrame(group * this.Groups + subframe, known_width);
        }
        public RawBitmap GetDecodedFrame(int frame, int known_width)
        {
            Stopwatch sw = new Stopwatch();
            if (this.DecodedFrames[frame] != null)
                return this.DecodedFrames[frame];

            EncodedCL2Frame rf = this.Frames[frame];
            RawBitmap bitmap = new RawBitmap(decompressCL2Data(rf.data, 10), this.Palette); //10: headers

            int frame_width;
            int frame_height;
            if (known_width > 0)
                frame_width = known_width;
            else
                frame_width = findWidth(rf);
            
            frame_height = bitmap.data.Length / frame_width;

            bitmap.Height = frame_height;
            bitmap.Width = frame_width;

            this.DecodedFrames[frame] = bitmap;
            GameConsole.ReportPerformance(PerformanceCategory.GraphicImporting, sw.ElapsedTicks);
            return bitmap;
        }
        private byte[] decompressCL2Data(byte[] data, int offset) {
            MemoryStream stream = new MemoryStream(data.Length * 10);

            byte N;

            //Iterate through codes
            for (int i = offset; i < data.Length; i++)//Skip 10 bytes (4 words) of header
            {
                N = data[i]; // This is always a code, with one of three possible meanings:
                if (N <= 0x7F)
                {
                    //Skip N pixels, they are transparent
                    for (int transparent = 0; transparent < N; transparent++)
                        stream.WriteByte(255);
                }
                else if (N > 0x80 && N < 0xBF)
                {
                    //Copy NEXT pixel (0xBF - N) times
                    for (int copy = 0; copy < (0xBF - N); copy++)
                        stream.WriteByte(data[i + 1]);
                    i++; //Skip one to get to next code
                }
                else if (N >= 0xBF && N <= 0xFF)
                {
                    //Copy next (0x100 - N) pixels verbatim.
                    for (int copy = 1; copy <= (0x100 - N); copy++)
                        stream.WriteByte(data[i + copy]);

                    //Skip pixels copied to get to next code
                    i += (0x100 - N);
                }
            }
            byte[] output = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(output, 0, (int)stream.Length);
            return output;
        }
        /// <summary>
        /// Get all RawBitmap frames decoded.
        /// </summary>
        /// <param name="known_width">0 for auto width, or specify a width</param>
        /// <returns></returns>
        public RawBitmap[] GetAllFrames(int known_width)
        {
            int frame_width;
            if (known_width > 0)
                frame_width = known_width;
            else
                frame_width = findWidth(this.Frames[0]);

            for (int i = 0; i < this.Frames.Count; i++)
            {
                GetDecodedFrame(i, frame_width);
            }

            return this.DecodedFrames;
        }
        public int FrameCount()
        {
            return this.Frames.Count;
        }
        public RawBitmap[] GetFrames(int start, int count, int known_width) {
            int frame_width;
            if (known_width > 0)
                frame_width = known_width;
            else
                frame_width = findWidth(this.Frames[0]);

            RawBitmap[] frames = new RawBitmap[count];
            for (int i = start; i < (start + count); i++)
            {
                frames[i - start] = GetDecodedFrame(i, frame_width);
            }
            return frames;
        }
        private int findWidth(EncodedCL2Frame rf)
        {
            //Attempt to find width. Works, but fails on some (corrupted) files such as plrgfx/warrior/wlb/wlbat.cl2
            int offset_to_32y = rf.data[2] + rf.data[3] * 256; //the second WORD.
            byte[] first_32_lines = new byte[offset_to_32y];
            Array.Copy(rf.data, first_32_lines, offset_to_32y);
            byte[] decompressed_first_32_lines = decompressCL2Data(first_32_lines, 10);
            return decompressed_first_32_lines.Length / 32;
        }
        

    }
}
