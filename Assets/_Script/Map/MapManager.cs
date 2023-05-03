using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

public class MapManager : SingletonMonoBehaviour<MapManager>
{
    [SerializeField] private int _width = 20, _height = 20;
    [SerializeField] private float _cellWidthSize = 1f, _cellHeightSize = 1f;
    public GridXZ<GridXZCell> storageGrid;

    [Header("PathFinding")] 
    public Pathfinding<GridXZ<GridXZCell>, GridXZCell> Pathfinding;
    
    
    
    
    void Start()
    {
        storageGrid = new GridXZ<GridXZCell>(_width, _height, _cellWidthSize, _cellHeightSize, transform.position,
            (grid, x, z) =>
            {
                StackStorage stackStorage = Instantiate(ResourceManager.Instance.stackStorage, grid.GetWorldPosition(x,z),Quaternion.identity ,transform);
                GridXZCell storageGridXZCell = new GridXZCell(grid, x, z, stackStorage);
                stackStorage.Init(grid, x, z, storageGridXZCell);
                return storageGridXZCell;
            });

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                storageGrid.GetItem(x,z).SetAdjacency();
            }
        }

        Pathfinding = new AStarPathFinding(storageGrid);
        
        
    }

    public LinkedList<GridXZCell> RequestPath(GridXZCell startXZCell, GridXZCell endXZCell)
    {
        return Pathfinding.FindPath(startXZCell,endXZCell);
    }

    

}