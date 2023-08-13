
using System.Collections.Generic;
using Shun_Grid_System;
using UnityEngine;

public class CellItem
{
    private GridXZ<CellItem> _grid;
    private GridXZCell<CellItem> _cell;

    public readonly Crate[] CratesStack;
    public int CurrentCrateCount = 0;
    private float _stackDepthSize;
    public CellItem(GridXZ<CellItem> grid, GridXZCell<CellItem> cell, int stackSize, float stackDepthSize)
    {
        CratesStack = new Crate[stackSize];
        _grid = grid;
        _cell = cell;
        _stackDepthSize = stackDepthSize;
    }

    public void AddToStack(Crate crate)
    {
        CratesStack[CurrentCrateCount] = crate;
        CurrentCrateCount++;
        
    }

    public void RemoveFromStack(Crate crate)
    {
        CratesStack[CurrentCrateCount] = null;
        CurrentCrateCount--;
    }
    
    public Vector3 GetTopStackWorldPosition()
    {
        return _grid.GetWorldPositionOfNearestCell(_cell.XIndex, _cell.YIndex) + (CratesStack.Length - CurrentCrateCount) * Vector3.down;
    }
    
    public Vector3 GetStackWorldPosition(int index)
    {
        return _grid.GetWorldPositionOfNearestCell(_cell.XIndex, _cell.YIndex) + (CratesStack.Length - index) * Vector3.down;
    }
    
    
    
    
    
}
