using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridXZCell
{
    [Header("Base")]
    private readonly GridXZ<GridXZCell> _gridXZ;
    public readonly int XIndex, ZIndex;
    public readonly List<GridXZCell> AdjacentItems = new ();
    public StackStorage StackStorage;

    public bool IsObstacle;

    [Header("A Star Pathfinding")] 
    public GridXZCell ParentXZCell = null; 
    public int FCost;
    public int HCost;
    public int GCost;
    
    
    public GridXZCell(GridXZ<GridXZCell> grid, int x, int z, StackStorage stackStorage)
    {
        _gridXZ = grid;
        XIndex = x;
        ZIndex = z;
        this.StackStorage = stackStorage;
    }

    public void SetAdjacency()
    {
        GridXZCell[] adjacentRawItems =
        {
            _gridXZ.GetItem(XIndex + 1, ZIndex),
            _gridXZ.GetItem(XIndex - 1, ZIndex),
            _gridXZ.GetItem(XIndex, ZIndex + 1),
            _gridXZ.GetItem(XIndex, ZIndex - 1)
        };

        foreach (var rawItem in adjacentRawItems)
        {
            if (rawItem != null)
            {
                AdjacentItems.Add(rawItem);
                //Debug.Log("("+_xIndex+","+_zIndex+") adjacent to ("+rawItem._xIndex+","+rawItem._zIndex+")");
            }
        }
        
    }
    

    public static (int xDiff, int zDiff) GetIndexDifference(GridXZCell first, GridXZCell second)
    {
        return (second.XIndex - first.XIndex , second.ZIndex-first.ZIndex);
    }
    
    public static (int xDiff, int zDiff) GetIndexDifferenceAbsolute(GridXZCell first, GridXZCell second)
    {
        return (Mathf.Abs(second.XIndex - first.XIndex), Mathf.Abs(second.ZIndex - first.ZIndex));
    }
}