using System;
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
        var openNodes = new Priority_Queue.SimplePriorityQueue<GridXZCell, double>(); // priority queue of open nodes
        var km = 0; // km = heuristic for estimating cost of travel along the last path
        var rhsValues =
            new Dictionary<GridXZCell, double>(); // rhsValues[x] = the current best estimate of the cost from x to the goal
        var gValues =
            new Dictionary<GridXZCell, double>(); // gValues[x] = the cost of the cheapest path from the start to x
        var predecessors =
            new Dictionary<GridXZCell, GridXZCell>(); // predecessors[x] = the node that comes before x on the best path from the start to x

        openNodes.Enqueue(endNode, CalculateKey(endNode, km, gValues, rhsValues));

        gValues[endNode] = double.PositiveInfinity;
        rhsValues[endNode] = 0;

        gValues[startNode] = 0;
        rhsValues[endNode] = double.PositiveInfinity;

        predecessors[startNode] = null;

        while (openNodes.Count > 0 &&
               (GetRhsValue(startNode, rhsValues) > CalculateKey(startNode, km, gValues, rhsValues) ||
                gValues[startNode] == double.PositiveInfinity))
            // I don't know if it like this or not !(GetRhsValue(startNode, rhsValues) > CalculateKey(startNode, km, gValues, rhsValues) || gValues[startNode] == double.PositiveInfinity)
        {
            if (!openNodes.TryDequeue(out GridXZCell currentNode)) return null; // there are no way to get to end
            Debug.Log("DStar current Node" + currentNode.XIndex + " " + currentNode.ZIndex);

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
                gValues[currentNode] = double.PositiveInfinity;

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

    private double CalculateKey(GridXZCell node, int km, Dictionary<GridXZCell, double> gValues,
        Dictionary<GridXZCell, double> rhsValues)
    {
        return Math.Min(GetRhsValue(node, rhsValues), GetGValue(node, gValues)) + node.HCost + km;
    }

    private double GetRhsValue(GridXZCell node, Dictionary<GridXZCell, double> rhsValues)
    {
        return rhsValues.TryGetValue(node, out double value) ? value : double.PositiveInfinity;
    }

    private double GetGValue(GridXZCell node, Dictionary<GridXZCell, double> gValues)
    {
        return gValues.TryGetValue(node, out double value) ? value : double.PositiveInfinity;
    }

    private void UpdateNode(GridXZCell node, GridXZCell endNode, int km,
        Dictionary<GridXZCell, double> rhsValues,
        Dictionary<GridXZCell, double> gValues, 
        Dictionary<GridXZCell, GridXZCell> predecessors,
        Priority_Queue.SimplePriorityQueue<GridXZCell, double> openNodes)
    {
        Debug.Log("DStar UpdateNode " + node.XIndex + " " + node.ZIndex);
        if (node != endNode)
        {
            /*
             * Get the min Successor
             */
            double minRhs = double.PositiveInfinity;
            GridXZCell minSucc = null;

            foreach (var successor in node.AdjacentItems)
            {
                double rhs = gValues.ContainsKey(successor)
                    ? gValues[successor] + GetDistanceCost(node, successor)
                    : double.PositiveInfinity;
                if (rhs < minRhs)
                {
                    minRhs = rhs;
                    minSucc = successor;
                }
            }

            rhsValues[node] = minRhs;
            predecessors[node] = minSucc;
        }

        if (openNodes.Contains(node))
        {
            openNodes.TryRemove(node);
        }

        if (GetGValue(node, gValues) != GetRhsValue(node, rhsValues))
        {
            gValues[node] = GetRhsValue(node, rhsValues);
            int hValue = GetDistanceCost(node, endNode);
            node.GCost = (int)gValues[node];
            node.HCost = hValue;
            node.FCost = (int)(gValues[node] + hValue + km);

            openNodes.Enqueue(node, node.FCost); // or replace node.FCost = CalculateKey
        }
    }

    private LinkedList<GridXZCell> RetracePath(GridXZCell startXZCell, GridXZCell endXZCell,
        Dictionary<GridXZCell, GridXZCell> predecessors)
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