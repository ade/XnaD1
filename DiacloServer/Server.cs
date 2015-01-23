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
using Lidgren.Network;
using DiacloLib;
using System.Threading;
using System.Timers;
using DiacloServer.AITemplates;
using System.Diagnostics;
using DiacloLib.Importer;
using DiacloLib.Importer.MonsterImporter;

namespace DiacloServer
{
    public class ServerArguments
    {
        
        public int clients { get; set; }
        public int port { get; set; }
        public ServerArguments(int clients, int port)
        {
            this.clients = clients;
            this.port = port;
        }
    }
    public static class Server
    {
        public static bool Run { get; set; }
        public static int MaxPlayers { get; set; }
        private static NetServer netServer;
        private static GameState gameState;
        private static Stopwatch gameUpdateTimer;
        private static Stopwatch Uptime;
        private static ImportLibrary ImportedContent;

        public static void RunServer(object o)
        {
            ServerArguments a = (ServerArguments)o;
            GameConsole.Write("Starting server ("+a.port + ") max="+a.clients, ConsoleMessageTypes.Info);

            Server.MaxPlayers = a.clients;
            NetConfiguration config = new NetConfiguration("DiacloGame");
            config.MaxConnections = Server.MaxPlayers + 2;
            config.Port = a.port;
            
            netServer = new NetServer(config);
            netServer.SetMessageTypeEnabled(NetMessageType.ConnectionApproval, true);
            netServer.Start();
            Server.Run = true;

            gameState = new GameState();
            gameState.Players = new Player[Server.MaxPlayers];

            //Set up imported content library
            Server.ImportedContent = new ImportLibrary();

            //Load town, create levels, etc
            InitWorld();

            //Load monster data
            ImportedContent.LoadMonsterTemplates();

            //Fill levels with monsters
            MakeNPCs(gameState.World);
         
            // create a buffer to read data into
            NetBuffer buffer = netServer.CreateBuffer();
            NetMessageType type;
            NetConnection sender;
            double timeSinceUpdate;
            gameUpdateTimer = Stopwatch.StartNew();
            Uptime = Stopwatch.StartNew();

            while (Server.Run)
            {
                
                //Update gamestate
                Server.gameUpdateTimer.Stop();
                timeSinceUpdate = ((double)gameUpdateTimer.ElapsedTicks / (double)Stopwatch.Frequency);
                gameUpdateTimer = Stopwatch.StartNew();

                GameConsole.ServerUpdateTimer = 1.0 / timeSinceUpdate;
                Server.GameStateUpdate((float)timeSinceUpdate);
                
                // check if any messages has been received
                while (netServer.ReadMessage(buffer, out type, out sender))
                {
                    switch (type)
                    {
                        case NetMessageType.DebugMessage:
                            GameConsole.Write(buffer.ReadString(), ConsoleMessageTypes.Debug);
                            break;
                        case NetMessageType.ConnectionApproval:
                            GameConsole.Write("Connection request", ConsoleMessageTypes.Debug);
                            int newslot = GetPlayerSlot();
                            if (newslot < 0)
                            {
                                GameConsole.Write("Connection rejected (full)", ConsoleMessageTypes.Debug);
                                sender.Disapprove("Server full");
                            }
                            else
                            {
                                GameConsole.Write("Connection accepted", ConsoleMessageTypes.Debug);
                                ServerPlayer sp = new ServerPlayer(sender, (byte)newslot);
                                setConnectionPlayer(sender,sp);
                                gameState.Players[newslot] = sp; 
                                sp.ServerStatus = PlayerServerStatus.Connecting;
                                sender.Approve();
                            }
                            break;
                        case NetMessageType.StatusChanged:
                            string statusMessage = buffer.ReadString();
                            NetConnectionStatus newStatus = (NetConnectionStatus)buffer.ReadByte();
                            switch (newStatus)
                            {
                                case NetConnectionStatus.Disconnected:
                                    ClientDisconnected(sender);
                                    break;
                            }
                            GameConsole.Write("New status for client " + sender + ": " + newStatus + " (" + statusMessage + ")", ConsoleMessageTypes.Debug);
                            break;
                        case NetMessageType.Data:
                            // A client sent a game message
                            ProcessGameMsg(buffer, sender);                           
                            break;
                    }
                }
                
                Thread.Sleep(1);
            }
            GameConsole.Write("Server shutting down");
            netServer.Shutdown("Server shutdown");

        }

