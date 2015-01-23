using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using DiacloLib;
using DiacloLib.Importer;
using Microsoft.Xna.Framework;

namespace Diaclo
{
    public enum DFontType
    {
        Small = 0,
        MediumGold = 1,
        BigGold = 2,
        Console = 255
    }

    public class GameFonts
    {
        private const int TypeCount = 3;
        private static int[] asciiIndexSmalltext = new int[] {
           //0   1   2   3   4   5   6   7   8   9
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, //0..
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, //10..
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, //20
            -1, -1, -1, 53, 43, 56, 57, 55, 54, 45, //30
            39, 40, 58, 38, 49, 36, 50, 51, 35, 26, //40
            27, 28, 29, 30, 31, 32, 33, 34, 47, 48, //50
            -1, 37, -1, 52, 61, 0, 1, 2, 3, 4, //60
            5, 6, 7, 8, 9, 10, 11, 12, 13, 14, //70
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, //80
            25, 41, 62, 42, 63, 64, 45, 0, 1, 2, //90
            3, 4, 5, 6, 7, 8, 9, 10, 11, 12, //100
            13, 14, 15, 16, 17, 18, 19, 20, 21, 22, //110
            23, 24, 25, -1, 65, -1, 66, -1, -1, -1, //120
        };
        private static int[] asciiIndexOther = new int[] {
           //0   1   2   3   4   5   6   7   8   9
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, //0..
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, //10..
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, //20
            -1, -1, -1, 36, 47, 37, -1, 38, 39, 46, //30
            41, 42, 40, 44, 51, 43, 52, 54, 35, 26, //40
            27, 28, 29, 30, 31, 32, 33, 34, 50, 49, //50
            -1, 45, -1, 53, -1,  0,  1,  2,  3,  4, //60
             5,  6,  7,  8,  9, 10, 11, 12, 13, 14, //70
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, //80
            25, -1, -1, -1, -1, -1, 46,  0,  1,  2, //90
             3,  4,  5,  6,  7,  8,  9, 10, 11, 12, //100
            13, 14, 15, 16, 17, 18, 19, 20, 21, 22, //110
            23, 24, 25, -1, -1, -1, -1, -1, -1, -1, //120
        };
        private Tileset[] tilesets;
        private int[][] lookupTables;
        private byte[][] spacing;
        private SpriteFont consoleFont;
        public GameFonts(GenericCEL[] sheets, SpriteFont consoleFont)
        {
            this.tilesets = new Tileset[GameFonts.TypeCount];
            this.lookupTables = new int[GameFonts.TypeCount][];
            this.spacing = new byte[GameFonts.TypeCount][];
            this.consoleFont = consoleFont;

            for (int i = 0; i < GameFonts.TypeCount; i++)
            {
                Texture2D texture = GfxConverter.CreateTileset(GameContent.GraphicsDevice, sheets[i].GetFrames(), 10);
                this.tilesets[i] = new Tileset(texture, sheets[i].GetFrame(0).Width, sheets[i].GetFrame(0).Height);
                this.spacing[i] = new byte[sheets[i].Frames];

                for (int s = 0; s < sheets[i].Frames; s++)
                {
                    RawBitmap frame = sheets[i].GetFrame(s);
                    if (i == 2 && (s == 18 || s == 6 || s == 14 || s == 49)) {
                        //bigtgold.cel contains a couple of errors that we need to adress..
                        //the letters G, O, S, 7 and ; overlap one pixel in X with the next letter
                        //replace the last column of pixels with transparent ones
                        frame.DrawRect(45, 0, 1, 45, 255);
                    }
                    this.spacing[i][s] = (byte)(frame.FindWidthByRightTrim() + 2);
                }
            }

            this.lookupTables[(int)DFontType.Small] = GameFonts.asciiIndexSmalltext;
            this.lookupTables[(int)DFontType.MediumGold] = GameFonts.asciiIndexOther;
            this.lookupTables[(int)DFontType.BigGold] = GameFonts.asciiIndexOther;

        }
        public int GetLineHeight(DFontType font)
        {
            if (font != DFontType.Console)
                return this.tilesets[(int)font].SpriteHeight;
            else
                return 13;

        }
        /// <summary>
        /// Render a string of text.
        /// </summary>
        /// <param name="type">Typeface (font size) to use</param>
        /// <returns>Width of drawn string</returns>
        public int Draw(string text, int x, int y, DFontType type, SpriteBatch spriteBatch)
        {
            return this.Draw(text, x, y, type, spriteBatch, true, 1.0f);
        }
        /// <summary>
        /// Render a string of text.
        /// </summary>
        /// <param name="type">Typeface (font size) to use</param>
        /// <param name="scale">Scale to use (1.0 = normal)</param>
        /// <returns>Width of drawn string</returns>
        public int Draw(string text, int x, int y, DFontType type, SpriteBatch spriteBatch, float scale)
        {
            return this.Draw(text, x, y, type, spriteBatch, true, scale);
        }
        /// <summary>
        /// Get width of a string if drawn
        /// </summary>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public int GetDrawWidth(string text, DFontType type)
        {
            return this.Draw(text, 0, 0, type, null, false, 1.0f);
        }
        /// <summary>
        /// Draw or calculate width of a string of text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="type">Typeface/font</param>
        /// <param name="spriteBatch"></param>
        /// <param name="draw">Draw the string? If false, calculate width only</param>
        /// <returns>Width of drawn string</returns>
        private int Draw(string text, int x, int y, DFontType type, SpriteBatch spriteBatch, bool draw, float scale)
        {
            if (type != DFontType.Console)
            {
                int[] lookup = this.lookupTables[(int)type];
                Tileset t = this.tilesets[(int)type];

                int index;
                int tilesetindex;
                int drawx = 0;
                float charscale = 1.0f;
                float lowerCaseModifier = 0.85f;
                int lowercaseOffset = t.SpriteHeight - (int)((float)t.SpriteHeight * lowerCaseModifier);
                int charOffsetY = 0;

                for (int i = 0; i < text.Length; i++)
                {
                    index = (int)text[i];
                    if (index > 31 && index < 128)
                    {
                        if (index == 32)
                        { //Space
                            drawx += this.spacing[(int)type][32];
                        }
                        else
                        {
                            tilesetindex = this.lookupTables[(int)type][index];
                            if (tilesetindex != -1)
                            {
                                if (index >= 97 && index <= 122)
                                {
                                    charscale = lowerCaseModifier; //lowercase
                                    charOffsetY = lowercaseOffset;
                                }
                                else
                                {
                                    charscale = 1.0f;
                                    charOffsetY = 0;
                                }
                                if (draw)
                                    t.Draw(spriteBatch, tilesetindex, drawx + x, y + charOffsetY, scale * charscale);
                                drawx += (int)((float)this.spacing[(int)type][tilesetindex] * charscale);
                            }
                        }
                    }
                }
                return drawx;
            }
            else
            {
                //Console font
                if(draw)
                    spriteBatch.DrawString(this.consoleFont, text, new Vector2(x, y), Color.White);
                return text.Length * 8;

            }
        }

    }

}
