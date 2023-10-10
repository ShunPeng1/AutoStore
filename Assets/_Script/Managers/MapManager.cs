using System;
using System.Collections;
using System.Collections.Generic;
using Shun_Grid_System;
using UnityEngine;
using UnityUtilities;

public class MapManager : SingletonMonoBehaviour<MapManager>
{
    
    [SerializeField] private Transform _mapParent;
    
    [Header("Grid")]
    [SerializeField] private int _width = 20, _length = 20, _stackSize = 10;
    [SerializeField] private float _cellWidthSize = 1f, _cellLengthSize = 1f, _stackDepthSize = 1f;

    [Header("Global Traffic Light")] 
    [SerializeField] private float _greenLightDuration = 10f;
    [SerializeField] private float _yellowLightDuration = 2f;
    
    public GridXZ<CellItem> WorldGrid;

    [SerializeField, HideInInspector] private List<StackFrame> _stackStorages = new List<StackFrame>();
    private Vector2Int[] _adjacencyDirections = new[] // Clockwise Direction
    {
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, -1),  // Down 
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(0, 1),   // Up
    };
    
    [Header("Map Prefabs")]
    [SerializeField] private StackFrame _stackFramePrefab;

    
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
        WorldGrid = new GridXZ<CellItem>(_width, _length, _cellWidthSize, _cellLengthSize, _mapParent.position);
        
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _length; z++)
            {
                GridXZCell<CellItem> storageGridXZCell = new (WorldGrid, x, z);
                WorldGrid.SetCell(storageGridXZCell, x,z);

                var cellItem = new CellItem(WorldGrid, storageGridXZCell, _stackSize, _stackDepthSize);

                storageGridXZCell.Item = cellItem;
            }
        }
    }

    private void InitializeAdjacency()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _length; z++)
            {
                var cell = WorldGrid.GetCell(x, z);
                
                /*
                foreach (var direction in _adjacencyDirections)
                {
                    var adjacentCell = WorldGrid.GetCell(cell.XIndex + direction.x, cell.YIndex + direction.y);
                    cell.SetAdjacencyCell(adjacentCell);
                }
                */
                
                // A 2 lane traffic like in real life where you cannot go in reverse direction in a forward lane
                int xMultiplier = z % 2 == 0 ? 1 : -1;
                var xAdjacentCell = WorldGrid.GetCell(cell.XIndex + xMultiplier, cell.YIndex);
                if(xAdjacentCell != null) cell.SetDirectionalAdjacencyCell(xAdjacentCell);
                
                int zMultiplier = x % 2 == 0 ? -1 : 1;
                var zAdjacentCell = WorldGrid.GetCell(cell.XIndex , cell.YIndex + zMultiplier);
                if(zAdjacentCell != null) cell.SetDirectionalAdjacencyCell(zAdjacentCell);
                
            }
        }
    }

    public bool IsMovableFromDirection(int x, int z)
    {
        float totalLightDuration = _greenLightDuration * 2 + _yellowLightDuration * 2;
        if (Time.time/totalLightDuration < _greenLightDuration)
        {
            return x == 0 && z != 0;
        }
        
        if (Time.time / totalLightDuration < _greenLightDuration + _yellowLightDuration) return false;
        
        if (Time.time / totalLightDuration < _greenLightDuration * 2 + _yellowLightDuration)
        {
            return x != 0 && z == 0;
        }
        
        return false;
        
    }

    public void SpawnStackStorage()
    {
        // Delete any existing spawned objects.
        DeleteStackStorage();

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _stackSize; y++)
            {
                for (int z = 0; z < _length; z++)
                {
                    Vector3 spawnPosition = GetStackWorldPosition(x, z, y);
                    StackFrame stackFrame = Instantiate(_stackFramePrefab, spawnPosition, Quaternion.identity, _mapParent).GetComponent<StackFrame>();
                    _stackStorages.Add(stackFrame);
                }
            }
        }
    }

    public void DeleteStackStorage()
    {
        if (_stackStorages == null)
            return;

        foreach (StackFrame stackFrame in _stackStorages)
        {
            if (stackFrame != null)
            {
                DestroyImmediate(stackFrame.gameObject); // Use DestroyImmediate in the editor.
            }
        }
        
        _stackStorages.Clear();
    }


    private Vector3 GetStackWorldPosition(int x, int z, int index)
    {
        return new Vector3(x * _cellWidthSize, 0,z * _cellLengthSize) + _mapParent.position + Vector3.down * ((index + 1) * _stackDepthSize);
    }
    
    
    
}