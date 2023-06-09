using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorage : MonoBehaviour
{
    private int _xIndex, _zIndex;
    private GridXZ<GridXZCell<StackStorage>> _grid ;
    private GridXZCell<StackStorage> _gridXZCellCell;
    public void Init(GridXZ<GridXZCell<StackStorage>> grid, int xIndex, int zIndex, GridXZCell<StackStorage> xzCell)
    {
        _grid = grid;
        _xIndex = xIndex;
        _zIndex = zIndex;
        _gridXZCellCell = xzCell;
    }
    
}
