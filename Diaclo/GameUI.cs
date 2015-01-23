using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using DiacloLib;
using Microsoft.Xna.Framework.Graphics;
using Diaclo.UIComponents;
using Microsoft.Xna.Framework.Input;
using DiacloLib.Importer;

namespace Diaclo
{
    public enum UIPanelType //Tied to a List<UIPanel>
    {
        CharacterPane = 0,
        ControlPanel = 1,
        LevelUpNotifier = 2
    }
    public enum ClickResult
    {
        CharButton,
        LevelUpStr,
        LevelUpMag,
        LevelUpDex,
        LevelUpVit
    }
    class GameUI
    {
        private ClickableElement ActiveElement;
        private List<UIPanel> Panels;
        private Game Parent;

        //main panel
        private Texture2D hp_full;
        private Texture2D mana_full;

        public GameUI(Game parent)
        {
            this.Panels = new List<UIPanel>();
            this.Parent = parent;
        }

        public bool ElementVisible(UIPanelType e)
        {
            return this.Panels[(int)e].Visible;
        }
        public void ToggleDisplay(UIPanelType e)
        {
            this.Panels[(int)e].Visible = !this.ElementVisible(e);
        }

        internal bool CatchClick(MouseState mousestate)
        {
            ClickableElement e = GetClickedItem(mousestate.X, mousestate.Y);
            if (e != null)
            {
                if (mousestate.LeftButton == ButtonState.Released && e == this.ActiveElement)
                {
                    //Left released on active element
                    ClickEvent(e.Type);
                    e.Pressed = false;
                    this.ActiveElement = null;
                }
                else if (mousestate.RightButton == ButtonState.Released && e == this.ActiveElement)
                {
                    //Right released on active element
                }
                else if (mousestate.LeftButton == ButtonState.Pressed && e != this.ActiveElement)
                {
                    //Left pressed on new item
                    if (this.ActiveElement != null)
                        this.ActiveElement.Pressed = false;
                    this.ActiveElement = e;
                    e.Pressed = true;
                }
                else if (mousestate.RightButton == ButtonState.Pressed && e != this.ActiveElement)
                {
                    //Right pressed
                }

                return true;
            }
            else
            {
                //No element - release pressed state on active object
                if (this.ActiveElement != null)
                    this.ActiveElement.Pressed = false;
                this.ActiveElement = null;
            }

            //Check if mouse click was inside UI
            if (mousestate.LeftButton == ButtonState.Pressed)
            {
                foreach (UIPanel p in this.Panels)
                {
                    if (p.Visible && (p.X < mousestate.X) && ((p.X + p.Width) > mousestate.X) && (p.Y < mousestate.Y) && ((p.Y + p.Height) > mousestate.Y))
                        return true; //catch click on empty panel space
                }
            }

            return false;
        }

