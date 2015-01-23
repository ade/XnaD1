using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace Diaclo
{
    public delegate void TextInputResult(string result);

    internal class TextInputBox: ForegroundActivity
    {
        //Constants
        public const string AllowAlphanumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public const string AllowFontSupported = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!%#&*()-+='\";:,.?/";
        public const string AllowFontSupportedSmall = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!%#&*()-+='\";:,.?/[]$@\\^_|~";

        //Fields
        public int MaxLength { get; set; }
        public TextInputResult OnInputFinished;
        protected string AllowedChars;
        protected DFontType TextType;
        protected int CaretPos;
        protected string Text;
        protected TilesetAnimation Caret;
        

        public TextInputBox(int x, int y, DFontType font, string allowedCharacters, int maxLength, TextInputResult resultTarget)
        {
            this.X = x;
            this.Y = y;
            this.TextType = font;
            this.AllowedChars = allowedCharacters;
            this.OnInputFinished += resultTarget;
            this.Text = "";
            this.MaxLength = maxLength;
            switch (font)
            {
                case DFontType.BigGold:
                    this.Caret = new TilesetAnimation(GameContent.PentSpinBig, 0, GameContent.PentSpinBig.Count() - 1, 0.1f);
                    break;
                case DFontType.MediumGold:
                case DFontType.Small:
                case DFontType.Console:
                    this.Caret = new TilesetAnimation(GameContent.PentSpinSmall, 0, GameContent.PentSpinSmall.Count() - 1, 0.1f);
                    break;
                
            }
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawCurrentText(this.X, this.Y, spriteBatch);
        }
        protected void DrawCurrentText(int x, int y, SpriteBatch spriteBatch)
        {
            string before = beforeCaret();
            int beforeWidth = 0;

            if (before != null)
                beforeWidth = GameContent.Font.Draw(before, x, y, this.TextType, spriteBatch);
            this.Caret.Draw(spriteBatch, this.X + beforeWidth, y);

            string after = afterCaret();
            if (after != null)
                GameContent.Font.Draw(after, x + beforeWidth + this.Caret.Tileset.SpriteWidth, y, this.TextType, spriteBatch);
        }
        public override void Update(float secondsPassed)
        {
            this.Caret.Update(secondsPassed);
        }
        public override void CharEntered(CharacterEventArgs e)
        {
            if (this.Text.Length < this.MaxLength && this.AllowedChars.Contains(e.Character.ToString()))
            {
                this.InsertAt(this.CaretPos, e.Character);
                this.CaretPos++;
            }
        }
        public override void KeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    this.DeleteAt(this.CaretPos);
                    break;
                case Keys.Right:
                    if (this.CaretPos < this.Text.Length) this.CaretPos++;
                    break;
                case Keys.Left:
                    if (CaretPos > 0) this.CaretPos--;
                    break;
                case Keys.Back:
                    this.DeleteAt(this.CaretPos-1);
                    if(this.CaretPos > 0) this.CaretPos--;
                    break;
                case Keys.Enter:
                    this.Finish(true);
                    break;
                case Keys.Escape:
                    this.Finish(false);
                    break;
            }
        }
        public void DeleteAt(int pos)
        {
            if (pos >= 0 && pos < this.Text.Length)
            {
                string pre = "";
                string after = "";
                if (pos > 0)
                {
                    pre = this.Text.Substring(0, pos);
                }
                if (pos < this.Text.Length - 1)
                {
                    after = this.Text.Substring(pos+1);
                }
                this.Text = pre + after;
                
            }
        }
        public void InsertAt(int pos, char character)
        {
            if (pos >= 0 && pos <= this.Text.Length)
            {
                string pre = "";
                string after = "";
                if (pos > 0)
                {
                    pre = this.Text.Substring(0, pos);
                }
                if (pos < this.Text.Length)
                {
                    after = this.Text.Substring(pos);
                }
                this.Text = pre + character.ToString() + after;

            }
        }
        private string beforeCaret() {
            if (this.CaretPos > 0 && this.Text.Length > 0)
                return this.Text.Substring(0, this.CaretPos);
            else
                return null;
        }
        private string afterCaret()
        {
            if (this.CaretPos < this.Text.Length && this.Text.Length > 0)
                return this.Text.Substring(this.CaretPos);
            else
                return null;
        }
        private void Finish(bool returnresult)
        {
            if (returnresult)
                this.OnInputFinished(this.Text);
            else
                this.OnInputFinished(null);
        }

    }
}
