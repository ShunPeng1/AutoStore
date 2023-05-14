using System;
using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;


/* <summary>
D* Lite is a pathfinding algorithm that is similar to A* but is designed to handle changing environments, such as a partially mapped or dynamic grid. The algorithm uses two values for each cell on the grid: the g-value and the rhs-value.

The g-value represents the cost of the optimal path from the start node to the current node. Initially, all g-values are set to infinity except for the start node, which is set to zero. As the algorithm explores the grid, it updates the g-values of cells that are closer to the start node with more accurate costs.

The rhs-value represents the cost of the second-best path from the start node to the current node. Initially, all rhs-values are set to infinity except for the start node, which is set to zero. As the algorithm explores the grid, it updates the rhs-values of cells that are farther from the start node with more accurate costs.

The algorithm starts by creating a priority queue of cells to be explored. The priority queue is sorted by the sum of the g-value and the heuristic value (H) of each cell. At each iteration, the algorithm checks the cell with the lowest priority in the priority queue. If the cell's g-value is greater than its rhs-value, the cell's g-value is updated to its rhs-value and the cell's predecessors are updated. If the cell's g-value is less than its rhs-value, the cell's rhs-value is updated to its g-value and the cell is added to the priority queue.

As the algorithm progresses, it updates the g-values and rhs-values of cells and adds new cells to the priority queue. When the goal cell is found, the algorithm returns the path from the start node to the goal node.

One of the key features of D* Lite is that it is designed to work efficiently in dynamic environments. When the environment changes, the algorithm can be re-run with the updated costs and a new path will be found that takes the changes into account. This makes it a useful algorithm for applications such as robotics, where the environment can change frequently.
*/
public class DStarLitePathFinding : Pathfinding<GridXZ<GridXZCell>, GridXZCell>
{
    private GridXZCell startNode, endNode;
    private Priority_Queue.SimplePriorityQueue<GridXZCell, QueueKey > openNodes = new ( new CompareFCostHCost()); // priority queue of open nodes
    private int km = 0; // km = heuristic for estimating cost of travel along the last path
    private Dictionary<GridXZCell, double> rhsValues = new (); // rhsValues[x] = the current best estimate of the cost from x to the goal
    private Dictionary<GridXZCell, double> gValues = new (); // gValues[x] = the cost of the cheapest path from the start to x
    private Dictionary<GridXZCell, GridXZCell> predecessors = new (); // predecessors[x] = the node that comes before x on the best path from the start to x
    private Dictionary<GridXZCell, float> dynamicObstacles = new(); // dynamicObstacle[x] = the cell that is found obstacle after find path and its found time

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

    public void ResetPath()
    {
        openNodes = new ( new CompareFCostHCost());
        gValues = new();
        rhsValues = new();
        predecessors = new();
    }

    public override LinkedList<GridXZCell> FindPath(GridXZCell startNode, GridXZCell endNode)
    {
        this.startNode = startNode;
        this.endNode = endNode;

        //ResetPath();
        
        gValues[endNode] = double.PositiveInfinity;
        rhsValues[endNode] = 0;

        gValues[startNode] =  double.PositiveInfinity;
        rhsValues[startNode] = double.PositiveInfinity;
        predecessors[startNode] = null;

        openNodes.Enqueue(endNode, new QueueKey((int) CalculateKey(endNode, startNode) , 0));

        
        while (openNodes.Count > 0 &&
               (GetRhsValue(startNode) > CalculateKey(startNode, startNode) ||
                gValues[startNode] == double.PositiveInfinity))
        {
            if (!openNodes.TryDequeue(out GridXZCell currentNode)) return null; // there are no way to get to end
            
            //Debug.Log("DStar current Node" + currentNode.XIndex + " " + currentNode.ZIndex);

            if (gValues[currentNode] > rhsValues[currentNode])
            {
                gValues[currentNode] = rhsValues[currentNode];

                foreach (var neighbor in currentNode.AdjacentCells)
                {
                    if (neighbor != null && !neighbor.IsObstacle)
                    {
                        UpdateNode(neighbor);
                    }
                }
            }
            else
            {
                gValues[currentNode] = double.PositiveInfinity;
                UpdateNode(currentNode);

                foreach (var neighbor in currentNode.AdjacentCells)
                {
                    if (neighbor != null && !neighbor.IsObstacle)
                    {
                        UpdateNode(neighbor);
                    }
                }

            }
        }

        return RetracePath(startNode, endNode);
    }

    public LinkedList<GridXZCell> UpdatePathDynamicObstacle(GridXZCell currentStartNode, GridXZCell nextNode, List<GridXZCell> foundDynamicObstacles)
    {
        this.startNode = currentStartNode;
        km += GetDistanceCost(currentStartNode, startNode);
        this.dynamicObstacles = new ();
        foreach (var obstacleCell in foundDynamicObstacles)
        {
            dynamicObstacles.Add(obstacleCell, Time.time);
            foreach (var neighbor in obstacleCell.AdjacentCells)
            {
                if (neighbor != null && !neighbor.IsObstacle)
                {
                    UpdateNode(neighbor);
                }
            }
        }
        
        return FindPath(startNode, endNode);
    }
    
    private double CalculateKey(GridXZCell currNode, GridXZCell startNode)
    {
        return Math.Min(GetRhsValue(currNode), GetGValue(currNode)) + GetDistanceCost(currNode, startNode) + km;
    }

    private double GetRhsValue(GridXZCell node)
    {
        return rhsValues.TryGetValue(node, out double value) ? value : double.PositiveInfinity;
    }

    private double GetGValue(GridXZCell node)
    {
        return gValues.TryGetValue(node, out double value) ? value : double.PositiveInfinity;
    }

    private void UpdateNode(GridXZCell node)
    {
        //Debug.Log("DStar UpdateNode " + node.XIndex + " " + node.ZIndex);
        if (node != endNode)
        {
            /*
             * Get the min rhs from Successors, then add it to the predecessors for traverse
             */
            double minRhs = double.PositiveInfinity;
            GridXZCell minSucc = null;

            foreach (var successor in node.AdjacentCells)
            {
                double rhs = gValues.ContainsKey(successor)
                    ? gValues[successor] + GetDistanceCost(node, successor)
                    : double.PositiveInfinity;
                if (rhs < minRhs && !successor.IsObstacle)
                {
                    minRhs = rhs;
                    minSucc = successor;
                }
            }

            rhsValues[node] = minRhs;
            predecessors[node] = minSucc ?? (predecessors[node] ?? null);
        }

        if (openNodes.Contains(node)) // refresh the old node
        {
            openNodes.TryRemove(node);
        }

        if (GetGValue(node) != GetRhsValue(node)) // Mainly if both not equal double.PositiveInfinity, meaning it found a path that is shorter
        {
            gValues[node] = GetRhsValue(node);
            int hValue = GetDistanceCost(node, startNode);
            node.GCost = (int)gValues[node];
            node.HCost = hValue;
            node.FCost = (int)(gValues[node] + hValue + km);

            openNodes.Enqueue(node, new QueueKey(node.FCost, node.HCost)); // Enqueue the new node
        }
    }

    private LinkedList<GridXZCell> RetracePath(GridXZCell startXZCell, GridXZCell endXZCell)
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