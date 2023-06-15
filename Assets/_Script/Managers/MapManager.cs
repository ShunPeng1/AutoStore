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
    public GridXZ<GridXZCell<StackStorage>> WorldGrid;

    /// <summary>
    /// Using the Strategy Pattern for the robot to receive 
    /// </summary>
    #region PathFindingAlgorithm

    private enum PathFindingAlgorithmType
    {
        DStar,
        AStar
    }

    [SerializeField] private PathFindingAlgorithmType _pathFindingAlgorithmType; 

    public IPathfindingAlgorithm<GridXZCell<StackStorage>, StackStorage> GetPathFindingAlgorithm()
    {
        return _pathFindingAlgorithmType switch
        {
            PathFindingAlgorithmType.AStar => new AStarPathFinding<StackStorage>(WorldGrid),
            PathFindingAlgorithmType.DStar => new DStarLitePathFinding<StackStorage>(WorldGrid),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion
    

    void Awake()
    {
        WorldGrid = new GridXZ<GridXZCell<StackStorage>>(_width, _height, _cellWidthSize, _cellHeightSize, transform.position
            ,(grid, x, z) =>
            {
                GridXZCell<StackStorage> storageGridXZCell = new (grid, x, z);
                return storageGridXZCell;
            }
        );

    }

    private void Start()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                GridXZCell<StackStorage> cell = WorldGrid.GetItem(x, z);
                cell.Item = Instantiate(ResourceManager.Instance.StackStorage, WorldGrid.GetWorldPositionOfNearestCell(x,z),Quaternion.identity ,transform);
                cell.Item.Init(WorldGrid, x,z, cell);
                cell.SetAdjacency();
            }
        }
    }
}