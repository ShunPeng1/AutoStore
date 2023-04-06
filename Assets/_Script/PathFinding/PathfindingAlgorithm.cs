using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingAlgorithm<TGrid>
{
    protected TGrid grid;

    public PathfindingAlgorithm(TGrid grid)
    {
        this.grid = grid;
    }
    
    public virtual void FindPath()
    {
        
    }
}
