using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AStarPathFinding : PathfindingAlgorithm<GridXZ<StackStorageGridCell>>
{
    public AStarPathFinding(GridXZ<StackStorageGridCell> gridXZ) : base(gridXZ)
    {
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns> the path between start and end</returns>
    public List<StackStorageGridCell> FindPath(StackStorageGridCell startCell, StackStorageGridCell endCell)
    {
        Priority_Queue.SimplePriorityQueue<StackStorageGridCell> openSet = new (); // to be travelled set
        HashSet<StackStorageGridCell> closeSet = new(); // travelled set 
        openSet.Enqueue(startCell, startCell.fCost);
        
        while (openSet.Count > 0)
        {
            var currentMinFCostCell = openSet.Dequeue();
            closeSet.Add(currentMinFCostCell);

            if (currentMinFCostCell == endCell)
            {
                return RetracePath(startCell, endCell);;
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
                    adjacentCell.hCost = GetDistanceCost(adjacentCell, endCell);
                    adjacentCell.parentCell = currentMinFCostCell;

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
    protected List<StackStorageGridCell> RetracePath(StackStorageGridCell start, StackStorageGridCell end)
    {
        List<StackStorageGridCell> path = new();
        StackStorageGridCell currentNode = end;
        while (currentNode != start && currentNode!= null) 
        {
            //Debug.Log("Path "+ currentNode.xIndex +" "+ currentNode.zIndex );
            path.Add(currentNode);
            currentNode = currentNode.parentCell;
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    protected virtual int GetDistanceCost(StackStorageGridCell start, StackStorageGridCell end)
    {
        (int xDiff, int zDiff) = StackStorageGridCell.GetIndexDifferenceAbsolute(start, end);

        // This value make the path go zigzag 
        //return xDiff > zDiff ? 14*zDiff+ 10*(xDiff-zDiff) : 14*xDiff + 10*(zDiff-xDiff);
        
        // This value make the path go L shape 
        return 10 * xDiff + 10 * zDiff;
    }
    
    
}
