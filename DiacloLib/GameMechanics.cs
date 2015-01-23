using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace DiacloLib
{
    public enum Direction   //          -          ^        -     
    {                       //       Y         /   NW  \        X
        SouthEast = 0,      //    +        /       4       \        +
        South = 1,          //         /   W               N   \
        SouthWest = 2,      //     /         3           5         \
        West = 3,           // <   SW 2                          6 NE  >
        NorthWest = 4,      //     \         1           7         /
        North = 5,          //         \   S               E   /
        NorthEast = 6,      //             \        0      /
        East = 7            //                 \   SE  /
    }                       //                     v                          

    public enum CharacterClass
    {
        Warrior,
        Rogue,
        Sorceror
    }
    public enum AttributeType
    {
        Strength = 0,
        Magic = 1,
        Dexterity = 2,
        Vitality = 3
    }
    public enum PlayerGraphicsWeapon
    {
        Axe,
        Sword,
        SwordShield,
        Mace,
        MaceShield,
        Unarmed,
        Bow,
        Staff,
        Shield
    }
    public enum PlayerGraphicsAction
    {
        IdleTown,
        Idle,
        Attack,
        Walk,
        WalkTown,
        CastFire,
        CastLightning,
        CastOther,
        TakeHit,
        Die
    }
    public enum PlayerGraphicsArmor
    {
        Light,
        Medium,
        Heavy
    }
    public static class GameMechanics
    {
        public static Point[] MoveDirectionDeltas = new Point[]
        {
            new Point(1, 1), //se (0)
            new Point(0, 1), //s (1)... 
            new Point(-1, 1),
            new Point(-1, 0),
            new Point(-1, -1),
            new Point(0, -1),
            new Point(1, -1), //...ne (6)
            new Point(1, 0), //e (7)
        };
        public static string[] MoveDirectionStrings = new string[] 
        {
            "se", //0
            "s",
            "sw",
            "w",
            "nw",
            "n",
            "ne",
            "e" // 7
        };
        public static Point DirectionToDelta(Direction d)
        {
            return GameMechanics.MoveDirectionDeltas[(int)d];
        }
        public static Direction AdjacentTileDirection(Point tile1pos, Point tile2pos)
        {
            Point delta = new Point(tile2pos.X - tile1pos.X, tile2pos.Y - tile1pos.Y);
            return DeltaToDirection(delta);
        }
        public static Direction DeltaToDirection(Point delta)
        {
            if (delta.X == 0 && delta.Y == -1)
                return Direction.North;
            if (delta.X == 1 && delta.Y == -1)
                return Direction.NorthEast;
            if (delta.X == 1 && delta.Y == 0)
                return Direction.East;
            if (delta.X == 1 && delta.Y == 1)
                return Direction.SouthEast;
            if (delta.X == 0 && delta.Y == 1)
                return Direction.South;
            if (delta.X == -1 && delta.Y == 1)
                return Direction.SouthWest;
            if (delta.X == -1 && delta.Y == 0)
                return Direction.West;
            if (delta.X == -1 && delta.Y == -1)
                return Direction.NorthWest;
            throw new Exception("DeltaToDirection: Tile delta not found (distance != 1 ?)");
        }
        public static bool PositionEnterable(Area a, Point request)
        {
            WorldCreature c = FindOccupantCreature(a, request);
            Square s = a.GetSquare(request);
            if (c == null && s != null)
            {
                return SquareCreatureEnterable(s);
            }
            return false;
        }
        public static bool MapTilesAdjacent(Point tile1, Point tile2) {
            return ((Math.Abs(tile1.X - tile2.X) <= 1 && Math.Abs(tile1.Y - tile2.Y) <= 1) && tile1 != tile2);
        }
        /// <summary>
        /// Return a direction string, for example "e" for east "ne" for northeast
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static string DirectionToString(Direction d)
        {
            return GameMechanics.MoveDirectionStrings[(int)d];
            /*
            string ret = "";
            if (d == Direction.East) ret = "e";
            if (d == Direction.North) ret = "n";
            if (d == Direction.NorthEast) ret = "ne";
            if (d == Direction.NorthWest) ret = "nw";
            if (d == Direction.South) ret = "s";
            if (d == Direction.SouthEast) ret = "se";
            if (d == Direction.SouthWest) ret = "sw";
            if (d == Direction.West) ret = "w";
            return ret;
             */ 
        }
        public static WorldCreature FindOccupantCreature(Area a, Point pos)
        {
            for (int i = 0; i < 8; i++)
            {
                Square s = a.GetSquare(new Point(MoveDirectionDeltas[i].X + pos.X, MoveDirectionDeltas[i].Y + pos.Y));
                if (s != null && s.Creature != null && s.Creature.TileMoveProgress < 1 && (s.Creature.TileMoveTo == pos || s.Creature.TileMoveFrom == pos))
                {
                    return s.Creature;
                }
            }
            return null;
        }
        private static bool SquareCreatureEnterable(Square s)
        {
            return s.PassablePlayer && s.getNPC() == null && s.getPlayer() == null;
        }
        public static Direction PointToDirection(int origin_x, int origin_y, int target_x, int target_y)
        {
            double angle = Math.Atan2(target_y - origin_y, target_x - origin_x) * 180 / Math.PI;
            Direction result = Direction.SouthEast;
            if (angle == 45 || (angle > 22.5 && angle <= 67.5)) result = Direction.SouthEast;
            if (angle == 90 || (angle > 67.5 && angle <= 112.5)) result = Direction.South;
            if (angle == 135 || (angle > 112.5 && angle <= 157.5)) result = Direction.SouthWest;
            if (angle == 180 || (angle > 157.5 || angle < -157.5)) result = Direction.West;
            if (angle == -135 || (angle > -157.5 && angle <= -112.5)) result = Direction.NorthWest;
            if (angle == -90 || (angle > -112.5 && angle <= -67.5)) result = Direction.North;
            if (angle == -45 || (angle > -67.5 && angle <= -22.5)) result = Direction.NorthEast;
            if (angle == 0 || (angle > -22.5 && angle <= 22.5)) result = Direction.East;
            
            return result;
        }
        public static float DurationToDurationsPerSecond(float duration)
        {
            return 1.0f / duration;
        }
        public static float DurationToUpdateInterval(float duration, float updatesPerDuration)
        {
            return 1.0f / (updatesPerDuration / duration);
        }
        public static bool DiagonalAllowed(Point origin, Point target, Area a)
        {
            Direction d = GameMechanics.AdjacentTileDirection(origin, target);
            Square s1 = null;
            Square s2 = null;
            switch (d)
            {
                case Direction.SouthEast:
                    s1 = NeighborByDirection(origin, a, Direction.East);
                    s2 = NeighborByDirection(origin, a, Direction.South);
                    break;
                case Direction.SouthWest:
                    s1 = NeighborByDirection(origin, a, Direction.West);
                    s2 = NeighborByDirection(origin, a, Direction.South);
                    break;
                case Direction.NorthWest:
                    s1 = NeighborByDirection(origin, a, Direction.North);
                    s2 = NeighborByDirection(origin, a, Direction.West);
                    break;
                case Direction.NorthEast:
                    s1 = NeighborByDirection(origin, a, Direction.North);
                    s2 = NeighborByDirection(origin, a, Direction.East);
                    break;
                default:
                    return true;
            }
            //Don't look for creatures here!
            bool enterable = s1.PassablePlayer && s2.PassablePlayer;
            return enterable;
        }
        private static Square NeighborByDirection(Point pos, Area map, Direction d)
        {
            return map.GetSquare(new Point(pos.X + GameMechanics.MoveDirectionDeltas[(int)d].X, pos.Y + GameMechanics.MoveDirectionDeltas[(int)d].Y));
        }
        /// <summary>
        /// Legacy speed value to duration in seconds
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public static float SpeedToDuration(byte speed)
        {
            switch (speed)
            {
                case 0: return 0.05f;
                case 1: return 0.1f;
                case 2: return 0.15f;
                case 3: return 0.20f;
                case 4: return 0.25f;
                case 5: return 0.3f;
            }
            return 0.05f;
        }
    }
}
