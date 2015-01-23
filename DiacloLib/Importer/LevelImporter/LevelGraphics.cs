using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib.Importer.LevelComponents;
using Microsoft.Xna.Framework;

namespace DiacloLib.Importer
{
    public class LevelGraphics
    {
        public TileDefinitions TILdata;
        public SquareAttributeDefinitions SOLdata;
        public SquareDefinitions MINdata;
        public Palette Palette;
        public FrameDefinitions Tiles;
        public int LevelID;

        public LevelGraphics(byte[] til, byte[] sol, byte[] min, byte[] cel, byte[] pal, int levelid)
        {
            this.LevelID = levelid;

            this.Palette = new Palette(pal);
            SOLdata = new SquareAttributeDefinitions(sol);

            //MIN file is 2 bytes for each frame. 16 or 10 frames per square
            int framesPerSquare = (min.Length / SOLdata.Tiles) / 2;
            int squares = SOLdata.Tiles;

            MINdata = new SquareDefinitions(min, framesPerSquare);
            TILdata = new TileDefinitions(til);
            Tiles = new FrameDefinitions(cel, this.Palette);

            //Apply the frametypes found in the MIN file to the graphics data in the CEL
            for (int s = 0; s < squares; s++)
            {
                SquareInfo square = MINdata.Squares[s];
                for (int i = 0; i < framesPerSquare; i++)
                {
                    int frameid = square.Frame[i] - 1; //Frame id is 1-based in file
                    if (frameid >= 0)
                    {
                        Tiles.EncodedFrames[frameid].FrameType = square.Type[i];
                    }
                }
            }

            
        }
        private void ReadCel(byte[] data)
        {
            Tiles = new FrameDefinitions(data, this.Palette);
        }
        public SquareInfo[] GetTileAsSquareInfo(int index)
        {
            Tile t = this.TILdata.Tiles[index];

            SquareInfo[] ret = new SquareInfo[4];
            ret[0] = this.MINdata.Squares[t.squares[0]];
            ret[1] = this.MINdata.Squares[t.squares[1]];
            ret[2] = this.MINdata.Squares[t.squares[2]];
            ret[3] = this.MINdata.Squares[t.squares[3]];
            return ret;
        }
        public Square[] GetTileAsSquares(int index)
        {
            Tile t = this.TILdata.Tiles[index];
            //SquareInfo[] info = this.GetTileAsSquareInfo(index);
            Square[] ret = new Square[4];

            ret[0] = this.GetSquare(t.squares[0]);
            ret[1] = this.GetSquare(t.squares[1]);
            ret[2] = this.GetSquare(t.squares[2]);
            ret[3] = this.GetSquare(t.squares[3]);

            return ret;
        }
        public Square GetSquare(int index) {
            SquareInfo info = this.MINdata.Squares[index];
            Square ret = new Square();
            ret.LevelID = (byte)this.LevelID;

            int frameCount = info.Frame.Length;
            for (int i = frameCount-1; i >= 0; i--)
            {
                ret.Frame[i + (16 - frameCount)] = (ushort)info.Frame[i];
            }
            ret.PassableMissile = !this.SOLdata.TileMissileBlock(index);
            ret.PassablePlayer = !this.SOLdata.TilePassable(index);
            ret.PassableSight = !this.SOLdata.TileLOSBlock(index);
            return ret;
        }
        public void PaintTile(int index, int x, int y, Area a)
        {
            Square[] squares = this.GetTileAsSquares(index);
            a.SetSquare(new Point(x, y), squares[0]);
            a.SetSquare(new Point(x+1, y), squares[1]);
            a.SetSquare(new Point(x, y+1), squares[2]);
            a.SetSquare(new Point(x+1, y+1), squares[3]);
        }

    }
}
