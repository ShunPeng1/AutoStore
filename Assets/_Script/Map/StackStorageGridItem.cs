using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorageGridItem
{
    [Header("Base")]
    private readonly GridXZ<StackStorageGridItem> _gridXZ;
    public readonly int xIndex, zIndex;
    public List<StackStorageGridItem> adjacentItems = new ();
    public StackStorage stackStorage;

    [Header("A Star Pathfinding")] 
    public StackStorageGridItem parentItem = null; 
    public int fCost => hCost+gCost;
    public int hCost;
    public int gCost;
    private float _weight = 0f;
    
    public StackStorageGridItem(GridXZ<StackStorageGridItem> grid, int x, int z, StackStorage stackStorage)
    {
        _gridXZ = grid;
        xIndex = x;
        zIndex = z;
        this.stackStorage = stackStorage;
        //AddAdjacency();
    }

    public void SetAdjacency()
    {
        StackStorageGridItem[] adjacentRawItems =
        {
            _gridXZ.GetItem(xIndex + 1, zIndex),
            _gridXZ.GetItem(xIndex - 1, zIndex),
            _gridXZ.GetItem(xIndex, zIndex + 1),
            _gridXZ.GetItem(xIndex, zIndex - 1)
        };

        foreach (var rawItem in adjacentRawItems)
        {
            if (rawItem != null)
            {
                adjacentItems.Add(rawItem);
                //Debug.Log("("+_xIndex+","+_zIndex+") adjacent to ("+rawItem._xIndex+","+rawItem._zIndex+")");
            }
        }
        
    }

    public void AddWeight(float adding)
    {
        _weight += adding;
        Debug.Log("Weigth");
        _gridXZ.TriggerGridObjectChanged(xIndex, zIndex);
    }

    public static (int xDiff, int zDiff) GetIndexDifference(StackStorageGridItem first, StackStorageGridItem second)
    {
        return (second.xIndex - first.xIndex , second.zIndex-first.zIndex);
    }
    
    public static (int xDiff, int zDiff) GetIndexDifferenceAbsolute(StackStorageGridItem first, StackStorageGridItem second)
    {
        return (Mathf.Abs(second.xIndex - first.xIndex), Mathf.Abs(second.zIndex - first.zIndex));
    }
}