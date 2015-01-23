using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace DiacloLib
{
    public interface IGameDrawable
    {
        void Draw(SpriteBatch spriteBatch, int x, int y);
    }
    public interface IGameUpdateable
    {
        void Update(float secondsPassed);
    }
    public interface IGameEntity: IGameDrawable, IGameUpdateable
    {
        
        
    }
}