        private void ClickEvent(ClickResult clicktype)
        {
            ClickResult ret = ClickResult.CharButton;
            bool clickevent = false;
            switch (clicktype)
            {
                case ClickResult.CharButton:
                    {
                        this.ToggleDisplay(UIPanelType.CharacterPane);
                        break;
                    }
                case ClickResult.LevelUpStr:
                case ClickResult.LevelUpMag:
                case ClickResult.LevelUpDex:
                case ClickResult.LevelUpVit:
                    clickevent = true;
                    ret = clicktype;
                    break;

            }
            if (clickevent)
                this.Parent.OnUIClick(ret);
        }
        private ClickableElement GetClickedItem(int x, int y)
        {
            ClickableElement e = null;
            foreach(UIPanel p in this.Panels) {
                e = p.Click(x, y);
                if (e != null) break;
            }
            return e;
        }
        public void LoadBase()
        {
            //Load UI Panels

            //Character screen
            Texture2D t = GameContent.GetCel(@"data\char.cel");
            UIPanel charPanel = new UIPanel(0,0, t.Width, t.Height, t);

            //Main panel (hp, mana etc)
            //Composite image, extract full health/mana indicator 
            GenericCEL panel_full = new GenericCEL(LegacyContent.GetMPQFile(@"ctrlpan\panel8.cel"), GameContent.DefaultPalette);
            GenericCEL panel_empty = new GenericCEL(LegacyContent.GetMPQFile(@"ctrlpan\p8bulbs.cel"), GameContent.DefaultPalette);
            RawBitmap hp_empty = panel_empty.GetFrame(0);
            RawBitmap mana_empty = panel_empty.GetFrame(1);
            RawBitmap hpfull = panel_full.GetFrame(0).Copy(96, 0, 88, 88);
            RawBitmap manafull = panel_full.GetFrame(0).Copy(464, 0, 88, 88);
            RawBitmap panel = panel_full.GetFrame(0);
            
            //Replace panel's full graphics with the empty one
            panel.Paste(hp_empty, 96, 0);
            panel.Paste(mana_empty, 464, 0);

            this.hp_full = GfxConverter.GetTexture2D(GameContent.GraphicsDevice, hpfull);
            this.mana_full = GfxConverter.GetTexture2D(GameContent.GraphicsDevice, manafull);
            
            //Set up panel object
            t = GfxConverter.GetTexture2D(GameContent.GraphicsDevice, panel);
            int panel_x = (Settings.SCREEN_WIDTH / 2) - (t.Width / 2);
            int panel_y = Settings.SCREEN_HEIGHT - t.Height;
            UIPanel mainPanel = new UIPanel(panel_x, panel_y, t.Width, t.Height, t);
            
            
            //Set up UI elements
            ClickableElement char_button = new ClickableElement(ClickResult.CharButton, mainPanel);
            char_button.X = 9;
            char_button.Y = 25;
            char_button.Width = 71;
            char_button.Height = 19;
            char_button.Tileset = GameContent.GetCelTileset(@"ctrlpan\panel8bu.cel");
            char_button.PressedFrame = 0;
            char_button.Parent = mainPanel;
            mainPanel.ClickListeners.Add(char_button);

            //Level up notification
            UIPanel levelUpNotificationPanel = new UIPanel(mainPanel.X + 30, mainPanel.Y - 40, 41, 22, null);
            ClickableElement lvlUpNotificationButton = new ClickableElement(ClickResult.CharButton, levelUpNotificationPanel);
            lvlUpNotificationButton.X = 0;
            lvlUpNotificationButton.Y = 0;
            lvlUpNotificationButton.Width = 41;
            lvlUpNotificationButton.Height = 22;
            GenericCEL lvlUpButtonsGfx = new GenericCEL(LegacyContent.GetMPQFile(@"data\charbut.cel"), GameContent.DefaultPalette);
            lvlUpNotificationButton.Tileset = new Tileset(1, 2, GfxConverter.CreateTileset(GameContent.GraphicsDevice, new RawBitmap[] { lvlUpButtonsGfx.GetFrame(1), lvlUpButtonsGfx.GetFrame(2) }));
            lvlUpNotificationButton.PressedFrame = 1;
            lvlUpNotificationButton.Frame = 0;
            levelUpNotificationPanel.Visible = true;
            levelUpNotificationPanel.ClickListeners.Add(lvlUpNotificationButton);
            
            //Level up buttons
            ClickableElement[] lvlUpButtons = new ClickableElement[4];
            lvlUpButtons[0] = new ClickableElement(ClickResult.LevelUpStr, charPanel);
            lvlUpButtons[1] = new ClickableElement(ClickResult.LevelUpMag, charPanel);
            lvlUpButtons[2] = new ClickableElement(ClickResult.LevelUpDex, charPanel);
            lvlUpButtons[3] = new ClickableElement(ClickResult.LevelUpVit, charPanel);
            int offset = 0;
            for (int i = 0; i < 4; i++)
            {
                lvlUpButtons[i].X = 137;
                lvlUpButtons[i].Y = 139 + offset;
                offset += 28;
                lvlUpButtons[i].Width = 41;
                lvlUpButtons[i].Height = 22;
                lvlUpButtons[i].Tileset = lvlUpNotificationButton.Tileset;
                lvlUpButtons[i].Frame = 0;
                lvlUpButtons[i].PressedFrame = 1;
                lvlUpButtons[i].Visible = false;
                charPanel.ClickListeners.Add(lvlUpButtons[i]);
            }


            //Add in right order! see UIPanelType enum
            this.Panels.Add(charPanel);
            this.Panels.Add(mainPanel);
            this.Panels.Add(levelUpNotificationPanel);

        }
        public void Draw(SpriteBatch spriteBatch, Player p)
        {
            if (this.ElementVisible(UIPanelType.CharacterPane))
            {
                UIPanel panel = this.Panels[(int)UIPanelType.CharacterPane];
                int panel_x = panel.X;
                int panel_y = panel.Y;
                GameFonts font = GameContent.Font;
                Character c = p.Character;
                //Set levelup buttons
                bool lvlupVisible = c.LevelUpPoints > 0;
                panel.ClickListeners[0].Visible = lvlupVisible;
                panel.ClickListeners[1].Visible = lvlupVisible;
                panel.ClickListeners[2].Visible = lvlupVisible;
                panel.ClickListeners[3].Visible = lvlupVisible;

                //Background
                panel.Draw(spriteBatch);

                //Name
                font.Draw(c.Name.ToUpper(), panel_x + 22, panel_y + 21, DFontType.Small, spriteBatch);

                //Attributes
                font.Draw(c.AttStr.ToString(), panel_x + 102, panel_y + 145, DFontType.Small, spriteBatch);
                font.Draw(c.AttMag.ToString(), panel_x + 102, panel_y + 173, DFontType.Small, spriteBatch);
                font.Draw(c.AttDex.ToString(), panel_x + 102, panel_y + 201, DFontType.Small, spriteBatch);
                font.Draw(c.AttVit.ToString(), panel_x + 102, panel_y + 229, DFontType.Small, spriteBatch);

                //points to spend
                font.Draw(c.LevelUpPoints.ToString(), panel_x + 102, panel_y + 257, DFontType.Small, spriteBatch);

                //Level
                font.Draw(c.Level.ToString(), panel_x + 82, panel_y + 60, DFontType.Small, spriteBatch);

                //Exp
                font.Draw(c.Experience.ToString(), panel_x + 218, panel_y + 60, DFontType.Small, spriteBatch);
                if (c.Level < 50)
                {
                    font.Draw(Player.LevelExperience[c.Level].ToString(), panel_x + 218, panel_y + 88, DFontType.Small, spriteBatch);
                }

                //Life,mana
                font.Draw(p.MaxHP.ToString(), panel_x + 102, panel_y + 294, DFontType.Small, spriteBatch);
                font.Draw(p.Mana.ToString(), panel_x + 102, panel_y + 322, DFontType.Small, spriteBatch);
                font.Draw(p.CurrentHP.ToString(), panel_x + 150, panel_y + 294, DFontType.Small, spriteBatch);

                //Armor class
                font.Draw(p.ArmorClass.ToString(), panel_x + 267, panel_y + 173, DFontType.Small, spriteBatch);

                //To hit
                font.Draw(p.ToHit + "%", panel_x + 267, panel_y + 201, DFontType.Small, spriteBatch);

                //Damage
                font.Draw(p.DamageMin + "-" + p.DamageMax, panel_x + 267, panel_y + 229, DFontType.Small, spriteBatch);
            }
            if (this.ElementVisible(UIPanelType.ControlPanel))
            {
                UIPanel panel = this.Panels[(int)UIPanelType.ControlPanel];
                panel.Draw(spriteBatch);

                //Overlay part of full hp gfx
                
                float hp = ((float)p.CurrentHP / (float)p.MaxHP);
                int hp_height = (int)Math.Floor(88 * hp);
                spriteBatch.Draw(this.hp_full, new Rectangle(panel.X + 96, panel.Y + 88 - hp_height, 88, hp_height),
                    new Rectangle(0, 88 - hp_height, 88, hp_height),
                    Color.White);
                 
            }
            if (p != null && p.Character.LevelUpPoints > 0)
            {
                UIPanel panel = this.Panels[(int)UIPanelType.LevelUpNotifier];
                GameContent.Font.Draw("Level up", panel.X - 15, panel.Y - 15, DFontType.Small, spriteBatch);
                panel.Draw(spriteBatch);
            }
        }

    }
}
