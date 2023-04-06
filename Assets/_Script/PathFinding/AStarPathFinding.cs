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
    public void FindPath(StackStorageGridItem startNode, StackStorageGridItem endNode)
    {
        Priority_Queue.SimplePriorityQueue<StackStorageGridItem> openSet = new ();
        HashSet<StackStorageGridItem> closeSet = new();
        openSet.Enqueue(startNode, startNode.FCost);
        
        while (openSet.Count > 0)
        {
            var smallestFCost = openSet.Dequeue();
            closeSet.Add(smallestFCost);

            if (smallestFCost == endNode)
            {
                return;
            }

            foreach (var adjacentItem in smallestFCost.adjacentItems)
            {
                if (false) // Not traverisble
                {
                    continue;
                }
                
                
            }
        }


    }

    private void GetDistanceCost(StackStorageGridItem first, StackStorageGridItem second)
    {
        return;
    }
}
