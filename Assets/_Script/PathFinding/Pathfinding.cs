using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPathfindingAlgorithm<TCell, TItem>
{
    public LinkedList<GridXZCell<TItem>> FirstTimeFindPath(TCell start, TCell end);
    public LinkedList<GridXZCell<TItem>> UpdatePathWithDynamicObstacle(TCell currentStartNode, List<TCell> foundDynamicObstacles);
}

public class Pathfinding<TGrid, TCell, TItem> : IPathfindingAlgorithm<TCell,TItem>
{
    protected TGrid Grid;

    public Pathfinding(TGrid grid)
    {
        this.Grid = grid;
    }
    
    public virtual LinkedList<GridXZCell<TItem>> FirstTimeFindPath(TCell start, TCell end)
    {
        return null;
    }

    public virtual LinkedList<GridXZCell<TItem>> UpdatePathWithDynamicObstacle(TCell currentStartNode, List<TCell> foundDynamicObstacles)
    {
        return null;
    }
}
