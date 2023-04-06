using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorageGridItem
{
    private readonly GridXZ<StackStorageGridItem> _gridXZ;
    private readonly int _xIndex, _zIndex;
    private float _weight = 0f;
    public List<StackStorageGridItem> adjacentItems = new ();
    public StackStorage stackStorage;

    public int FCost
    {
        get => FCost;
        set => FCost = value;
    }

    private int hCost;
    private int gCost;
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
                Debug.Log("("+_xIndex+","+_zIndex+") adjacent to ("+rawItem._xIndex+","+rawItem._zIndex+")");
            }
        }
        
    }

    public void AddWeight(float adding)
    {
        _weight += adding;
        Debug.Log("Weigth");
        _gridXZ.TriggerGridObjectChanged(_xIndex, _zIndex);
    }

    public static Vector2 GetIndexDistance(StackStorageGridItem first, StackStorageGridItem second)
    {
        return new Vector2(0, 0);
    }
}