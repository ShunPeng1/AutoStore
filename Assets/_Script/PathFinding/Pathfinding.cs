using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PathfindingAlgorithmType
{
    AStar,
    DStarLite,
    MyStar
}

public class Pathfinding<TGrid, TCell>
{
    protected PathfindingAlgorithmType AlgorithmType;
    protected TGrid Grid;

    public Pathfinding(TGrid grid)
    {
        this.Grid = grid;
    }
    
    public virtual LinkedList<GridXZCell> FindPath(TCell start, TCell end)
    {
        return null;
    }
    
    public virtual LinkedList<GridXZCell> InitializePathFinding(TCell start, TCell end)
    {
        return null;
    }
    
}
