using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DStarLitePathFinding : Pathfinding<GridXZ<GridXZCell>, GridXZCell>
{
    public DStarLitePathFinding(GridXZ<GridXZCell> gridXZ) : base(gridXZ)
    {
    }

    private struct QueueKey
    {
        public int FCost;
        public int HCost;

        public QueueKey(int fCost = 0, int hCost = 0)
        {
            FCost = fCost;
            HCost = hCost;
        }
    }
    private class CompareFCostHCost : IComparer<QueueKey>
    {
        public int Compare(QueueKey x, QueueKey y)
        {
            int compare = x.FCost.CompareTo(y.FCost);
            if (compare == 0)
            {
                compare = x.HCost.CompareTo(y.HCost);
            }
            return compare;
        }
    }
    
    
    public override LinkedList<GridXZCell> FindPath(GridXZCell startNode, GridXZCell endNode)
    {
        var openNodes = new Priority_Queue.SimplePriorityQueue<GridXZCell, QueueKey >( new CompareFCostHCost()); // priority queue of open nodes
        var km = 0; // km = heuristic for estimating cost of travel along the last path
        var rhsValues =
            new Dictionary<GridXZCell, double>(); // rhsValues[x] = the current best estimate of the cost from x to the goal
        var gValues =
            new Dictionary<GridXZCell, double>(); // gValues[x] = the cost of the cheapest path from the start to x
        var predecessors =
            new Dictionary<GridXZCell, GridXZCell>(); // predecessors[x] = the node that comes before x on the best path from the start to x
        
        gValues[endNode] = double.PositiveInfinity;
        rhsValues[endNode] = 0;

        gValues[startNode] =  double.PositiveInfinity;
        rhsValues[startNode] = double.PositiveInfinity;
        predecessors[startNode] = null;

        openNodes.Enqueue(endNode, new QueueKey((int) CalculateKey(endNode, startNode, km, gValues, rhsValues) , 0));

        
        while (openNodes.Count > 0 &&
               (GetRhsValue(startNode, rhsValues) > CalculateKey(startNode, startNode, km, gValues, rhsValues) ||
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
                        UpdateNode(neighbor, startNode, endNode, km, rhsValues, gValues, predecessors, openNodes);
                    }
                }
            }
            else
            {
                gValues[currentNode] = double.PositiveInfinity;
                UpdateNode(currentNode, startNode, endNode, km, rhsValues, gValues, predecessors, openNodes);

                foreach (var neighbor in currentNode.AdjacentItems)
                {
                    if (neighbor != null && !neighbor.IsObstacle)
                    {
                        UpdateNode(neighbor, startNode, endNode, km, rhsValues, gValues, predecessors, openNodes);
                    }
                }

            }
        }

        return RetracePath(startNode, endNode, predecessors);
    }

    private double CalculateKey(GridXZCell currNode, GridXZCell startNode, int km, Dictionary<GridXZCell, double> gValues,
        Dictionary<GridXZCell, double> rhsValues)
    {
        return Math.Min(GetRhsValue(currNode, rhsValues), GetGValue(currNode, gValues)) + GetDistanceCost(currNode, startNode) + km;
    }

    private double GetRhsValue(GridXZCell node, Dictionary<GridXZCell, double> rhsValues)
    {
        return rhsValues.TryGetValue(node, out double value) ? value : double.PositiveInfinity;
    }

    private double GetGValue(GridXZCell node, Dictionary<GridXZCell, double> gValues)
    {
        return gValues.TryGetValue(node, out double value) ? value : double.PositiveInfinity;
    }

    private void UpdateNode(GridXZCell node,  GridXZCell startNode,GridXZCell endNode, int km,
        Dictionary<GridXZCell, double> rhsValues,
        Dictionary<GridXZCell, double> gValues, 
        Dictionary<GridXZCell, GridXZCell> predecessors,
        Priority_Queue.SimplePriorityQueue<GridXZCell, QueueKey> openNodes)
    {
        Debug.Log("DStar UpdateNode " + node.XIndex + " " + node.ZIndex);
        if (node != endNode)
        {
            /*
             * Get the min rhs from Successors
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
            predecessors[node] = minSucc ?? (predecessors[node] ?? null);
        }

        if (openNodes.Contains(node))
        {
            openNodes.TryRemove(node);
        }

        if (GetGValue(node, gValues) != GetRhsValue(node, rhsValues))
        {
            gValues[node] = GetRhsValue(node, rhsValues);
            int hValue = GetDistanceCost(node, startNode);
            node.GCost = (int)gValues[node];
            node.HCost = hValue;
            node.FCost = (int)(gValues[node] + hValue + km);

            openNodes.Enqueue(node, new QueueKey(node.FCost, node.HCost)); // or replace node.FCost = CalculateKey
        }
    }

    private LinkedList<GridXZCell> RetracePath(GridXZCell startXZCell, GridXZCell endXZCell,
        Dictionary<GridXZCell, GridXZCell> predecessors)
    {
        LinkedList<GridXZCell> path = new LinkedList<GridXZCell>();
        GridXZCell currentCell = startXZCell;

        while (currentCell != endXZCell)
        {
            path.AddLast(currentCell);
            currentCell = predecessors[currentCell];
        }

        path.AddLast(endXZCell);

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