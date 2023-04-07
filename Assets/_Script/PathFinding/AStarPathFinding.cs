using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AStarPathFinding : PathfindingAlgorithm<GridXZ<StackStorageGridItem>>
{
    public AStarPathFinding(GridXZ<StackStorageGridItem> gridXZ) : base(gridXZ)
    {
        Debug.Log("Init A Star");
    }
    public List<StackStorageGridItem> FindPath(StackStorageGridItem startNode, StackStorageGridItem endNode)
    {
        Priority_Queue.SimplePriorityQueue<StackStorageGridItem> openSet = new ();
        HashSet<StackStorageGridItem> closeSet = new();
        openSet.Enqueue(startNode, startNode.fCost);
        
        while (openSet.Count > 0)
        {
            var currentMinFCostItem = openSet.Dequeue();
            closeSet.Add(currentMinFCostItem);

            if (currentMinFCostItem == endNode)
            {
                return Retrace(startNode, endNode);;
            }

            foreach (var adjacentItem in currentMinFCostItem.adjacentItems)
            {
                if (closeSet.Contains(adjacentItem))
                {
                    continue;
                }

                int newGCostToNeighbour = currentMinFCostItem.gCost + GetDistanceCost(currentMinFCostItem, adjacentItem);
                if (newGCostToNeighbour < adjacentItem.gCost || !openSet.Contains(adjacentItem))
                {
                    adjacentItem.gCost = newGCostToNeighbour;
                    adjacentItem.hCost = GetDistanceCost(adjacentItem, endNode);
                    adjacentItem.parentItem = currentMinFCostItem;

                    if (!openSet.Contains(adjacentItem))
                    {
                        openSet.Enqueue(adjacentItem, adjacentItem.fCost);
                    }
                }

            }
        }
        //Not found a path to the end
        return null;
    }

    /// <summary>
    /// Get a forward Item that the pathfinding was found
    /// </summary>
    List<StackStorageGridItem> Retrace(StackStorageGridItem start, StackStorageGridItem end)
    {
        List<StackStorageGridItem> path = new();
        StackStorageGridItem currentNode = end;
        while (currentNode != start && currentNode!= null)
        {
            //Debug.Log("Path "+ currentNode.xIndex +" "+ currentNode.zIndex );
            path.Add(currentNode);
            currentNode = currentNode.parentItem;
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    private int GetDistanceCost(StackStorageGridItem start, StackStorageGridItem end)
    {
        (int xDiff, int zDiff) = StackStorageGridItem.GetIndexDifferenceAbsolute(start, end);

        return xDiff > zDiff ? 14*zDiff+ 10*(xDiff-zDiff) : 14*xDiff + 10*(zDiff-xDiff);
    }
}
