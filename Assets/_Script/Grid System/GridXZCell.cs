using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridXZCell<TItem>
{
    [Header("Base")]
    private readonly GridXZ<GridXZCell<TItem>> _gridXZ;
    public readonly int XIndex, ZIndex;
    public readonly List<GridXZCell<TItem>> AdjacentCells = new ();
    public TItem Item;
    public bool IsObstacle;

    [Header("A Star Pathfinding")] 
    public GridXZCell<TItem> ParentXZCell = null; 
    public int FCost;
    public int HCost;
    public int GCost;
    
    
    public GridXZCell(GridXZ<GridXZCell<TItem>> grid, int x, int z, TItem item = default)
    {
        _gridXZ = grid;
        XIndex = x;
        ZIndex = z;
        Item = item;
    }

    public void SetAdjacency()
    {
        GridXZCell<TItem>[] adjacentRawItems =
        {
            //in counter clockwise order
            _gridXZ.GetItem(XIndex + 1, ZIndex),
            _gridXZ.GetItem(XIndex, ZIndex + 1),
            _gridXZ.GetItem(XIndex - 1, ZIndex),
            _gridXZ.GetItem(XIndex, ZIndex - 1)
        };

        foreach (var rawItem in adjacentRawItems)
        {
            if (rawItem != null)
            {
                AdjacentCells.Add(rawItem);
                //Debug.Log("("+_xIndex+","+_zIndex+") adjacent to ("+rawItem._xIndex+","+rawItem._zIndex+")");
            }
        }
        
    }
    

    public static (int xDiff, int zDiff) GetIndexDifference(GridXZCell<TItem> first, GridXZCell<TItem> second)
    {
        return (second.XIndex - first.XIndex , second.ZIndex-first.ZIndex);
    }
    
    public static (int xDiff, int zDiff) GetIndexDifferenceAbsolute(GridXZCell<TItem> first, GridXZCell<TItem> second)
    {
        return (Mathf.Abs(second.XIndex - first.XIndex), Mathf.Abs(second.ZIndex - first.ZIndex));
    }
}