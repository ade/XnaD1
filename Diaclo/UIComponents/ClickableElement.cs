using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;

namespace Diaclo.UIComponents
{
    public class ClickableElement
    {
        public ClickResult Type;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public UIPanel Parent;
        public Tileset Tileset;
        public int PressedFrame = -1;
        public int Frame = -1;
        public bool Pressed;
        public bool Visible;

        public ClickableElement(ClickResult type, UIPanel parent)
        {
            this.Type = type;
            this.Parent = parent;
            this.Visible = true;
        }
        public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            if (this.Tileset != null && this.Visible)
            {
                if (Pressed && this.PressedFrame != -1)
                {
                    this.Tileset.Draw(spriteBatch, this.PressedFrame, this.Parent.X + this.X, this.Parent.Y + this.Y);
                }
                else if (!Pressed && this.Frame != -1)
                {
                    this.Tileset.Draw(spriteBatch, this.Frame, this.Parent.X + this.X, this.Parent.Y + this.Y);
                }
            }
        }
    }
}
