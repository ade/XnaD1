using System;
using System.Collections.Generic;
using System.Text;
using DiacloLib;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Diaclo
{
    internal delegate void MenuItemSelectedDlg(int index, object tag);
    internal class GameMenu : ForegroundActivity
    {
        internal DFontType FontType;
        internal MenuItemSelectedDlg OnMenuSelect;
        private List<GameMenuItem> MenuItems;
        private TilesetAnimation Selection;
        internal int LineHeight;
        private int textOffsetX;
        private int selectedItem;
        internal int SelectedItem
        {
            get
            {
                return this.selectedItem;
            }
            set
            {
                if (value >= 0 && value < this.MenuItems.Count)
                    this.selectedItem = value;
            }
        }


        public GameMenu(int positionX, int positionY, DFontType fonttype, MenuItemSelectedDlg eventListener)
        {
            this.X = positionX;
            this.Y = positionY;
            this.OnMenuSelect += eventListener;
            this.MenuItems = new List<GameMenuItem>();
            this.FontType = fonttype;
            switch (fonttype)
            {
                case DFontType.BigGold:
                    this.LineHeight = 45;
                    this.Selection = new TilesetAnimation(GameContent.PentSpinBig, 0, GameContent.PentSpinBig.Count() - 1, 0.1f);
                    this.textOffsetX = 60;
                    break;
                case DFontType.MediumGold:
                    this.LineHeight = 25;
                    this.Selection = new TilesetAnimation(GameContent.PentSpinSmall, 0, GameContent.PentSpinSmall.Count() - 1, 0.1f); //get from pcx?
                    this.textOffsetX = 30;
                    break;
                case DFontType.Console:
                case DFontType.Small:
                    this.LineHeight = 15;
                    this.Selection = new TilesetAnimation(GameContent.PentSpinSmall, 0, GameContent.PentSpinSmall.Count() - 1, 0.1f);
                    this.textOffsetX = 20;
                    break;
            }

        }
        public void AddItem(string title)
        {
            this.MenuItems.Add(new GameMenuItem(title, null, this.MenuItems.Count, this));
        }
        public GameMenuItem AddItem(string title, object tag)
        {
            GameMenuItem ret = new GameMenuItem(title, tag, this.MenuItems.Count, this);
            this.MenuItems.Add(ret);
            return ret;
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < this.MenuItems.Count; i++)
            {
                GameContent.Font.Draw(this.MenuItems[i].Title, this.X + this.textOffsetX, this.Y + this.LineHeight * i, this.FontType, spriteBatch);
            }
            this.Selection.Draw(spriteBatch, this.X, this.Y + this.LineHeight * this.selectedItem);
        }
        public override void Update(float secondsPassed)
        {
            this.Selection.Update(secondsPassed);
        }

        internal void ItemSelected()
        {
            this.OnMenuSelect(this.SelectedItem, this.MenuItems[this.selectedItem].Tag);
        }
        public override void KeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    Game.CueSound(Res.SND_SYSTEM_MENU_SELECT);
                    this.ItemSelected();
                    break;
                case Keys.Escape:
                    this.Close();
                    break;
                case Keys.Down:
                    Game.CueSound(Res.SND_SYSTEM_MENU_MOVE);
                    this.SelectedItem += 1;
                    break;
                case Keys.Up:
                    Game.CueSound(Res.SND_SYSTEM_MENU_MOVE);
                    this.SelectedItem -= 1;
                    break;
            }
        }

        private void Close()
        {
            this.OnMenuSelect(-1, null);
        }
    }
}