        private static void InitWorld()
        {
            gameState.World = new World();
            gameState.World.Areas = new Area[1];

            LoadTown();
                       
          
        }
        private static void CreateTestEnvironment()
        {
            //Make a test world
            gameState.World = new World();
            gameState.World.Areas = new Area[1];
            gameState.World.Areas[0] = new Area(50, 50);
            gameState.World.Areas[0].Name = "Test environment";
            gameState.World.Areas[0].ID = 0;
            for (int x = 0; x < gameState.World.Areas[0].Width; x++)
            {
                for (int y = 0; y < gameState.World.Areas[0].Height; y++)
                {
                    Square s = new Square();
                    s.LevelID = 0;
                    s.setFrame(14, 1);
                    s.setFrame(15, 2);
                    s.Position = new Point(x, y);
                    gameState.World.Areas[0].SetSquare(s.Position, s);
                }
            }

            //Make a obstacle
            Square modify = gameState.World.Areas[0].GetSquare(new Point(1, 0));
            modify.setFrame(10, 3492);
            modify.setFrame(11, 3493);
            modify.setFrame(12, 3494);
            modify.setFrame(13, 3495);
            modify.setFrame(14, 3496);
            modify.setFrame(15, 3497);
            modify.PassablePlayer = false;
        }
        private static void MakeNPCs(World w)
        {
            int id = 0;
            for (int x = 65; x < 66; x++)
            {
                for (int y = 66; y < 67; y++)
                {
                    //AISkeleton skeleton = new AISkeleton(gameState.World, 0, new Point(x, y), gameState, 1, 10, NPCType.Undead, 20, 5, 1, 500, 5, 20, 1, 0.65f, 0.4f, 0.4f, 0.3f);
                    ServerNPC npc = ServerNPC.FromTemplate(gameState, 0, new Point(x, y), ImportedContent.MonsterTemplates[0], id);
                    
                    id++;
                }
            }
        }
        private static void LoadTown()
        {
            //Read Town dungeon definition files .DUN to area 0
            byte[][] towndun = new byte[4][];
            towndun[0] = LegacyContent.GetMPQFile(@"levels\towndata\sector1s.dun");
            towndun[1] = LegacyContent.GetMPQFile(@"levels\towndata\sector2s.dun");
            towndun[2] = LegacyContent.GetMPQFile(@"levels\towndata\sector3s.dun");
            towndun[3] = LegacyContent.GetMPQFile(@"levels\towndata\sector4s.dun");
            DungeonDefinition dd = new DungeonDefinition(towndun);

            //Paint Town tiles
            dd.PaintDungeon(3, 0, 0);
            dd.PaintDungeon(2, 0, 23);
            dd.PaintDungeon(1, 23, 0);
            dd.PaintDungeon(0, 23, 23);

            //Load all gfx & data files associated with town
            LevelGraphics lg = LevelCache.LoadLevel(0);

            //Set area 0
            gameState.World.Areas[0] = new Area((ushort)(dd.Width * 2), (ushort)(dd.Height * 2)); //*2 since a tile is 2x2 squares
            gameState.World.Areas[0].Name = "Tristram";
            gameState.World.Areas[0].ID = 0;
            
            //Convert tiles to Squares
            int index = 0;
            for (int y = 0; y < dd.Height; y++)
            {
                for (int x = 0; x < dd.Width; x++)
                {
                    lg.PaintTile(dd.TileID[index], x * 2, y * 2, gameState.World.Areas[0]);
                    index++;
                }
            }
        }

