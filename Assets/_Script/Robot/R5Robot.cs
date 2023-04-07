using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class R5Robot : Robot
{
    [Header("Grid Movement")] [SerializeField]
    private float movementSpeed = 1f;

    [SerializeField] private float preemptiveDistance = 0.05f;
    private int _xIndex, _zIndex;

    private Vector3 _moveToPosition;
    private GridXZ<StackStorageGridItem> _currentGrid;


    [Header("PathFinding")]
    [SerializeField] private LineRenderer debugLineRenderer;
    [SerializeField] private Transform testDestination;
    private List<StackStorageGridItem> _path;

    IEnumerator Start()
    {
        _moveToPosition = transform.position;
        yield return null;
        _currentGrid = MapManager.Instance.storageGrid;
        (_xIndex, _zIndex) = _currentGrid.GetXZ(transform.position);
        yield return null;
    }

    // Update is called once per frame
    void Update()
    {
        MoveAlongGrid();
        ShowPath();
    }

    
    private void MoveAlongGrid()
    {
        if(_currentGrid == null) return;
        
        transform.position = Vector3.MoveTowards(transform.position, _moveToPosition, movementSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, _moveToPosition) <= preemptiveDistance)
        {
            PathFinding();
        }
   
    }

    private void PlayerControl()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"), vertical = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(horizontal) == 1f)
        {
            var item = _currentGrid.GetItem(_xIndex + (int)horizontal, _zIndex);
            // If walkable
            if (item != default(StackStorageGridItem))
            {
                _xIndex += (int)horizontal;
                _moveToPosition = _currentGrid.GetWorldPosition(_xIndex, _zIndex) +
                                  Vector3.up * transform.position.y;
            }
        }
        else if (Mathf.Abs(vertical) == 1f)
        {
            var item = _currentGrid.GetItem(_xIndex, _zIndex + (int)vertical);
            // If walkable
            if (item != default(StackStorageGridItem))
            {
                _zIndex += (int)vertical;
                _moveToPosition = _currentGrid.GetWorldPosition(_xIndex, _zIndex) +
                                  Vector3.up * transform.position.y;
            }
        }   
    }

    private void PathFinding()
    {
        var startCell = _currentGrid.GetItem(_xIndex, _zIndex);
        var endCell = _currentGrid.GetItem(testDestination.position);
        
        _path = MapManager.Instance.RequestPath(startCell, endCell);
        if (_path == null || _path.Count <= 1) return;

        var nextDestination = _path[1];

        _xIndex = nextDestination.xIndex;
        _zIndex = nextDestination.zIndex;
        _moveToPosition = _currentGrid.GetWorldPosition(_xIndex, _zIndex) + Vector3.up * transform.position.y;
        
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }
    
    
    void ShowPath()
    {
        debugLineRenderer.positionCount = _path.Count;
        for (int i = 0; i < _path.Count; i++)
        {
            debugLineRenderer.SetPosition(i, _path[i].stackStorage.transform.position);
        }
    }
}