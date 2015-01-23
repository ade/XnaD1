using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace DiacloLib
{
    public class TilesetAnimation: IGameEntity
    {
        public Tileset Tileset;
        public int StartFrame;
        public int EndFrame;
        public float UpdateInterval;
        private int currentFrame;
        private float secondsPassedOnFrame = 0;
        public bool Loop = true;

        public int CurrentFrame
        {
            get { return currentFrame; }
            set { 
                currentFrame = value;
                secondsPassedOnFrame = 0;
            }
        }
 
        public TilesetAnimation(Tileset tileset, int startFrame, int endFrame, float updateInterval)
        {
            this.Tileset = tileset;
            this.StartFrame = startFrame;
            this.currentFrame = startFrame;
            this.EndFrame = endFrame;
            this.UpdateInterval = updateInterval;
        }
        public void Update(float secondsPassed)
        {
            if (this.UpdateInterval > 0)
            {
                secondsPassedOnFrame += secondsPassed;
                if (secondsPassedOnFrame > this.UpdateInterval)
                {
                    this.currentFrame += (int)Math.Floor(secondsPassedOnFrame / this.UpdateInterval);

                    if (currentFrame > EndFrame)
                    {
                        if (Loop)
                            this.currentFrame = this.StartFrame;
                        else
                            this.currentFrame = this.EndFrame;
                    }
                    secondsPassedOnFrame = 0;
                }
            }
        }
        public void Draw(SpriteBatch spriteBatch, int x, int y) {
            this.Tileset.Draw(spriteBatch, this.currentFrame, x, y);
        }
    }
}
