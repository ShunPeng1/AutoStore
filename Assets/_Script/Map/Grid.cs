using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid 
{
    private int _width, _height;
    private float _cellWidthSize, _cellHeightSize;
    private Vector3 _originPosition;
    
    public Grid(int width = 100, int height = 100, float cellWidthSize = 1f, float cellHeightSize = 1f, Vector3 originPosition = new Vector3())
    {
        _width = width;
        _height = height;
        _cellHeightSize = cellHeightSize;
        _cellWidthSize = cellWidthSize;
        _originPosition = originPosition;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Debug.DrawLine(GetWorldPosition(x,y) , GetWorldPosition(x+1,y), Color.black);
                Debug.DrawLine(GetWorldPosition(x,y) , GetWorldPosition(x,y+1), Color.black);
            }
        }
    }

    private  Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x * _cellWidthSize, y * _cellHeightSize, 0) + _originPosition;
    }
}
