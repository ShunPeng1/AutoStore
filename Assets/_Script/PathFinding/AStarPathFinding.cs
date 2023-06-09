using System.Collections.Generic;

namespace _Script.PathFinding
{
    public class AStarPathFinding<TItem> : Pathfinding<GridXZ<GridXZCell<TItem>>, GridXZCell<TItem>, TItem>
    {
        private GridXZCell<TItem> _startNode, _endNode;
        private Dictionary<GridXZCell<TItem>, double> _hValues = new (); // rhsValues[x] = the current best estimate of the cost from x to the goal
        private Dictionary<GridXZCell<TItem>, double> _gValues = new (); // gValues[x] = the cost of the cheapest path from the start to x

        public AStarPathFinding(GridXZ<GridXZCell<TItem>> gridXZ) : base(gridXZ)
        {
        }

        public override LinkedList<GridXZCell<TItem>> FirstTimeFindPath(GridXZCell<TItem> startNode, GridXZCell<TItem> endNode)
        {
            _startNode = startNode;
            _endNode = endNode;
            return FindPath(startNode, endNode);
        }
    
        /// <summary>
        /// 
        /// </summary>
        /// <returns> the path between start and end</returns>
        public LinkedList<GridXZCell<TItem>> FindPath(GridXZCell<TItem> startXZCell, GridXZCell<TItem> endXZCell)
        {
            Priority_Queue.SimplePriorityQueue<GridXZCell<TItem>> openSet = new (); // to be travelled set
            HashSet<GridXZCell<TItem>> closeSet = new(); // travelled set 
            openSet.Enqueue(startXZCell, startXZCell.FCost);
        
            while (openSet.Count > 0)
            {
                var currentMinFCostCell = openSet.Dequeue();
                closeSet.Add(currentMinFCostCell);

                if (currentMinFCostCell == endXZCell)
                {
                    return RetracePath(startXZCell, endXZCell);;
                }

                foreach (var adjacentCell in currentMinFCostCell.AdjacentCells)
                {
                    if (closeSet.Contains(adjacentCell)) // skip for travelled ceil 
                    {
                        continue;
                    }

                    int newGCostToNeighbour = currentMinFCostCell.GCost + GetDistanceCost(currentMinFCostCell, adjacentCell);
                    if (newGCostToNeighbour < adjacentCell.GCost || !openSet.Contains(adjacentCell))
                    {
                        int hCost = GetDistanceCost(adjacentCell, endXZCell);
                        
                        adjacentCell.GCost = newGCostToNeighbour;
                        adjacentCell.HCost = hCost;
                        adjacentCell.FCost = newGCostToNeighbour + hCost;
                        adjacentCell.ParentXZCell = currentMinFCostCell;

                        if (!openSet.Contains(adjacentCell)) // Not in open set
                        {
                            openSet.Enqueue(adjacentCell, adjacentCell.FCost);
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
        protected LinkedList<GridXZCell<TItem>> RetracePath(GridXZCell<TItem> start, GridXZCell<TItem> end)
        {
            LinkedList<GridXZCell<TItem>> path = new();
            GridXZCell<TItem> currentNode = end;
            while (currentNode != start && currentNode!= null) 
            {
                //Debug.Log("Path "+ currentNode.xIndex +" "+ currentNode.zIndex );
                path.AddFirst(currentNode);
                currentNode = currentNode.ParentXZCell;
            }
            path.AddFirst(start);
            return path;
        }

        protected virtual int GetDistanceCost(GridXZCell<TItem> start, GridXZCell<TItem> end)
        {
            (int xDiff, int zDiff) = GridXZCell<TItem>.GetIndexDifferenceAbsolute(start, end);

            // This value make the path go zigzag 
            //return xDiff > zDiff ? 14*zDiff+ 10*(xDiff-zDiff) : 14*xDiff + 10*(zDiff-xDiff);
        
            // This value make the path go L shape 
            return 10 * xDiff + 10 * zDiff;
        }
    
    
    }
}

