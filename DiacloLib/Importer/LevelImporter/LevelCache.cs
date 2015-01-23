using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using DiacloLib.Importer.LevelComponents;

namespace DiacloLib.Importer
{
    public enum LevelType
    {
        Town = 0,
        Church = 1,
        Catacombs = 2,
        Caves = 3,
        Hell = 4
    }
    public class LevelCache
    {
        private LevelGraphics[] levelGraphicsData;
        private Tileset[] levelTilesets;
        private GraphicsDevice graphicsDevice;
        public LevelCache(GraphicsDevice g)
        {
            this.levelGraphicsData = new LevelGraphics[5];
            this.levelTilesets = new Tileset[5];
            this.graphicsDevice = g;
        }
        public static LevelGraphics LoadLevel(int index)
        {
            LevelGraphics ret;
            if (index == 0)
            {
                ret = new LevelGraphics(LegacyContent.GetMPQFile(@"levels\towndata\town.til"), LegacyContent.GetMPQFile(@"levels\towndata\town.sol"), LegacyContent.GetMPQFile(@"levels\towndata\town.min"), LegacyContent.GetMPQFile(@"levels\towndata\town.cel"), LegacyContent.GetMPQFile(@"levels\towndata\town.pal"), 0);
            }
            else
            {
                ret = new LevelGraphics(
                    LegacyContent.GetMPQFile(@"levels\l" + index + @"data\l" + index + @".til"),
                    LegacyContent.GetMPQFile(@"levels\l" + index + @"data\l" + index + @".sol"),
                    LegacyContent.GetMPQFile(@"levels\l" + index + @"data\l" + index + @".min"),
                    LegacyContent.GetMPQFile(@"levels\l" + index + @"data\l" + index + @".cel"),
                    LegacyContent.GetMPQFile(@"levels\l" + index + @"data\l" + index + @".pal"),
                    index);
            }
            return ret;
        }
        public void CreateLevelTilesets(int index) {
            if (this.levelGraphicsData[index] == null)
            {

                this.levelGraphicsData[index] = LoadLevel(index);

                //Decode legacy format
                FrameDefinitions fd = this.levelGraphicsData[index].Tiles;
                RawBitmap[] bitmaps = new RawBitmap[fd.Frames+1];
                bitmaps[0] = new RawBitmap(32, 32, this.levelGraphicsData[index].Palette); //First tile (index 0) is blank (so that we match with file indexes)
                
                for (int i = 1; i < fd.Frames+1; i++)
                {
                    bitmaps[i] = fd.GetFrame(i-1);
                }
                //Convert to tileset texture
                Texture2D texture = GfxConverter.CreateTileset(this.graphicsDevice, bitmaps, (int)Math.Floor(Math.Sqrt(bitmaps.Length)));

                //Make tileset object
                Tileset t = new Tileset(texture, 32, 32);

                this.levelTilesets[index] = t;
            }
        }
        public Tileset GetTileset(ushort level_id)
        {
            if(this.levelGraphicsData[level_id] == null) 
                this.CreateLevelTilesets(level_id);
            return this.levelTilesets[level_id];
        }
    }
}
