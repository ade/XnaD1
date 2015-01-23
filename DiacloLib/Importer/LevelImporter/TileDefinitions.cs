using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer.LevelComponents
{
    /// <summary>
    /// .TIL file: tile definitions
    /// A til entry is simply 4 squaredefinitions index.
    /// They are ordered like this :
    ///     0
    ///   2   1
    ///     3
    /// </summary>
    public class TileDefinitions
    {
        public byte[] data;
        public int TileCount;
        public Tile[] Tiles;

        public TileDefinitions(byte[] data)
        {
            this.data = data;
            this.TileCount = data.Length / 8;
            IntelStream stream = new IntelStream(data);
            this.Tiles = new Tile[this.TileCount+1];
            this.Tiles[0] = new Tile();
            
            for (int i = 1; i < this.TileCount +1; i++)
            {
                Tile t = (this.Tiles[i] = new Tile());
                t.squares[0] = (int)stream.ReadWord();
                t.squares[1] = (int)stream.ReadWord();
                t.squares[2] = (int)stream.ReadWord();
                t.squares[3] = (int)stream.ReadWord();
            }
        }
    }
}
