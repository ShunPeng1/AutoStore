using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorage : MonoBehaviour
{
    private int _xIndex, _zIndex;
    private GridXZ<StackStorageGridItem> _grid ;
    private StackStorageGridItem _item;
    public void Init(GridXZ<StackStorageGridItem> grid, int xIndex, int zIndex, StackStorageGridItem item)
    {
        _grid = grid;
        _xIndex = xIndex;
        _zIndex = zIndex;
        _item = item;
    }
    
}
