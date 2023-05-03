using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackStorage : MonoBehaviour
{
    private int _xIndex, _zIndex;
    private GridXZ<GridXZCell> _grid ;
    private GridXZCell _xzCell;
    public void Init(GridXZ<GridXZCell> grid, int xIndex, int zIndex, GridXZCell xzCell)
    {
        _grid = grid;
        _xIndex = xIndex;
        _zIndex = zIndex;
        _xzCell = xzCell;
    }
    
}
