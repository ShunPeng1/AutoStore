using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DStarLitePathFinding : Pathfinding<GridXZ<GridXZCell>, GridXZCell>
{
    public DStarLitePathFinding(GridXZ<GridXZCell> gridXZ) : base(gridXZ)
    {
    }

    public override LinkedList<GridXZCell> FindPath(GridXZCell startNode, GridXZCell endNode)
    {
        var openNodes = new Priority_Queue.SimplePriorityQueue<GridXZCell, int>(); // priority queue of open nodes
        var km = 0; // km = heuristic for estimating cost of travel along the last path
        var rhsValues =
            new Dictionary<GridXZCell, int>(); // rhsValues[x] = the current best estimate of the cost from x to the goal
        var gValues =
            new Dictionary<GridXZCell, int>(); // gValues[x] = the cost of the cheapest path from the start to x
        var predecessors =
            new Dictionary<GridXZCell, GridXZCell>(); // predecessors[x] = the node that comes before x on the best path from the start to x

        openNodes.Enqueue(endNode, CalculateKey(endNode, km, gValues, rhsValues));

        gValues[endNode] = int.MaxValue;
        rhsValues[endNode] = 0;
        
        gValues[startNode] = 0;
        rhsValues[endNode] = int.MaxValue;
        
        predecessors[startNode] = null;

        while (openNodes.Count > 0 && (GetRhsValue(startNode, rhsValues) > CalculateKey(startNode, km, gValues, rhsValues) || gValues[startNode] == int.MaxValue))
            // I don't know if it like this or not !(GetRhsValue(startNode, rhsValues) > CalculateKey(startNode, km, gValues, rhsValues) || gValues[startNode] == int.MaxValue)
        {
            if (! openNodes.TryDequeue(out GridXZCell currentNode)) return null; // there are no way to get to end
            Debug.Log("DStar current Node"+ currentNode.XIndex + " "+ currentNode.ZIndex);
            
            if (gValues[currentNode] > rhsValues[currentNode])
            {
                gValues[currentNode] = rhsValues[currentNode];

                foreach (var neighbor in currentNode.AdjacentItems)
                {
                    if (neighbor != null && !neighbor.IsObstacle)
                    {
                        UpdateNode(neighbor, endNode, km, rhsValues, gValues, predecessors, openNodes);
                    }
                }
            }
            else
            {
                gValues[currentNode] = int.MaxValue;

                foreach (var neighbor in currentNode.AdjacentItems)
                {
                    if (neighbor != null && !neighbor.IsObstacle)
                    {
                        UpdateNode(neighbor, endNode, km, rhsValues, gValues, predecessors, openNodes);
                    }
                }

                UpdateNode(currentNode, endNode, km, rhsValues, gValues, predecessors, openNodes);
            }
        }

        return RetracePath(startNode, endNode, predecessors);
    }

    private int CalculateKey(GridXZCell node, int km, Dictionary<GridXZCell, int> gValues,Dictionary<GridXZCell, int> rhsValues)
    {
        return Mathf.Min(GetRhsValue(node, rhsValues), GetGValue(node, gValues)) + node.HCost + km;
    }

    private int GetRhsValue(GridXZCell node, Dictionary<GridXZCell, int> rhsValues)
    {
        return rhsValues.TryGetValue(node, out int value) ? value : int.MaxValue;
    }

    private int GetGValue(GridXZCell node, Dictionary<GridXZCell, int> gValues)
    {
        return gValues.TryGetValue(node, out int value) ? value : int.MaxValue;
    }
    private void UpdateNode(GridXZCell node, GridXZCell endNode, int km, Dictionary<GridXZCell, int> rhsValues, Dictionary<GridXZCell, int> gValues, Dictionary<GridXZCell, GridXZCell> predecessors, Priority_Queue.SimplePriorityQueue<GridXZCell, int> openNodes)
    {
        Debug.Log("DStar UpdateNode "+ node.XIndex +" "+ node.ZIndex);
        if (node != endNode)
        {
            int minRhs = int.MaxValue;

            foreach (var successor in node.AdjacentItems)
            {
                int rhs = gValues.ContainsKey(successor) ? gValues[successor] + GetDistanceCost(node, successor) : int.MaxValue;
                minRhs = Mathf.Min(minRhs, rhs);
            }

            rhsValues[node] = minRhs;
        }

        if (openNodes.Contains(node))
        {
            openNodes.TryRemove(node);
        }

        if (GetGValue(node, gValues) != GetRhsValue(node, rhsValues))
        {
            gValues[node] = GetRhsValue(node, rhsValues);
            int hValue = GetDistanceCost(node, endNode);
            node.GCost = gValues[node];
            node.HCost = hValue;
            node.FCost = gValues[node] + hValue + km;

            openNodes.Enqueue(node, node.FCost); // or replace node.FCost = CalculateKey
        }
    }

    private LinkedList<GridXZCell> RetracePath(GridXZCell startXZCell, GridXZCell endXZCell, Dictionary<GridXZCell, GridXZCell> predecessors)
    {
        LinkedList<GridXZCell> path = new LinkedList<GridXZCell>();
        GridXZCell currentCell = endXZCell;

        while (currentCell != startXZCell)
        {
            path.AddFirst(currentCell);
            currentCell = predecessors[currentCell];
        }

        path.AddFirst(startXZCell);

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