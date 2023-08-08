using System.Collections;
using System.Collections.Generic;
using Shun_Grid_System;
using UnityEngine;

public class StackStorage : MonoBehaviour
{
    private int _xIndex, _zIndex;
    private GridXZ<CellItem> _grid ;
    private CellItem _cellItem;
    public void Initialize(GridXZ<CellItem> grid, int xIndex, int zIndex, CellItem cellItem)
    {
        _grid = grid;
        _xIndex = xIndex;
        _zIndex = zIndex;
        _cellItem = cellItem;
    }
    
}
