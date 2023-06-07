using System;
using System.Collections;
using System.Collections.Generic;
using _Script.PathFinding;
using UnityEngine;
using UnityUtilities;

public class MapManager : SingletonMonoBehaviour<MapManager>
{
    [SerializeField] private int _width = 20, _height = 20;
    [SerializeField] private float _cellWidthSize = 1f, _cellHeightSize = 1f;
    public GridXZ<GridXZCell> StorageGrid;

    void Start()
    {
        StorageGrid = new GridXZ<GridXZCell>(_width, _height, _cellWidthSize, _cellHeightSize, transform.position,
            (grid, x, z) =>
            {
                StackStorage stackStorage = Instantiate(ResourceManager.Instance.StackStorage, grid.GetWorldPosition(x,z),Quaternion.identity ,transform);
                GridXZCell storageGridXZCell = new GridXZCell(grid, x, z, stackStorage);
                stackStorage.Init(grid, x, z, storageGridXZCell);
                return storageGridXZCell;
            });

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                StorageGrid.GetItem(x,z).SetAdjacency();
            }
        }
        
    }
    
    

}