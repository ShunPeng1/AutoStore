
using System.Collections.Generic;
using Shun_Grid_System;
using UnityEngine;

public class CellItem
{
    private readonly GridXZ<CellItem> _grid;
    private readonly GridXZCell<CellItem> _cell;

    public readonly Bin[] BinsStack;
    public int CurrentBinCount = 0;
    private float _stackDepthSize;
    public CellItem(GridXZ<CellItem> grid, GridXZCell<CellItem> cell, int stackSize, float stackDepthSize)
    {
        BinsStack = new Bin[stackSize];
        _grid = grid;
        _cell = cell;
        _stackDepthSize = stackDepthSize;
    }

    public void AddToStack(Bin bin)
    {
        BinsStack[CurrentBinCount] = bin;
        CurrentBinCount++;
    }

    public Bin RemoveTopBinFromStack()
    {
        var bin = BinsStack[CurrentBinCount];
        BinsStack[CurrentBinCount] = null;
        CurrentBinCount--;
        return bin;
    }
    
    public Vector3 GetTopStackWorldPosition()
    {
        return _grid.GetWorldPositionOfNearestCell(_cell.XIndex, _cell.YIndex) + (BinsStack.Length - CurrentBinCount) * Vector3.down;
    }
    
    public Vector3 GetStackWorldPosition(int index)
    {
        return _grid.GetWorldPositionOfNearestCell(_cell.XIndex, _cell.YIndex) + (BinsStack.Length - index) * Vector3.down;
    }
    
    
    
    
    
}
