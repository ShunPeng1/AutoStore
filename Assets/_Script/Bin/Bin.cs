
using _Script.Robot;
using Shun_Grid_System;
using UnityEngine;
using UnityEngine.Serialization;

public class Bin : MonoBehaviour
{
    private GridXZ<CellItem> _grid;

    public Robot CarryingRobot;
    private GridXZCell<CellItem> _currentCell; 

    
    private void Start()
    {
        _grid = MapManager.Instance.WorldGrid;
        _currentCell = _grid.GetCell(transform.position);
    }

    
}
