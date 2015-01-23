using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;

namespace Diaclo
{
    internal class TextInputDialog: TextInputBox
    {
        public string Title { get; set; }
        private int lineHeight;
        
        public TextInputDialog(int x, int y, DFontType font, string allowedChars, int maxlength, string title, TextInputResult resultTarget) : base(x,y,font,allowedChars,maxlength,resultTarget)
        {
            this.lineHeight = (int)((float)GameContent.Font.GetLineHeight(font) * 1.5);
            this.Title = title;
        }
        public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            GameContent.Font.Draw(Title, this.X, this.Y, this.TextType, spriteBatch);
            this.DrawCurrentText(this.X, this.Y + this.lineHeight, spriteBatch);
        }
    }
}
