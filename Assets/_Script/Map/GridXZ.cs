using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridXZ<TItem>
{
    public event EventHandler<OnGridValueChangedEventArgs> EOnGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int z;
    }
    
    private int _width, _height;
    private float _cellWidthSize, _cellHeightSize;
    private Vector3 _originPosition;

    private TItem[,] _gridItems;
    
    public GridXZ(int width = 100, int height = 100, float cellWidthSize = 1f, float cellHeightSize = 1f, Vector3 originPosition = new Vector3(), Func<GridXZ<TItem>, int, int,TItem> createGridItem = null)
    {
        _width = width;
        _height = height;
        _cellHeightSize = cellHeightSize;
        _cellWidthSize = cellWidthSize;
        _originPosition = originPosition;
        _gridItems = new TItem[_width, _height];
        
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                _gridItems[x,z] = createGridItem!=null? createGridItem(this, x, z): default;
                //Debug.DrawLine(GetWorldPosition(x,z) , GetWorldPosition(x+1,z), Color.red, 10f);
                //Debug.DrawLine(GetWorldPosition(x,z) , GetWorldPosition(x,z+1), Color.red, 10f);
            }
        }
    }
    
    public (int , int ) GetXZ(Vector3 position)
    {
        int x = Mathf.FloorToInt((position - _originPosition).x / _cellWidthSize);
        int z = Mathf.FloorToInt((position - _originPosition).z / _cellHeightSize);
        return (x,z);
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x * _cellWidthSize, 0, z * _cellHeightSize) + _originPosition;
    }

    public void SetItem(TItem item, int xIndex, int zIndex)
    {
        if (xIndex < _width && xIndex >= 0 && zIndex < _height && zIndex >= 0)
        {
            _gridItems[xIndex, zIndex] = item;
            TriggerGridObjectChanged(xIndex, zIndex);
        }
    }

    public void TriggerGridObjectChanged(int xIndex, int zIndex)
    {
        if( EOnGridValueChanged != null) EOnGridValueChanged(this, new OnGridValueChangedEventArgs{x = xIndex, z = zIndex});

    }
    
    public void SetItem(TItem item, Vector3 position)
    {
        (int xIndex, int zIndex) = GetXZ(position);
        if(xIndex<_width && xIndex >=0 && zIndex < _height && zIndex >= 0)
        {
            _gridItems[xIndex, zIndex] = item;
        };
    }
    
    public TItem GetItem(int xIndex, int zIndex)
    {
        if(xIndex<_width && xIndex >=0 && zIndex < _height && zIndex >= 0) return _gridItems[xIndex, zIndex];
        return default(TItem);
    }
    
    public TItem GetItem(Vector3 position)
    {
        (int xIndex, int zIndex) = GetXZ(position);
        if(xIndex<_width && xIndex >=0 && zIndex < _height && zIndex >= 0)
        {
            return _gridItems[xIndex, zIndex];
        }
        return default(TItem);
    }
    
}
