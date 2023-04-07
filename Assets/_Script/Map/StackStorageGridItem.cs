using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorageGridItem
{
    [Header("Base")]
    private readonly GridXZ<StackStorageGridItem> _gridXZ;
    private readonly int _xIndex, _zIndex;
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
        _xIndex = x;
        _zIndex = z;
        this.stackStorage = stackStorage;
        //AddAdjacency();
    }

    public void SetAdjacency()
    {
        StackStorageGridItem[] adjacentRawItems =
        {
            _gridXZ.GetItem(_xIndex + 1, _zIndex),
            _gridXZ.GetItem(_xIndex - 1, _zIndex),
            _gridXZ.GetItem(_xIndex, _zIndex + 1),
            _gridXZ.GetItem(_xIndex, _zIndex - 1)
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
        _gridXZ.TriggerGridObjectChanged(_xIndex, _zIndex);
    }

    public static (int xDiff, int zDiff) GetIndexDifference(StackStorageGridItem first, StackStorageGridItem second)
    {
        return (second._xIndex - first._xIndex , second._zIndex-first._zIndex);
    }
    
    public static (int xDiff, int zDiff) GetIndexDifferenceAbsolute(StackStorageGridItem first, StackStorageGridItem second)
    {
        return (Mathf.Abs(second._xIndex - first._xIndex), Mathf.Abs(second._zIndex - first._zIndex));
    }
}