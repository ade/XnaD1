using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace DiacloLib.Importer
{
    public static class GfxConverter
    {
        /// <summary>
        /// Create a Texture2D object to use for drawing.
        /// </summary>
        /// <param name="g">The GraphicsDevice to associate with</param>
        /// <param name="bitmap">Raw image data</param>
        /// <param name="palette">Palette</param>
        /// <param name="translation">Translation</param>
        public static Texture2D GetTexture2D(GraphicsDevice g, RawBitmap bitmap)
        {
            Stopwatch sw = Stopwatch.StartNew();

            Texture2D ret = new Texture2D(g, bitmap.Width, bitmap.Height, 1, TextureUsage.None, SurfaceFormat.Color);

            Color[] data = new Color[bitmap.Width * bitmap.Height]; 
            for (int y = bitmap.Height-1; y >= 0; y--)
            {
                for (int x = 0; x < bitmap.Width; x++)
                    data[(bitmap.Height - 1) * bitmap.Width - bitmap.Width * y + x] = bitmap.Palette.GetColor(bitmap.data[y * bitmap.Width + x]);
            }
            ret.SetData(data);

            GameConsole.ReportPerformance(PerformanceCategory.GraphicImporting, sw.ElapsedTicks);
            return ret;
        }

        /// <summary>
        /// Create a tileset from an array of RawBitmap
        /// </summary>
        /// <param name="input">The raw bitmaps</param>
        /// <param name="columns">The amount of columns</param>
        /// <returns></returns>
        public static Texture2D CreateTileset(GraphicsDevice g, RawBitmap[] input, int columns) {
            Stopwatch sw = Stopwatch.StartNew();
            if (columns == 0) columns = (int)Math.Floor(Math.Sqrt(input.Length));
            int sprite_height = input[0].Height;
            int sprite_width = input[0].Width;
            int rows = (int)Math.Ceiling((double)input.Length / columns);
            int ts_width = sprite_width * columns;
            int ts_height = sprite_height * rows;

            Texture2D ret = new Texture2D(g, ts_width, ts_height);
            Color[] data = new Color[ts_width * ts_height];

            int col = 0, row = 0;
            for (int i = 0; i < input.Length; i++)
            {
                int x = col * sprite_width;
                int y = row * sprite_height;
                col++;
                if (col == columns)
                {
                    row++;
                    col = 0;
                }
                int draw_y;
                int draw_x;
                for (int sub_y = 0; sub_y < sprite_height; sub_y++)
                {
                    for (int sub_x = 0; sub_x < sprite_width; sub_x++)
                    {
                        draw_x = x + sub_x;
                        draw_y = y + (sprite_height - 1) - sub_y;
                        data[ts_width*draw_y + draw_x] = input[i].Palette.GetColor(input[i].data[sub_y * sprite_width + sub_x]);
                    }
                }
                
                
            }
            ret.SetData(data);
            //ret.Save(@"C:\temp\ts\ts" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "-" + DateTime.Now.Hour + DateTime.Now.Minute + "_" + DateTime.Now.Second + DateTime.Now.Millisecond + ".png", ImageFileFormat.Png);
            GameConsole.ReportPerformance(PerformanceCategory.GraphicImporting, sw.ElapsedTicks);
            return ret;
            
        }
        public static Texture2D CreateTileset(GraphicsDevice g, RawBitmap[] input)
        {
            return CreateTileset(g, input, 0);
        }
        public static Texture2D CreateTileset(GraphicsDevice g, CL2Container cl2, int columns, int known_width)
        {
            RawBitmap[] frames = cl2.GetAllFrames(known_width);
            return CreateTileset(g, frames, columns);
        }

    }
}
