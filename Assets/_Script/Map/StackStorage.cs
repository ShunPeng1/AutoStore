using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorage : MonoBehaviour
{
    private int _xIndex, _zIndex;
    private GridXZ<StackStorageGridCell> _grid ;
    private StackStorageGridCell _cell;
    public void Init(GridXZ<StackStorageGridCell> grid, int xIndex, int zIndex, StackStorageGridCell cell)
    {
        _grid = grid;
        _xIndex = xIndex;
        _zIndex = zIndex;
        _cell = cell;
    }
    
}
