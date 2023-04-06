using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathFinding : PathfindingAlgorithm<GridXZ<StackStorageGridItem>>
{
    public AStarPathFinding(GridXZ<StackStorageGridItem> gridXZ) : base(gridXZ)
    {
        Debug.Log("Init A Star");
    }
    public override void FindPath()
    {
        //Debug.Log("Find path");
    }

}
