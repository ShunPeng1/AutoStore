using System;
using System.Collections;
using System.Collections.Generic;
using Shun_Grid_System;
using UnityEngine;
using UnityUtilities;

public class MapManager : SingletonMonoBehaviour<MapManager>
{
    [SerializeField] private int _width = 20, _height = 20;
    [SerializeField] private float _cellWidthSize = 1f, _cellHeightSize = 1f;
    public GridXZ<CellItem> WorldGrid;

    private Vector2Int[] _adjacencyDirections = new[] // Clockwise Direction
    {
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, -1),  // Down 
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(0, 1),   // Up
    };
    
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

    public IPathfindingAlgorithm<GridXZ<CellItem>,GridXZCell<CellItem>, CellItem> GetPathFindingAlgorithm()
    {
        return _pathFindingAlgorithmType switch
        {
            PathFindingAlgorithmType.AStar => new AStarPathFinding<GridXZ<CellItem>,GridXZCell<CellItem>, CellItem> (WorldGrid),
            PathFindingAlgorithmType.DStar => new DStarLitePathFinding<GridXZ<CellItem>,GridXZCell<CellItem>, CellItem> (WorldGrid),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion
    

    void Awake()
    {
        InitializeGrid();
        InitializeAdjacency();
    }

    private void InitializeGrid()
    {
        WorldGrid = new GridXZ<CellItem>(_width, _height, _cellWidthSize, _cellHeightSize, transform.position);
        
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                GridXZCell<CellItem> storageGridXZCell = new (WorldGrid, x, z);
                WorldGrid.SetCell(storageGridXZCell, x,z);

                storageGridXZCell.Item = new CellItem();

                var stackStorage = Instantiate(ResourceManager.Instance.StackStorage, WorldGrid.GetWorldPositionOfNearestCell(x,z),Quaternion.identity ,transform);
                stackStorage.Initialize(WorldGrid, x,z, storageGridXZCell.Item);
                                
            }
        }
    }

    private void InitializeAdjacency()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                var cell = WorldGrid.GetCell(x, z);
                foreach (var direction in _adjacencyDirections)
                {
                    var adjacentCell = WorldGrid.GetCell(cell.XIndex + direction.x, cell.YIndex + direction.y);
                    cell.SetAdjacencyCell(adjacentCell);
                }
                
            }
        }
    }

    private void Start()
    {
        
    }
}