        public static void GameStateUpdate(float secondsPassed)
        {
            gameState.Update(secondsPassed);
           
        }
        private static void ClientDisconnected(NetConnection sender)
        {
            gameState.Players[getPlayerByConnection(sender).ID] = null;
        }
        public static void ShutDown()
        {
            Server.Run = false;
        }
        private static int GetPlayerSlot()
        {
            //Find a free player slot in range (0 - max clients)
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (gameState.Players[i] == null) return i;
            }
            return -1;
        }
        private static void ProcessGameMsg(NetBuffer buffer, NetConnection sender)
        {
            ServerPlayer p = getPlayerByConnection(sender);
            Point request;
            NetBuffer response;
            ProtocolClientToServer MessageType = (ProtocolClientToServer)buffer.ReadUInt16();
            switch (MessageType)
            {
                case ProtocolClientToServer.Handshake:
                    int client_version = buffer.ReadUInt16();
                    if (client_version < Settings.SERVER_LOWEST_CLIENT_VERSION)
                    {
                        DropPlayer(sender, "Client version too low");
                    }
                    else
                    {
                        GameConsole.Write("S2C Handshake ok, please send player info", ConsoleMessageTypes.Debug);
                        NetProtocol.S2C_HandshakeOK(netServer, sender, p.ID);
                    }
                    break;
                case ProtocolClientToServer.PlayerInfo:
                    
                    //Read player attributes from client (trusted client mode...)
                    p.Character = Serializer.ReadCharacter(buffer);
                    p.RefreshAttributes();
                    p.CurrentHP = p.MaxHP;
                    p.World = gameState.World;
                    GameConsole.Write("[S] " + p.Character.Name + " connected", ConsoleMessageTypes.Info);

                    //Transmit map
                    NetBuffer map = NetProtocol.S2C_MapInfo(netServer, gameState.World);
                    GameConsole.Write("[S] Sending world... (" + map.LengthBytes + " bytes)", ConsoleMessageTypes.Debug);
                    sender.SendMessage(map, NetChannel.ReliableInOrder1);

                    //Send spawn command
                    Server.PlayerSpawn(p, 0, FindSpawnPoint(0));
                    
                    GameConsole.Write("[S] Spawning " + p.Character.Name + " at " + p.Position, ConsoleMessageTypes.Debug);
                    
                    p.ServerStatus = PlayerServerStatus.Playing;
                    break;
                case ProtocolClientToServer.Walk:
                    //Client has requested to walk one tile to his new square.
                    //GameConsole.Write("Walk request", ConsoleMessageTypes.Debug);
                    //Get requested tile.
                    request = Serializer.ReadPoint(buffer);

                    //Validate
                    if(Server.ValidateWalk(p, request))
                    {
                        //Walk is initiated - send status update
                        NetProtocol.S2C_Walk(netServer, sender, request, p.Position, true);
                    }
                    else
                    {
                        //Walk is disapproved and player must not update his position 
                        GameConsole.Write("Walk denied for player " + p.ID + ": " + request.ToString());
                        NetProtocol.S2C_Walk(netServer, sender, request, p.Position, false);
                    }
                    //Sync other players' view (todo)

                    break;
                case ProtocolClientToServer.MeleeAttack:
                    //Client is attacking an adjacent tile with a melee weapon
                    request = Serializer.ReadPoint(buffer);
                    if (Server.PlayerAttackSquare(p, request))
                    {
                        //GameConsole.Write("s: " + p.Name + " attacks " + request.ToString());
                    }
                    break;
                case ProtocolClientToServer.RequestStatPoint:
                    //Player wants to spend a level point
                    AttributeType stat = (AttributeType)buffer.ReadByte();
                    if (p.Character.LevelUpPoints > 0)
                    {
                        p.AddStat(stat);
                        response = NetProtocol.S2C_PlayerStatsUpdate(netServer, p);
                        sender.SendMessage(response, NetChannel.ReliableInOrder1);
                    }
                    break;
                case ProtocolClientToServer.RequestRespawn:
                    //Player wants to respawn
                    if (p.Action == PlayerAction.Dead)
                    {
                        p.SetLocation(0, FindSpawnPoint(0)); //town
                        p.CurrentHP = p.MaxHP;
                        p.StartAction(PlayerAction.Idle, 0);
                        p.Direction = Direction.SouthEast;
                        response = NetProtocol.S2C_Spawn(netServer, p.Area.ID, p.Position);
                        sender.SendMessage(response, NetChannel.ReliableInOrder1);

                        //Send updated status
                        sender.SendMessage(NetProtocol.S2C_PlayerStatus(netServer, p), NetChannel.ReliableInOrder1);

                    }

                    break;
            }
        }

        private static void PlayerSpawn(ServerPlayer p, ushort areaid, Point location)
        {
            //Set location
            p.SetLocation(areaid, location);

            //Send dynamic area info
            NetBuffer b = NetProtocol.S2C_AreaNPCList(netServer, gameState.World.Areas[areaid]);
            p.NetConnection.SendMessage(b, NetChannel.ReliableInOrder1);

            //Send spawn command
            b = NetProtocol.S2C_Spawn(netServer, areaid, location);
            p.NetConnection.SendMessage(b, NetChannel.ReliableInOrder1);
        }

        private static Point FindSpawnPoint(int areaid)
        {
            //Check for telefrag (todo)

            //Test
            //return new Point(0, 0);

            //Tristram
            return new Point(76, 69);
        }

