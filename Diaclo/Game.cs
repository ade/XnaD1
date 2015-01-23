using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using DiacloLib;
using DiacloServer;
using System.Threading;
using Lidgren.Network;
using Lidgren.Network.Xna;
using DiacloServer.AITemplates;
using System.Diagnostics;
using MpqReader;
using DiacloLib.Importer;

namespace Diaclo
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    ///
    public enum ClientStates
    {
        MainMenu,
        StartingLocalGame,
        Playing,
        PlayerDead,
        Loading
    }
    public class Game : Microsoft.Xna.Framework.Game
    {

        GraphicsDeviceManager graphics;
        public static SoundManager SoundManager;
        SpriteBatch spriteBatch;
        public ClientStates State;
        private KeyboardState savedKeyboardState;
        private MouseState savedMouseState;
        private bool showConsole = false;
        private bool showPerformance = false;
        GameState gameState;
        NetClient netClient;
        NetBuffer netBuffer;
        ClientPlayer MyPlayer;
        Viewport2D View;
        GameUI UI;
        Stack<ForegroundActivity> ActivityStack;
        Queue<Point> WalkPath;

        //Queued action
        PlayerAction queuedAction;
        Point queuedPoint;
  
        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            this.State = ClientStates.MainMenu;
            this.graphics.PreferredBackBufferWidth = Settings.SCREEN_WIDTH;
            this.graphics.PreferredBackBufferHeight = Settings.SCREEN_HEIGHT;
            this.IsMouseVisible = true;
            this.UI = new GameUI(this);
            Mouse.WindowHandle = this.Window.Handle;
            this.ActivityStack = new Stack<ForegroundActivity>();
           

            //Init sound
            SoundManager = new SoundManager();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            //Init keyboard listener
            EventInput.Initialize(this.Window);
            EventInput.CharEntered += this.OnCharEntered;
            EventInput.KeyDown += this.OnKeyDown;
            EventInput.KeyUp += this.OnKeyUp;

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            
            GameContent.GraphicsDevice = this.GraphicsDevice;
            GameContent.LoadBase(this.Content);
            UI.LoadBase();
            this.View = new Viewport2D(GameContent.SystemMouseMap);

            SetupState(ClientStates.MainMenu);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            Stopwatch sw = Stopwatch.StartNew();
            // TODO: Add your update logic here
            KeyboardState newKeyboardState = Keyboard.GetState();
            float secondsPassed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (this.State == ClientStates.Playing || this.State == ClientStates.PlayerDead)
            {
                this.gameState.Update(secondsPassed);

                if (MyPlayer.Action == PlayerAction.Idle && WalkPath != null && WalkPath.Count > 0)
                {
                    //Get walk target
                    Point pos = WalkPath.Dequeue();

                    //Set player in motion already (predictive)
                    if (MyPlayer.Walk(this.gameState, pos))
                    {
                        //Ask server if we can move to pos
                        NetProtocol.C2S_Walk(netClient, pos);
                    }
                }
            }

            //Update animation on active element
            if (ActivityStack.Count > 0)
            {
                ForegroundActivity activeActivity = ActivityStack.Peek();
                activeActivity.Update(secondsPassed);
            }

            GameConsole.ReportPerformance(PerformanceCategory.ClientGameUpdate, sw.ElapsedTicks);
            SoundManager.Update();

            this.UpdateInput(secondsPassed, newKeyboardState);
            this.UpdateNetwork(secondsPassed);
            this.UpdateActionQueue();
            base.Update(gameTime);

        }

        private void UpdateActionQueue()
        {
            if (this.State == ClientStates.Playing && MyPlayer.Action == PlayerAction.Idle && queuedAction != PlayerAction.Idle)
            {
                PlayerRequestAction(queuedAction, queuedPoint);
                queuedAction = PlayerAction.Idle;
            }
        }

        private void UpdateNetwork(float secondsPassed)
        {
            Stopwatch sw = new Stopwatch();
            if (netClient != null)
            {
                NetMessageType type;
                while (netClient.ReadMessage(netBuffer, out type))
                {
                    switch (type)
                    {
                        case NetMessageType.DebugMessage:
                            GameConsole.Write(netBuffer.ReadString(), ConsoleMessageTypes.Debug);
                            break;

                        case NetMessageType.StatusChanged:
                            GameConsole.Write("New status: " + netClient.Status + " (Reason: " + netBuffer.ReadString() + ")", ConsoleMessageTypes.Debug);
                            switch (netClient.Status)
                            {
                                case NetConnectionStatus.Connected:
                                    GameConsole.Write("C2S Sending handshake data", ConsoleMessageTypes.Debug);
                                    NetProtocol.C2S_Handshake(netClient);
                                    break;
                            }
                            break;

                        case NetMessageType.Data:
                            // Handle data in buffer here
                            ProcessNetMsg(netBuffer, netClient);
                            
                            break;
                    }
                }
            }
            GameConsole.ReportPerformance(PerformanceCategory.ClientNetworkUpdate, sw.ElapsedTicks);
        }

        private void ProcessNetMsg(NetBuffer netBuffer, NetClient netClient)
        {
            ProtocolServerToClient command = (ProtocolServerToClient) netBuffer.ReadUInt16();
            switch (command)
            {
                case ProtocolServerToClient.HandshakeOK:
                    ushort server_version = netBuffer.ReadUInt16();
                    byte my_id = netBuffer.ReadByte();
                    
                    //We got our id, update player object & gamestate
                    MyPlayer.ID = my_id;
                    gameState.Players[my_id] = MyPlayer;

                    if (server_version < Settings.CLIENT_LOWEST_SERVER_VERSION)
                    {
                        //Do something (todo)
                    }

                    //Send our character info
                    GameConsole.Write("C2S: Handshake successfull, sending player info", ConsoleMessageTypes.Debug);
                    NetProtocol.C2S_PlayerInfo(netClient, MyPlayer);
                    break;
                case ProtocolServerToClient.MapInfo:
                    GameConsole.Write("Building world...", ConsoleMessageTypes.Debug);
                    this.gameState.World = ClientSerializer.ReadWorld(netBuffer);
                    MyPlayer.World = this.gameState.World;
                    GameConsole.Write("Received world (" + this.gameState.World.Areas.Length + " areas)", ConsoleMessageTypes.Debug);
                    break;
                case ProtocolServerToClient.AreaNPCList:
                    ClientSerializer.ReadAreaNPCList(netBuffer, gameState.World);
                    break;
                case ProtocolServerToClient.SpawnPlayer:
                    {
                        ushort area_id = netBuffer.ReadUInt16();
                        Point pos = Serializer.ReadPoint(netBuffer);
                        
                        MyPlayer.SetLocation(area_id, pos);
                        
                        GameConsole.Write("Loading...", ConsoleMessageTypes.Info);
                        //Push loading graphics to screen
                        this.State = ClientStates.Loading;
                        this.Tick();

                        GameContent.LoadArea(gameState.World.Areas[MyPlayer.AreaID], MyPlayer);
                        View.Position = new Point(pos.X, pos.Y);
                        GameConsole.Write("Spawning @ area " + area_id + ", " + pos.ToString(), ConsoleMessageTypes.Debug);
                        GameConsole.Write("Entering " + this.MyPlayer.Area.Name);

                        SetupState(ClientStates.Playing);
                    }
                    break;
                case ProtocolServerToClient.Walk:
                    {
                        //x,y,yes/no
                        Point pos = netBuffer.ReadPoint();
                        Point correction = netBuffer.ReadPoint();
                        
                        bool b = netBuffer.ReadBoolean();
                        if (b)
                        {
                            //Move OK: proceed (no correction)
                        }
                        else
                        {
                            //warp to correction (todo)
                            MyPlayer.RevertMove();
                            MyPlayer.Position = correction;
                            WalkPath = null;
                        }
                    }
                    break;
                case ProtocolServerToClient.NPCUpdate:
                    ClientSerializer.UpdateNPC(netBuffer, MyPlayer.Area);
                    break;
                case ProtocolServerToClient.BattleResult:
                    BattleResult result = ClientSerializer.ReadBattleResult(netBuffer, gameState);
                    result.Apply();
                    CombatLog(result);
                    break;
                case ProtocolServerToClient.PlayerStatusUpdate:
                    {
                        ClientSerializer.UpdatePlayerStatus(netBuffer, gameState);
                    }
                    break;
                case ProtocolServerToClient.PlayerStatsUpdate:
                    ClientSerializer.ReadCharacterUpdate(netBuffer, MyPlayer);
                    break;
            }
        }
        public void OnMainMenuSelect(int index, object tag)
        {
            
            switch(index)
            {   
                case 0:
                    //Quickstart
                    Character c = new Character(CharacterClass.Warrior);
                    c.Name = "Player1";
                    StartSingleplayerGame(c);
                    ActivityStack.Clear();
                    break;
                case 1:
                    ShowSinglePlayerMenu();
                    break;
                case 2://exit
                    this.TerminateGame();
                    break;
            }
            
        }
        public void OnCharEntered(CharacterEventArgs e) {
            //Fired when a keyboard character is entered (window message)
            if(this.ActivityStack.Count > 0)
                this.ActivityStack.Peek().CharEntered(e);
        }
        public void OnKeyDown(KeyEventArgs e)
        {
            //Key down (window message)
            if (this.ActivityStack.Count > 0)
                this.ActivityStack.Peek().KeyDown(e);
        }
        public void OnKeyUp(KeyEventArgs e)
        {
            //Key up (window message)
            if (this.ActivityStack.Count > 0)
                this.ActivityStack.Peek().KeyUp(e);
        }
        private void ShowSinglePlayerMenu()
        {
            //Get available chars
            string[] chars = CharacterLibrary.GetCharacters();

            GameMenu charselect = new GameMenu(100, 100, DFontType.MediumGold, this.OnSingleplayerMenuSelect);
            for(int i = 0; i < chars.Length; i++) {
                charselect.AddItem(chars[i], chars[i]);
            }
            charselect.AddItem("New Character");
            ActivityStack.Push(charselect);
        }
        private void OnNewCharacterNameEntered(string result)
        {
            if (result != null)
            {
                Character c = new Character(CharacterClass.Warrior);
                c.Name = result;
                StartSingleplayerGame(c);
                ActivityStack.Clear();
            }
            else
            {
                ActivityStack.Pop();
            }
        }
        private void OnSingleplayerMenuSelect(int index, object tag)
        {
            if (index == -1)
            {
                //Go back to main menu
                ActivityStack.Pop();
            } 
            else if (tag == null)
            {
                //New char
                TextInputDialog charname = new TextInputDialog(100, 100, DFontType.MediumGold, Settings.ALLOWED_CHARACTER_NAMES, 10, "Enter Name:", this.OnNewCharacterNameEntered);
                ActivityStack.Push(charname);
            }
            else
            {
                string charname = (string)tag;
                StartSingleplayerGame(CharacterLibrary.LoadCharacter(charname));
                ActivityStack.Clear();
            }

        }
        private void SetupState(ClientStates clientStates)
        {
            switch(clientStates) {
                case ClientStates.MainMenu:
                    //Create main menu
                    GameMenu mainmenu = new GameMenu(100, 100, DFontType.MediumGold, this.OnMainMenuSelect);
                    mainmenu.AddItem("Quick start");
                    mainmenu.AddItem("Singleplayer");
                    mainmenu.AddItem("Exit");
                    ActivityStack.Push(mainmenu);
                    this.State = ClientStates.MainMenu;
                    break;
                case ClientStates.Playing:
                    if (!this.UI.ElementVisible(UIPanelType.ControlPanel)) this.UI.ToggleDisplay(UIPanelType.ControlPanel);
                    this.showConsole = false;
                    this.State = ClientStates.Playing;
                    break;
                case ClientStates.PlayerDead:
                    if (this.UI.ElementVisible(UIPanelType.ControlPanel)) this.UI.ToggleDisplay(UIPanelType.ControlPanel);
                    if (this.UI.ElementVisible(UIPanelType.CharacterPane)) this.UI.ToggleDisplay(UIPanelType.CharacterPane);
                    this.State = ClientStates.PlayerDead;
                    break;
            }
        }

        private void CombatLog(BattleResult result)
        {

            string outcome = "";
            if (result.Outcome == BattleOutcome.Hit)
                outcome = "hits";
            else if (result.Outcome == BattleOutcome.Miss)
                outcome = "misses";
            else if (result.Outcome == BattleOutcome.Immune)
                outcome = "fails attack (immune) on ";

            switch (result.Type)
            {
                case BattleTypes.MonsterVsPlayer:
                    BaseNPC attack = (BaseNPC)result.Attacker;
                    Player p = (Player)result.Target;
                    GameConsole.Write("NPC[" + attack.ID + "] "+ outcome + " " + p.Character.Name + " for " + result.TargetTakeDamage + " damage.");
                    break;
                case BattleTypes.PlayerVsMonster:
                    Player attacker = (Player)result.Attacker;
                    BaseNPC target = (BaseNPC)result.Target;
                    GameConsole.Write(attacker.Character.Name + " " + outcome + " NPC[" + target.ID + "] for " + result.TargetTakeDamage + " damage.");
                    break;
            }
            if (result.TargetNewHP <= 0)
            {
                if (result.Target == MyPlayer)
                {
                    //Player dies
                    GameConsole.Write("You die.");
                    SetupState(ClientStates.PlayerDead);
                }
            }
        }

        protected void UpdateInput(float secondsPassed, KeyboardState ks)
        {
            //Mouse
            MouseState mousestate = Mouse.GetState();
            if (mousestate.LeftButton != savedMouseState.LeftButton || mousestate.RightButton != savedMouseState.RightButton)
            {
                if (UI.CatchClick(mousestate))
                {
                    //UI click (do nothing)
                }
                else
                {
                    if (MouseLeft(savedMouseState))
                    {
                        //Game field click
                        if (this.State == ClientStates.Playing)
                        {
                            Point pos = View.Pick(Mouse.GetState().X, Mouse.GetState().Y);
                            if (ks.IsKeyDown(Keys.LeftControl))
                                GameConsole.Write("[Click] coords " + pos.ToString());

                            //Determine action
                            if (ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift))
                            {
                                PlayerRequestAction(PlayerAction.Melee, pos);
                            }
                            else
                            {
                                PlayerRequestAction(PlayerAction.Walk, pos);
                            }
                        }
                    }
                }
            }

            //Keyboard

            //Console
            if (InputEvent(savedKeyboardState, ks, Keys.F1))
                this.showConsole = !this.showConsole;
            if (InputEvent(savedKeyboardState, ks, Keys.F2))
                this.showPerformance = !this.showPerformance;

            if (ActivityStack.Count == 0)
            {
                if (this.State == ClientStates.Playing)
                {
                    //Test
                    if (InputEvent(savedKeyboardState, ks, Keys.Up))
                        View.Position = new Point(View.Position.X, View.Position.Y - 1);
                    if (InputEvent(savedKeyboardState, ks, Keys.Down))
                        View.Position = new Point(View.Position.X, View.Position.Y + 1);
                    if (InputEvent(savedKeyboardState, ks, Keys.Right))
                        View.Position = new Point(View.Position.X + 1, View.Position.Y);
                    if (InputEvent(savedKeyboardState, ks, Keys.Left))
                        View.Position = new Point(View.Position.X - 1, View.Position.Y);

                    //UI
                    if (InputEvent(savedKeyboardState, ks, Keys.C))
                        UI.ToggleDisplay(UIPanelType.CharacterPane);

                    //Menu
                    if (InputEvent(savedKeyboardState, ks, Keys.Escape))
                    {
                        ShowSystemMenu();
                    }
                }
                else if (this.State == ClientStates.PlayerDead)
                {
                    if (InputEvent(savedKeyboardState, ks, Keys.Enter))
                        RequestRespawn();
                }

            }

            //Fullscreen
            if (InputEvent(savedKeyboardState, ks, Keys.Enter) && (ks.IsKeyDown(Keys.LeftAlt) || ks.IsKeyDown(Keys.RightAlt)))
                this.graphics.ToggleFullScreen();

            //Debug
            if (InputEvent(savedKeyboardState, ks, Keys.T))
            {

            }

            this.savedKeyboardState = ks;
            this.savedMouseState = Mouse.GetState();

        }

        private void RequestRespawn()
        {
            if (this.State == ClientStates.PlayerDead)
            {
                NetBuffer b = NetProtocol.C2S_RequestRespawn(netClient);
                netClient.SendMessage(b, NetChannel.ReliableInOrder1);
            }
        }

        private void ShowSystemMenu()
        {
            GameMenu system = new GameMenu(100, 100, DFontType.BigGold, this.OnSystemMenuSelect);
            system.AddItem("Return to game");
            system.AddItem("Save and exit");
            ActivityStack.Push(system);
        }
        public void OnSystemMenuSelect(int index, object tag)
        {
            switch (index)
            {
                case -1:
                case 0:
                    ActivityStack.Pop();
                    break;
                case 1:
                    CharacterLibrary.SaveCharacter(MyPlayer.Character);
                    this.TerminateGame();
                    break;
            }
            
        }
        private void TextureLoadSpeedTest()
        {
            Stopwatch sw = Stopwatch.StartNew();
            byte[] b = LegacyContent.GetMPQFile(@"PlrGFX\Warrior\wlb\wlbat.CL2");

            CL2Container animation_raw = new CL2Container(b, new Palette(Palette.DefaultPalette));
            //RawBitmap bmp = animation_raw.GetDecodedFrame(55, 96);

            //test_texture = GfxConverter.GetTexture2D(this.GraphicsDevice, bmp, NullTranslation);

            RawBitmap[] all = animation_raw.GetAllFrames(96);
            Texture2D tileset = GfxConverter.CreateTileset(this.GraphicsDevice, all, 16);

            sw.Stop();
            GameConsole.Write("That took " + ((double)sw.ElapsedTicks / (double)Stopwatch.Frequency) + " seconds");
        }
        private void PlayerMove(Point pos)
        {
            Point start = MyPlayer.Position;
            
            if (MyPlayer.Action == PlayerAction.Walk)
            {
                start = MyPlayer.TileMoveTo;
            }
            if(pos != start)
                this.WalkPath = PathFinding.Navigate(start, pos, MyPlayer.Area);
            
        }
        private bool MouseLeft(MouseState old)
        {
            MouseState m = Mouse.GetState();
            if (m.X < 0 || m.Y < 0 || m.X > Settings.SCREEN_WIDTH || m.Y > Settings.SCREEN_HEIGHT)
                return false;

            return (old.LeftButton == ButtonState.Released && m.LeftButton == ButtonState.Pressed);
        }
        private Boolean InputEvent(KeyboardState old, KeyboardState ks, Keys key)
        {
            return (old.IsKeyUp(key) && ks.IsKeyDown(key));
        }

        private void TerminateGame()
        {
            
            this.Exit();
            
        }

        private void StartSingleplayerGame(Character c)
        {
            this.State = ClientStates.StartingLocalGame;
            Thread srv = new Thread(new ParameterizedThreadStart(Server.RunServer));
            srv.Start(new ServerArguments(1, Settings.SERVER_PORT));

            this.gameState = new GameState();
            this.gameState.Players = new Player[1];

            MyPlayer = new ClientPlayer(c);
            
            NetConfiguration config = new NetConfiguration("DiacloGame"); // needs to be same on client and server!
            netClient = new NetClient(config);
            netBuffer = netClient.CreateBuffer();

            GameConsole.Write("Connecting....");
            netClient.Connect("127.0.0.1", Settings.SERVER_PORT);
            this.showConsole = true;

            
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            Stopwatch sw = Stopwatch.StartNew();
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();

            //if(this.test != null)  spriteBatch.Draw(this.test, new Vector2(0, 0), Color.White);
            /*
            GameContent.Font.Draw("abcdefghij", 10, 400, DFontType.BigGold, spriteBatch);
            GameContent.Font.Draw("klmnopqrst", 10, 450, DFontType.BigGold, spriteBatch);
            GameContent.Font.Draw("uvwxyz1234", 10, 500, DFontType.BigGold, spriteBatch);
            GameContent.Font.Draw("567890-=+(", 10, 550, DFontType.BigGold, spriteBatch);
            GameContent.Font.Draw(")[]\"\"'':;,", 10, 600, DFontType.BigGold, spriteBatch);
            GameContent.Font.Draw("./?!&%#$*X", 10, 650, DFontType.BigGold, spriteBatch);
            GameContent.Font.Draw("X@\\^_|~", 10, 700, DFontType.BigGold, spriteBatch);
            */
            
            switch (this.State)
            {
                case ClientStates.MainMenu:
                    
                    break;
                case ClientStates.Playing:
                    if (!this.savedKeyboardState.IsKeyDown(Keys.LeftControl))
                    {
                        View.Position = MyPlayer.Position;
                        View.PositionDeviation = MyPlayer.PositionDeviation;
                    }

                    DrawView();
                    break;
                case ClientStates.PlayerDead:
                    DrawView();
                    GameContent.Font.Draw("Press enter to respawn", Settings.SCREEN_WIDTH / 2 - 150, Settings.SCREEN_HEIGHT / 2 + 100, DFontType.MediumGold, spriteBatch);
                    break;
                case ClientStates.Loading:
                    GameContent.Font.Draw("LOADING",Settings.SCREEN_WIDTH/2 - 100, Settings.SCREEN_HEIGHT/2 - 50, DFontType.BigGold, spriteBatch);
                    break;
            }

            if (showConsole)
            {
                spriteBatch.Draw(GameContent.ConsoleBackground, new Vector2(0, 0), Color.White);

                if (!showPerformance)
                {
                    int max_msg = GameConsole.Messages.Count;
                    for (int i = 0; i < max_msg && i < 13; i++)
                    {
                        int msg = GameConsole.Messages.Count - i - 1;
                        spriteBatch.DrawString(GameContent.ConsoleFont, GameConsole.Messages[msg].Message, new Vector2(3, 185 - i * 13), GameConsole.Messages[msg].Color);
                    }
                }
                else
                {
                    int max_msg = GameConsole.PerformanceCounter.Length;
                    for (int i = 0; i < max_msg && i < 13; i++)
                    {
                        double time = GameConsole.PerformanceCounter[i];
                        string desc = ((PerformanceCategory)i).ToString();
                        spriteBatch.DrawString(GameContent.ConsoleFont, desc + " :" + time, new Vector2(3, 185 - i * 13), Color.Black);
                    }
                    spriteBatch.DrawString(GameContent.ConsoleFont, "Client draw ps: " + GameConsole.ClientDrawTimer, new Vector2(300, 172), Color.Black);
                    spriteBatch.DrawString(GameContent.ConsoleFont, "Server update ps: " + GameConsole.ServerUpdateTimer, new Vector2(300, 159), Color.Black);
                }

                
            }

            //Draw UI
            UI.Draw(spriteBatch, MyPlayer);
            
            /*
            float secondsPassed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float fps = 1 / secondsPassed;
            spriteBatch.DrawString(C.ConsoleFont, fps + " fps", new Vector2(10, 10), Color.Yellow);
             */ 


            //Draw menu items
            if (ActivityStack.Count > 0)
            {
                ForegroundActivity activeActivity = ActivityStack.Peek();
                activeActivity.Draw(spriteBatch);
            }

            base.Draw(gameTime);
            spriteBatch.End();
            sw.Stop();
            GameConsole.ReportPerformance(PerformanceCategory.ClientDraw, sw.ElapsedTicks);
            GameConsole.ClientDrawTimer = 1/((double)sw.ElapsedTicks / (double)Stopwatch.Frequency);
        }
        private void DrawView()
        {
            View.Draw(spriteBatch, this.gameState, MyPlayer.Area, GameContent.ConsoleFont);
        }
        private void PlayerRequestAction(PlayerAction a, Point target)
        {
            //If not idle, or not walking, queue action
            if (MyPlayer.Action == PlayerAction.Walk && a == PlayerAction.Walk)
            {
                //Override walk (make new path)
                PlayerMove(target);
            } 
            else if (MyPlayer.Action != PlayerAction.Idle)
            {
                queuedAction = a;
                queuedPoint = target;
            }
            else //Idle; perform action
            {
                switch (a)
                {
                    case PlayerAction.Melee:
                        //Attack
                        PlayerMeleeAttack(target);
                        break;
                    case PlayerAction.Walk:
                        PlayerMove(target);
                        break;
                }
            }

        }

        private void PlayerMeleeAttack(Point target)
        {
            if (!GameMechanics.MapTilesAdjacent(MyPlayer.Position, target))
            {
                //Translate to direction
                Direction d = GameMechanics.PointToDirection(MyPlayer.Position.X, MyPlayer.Position.Y, target.X, target.Y);
                Point delta = GameMechanics.DirectionToDelta(d);
                target = new Point(MyPlayer.Position.X + delta.X, MyPlayer.Position.Y + delta.Y);
            }
            if (MyPlayer.MeleeAttack(target))
                NetProtocol.C2S_MeleeAttack(netClient, target);
        }



        internal void OnUIClick(ClickResult ret)
        {
            switch (ret)
            {
                case ClickResult.LevelUpStr:
                    RequestStat(AttributeType.Strength);
                    break;
                case ClickResult.LevelUpMag:
                    RequestStat(AttributeType.Magic);
                    break;
                case ClickResult.LevelUpDex:
                    RequestStat(AttributeType.Dexterity);
                    break;
                case ClickResult.LevelUpVit:
                    RequestStat(AttributeType.Vitality);
                    break;
            }
        }

        private void RequestStat(AttributeType stat)
        {
            NetProtocol.C2S_RequestStat(netClient, stat);
        }

        /// <summary>
        /// Load/play a sound
        /// </summary>
        /// <param name="name"></param>
        public static void CueSound(string name)
        {
            if (!SoundManager.SoundLoaded(name))
            {
                SoundManager.LoadSound(LegacyContent.GetMPQFile(name), name);
            }
            SoundManager.PlaySound(name);
        }
 
    }
}
