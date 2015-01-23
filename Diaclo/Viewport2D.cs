using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DiacloLib;

namespace Diaclo
{ 
    public class Viewport2D
    {
        public Point Position { get; set; }
        public Point PositionDeviation { get; set; } //in pixels
        public Vector2 Offset { get; set; }
        private Point TopLeftScreenPos;
        private Point TopLeftMapTile;
        private Texture2D MouseMap;
        private Point[,] TilePosition;

        public Viewport2D(Texture2D mousemap)
        {
            this.MouseMap = mousemap;
        }
        public void Draw(SpriteBatch spriteBatch, GameState gameState, Area area, SpriteFont debug_font)
        {
            DrawArea(spriteBatch, gameState,area, debug_font);
            //DrawPlayers(spriteBatch, gameState);

            /*
            //Todo: optimization, draw those in viewarea only
            foreach (BaseNPC npc in area.npcs)
            {
                npc.Draw(spriteBatch, MapCoordToDrawCoord(npc.Position), test);
            }
             */ 
        }
        public void DrawSquareObjects(SpriteBatch spriteBatch, Square s, Point screenPos)
        {
            /*
            for (int i = 0; i < gameState.Players.Length; i++)
            {
                Player p = gameState.Players[i];
                if (p != null && p.Position == pos)
                {
                    gameState.Players[i].DrawOnTile(spriteBatch, screenPos);
                }
            }
             */
            if (s.Corpse != null)
            {
                ((ClientNPC)s.Corpse).Draw(spriteBatch, screenPos.X, screenPos.Y);
            }
            if (s.getNPC() != null)
            {
                ((ClientNPC)s.getNPC()).Draw(spriteBatch, screenPos.X, screenPos.Y);
            }
            if (s.getPlayer() != null)
            {
                ((ClientPlayer)s.getPlayer()).Draw(spriteBatch, screenPos.X, screenPos.Y);
            }

        }
        /*
        public void DrawPlayers(SpriteBatch spriteBatch, GameState gameState)
        {
            for (int i = 0; i < gameState.Players.Length; i++)
            {
                if (gameState.Players[i] != null)
                {
                    gameState.Players[i].Draw(spriteBatch, MapCoordToDrawCoord(gameState.Players[i].Position));
                }
            }
        }
         */ 
        public Point MapCoordToDrawCoord(Point position)
        {
            //Subtract position offset
            position.X -= TopLeftMapTile.X;
            position.Y -= TopLeftMapTile.Y;

            //Determine row, col
            return this.TilePosition[position.X+100, position.Y+100];

        }
        public void DrawArea(SpriteBatch spriteBatch, GameState gameState, Area area, SpriteFont debug_font)
        {
            int squareHeight = 32;
            int squareWidth = 64;
            int center_x = Settings.SCREEN_WIDTH / 2 - squareWidth/2; // 32 being half a square
            int center_y = Settings.SCREEN_HEIGHT / 2 - squareHeight/2;

            //spriteBatch.DrawString(debug_font, "CENTER", new Vector2(center_x, center_y), Color.Red);

            //Determine the amount of tiles we need to draw on screen
            int sprite_columns = (int)Math.Ceiling((double)Settings.SCREEN_WIDTH / (double)squareWidth);
            int sprite_rows = 4 + (int)Math.Ceiling((double)Settings.SCREEN_HEIGHT / (double)(squareHeight/2));  //4 extra for offsetting +-1 tile

            //init tile position vector if we need to (window resize)
            if (this.TilePosition == null || this.TilePosition.GetUpperBound(0) != sprite_columns-1 || this.TilePosition.GetUpperBound(1) != sprite_rows)
            {
                this.TilePosition = new Point[sprite_columns+201,sprite_rows+200];
            }
                

            //we need an odd number to center a tile
            if (sprite_columns % 2 != 1) sprite_columns++; 
            if (sprite_rows % 2 != 1) sprite_rows++; 

            //Sprites to the left and on top of center
            int sprites_side = (sprite_columns - 1) / 2;
            int sprites_top = (sprite_rows - 1) / 4;

            //Now we can determine the size of the area to draw and where to start.
            int draw_width = sprite_columns * squareWidth + squareWidth;
            int draw_height = sprite_rows * squareHeight/2 + squareHeight;
            int start_x = center_x - sprites_side * squareWidth - this.PositionDeviation.X; //-((draw_width - Settings.SCREEN_WIDTH) / 2);
            int start_y = center_y - sprites_top * squareHeight - this.PositionDeviation.Y;//-((draw_height - Settings.SCREEN_HEIGHT) / 2);

            //First viewable squares (top left)
            int start_map_x = this.Position.X - sprites_top - sprites_side;
            int start_map_y = this.Position.Y - sprites_top + sprites_side;

            //Save the top left tile so we can mousepick a map coordinate later
            this.TopLeftScreenPos = new Point(start_x, start_y);
            this.TopLeftMapTile = new Point(start_map_x, start_map_y);

            //Now add 10 extra rows in order for tall objects to be drawn correctly at the bottom of the screen.
            sprite_rows += 10;

            int draw_x = start_x;
            int draw_y = start_y;
            int map_x = start_map_x;
            int map_y = start_map_y;
            int screen_map_x = 0;
            int screen_map_y = 0;
            bool offset = false;
            int vertical_offset = 0;
            
            Square draw;
            for (int row = 0; row < sprite_rows; row++)
            {
                //On even rows; add vertical map offset
                map_x = start_map_x + vertical_offset; //(int)Math.Floor((double)row / 2);
                map_y = start_map_y + vertical_offset; //(int)Math.Floor((double)row / 2); 
                screen_map_x = vertical_offset;
                screen_map_y = vertical_offset;

                if (offset)
                {
                    //On odd rows, move left 32px
                    draw_x = start_x - 32;
                    //On odd rows, move 1 tile down
                    map_y++;
                    screen_map_y++;
                }
                else draw_x = start_x;
                for (int col = 0; col < sprite_columns || (col <= sprite_columns && offset); col++)
                {
                    draw = area.GetSquare(new Point(map_x, map_y));
                    if (draw != null)
                    {
                        draw.DrawLow(spriteBatch, draw_x, draw_y);
                        this.DrawSquareObjects(spriteBatch, draw, new Point(draw_x, draw_y));
                        draw.DrawHigh(spriteBatch, draw_x, draw_y);
                    }
                    //spriteBatch.DrawString(debug_font, map_x + ", " + map_y, new Vector2(draw_x, draw_y), Color.White);
                    
                    //Keep track of where we put this tile
                    this.TilePosition[screen_map_x + 100,screen_map_y + 100] = new Point(draw_x, draw_y);

                    //Next tile is 64px right
                    draw_x += 64;
                    //Next tile is +1x -1y map coordinates
                    map_x++;
                    map_y--;
                    screen_map_x++;
                    screen_map_y--;

                }
                //Add 16px downwards to drawpoint
                draw_y += 16;

                //if we're finishing an offset row - move down in map coords
                if (offset) vertical_offset++;

                //Every other row is offset.
                offset = !offset;
            }

        }
        public Point Pick(int ScreenX, int ScreenY)
        {
            //Adjusted mouse coordinates
            int mouseX = ScreenX - (int)this.TopLeftScreenPos.X;
            int mouseY = ScreenY - (int)this.TopLeftScreenPos.Y;

            if (mouseX < 0 || mouseY < 0) return new Point(0,0);

            //Find which bounding rectangle we are in
            int rectX = (int) Math.Floor((float)mouseX / 64);
            int rectY = (int) Math.Floor((float)mouseY / 32);

            //Mousemap coordinates
            int mouseMapX = mouseX % 64;
            int mouseMapY = mouseY % 32;

            //Get subzone from hit-test image
            Rectangle sourceRectangle = new Rectangle(mouseMapX, mouseMapY, 1, 1);
            Color[] retrievedColor = new Color[1];
            MouseMap.GetData<Color>(0, sourceRectangle, retrievedColor, 0, 1);
            int dX = 0, dY = 0;
            if (retrievedColor[0] == Color.Red)
            {
                dX = -1;
            }
            else if (retrievedColor[0] == Color.Lime)
            {
                dY = -1;
            }
            else if (retrievedColor[0] == Color.Yellow)
            {
                dX = 1;
            }
            else if (retrievedColor[0] == Color.Blue)
            {
                dY = 1;
            }

            //The map offset coordinates from the base rectangle..
            int tileX = rectY + rectX;
            int tileY = rectY - rectX;

            //Add modifiers from hittest
            tileX += dX;
            tileY += dY;

            //We now have the tile from screen order, but we need to apply our current area position
            tileX += (int)TopLeftMapTile.X;
            tileY += (int)TopLeftMapTile.Y;

            return new Point(tileX, tileY);
        }


    }
}