        public static bool PlayerAttackSquare(ServerPlayer p, Point pos)
        {
            bool result = false;
            
            if (p.MeleeAttack(pos))
            {
                p.Direction = GameMechanics.AdjacentTileDirection(p.Position, pos);
                GameConsole.Write("PlayerAttackSquare " + GetUptime().ToString());
                result = true;
                //sync other players   (todo)
            }

            return result;
        }
        private static ServerPlayer getPlayerByConnection(NetConnection c)
        {
            return (ServerPlayer)c.Tag;
        }
        private static void setConnectionPlayer(NetConnection c, ServerPlayer sp)
        {
            c.Tag = sp;
        }
        private static void DropPlayer(NetConnection sender, string reason)
        {
            GameConsole.Write("Dropping player id " + getPlayerByConnection(sender) + ": " + reason);
            sender.Disconnect(reason,0);
            
        }

        private static bool ValidateWalk(ServerPlayer p, Point request)
        {
            if (p.Action == PlayerAction.Walk)
            {
                //Overlapping request could be due to lag of first walk request (message A too close to message B due to varying lag/game slowdown)
                //A clever client could exploit this, so this needs some improvement (todo)
                float overlap = Settings.SERVER_MESSAGE_COMPACTION_TOLERANCE;

                //check if we are close to completing previous action
                float timeLeft = p.GetTimeUntilIdle();
                if (overlap > timeLeft)
                {
                    p.FinishMove();
                }
            }
            if (p.Walk(gameState, request))
            {
                //float walkspeed = 0.4f;
                //p.StartAction(PlayerAction.Walking, walkspeed);
                //p.StartMove(request, GameMechanics.DurationToDurationsPerSecond(walkspeed));

                //Aggro npc's in range of new tile
                Server.NPCNotify(p,request);
                return true;
            }
            else
            {
                return false;
            }

        }

        private static void NPCNotify(Player p, Point request)
        {
            //When a player moves, notify enemy units of his presence
            foreach (ServerNPC n in gameState.World.Areas[p.AreaID].npcs)
            {
                if (n.EnemyValid(p))
                {
                    if (n.State == AIState.Inactive)
                    {
                        n.SetState(AIState.Active);
                    }
                }
            }
            
        }

        internal static void OnPlayerMeleeFinished(ServerPlayer serverPlayer, BattleResult r)
        {
            //Called from ServerPlayer when a melee attack is successful
            //Send result to all players in player's area
            NetBuffer b = NetProtocol.S2C_BattleResult(netServer, r);
            SendToArea(b, serverPlayer.Area);
            r.Apply();
        }
        private static double GetUptime()
        {
            return ((double)Uptime.ElapsedTicks / (double)Stopwatch.Frequency);
        }
        private static void SendToArea(NetBuffer b, Area area)
        {
            foreach (Player p in gameState.Players)
            {
                if (p.Area == area)
                {
                    ServerPlayer sp = (ServerPlayer)p;
                    sp.NetConnection.SendMessage(b, NetChannel.ReliableInOrder1);
                }
            }
        }

        internal static void OnNPCStatusChange(ServerNPC n)
        {
            //Send status to area
            NetBuffer buffer = NetProtocol.S2C_NPCUpdate(netServer, n);
            SendToArea(buffer, n.Area);
        }
        internal static void OnNPCAttack(ServerNPC n, BattleResult result)
        {
            //If npc has attacked, send attack result
            NetBuffer buffer = NetProtocol.S2C_BattleResult(netServer, result);
            Server.SendToArea(buffer, n.Area);
            result.Apply();
        }

        internal static void OnPlayerActionFailed(ServerPlayer serverPlayer, PlayerAction a)
        {
            //Player's action was interrupted
            if (a == PlayerAction.Walk)
            {
                //Inform player of his position, since client uses prediction movement
                NetBuffer b = NetProtocol.S2C_PlayerStatus(netServer, serverPlayer);
                SendToArea(b, serverPlayer.Area);
            }
        }

        internal static void OnPlayerGainXP(ServerPlayer player)
        {
            //Notify player of his new XP amount
            //todo: optimize a bit (dont send attributes)
            NetBuffer b = NetProtocol.S2C_PlayerStatsUpdate(netServer, player);
            player.NetConnection.SendMessage(b, NetChannel.ReliableInOrder1);
        }
    }
}
