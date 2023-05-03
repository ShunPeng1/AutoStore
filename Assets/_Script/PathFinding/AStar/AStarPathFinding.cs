using System.Collections.Generic;

namespace _Script.PathFinding.AStar
{
    public class AStarPathFinding : Pathfinding<GridXZ<GridXZCell>, GridXZCell>
    {
        public AStarPathFinding(GridXZ<GridXZCell> gridXZ) : base(gridXZ)
        {
        }
    
        /// <summary>
        /// 
        /// </summary>
        /// <returns> the path between start and end</returns>
        public override LinkedList<GridXZCell> FindPath(GridXZCell startXZCell, GridXZCell endXZCell)
        {
            Priority_Queue.SimplePriorityQueue<GridXZCell> openSet = new (); // to be travelled set
            HashSet<GridXZCell> closeSet = new(); // travelled set 
            openSet.Enqueue(startXZCell, startXZCell.fCost);
        
            while (openSet.Count > 0)
            {
                var currentMinFCostCell = openSet.Dequeue();
                closeSet.Add(currentMinFCostCell);

                if (currentMinFCostCell == endXZCell)
                {
                    return RetracePath(startXZCell, endXZCell);;
                }

                foreach (var adjacentCell in currentMinFCostCell.adjacentItems)
                {
                    if (closeSet.Contains(adjacentCell)) // skip for travelled ceil 
                    {
                        continue;
                    }

                    int newGCostToNeighbour = currentMinFCostCell.gCost + GetDistanceCost(currentMinFCostCell, adjacentCell);
                    if (newGCostToNeighbour < adjacentCell.gCost || !openSet.Contains(adjacentCell))
                    {
                        adjacentCell.gCost = newGCostToNeighbour;
                        adjacentCell.hCost = GetDistanceCost(adjacentCell, endXZCell);
                        adjacentCell.ParentXZCell = currentMinFCostCell;

                        if (!openSet.Contains(adjacentCell)) // Not in open set
                        {
                            openSet.Enqueue(adjacentCell, adjacentCell.fCost);
                        }
                    }

                }
            }
            //Not found a path to the end
            return null;
        }

        /// <summary>
        /// Get a list of Cell that the pathfinding was found
        /// </summary>
        protected LinkedList<GridXZCell> RetracePath(GridXZCell start, GridXZCell end)
        {
            LinkedList<GridXZCell> path = new();
            GridXZCell currentNode = end;
            while (currentNode != start && currentNode!= null) 
            {
                //Debug.Log("Path "+ currentNode.xIndex +" "+ currentNode.zIndex );
                path.AddFirst(currentNode);
                currentNode = currentNode.ParentXZCell;
            }
            path.AddFirst(start);
            return path;
        }

        protected virtual int GetDistanceCost(GridXZCell start, GridXZCell end)
        {
            (int xDiff, int zDiff) = GridXZCell.GetIndexDifferenceAbsolute(start, end);

            // This value make the path go zigzag 
            //return xDiff > zDiff ? 14*zDiff+ 10*(xDiff-zDiff) : 14*xDiff + 10*(zDiff-xDiff);
        
            // This value make the path go L shape 
            return 10 * xDiff + 10 * zDiff;
        }
    
    
    }
}
