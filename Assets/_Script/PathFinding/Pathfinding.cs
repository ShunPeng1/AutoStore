using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPathfindingAlgorithm<TCell>
{
    public LinkedList<GridXZCell> FirstTimeFindPath(TCell start, TCell end);
    public LinkedList<GridXZCell> UpdatePathWithDynamicObstacle(TCell currentStartNode, List<TCell> foundDynamicObstacles);
}

public class Pathfinding<TGrid, TCell> : IPathfindingAlgorithm<TCell>
{
    protected TGrid Grid;

    public Pathfinding(TGrid grid)
    {
        this.Grid = grid;
    }
    
    public virtual LinkedList<GridXZCell> FirstTimeFindPath(TCell start, TCell end)
    {
        return null;
    }

    public virtual LinkedList<GridXZCell> UpdatePathWithDynamicObstacle(TCell currentStartNode, List<TCell> foundDynamicObstacles)
    {
        return null;
    }
}
