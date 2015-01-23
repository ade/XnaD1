using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace DiacloLib
{

    /// <summary>
    /// An area is a rectangular composite of Squares.
    /// </summary>
    public class Area
    {
        public List<BaseNPC> npcs;
        public List<Player> Players;
        public String Name;
        public UInt16 Width;
        public UInt16 Height;
        public Square[] Squares;
        public ushort ID {get; set;}
        public Area(UInt16 width, UInt16 height)
        {
            this.Width = width;
            this.Height = height;
            int tiles = this.Width * this.Height;
            this.Squares = new Square[tiles];
            this.npcs = new List<BaseNPC>();
            this.Players = new List<Player>();
        }
        public void SetSquare(Point pos, Square square)
        {
            this.Squares[pos.X + (pos.Y * Width)] = square;
            square.Position = pos;
        }
        public Square GetSquare(Point pos)
        {
            if (pos.X < 0 || pos.Y < 0) return null;
            if (pos.X > this.Width - 1 || pos.Y > this.Height - 1) return null;
            int index = pos.X + (pos.Y * Width);
            if (index < 0 || index >= this.Squares.Length)
                return null;
            return this.Squares[index];
        }
        public bool PointWithinBounds(Point p)
        {
            if (p.X < 0 || p.Y < 0 || p.X > (this.Width - 1) || p.Y > (this.Height - 1))
                return false;
            else
                return true;
        }
        
    }
}
