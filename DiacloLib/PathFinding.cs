//http://www.policyalmanac.org/games/aStarTutorial.htm
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Diagnostics;

namespace DiacloLib
{
    public class PathNode : IComparable<PathNode>, IMinHeapIndexable
    {
        public bool Closed = false;
        public int Score = 0;
        public int MoveCost = 0;
        public PathNode Parent;
        public Point Position;
        public int HeapLocation;
        public int getHeapIndex()
        {
            return HeapLocation;
        }
        public void setHeapIndex(int index)
        {
            HeapLocation = index;
        }
        public int CompareTo(PathNode other)
        {
            return this.Score.CompareTo(other.Score);
        }
    }
    public class PathFinding
    {
        
        public static UInt16[] MoveCost = new UInt16[]
            { //14 for diagonal, 10 for vert/horiz
                14, //SE
                10, //S
                14, //SW
                10, //W
                14, //NW
                10, //N
                14, //NE
                10  //E
            };
        public static Queue<Point> Navigate(Point start_pos, Point destination_pos, Area map)
        {
            Stopwatch sw = Stopwatch.StartNew();
            MinHeap<PathNode> opened = new MinHeap<PathNode>();
            PathNode[,] grid = new PathNode[map.Width, map.Height];
            Queue<Point> ret;

            //If target is outside map, cancel
            if (!map.PointWithinBounds(destination_pos))
                return null;

            //If target is unenterable, cancel
            if(!map.GetSquare(destination_pos).PassablePlayer) {
                return null;
            }

            //Check if distance == 1
            if (GameMechanics.MapTilesAdjacent(start_pos, destination_pos))
            {
                //Tile adjacent, no need for pathfind. Check if enterable
                if(GameMechanics.PositionEnterable(map, destination_pos) && GameMechanics.DiagonalAllowed(start_pos, destination_pos, map)) {
                    //Enterable
                    ret = new Queue<Point>();
                    ret.Enqueue(destination_pos);
                    return ret;
                } else {
                    return null;
                }
            }

            // Start at distance of 0 (start at target, nodes will be linked in reverse).
            PathNode start = (grid[destination_pos.X, destination_pos.Y] = new PathNode());
            PathNode destination = null;
            start.Position = destination_pos;
            start.Score = 0;
            start.Closed = true;
            opened.Add(start);

            while (opened.Count > 0 && destination == null)
            {
                //Select the node with the best score from the list of opened nodes
                PathNode best = opened.Pop();
                //Close this node so we don't analyze it's options again
                best.Closed = true;
                Square neighbor;

                //Check all 8 directions (surrounding tiles)
                for (int i = 0; i < GameMechanics.MoveDirectionDeltas.Length; i++)
                {
                    neighbor = map.GetSquare(new Point(best.Position.X + GameMechanics.MoveDirectionDeltas[i].X, best.Position.Y + GameMechanics.MoveDirectionDeltas[i].Y));

                    //Check for destination
                    if (neighbor != null && neighbor.Position == start_pos && GameMechanics.DiagonalAllowed(best.Position, neighbor.Position, map))
                    {
                        destination = new PathNode();
                        destination.Parent = best;
                        break;
                    } 
                    else if(neighbor != null)
                    {
                        //Node is unopened, open it and calculate score
                        PathNode node;
                        node = grid[neighbor.Position.X, neighbor.Position.Y];

                        if (node == null)
                        {
                            node = new PathNode();
                            if (!GameMechanics.PositionEnterable(map, neighbor.Position) || !GameMechanics.DiagonalAllowed(best.Position, neighbor.Position, map))
                            {
                                node.Closed = true;
                                node.Score = int.MaxValue;
                            }
                            else
                            {
                                node.Parent = best;
                                node.MoveCost = best.MoveCost + MoveCost[i];
                                node.Position = neighbor.Position;

                                int estimatedDistanceLeft = ManhattanDistance(neighbor.Position.X, neighbor.Position.Y, start_pos.X, start_pos.Y) * 10;
                                node.Score = node.MoveCost + estimatedDistanceLeft;
                                opened.Add(node);
                            }
                            grid[neighbor.Position.X, neighbor.Position.Y] = node;
                            
                        }
                        else if (node.Closed == false)
                        {
                            //Node is opened
                            int costFromCurrent = best.MoveCost + MoveCost[i];
                            if (costFromCurrent < node.MoveCost)
                            {
                                //MoveCost was better (lower) if coming from this square, so change the parent and recalculate score
                                node.Parent = best;
                                node.MoveCost = costFromCurrent;
                                int estimatedDistanceLeft = ManhattanDistance(neighbor.Position.X, neighbor.Position.Y, start_pos.X, start_pos.Y) * 10;
                                node.Score = node.MoveCost + estimatedDistanceLeft;
                                opened.Changed(node);
                            }
                             
                        }

                    }
                }
            }

            if (destination != null)
            {
                //A path was found. Make a list containing all positions needed to get there.
                //This is done from "destination"'s parent, which is the first step, to the "start", which is the target tile (where we started the pathfinding algoritm)
                ret = new Queue<Point>();
                PathNode p = destination.Parent; //Skip first tile (caller's position)
                while (p != start)
                {
                    ret.Enqueue(p.Position);
                    p = p.Parent;
                }
                ret.Enqueue(start.Position); //Add destination tile

                
            }
            else
            {
                //No path to target.
                ret = null;
            }

            GameConsole.ReportPerformance(PerformanceCategory.PathFinding, sw.ElapsedTicks);
            return ret;
            
        }
         

        public static int ManhattanDistance(int ax, int ay, int bx, int by)
        {
            //Moving only horizontally and vertically, calculate the "manhattan" distance to target, ignoring potential obstacles
            return Math.Abs(ax - bx) + Math.Abs(ay - by);
        }
        public static double FlightDistance(Point a, Point b)
        {
            return Math.Sqrt(Math.Pow(a.X - b.X,2) + Math.Pow(a.Y - b.Y, 2));
        }
        
    }
}
