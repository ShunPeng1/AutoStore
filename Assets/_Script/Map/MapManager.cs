using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;

public class MapManager : SingletonMonoBehaviour<MapManager>
{
    [SerializeField] private int _width = 20, _height = 20;
    [SerializeField] private float _cellWidthSize = 1f, _cellHeightSize = 1f;
    public GridXZ<StackStorageGridItem> storageGrid;

    [Header("PathFinding")] 
    public AStarPathFinding pathfindingAlgorithm;
    
    
    void Start()
    {
        storageGrid = new GridXZ<StackStorageGridItem>(_width, _height, _cellWidthSize, _cellHeightSize, transform.position,
            (grid, x, z) =>
            {
                StackStorage stackStorage = Instantiate(ResourceManager.Instance.stackStorage, grid.GetWorldPosition(x,z),Quaternion.identity ,transform);
                StackStorageGridItem storageGridItem = new StackStorageGridItem(grid, x, z, stackStorage);
                stackStorage.Init(grid, x, z, storageGridItem);
                return storageGridItem;
            });

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                storageGrid.GetItem(x,z).SetAdjacency();
            }
        }

        pathfindingAlgorithm = new AStarPathFinding(storageGrid);
        
        
    }

    public List<StackStorageGridItem> RequestPath(StackStorageGridItem startCell, StackStorageGridItem endCell)
    {
        return pathfindingAlgorithm.FindPath(startCell,endCell);
    }

    

}