using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorageGridItem
{
    private GridXZ<StackStorageGridItem> _gridXZ;
    private readonly int _xIndex, _zIndex;
    public float weight = 0f;

    public StackStorageGridItem(GridXZ<StackStorageGridItem> grid, int x, int z)
    {
        _gridXZ = grid;
        _xIndex = x;
        _zIndex = z;
    }

    public void AddWeight(float adding)
    {
        weight += adding;
        Debug.Log("Weigth");
        _gridXZ.TriggerGridObjectChanged(_xIndex,_zIndex);
    }
}
