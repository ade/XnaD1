using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using DiacloLib;
using MpqReader;
using DiacloLib.Importer;

namespace Diaclo
{
    public enum DiacloUITextures
    {
        CharacterPane
    }

    public class GameContent
    {
        public static Dictionary<String, Tileset> PlayerTilesets;
        public static Dictionary<String, Tileset> MonsterTilesets;
        public static Palette DefaultPalette;
        public static GraphicsDevice GraphicsDevice;
        public static ContentManager content;
        public static Color MainFontWhite = new Color(205, 205, 205);
        public static Texture2D ConsoleBackground;
        public static SpriteFont ConsoleFont;
        public static Texture2D SystemMouseMap; //Hittest image for mouse
        public static LevelCache LevelData;
        public static GameFonts Font;
        public static Tileset PentSpinBig;
        public static Tileset PentSpinSmall;
 
        /// <summary>
        /// Load basic data
        /// </summary>
        public static void LoadBase(ContentManager content)
        {
            GameContent.content = content;
            GameContent.PlayerTilesets = new Dictionary<string, Tileset>();
            GameContent.MonsterTilesets = new Dictionary<string,Tileset>();
            GameContent.ConsoleBackground = GameContent.content.Load<Texture2D>("textures/ui/console_back");
            GameContent.ConsoleFont = GameContent.content.Load<SpriteFont>("fonts/Console");
            GameContent.SystemMouseMap = GameContent.content.Load<Texture2D>("system/hittest");

            //Init level data
            GameContent.LevelData = new LevelCache(GameContent.GraphicsDevice);

            //Set up gfx
            GameContent.DefaultPalette = new Palette(LegacyContent.GetMPQFile(@"levels\towndata\town.pal"));

            //Load fonts
            GenericCEL smaltext = new GenericCEL(LegacyContent.GetMPQFile(@"ctrlpan\smaltext.cel"), GameContent.DefaultPalette, 13);
            GenericCEL medtexts = new GenericCEL(LegacyContent.GetMPQFile(@"data\medtexts.cel"), GameContent.DefaultPalette, 22);
            GenericCEL bigtgold = new GenericCEL(LegacyContent.GetMPQFile(@"data\bigtgold.cel"), GameContent.DefaultPalette, 46);
            GameContent.Font = new GameFonts(new GenericCEL[] {smaltext,medtexts,bigtgold}, ConsoleFont);

            //Load menu item selection indicator (spinning pentagram)
            GameContent.PentSpinBig = GetCelTileset(@"data\pentspin.cel");
            GameContent.PentSpinSmall = GetCelTileset(@"data\pentspn2.cel");


        }
        /// <summary>
        /// Convert a CL2 animation to a drawable creature tileset
        /// </summary>
        /// <param name="filename">CL2 filename and path e.g. "plrgfx\warrior\wla\wlast.cl2"</param>
        /// <param name="d">The direction of the character</param>
        /// <returns></returns>
        public static Tileset[] BuildCreatureTileset(string filename, Direction d, bool highlights)
        {
            Tileset[] ret = new Tileset[2];

            int direction = (int)d;

            //Get raw file, contains current animation for all directions
            byte[] mpqfile = LegacyContent.GetMPQFile(filename);
            CL2Container cl2 = new CL2Container(mpqfile, GameContent.DefaultPalette);
            int frames = cl2.FrameCount();

            //Get frames per direction (8 directions)
            int frames_per_direction = frames / 8;
            int start = frames_per_direction * direction;

            //Get only frames for current direction
            RawBitmap[] cl2frames = cl2.GetFrames(start, frames_per_direction, 0);

            //Create a tileset
            int tileset_columns = (int)Math.Floor(Math.Sqrt(frames_per_direction));
            Texture2D texture = GfxConverter.CreateTileset(GameContent.GraphicsDevice, cl2frames, tileset_columns);

            //Calculate offset
            int offsetx = -(cl2frames[0].Width / 2) + 32;
            int offsety = -cl2frames[0].Height + 32;

            Tileset creature = new Tileset(texture, frames_per_direction, cl2frames[0].Width, cl2frames[0].Height);
            creature.OffsetX = offsetx;
            creature.OffsetY = offsety;

            //Create selection highlights
            RawBitmap[] highlightframes;
            Tileset highlight;
            if (highlights)
            {
                highlightframes = new RawBitmap[cl2frames.Length];
                for (int i = 0; i < cl2frames.Length; i++)
                {
                    highlightframes[i] = cl2frames[i].CreateHighlight(160);
                }

                highlight = new Tileset(GfxConverter.CreateTileset(GameContent.GraphicsDevice, highlightframes, tileset_columns), cl2frames[0].Width, cl2frames[0].Height);
                highlight.OffsetX = creature.OffsetX;
                highlight.OffsetY = creature.OffsetY;
                ret[1] = highlight;
            }
            ret[0] = creature;

            return ret;
        }
        public static Tileset BuildCreatureTileset(string filename, Direction d)
        {
            return BuildCreatureTileset(filename, d, false)[0];
        }

        
        /// <summary>
        /// Preload the specified area index to system memory and bind square objects to their tileset.
        /// </summary>
        /// <param name="area"></param>
        public static void LoadArea(Area area, ClientPlayer p)
        {
            //Empty cache
            //GameContent.PlayerTilesets = new Dictionary<string, Tileset>();
            //GameContent.MonsterTilesets = new Dictionary<string, Tileset>();

            //Load area graphics/tileset
            for (int i = 0; i < area.Squares.Length; i++)
            {
                area.Squares[i].Tileset = GameContent.LevelData.GetTileset(area.Squares[i].LevelID);
            }

            //Cache NPCs
            for (int i = 0; i < area.npcs.Count; i++ )
            {
                //Animation
                ((ClientNPC)area.npcs[i]).CacheTilesets();

                //Sounds
                LoadSoundList(((ClientNPC)area.npcs[i]).CacheSounds());
            }

            //Cache Player
            p.CacheAnimations();
            string[] playerSound = p.CacheSounds();
            LoadSoundList(playerSound);

            //Generic/common sound
            string[] miscSound = new string[2];
            miscSound[0] = Res.SND_WORLD_SWING;
            miscSound[1] = Res.SND_WORLD_SWING2;
            LoadSoundList(miscSound);
            
        }
        private static void LoadSoundList(string[] sounds) {
            for (int i = 0; i < sounds.Length; i++)
            {
                if (!Game.SoundManager.SoundLoaded(sounds[i]))
                {
                    Game.SoundManager.LoadSound(LegacyContent.GetMPQFile(sounds[i]), sounds[i]);
                }
            }
        }
        /// <summary>
        /// Get the first frame of a generic cel encoded sprite with the default palette
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Texture2D GetCel(string filename)
        {
            return GetCel(filename, 0, GameContent.DefaultPalette, 0);
        }
        /// <summary>
        /// Get the first frame of a generic cel encoded sprite using the specified palette
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Texture2D GetCel(string filename, Palette p)
        {
            return GetCel(filename, 0, p, 0);
        }
        /// <summary>
        /// Get a generic cel encoded sprite
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="frameindex"></param>
        /// <param name="palette"></param>
        /// <param name="known_width"></param>
        /// <returns></returns>
        public static Texture2D GetCel(string filename, int frameindex, Palette palette, int known_width)
        {
            GenericCEL CEL = new GenericCEL(LegacyContent.GetMPQFile(filename), palette, known_width);
            return GfxConverter.GetTexture2D(GameContent.GraphicsDevice, CEL.GetFrame(frameindex));
        }


