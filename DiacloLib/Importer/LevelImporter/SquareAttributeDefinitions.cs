using System;
using System.Collections.Generic;
using System.Text;

namespace DiacloLib.Importer.LevelComponents
{
    /// <summary>
    /// .SOL File contains square attributes such as passability
    /// </summary>
    public class SquareAttributeDefinitions
    {
        public byte[] data;
        public int Tiles;
        public SquareAttributeDefinitions(byte[] data)
        {
            this.data = data;
            this.Tiles = this.data.Length;
        }
        public bool TilePassable(int index) {
            return ((this.data[index] & 1) > 0);
        }
        public bool TileLOSBlock(int index)
        {
            return ((this.data[index] & 2) > 0);
        }
        public bool TileMissileBlock(int index)
        {
            return ((this.data[index] & 4) > 0);
        }
    }
}
