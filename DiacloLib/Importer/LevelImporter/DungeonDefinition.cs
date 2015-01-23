using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer
{
    /// <summary>
    /// DUN file parser. Used to parse town layout files.
    /// </summary>
    public class DungeonDefinition
    {
        //subareas
        private class DUNFile
        {
            public int width;
            public int height;
            public int[] tileid;
        }
        private DUNFile[] duns;

        //composited
        public int[] TileID;         
        public int Width;
        public int Height; 
        public DungeonDefinition(byte[][] filedata)
        {
            this.duns = new DUNFile[filedata.Length];

            //Read all tile ids
            for (int file_number = 0; file_number < filedata.Length; file_number++)
            {
                IntelStream stream = new IntelStream(filedata[file_number]);
                DUNFile dun = new DUNFile();
                dun.width = stream.ReadWord();
                dun.height = stream.ReadWord();
                int tiles = dun.width * dun.height;
                dun.tileid = new int[tiles];
 
                int entriesPerTile = (((int)stream.Length - 4) / tiles) / 2;

                for (int tile_number = 0; tile_number < tiles; tile_number++)
                {
                    ushort tileid = stream.ReadWord();
                    dun.tileid[tile_number] = tileid;
                }
                this.duns[file_number] = dun;
            }

            //Get final dimensions, initialize square area
            int totalTiles = 0;
            for (int i = 0; i < this.duns.Length; i++)
            {
                totalTiles += this.duns[i].tileid.Length;
            }
            this.Width = (this.Height = (int)Math.Sqrt(totalTiles));
            this.TileID = new int[totalTiles];

        }
        private int getDunCombinedWidths(int index)
        {
            int ret = 0;
            for (int i = 0; i < index; i++)
            {
                ret += this.duns[i].width;
            }
            return ret;
        }
        private int getDunCombinedHeights(int index)
        {
            int ret = 0;
            for (int i = 0; i < index; i++)
            {
                ret += this.duns[i].height;
            }
            return ret;
        }
        /// <summary>
        /// Paint a DUN subarea to composite. Must be done before using tile ids!
        /// </summary>
        /// <param name="index">Index of DUN</param>
        /// <param name="x">Tile X to start on</param>
        /// <param name="y">Tile Y to start on</param>
        public void PaintDungeon(int index, int x, int y)
        {
            int draw_y;
            int draw_x;
            DUNFile dun = this.duns[index];
            for (int sub_y = 0; sub_y < dun.height; sub_y++)
            {
                for (int sub_x = 0; sub_x < dun.width; sub_x++)
                {
                    draw_x = x + sub_x;
                    draw_y = y + sub_y;
                    this.TileID[this.Width * draw_y + draw_x] = dun.tileid[sub_y * dun.width + sub_x];
                }
            }
        }
    }
}
