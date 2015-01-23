using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer
{
    /// <summary>
    /// Legacy 8-bit bitmap, every byte is a palette index, rows are stored bottom -> top!
    /// </summary>
    public class RawBitmap
    {
        public byte[] data;
        public int Width { get; set; }
        public int Height { get; set;}
        public Palette Palette { get; set; }
        public RawBitmap(byte[] raw_data, Palette p)
        {
            data = raw_data;
            Width = 0;
            this.Palette = p;
        }
        public RawBitmap(int width, int height, Palette p)
        {
            this.Width = width;
            this.Height = height;
            data = new byte[width*height];
            this.Palette = p;
        }
        /// <summary>
        /// Calculate the width of the image if all transparent space on right side is removed
        /// </summary>
        /// <returns></returns>
        public int FindWidthByRightTrim()
        {
            for (int x = this.Width-1; x > 0; x--)
            {
                for (int y = 0; y < this.Height; y++)
                {
                    if (this.GetPixel(x, y) != 255)
                        return x + 1;
                }
            }
            return 1;
        }
        /// <summary>
        /// Return pixel (palette index) at specified coords
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public byte GetPixel(int x, int y)
        {
            return this.data[this.Width * (this.Height-1) - this.Width * y + x];
        }
        /// <summary>
        /// Set pixel (palette index) at specified coords
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public void SetPixel(int x, int y, byte color)
        {
            this.data[this.Width * (this.Height-1) - this.Width * y + x] = color;
        }
        public void Paste(RawBitmap pic, int ax, int ay)
        {
            for (int y = 0; y < pic.Height; y++)
            {
                for (int x = 0; x < pic.Height; x++)
                {
                    this.SetPixel(ax + x, ay + y, pic.GetPixel(x,y));
                }
            }
        }
        /// <summary>
        /// Copy a subregion to a new RawBitmap
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public RawBitmap Copy(int x, int y, int width, int height)
        {
            RawBitmap ret = new RawBitmap(width, height, this.Palette);
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    ret.SetPixel(dx, dy, this.GetPixel(x + dx, y + dy));
                }
            }
            return ret;
        }
        /// <summary>
        /// Fill entire image with the specified palette index
        /// </summary>
        /// <param name="color"></param>
        public void Blank(byte color)
        {
            for (int x = 0; x < this.Width; x++) {
                for (int y = 0; y < this.Height; y++)
                {
                    this.SetPixel(x, y, color);
                }
            }
        }
        /// <summary>
        /// Construct a highlight-overlay (find edges)
        /// </summary>
        /// <param name="highlightColor"></param>
        /// <returns></returns>
        public RawBitmap CreateHighlight(byte highlightColor)
        {
            byte mask = 255;
            RawBitmap ret = new RawBitmap(this.Width, this.Height, this.Palette);
            ret.Blank(mask);

            int n,s,e,w;
            for (int x = 0; x < this.Width; x++)
            {
                for (int y = 0; y < this.Height; y++)
                {
                    n=255;
                    s=255;
                    e=255;
                    w=255;
                    if (x > 0) w = this.GetPixel(x - 1, y);
                    if (y > 0) n = this.GetPixel(x, y - 1);
                    if (x < this.Width - 1) e = this.GetPixel(x + 1, y);
                    if (y < this.Height - 1) s = this.GetPixel(x, y + 1);
                    if ((e != 255 || w != 255 || s != 255 || n != 255) && this.GetPixel(x,y) == 255)
                        ret.SetPixel(x, y, highlightColor);
                }
            }
            return ret;
        }
        /// <summary>
        /// Draw a solid rectangle in the specified color
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="color"></param>
        public void DrawRect(int x, int y, int width, int height, byte color)
        {
            for (int dx = x; dx < x + width; dx++)
            {
                for (int dy = y; dy < y + height; dy++)
                {
                    this.SetPixel(dx, dy, color);
                }
            }
        }
    }
}