        /// <summary>
        /// Create a tileset textures from the specified file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="palette"></param>
        /// <param name="known_width"></param>
        /// <returns></returns>
        public static Texture2D GetCelTilesetTexture(string filename, Palette palette, int known_width)
        {
            GenericCEL CEL = new GenericCEL(LegacyContent.GetMPQFile(filename), palette, known_width);
            return GfxConverter.CreateTileset(GameContent.GraphicsDevice, CEL.GetFrames(), 0);
        }
        /// <summary>
        /// Create a Tileset object from a CEL file with several frames with the same dimensions.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Tileset GetCelTileset(string filename)
        {
            return GetCelTileset(filename, GameContent.DefaultPalette, 0);
        }
        /// <summary>
        /// Create a Tileset object from a CEL file with several frames with the same dimensions.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="palette"></param>
        /// <param name="known_width"></param>
        /// <returns></returns>
        public static Tileset GetCelTileset(string filename, Palette palette, int known_width)
        {
            GenericCEL CEL = new GenericCEL(LegacyContent.GetMPQFile(filename), palette, known_width);
            RawBitmap[] frames = CEL.GetFrames();
            Texture2D texture = GfxConverter.CreateTileset(GameContent.GraphicsDevice, frames, 0);
            return new Tileset(texture, frames[0].Width, frames[0].Height);
        }
 
    }
}
