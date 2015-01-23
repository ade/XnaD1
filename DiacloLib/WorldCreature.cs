using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml.Serialization;

namespace DiacloLib
{

    public abstract class WorldCreature: IGameUpdateable
    {
        //Identification
        public int ID;

        //Position
        private int _areaid = -1;
        private Point _position;
        public Point PositionDeviation { get; set; }
        public World World { get; set; }

        //Direction & movement
        public Direction Direction { get; set; }
        public Point TileMoveFrom;
        public Point TileMoveTo;
        public float TileMoveProgress = 1;
        public float TileMoveSpeed = 2.5f;

        //Stats
        public int MaxHP;
        public int CurrentHP;

        //Abstract methods
        public abstract void Hurt(int amount, int newHP, WorldCreature offender);


        public int AreaID
        {
            get { return _areaid; }
        }
        public Area Area
        {
            get
            {
                return World.Areas[this._areaid];
            }
        }
        public Point Position {
            get { return _position; }
            set { 
                if(this._areaid != -1)
                    this.Area.GetSquare(this._position).Creature = null;
                this._position = value;
                this.Area.GetSquare(this._position).Creature = this;
            } 
        }

        public void SetLocation(int areaid, Point pos)
        {
            if (this._areaid != -1)
            {
                this.Area.GetSquare(this.Position).Creature = null;
                if (this is Player)
                    this.Area.Players.Remove((Player)this);
            }
            this._areaid = areaid;
            this.Position = pos;
            this.Area.GetSquare(this.Position).Creature = this;
            if (this is Player)
                this.Area.Players.Add((Player)this);
        }
        public bool StartMove(Point to, float tilesPerSecond)
        {
            if(GameMechanics.MapTilesAdjacent(this.Position, to)) {
                this.TileMoveProgress = 0;
                this.TileMoveFrom = this.Position;
                this.TileMoveTo = to;
                this.Direction = GameMechanics.AdjacentTileDirection(this.TileMoveFrom, to);
                this.TileMoveSpeed = tilesPerSecond;
                return true;
            } else{
                return false;
            }
        }
        public virtual void FinishMove()
        {
            this.Position = TileMoveTo;
            this.TileMoveProgress = 1;
            this.PositionDeviation = new Point(0, 0);
        }
        public virtual void RevertMove()
        {
            Square origin = this.Area.GetSquare(this.TileMoveFrom);
            if(origin.getNPC() == this || (origin.getNPC()==null && origin.getPlayer()==null))
            {
                this.Position = this.TileMoveFrom;
            }
            this.TileMoveProgress = 1;
            this.PositionDeviation = new Point(0, 0);
        }
        public virtual void Update(float secondsPassed)
        {
            if (this.TileMoveProgress < 1)
                TileMoveIncrement(secondsPassed);
        }
        public void TileMoveIncrement(float secondsPassed)
        {

            this.TileMoveProgress += secondsPassed * this.TileMoveSpeed;
            int deviationOffset = 0; //moving away from original tile (0), or approaching the target one? (-1)
            if (this.TileMoveProgress > 1)
            {
                this.TileMoveProgress = 1;
            }
            if (this.TileMoveProgress > 0.5)
                deviationOffset = -1;

            Vector2 pixelModifier = DirectionPixelMultiplier(this.Direction);
            
            //this.PositionDeviation = new Vector2(this.PositionDeviation.X + Settings.PLAYER_WALK_SPEED * secondsPassed * 32 * pixelModifier.X, this.PositionDeviation.Y + Settings.PLAYER_WALK_SPEED * secondsPassed * pixelModifier.Y * 16);
            this.PositionDeviation = new Point((int)(deviationOffset * 32 * pixelModifier.X + 32 * pixelModifier.X * this.TileMoveProgress), (int)(deviationOffset * 16 * pixelModifier.Y + 16 * pixelModifier.Y * this.TileMoveProgress));

            //if (Math.Abs(this.PositionDeviation.X) >= Math.Abs(pixelModifier.X * 32) && Math.Abs(this.PositionDeviation.Y) >= Math.Abs(pixelModifier.Y * 16))
            if(this.TileMoveProgress > 0.5 && this.Position != this.TileMoveTo)
            {
                this.Position = this.TileMoveTo;
            }
        
        }

        public Vector2 DirectionPixelMultiplier(Direction d)
        {
            switch (this.Direction)
            {
                case Direction.North:
                    return new Vector2(1, -1);
                case Direction.NorthEast:
                    return new Vector2(2, 0);
                case Direction.East:
                    return new Vector2(1, 1);
                case Direction.SouthEast:
                    return new Vector2(0, 2);
                case Direction.South:
                    return new Vector2(-1, 1);
                case Direction.SouthWest:
                    return new Vector2(-2, 0);
                case Direction.West:
                    return new Vector2(-1, -1);
                case Direction.NorthWest:
                    return new Vector2(0, -2);
                default:
                    return new Vector2(0, 0);
            }
        }

        
    }
}
