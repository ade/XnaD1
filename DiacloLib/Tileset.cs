using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DiacloLib
{
    public class Tileset
    {
        private int columns;
        private int rows;
        private Texture2D texture;
        private int spriteWidth;
        private int spriteHeight;
        private int count;
        private int offsetX; //Offset when drawn on screen position 
        private int offsetY;
        public int OffsetX
        {
            get { return offsetX; }
            set { offsetX = value; }
        }
        public int OffsetY
        {
            get { return offsetY; }
            set { offsetY = value; }
        }
        public int SpriteWidth
        {
            get { return this.spriteWidth; }
        }
        public int SpriteHeight
        {
            get { return this.spriteHeight; }
        }
        
        //public int Rows { get; set; }
        //public int Columns { get; set; }
        //public int id { get; set; }
        
        private Texture2D getTexture()
        {
            return texture;
        }

        /// <summary>
        /// Create a tileset from "texture" where every frame is "width" by "height" big.
        /// </summary>
        public Tileset(Texture2D texture, int width, int height)
        {
            this.spriteWidth = width;
            this.spriteHeight = height;
            this.columns = texture.Width / width;
            this.rows = texture.Height / height;
            this.texture = texture;
            this.count = this.columns * this.rows;//Empty frames if not full tileset!
        }
        /// <summary>
        /// Create a tileset with the specified amount of frames in every row and column.
        /// </summary>
        public Tileset(int cols, int rows, Texture2D texture)
        {
            this.spriteWidth = texture.Width / cols;
            this.spriteHeight = texture.Height / rows;
            this.columns = cols;
            this.rows = rows;
            this.texture = texture;
            this.count = cols * rows; //Empty frames if not full tileset!
        }
        /// <summary>
        /// Create a tileset with the specified amount of frames, rows and columns.
        /// </summary>
        public Tileset(int frames, int cols, int rows, Texture2D texture)
        {
            this.spriteWidth = texture.Width / cols;
            this.spriteHeight = texture.Height / rows;
            this.columns = cols;
            this.count = frames;
            this.rows = rows;
            this.texture = texture;
        }
        /// <summary>
        /// Create a tileset with the specified amount of frames and sprite dimensions.
        /// </summary>
        public Tileset(Texture2D texture, int frames, int frame_width, int frame_height)
        {
            this.spriteWidth = frame_width;
            this.spriteHeight = frame_height;
            this.columns = texture.Width / spriteWidth;
            this.rows = texture.Height / spriteHeight;
            this.texture = texture;
            this.count = frames;
        }
        /// <summary>
        /// Draw a frame from the tileset, zero based.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, int frame, int x, int y)
        {
            int row = (int)Math.Floor((double)frame / this.columns);
            int col = frame - (row * this.columns);

            this.Draw(spriteBatch, row, col, x, y, 1.0f);
        }
        /// <summary>
        /// Draw a frame from the tileset, zero based, with scale
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, int frame, int x, int y, float scale)
        {
            int row = (int)Math.Floor((double)frame / this.columns);
            int col = frame - (row * this.columns);

            this.Draw(spriteBatch, row, col, x, y, scale);
        }
        /// <summary>
        /// Draw a frame from the tileset by row and column number.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, int row, int col, int x, int y, float scale)
        {
            int sourceX = col * spriteWidth;
            int sourceY = spriteHeight * row;

            spriteBatch.Draw(this.texture,
                    new Rectangle(x + this.offsetX, y + this.offsetY, (int)((float)spriteWidth * scale), (int)((float)spriteHeight * scale)), //Destination
                    new Rectangle(sourceX, sourceY, spriteWidth, spriteHeight), //Source rectangle
                    Color.White, //color mode
                    0, //Rotation
                    new Vector2(0, 0), //origin top left
                    SpriteEffects.None, 0);
        
        }
        public int Count()
        {
            return count;
        }
    }
}
