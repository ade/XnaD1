using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DiacloLib
{
    /// <summary>
    /// A square is a composite of 16 frames from a tileset as follows: 
    /// 0  1
    /// 2  3 
    /// ..
    /// 12 13
    /// 14 15
    /// Origin is at the top left corner of frame 14! This is the "ground level" tile
    /// That means coordinnates are (-224,0)/(-224,32)/(-192,0)/(-192,32)/.../(-32,0)/(-32,32)/(0,0)/(0,32).
    /// Add tileset object references along with a frame for that tileset for every frame to draw.
    /// A square can also be non-drawable, for server use, it will then use numbers for every tileset instead of a object.
    /// </summary>
    public class Square
    {
        public WorldCreature Creature; //a tile can contain one creature
        public WorldCreature Corpse;
        public Tileset Tileset { get; set; }
        public byte LevelID { get; set; }
        public UInt16[] Frame { get; set; }
        public Boolean PassablePlayer;
        public Boolean PassableMissile;
        public Boolean PassableSight;
        public Point Position { get; set; }
        public Boolean Drawable { get; set; }
        public Square()
        {
            PassablePlayer = true;
            PassableMissile = true;
            PassableSight = true;
            this.Frame = new UInt16[16];
        }
        public void setFrame(byte index, UInt16 frame)
        {
            this.Frame[index] = frame;
        }
        
        public void DrawHigh(SpriteBatch spriteBatch, int x, int y)
        {
            /* Frames are drawn in a 32x32 grid;
             * 0    1
             * 2    3
             * 4    5
             * 6    7
             * 8    9
             * 10   11
             * 12   13
             * 14   15     <--- "ground" level: not drawn here
             */
            int row = 1;
            for (int i = 12; i >= 0; i -= 2)
            {
                if (this.Frame[i] != 0)
                    this.Tileset.Draw(spriteBatch, this.Frame[i], x, y - (row * 32));
                row++;
            }
            row = 1;
            for (int i = 13; i >= 1; i -= 2)
            {
                if (this.Frame[i] != 0)
                    this.Tileset.Draw(spriteBatch, this.Frame[i], x + 32, y - (row * 32));
                row++;
            }
        }
        public void DrawLow(SpriteBatch spriteBatch, int x, int y)
        {
            /* Frames are drawn in a 32x32 grid;
             * 0    1
             * 2    3
             * 4    5
             * 6    7
             * 8    9
             * 10   11
             * 12   13
             * 14   15     <--- "ground" level: ONLY drawn here
             */
            if (this.Frame[14] != 0)
                this.Tileset.Draw(spriteBatch, this.Frame[14], x, y);

            if (this.Frame[15] != 0)
                this.Tileset.Draw(spriteBatch, this.Frame[15], x + 32, y);
           
            
        }
        public Player getPlayer()
        {
            if (Creature is Player) return (Player)Creature;
            return null;
        }
        public BaseNPC getNPC()
        {
            if (Creature is BaseNPC) return (BaseNPC)Creature;
            return null;
        }
    }
}
