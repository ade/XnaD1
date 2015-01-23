using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Diaclo.UIComponents
{
    public class UIPanel
    {
        public Texture2D Texture;
        public bool Visible = false;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public List<ClickableElement> ClickListeners;
        public ClickableElement Click(int x, int y)
        {
            int sub_x = x - this.X;
            int sub_y = y - this.Y;

            if (this.Visible)
            {
                foreach (ClickableElement e in this.ClickListeners)
                {
                    if (e.Visible && (e.X < sub_x) && ((e.X + e.Width) > sub_x) && (e.Y < sub_y) && ((e.Y + e.Height) > sub_y))
                    {
                        return e;
                    }
                }
            }
            return null;
        }
        public UIPanel(int x, int y, int width, int height, Texture2D texture)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.Texture = texture;
            this.ClickListeners = new List<ClickableElement>();
        }

        internal void Draw(SpriteBatch spriteBatch)
        {
            if(this.Texture != null)
                spriteBatch.Draw(this.Texture, new Vector2(this.X, this.Y), Color.White);
            
            foreach (ClickableElement e in this.ClickListeners)
            {
                e.Draw(spriteBatch);
            }
        }
    }
}
