using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;

namespace Diaclo
{
    abstract class ForegroundActivity
    {
        public int X { get; set; }
        public int Y { get; set; }

        public abstract void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch);
        public virtual void Update(float secondsPassed) 
        {
        }
        public virtual void KeyDown(KeyEventArgs e)
        {
        }
        public virtual void KeyUp(KeyEventArgs e)
        {
        }
        public virtual void CharEntered(CharacterEventArgs e)
        {
        }
    }
}
