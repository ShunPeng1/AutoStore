using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridXZ<T>
{
    private int _width, _height;
    private float _cellWidthSize, _cellHeightSize;
    private Vector3 _originPosition;

    private T[,] _items;
    
    public GridXZ(int width = 100, int height = 100, float cellWidthSize = 1f, float cellHeightSize = 1f, Vector3 originPosition = new Vector3())
    {
        _width = width;
        _height = height;
        _cellHeightSize = cellHeightSize;
        _cellWidthSize = cellWidthSize;
        _originPosition = originPosition;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Debug.DrawLine(GetWorldPosition(x,z) , GetWorldPosition(x+1,z), Color.red, 100f);
                Debug.DrawLine(GetWorldPosition(x,z) , GetWorldPosition(x,z+1), Color.red, 100f);
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

    public void SetItem(T item, int xIndex, int zIndex)
    {
        if(xIndex<_width && xIndex >=0 && zIndex < _height && zIndex >= 0) _items[xIndex, zIndex] = item;
    }
    
    public void SetItem(T item, Vector3 position)
    {
        (int xIndex, int zIndex) = GetXZ(position);
        if(xIndex<_width && xIndex >=0 && zIndex < _height && zIndex >= 0)
        {
            _items[xIndex, zIndex] = item;
        };
    }
    
    public object GetItem(int xIndex, int zIndex)
    {
        if(xIndex<_width && xIndex >=0 && zIndex < _height && zIndex >= 0) return _items[xIndex, zIndex];
        return null;
    }
    
    public object GetItem(Vector3 position)
    {
        (int xIndex, int zIndex) = GetXZ(position);
        if(xIndex<_width && xIndex >=0 && zIndex < _height && zIndex >= 0)
        {
            return _items[xIndex, zIndex];
        }
        return null;
    }

}
