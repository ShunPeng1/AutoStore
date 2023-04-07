using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class R5Robot : MonoBehaviour
{
    [Header("Grid Movement")] [SerializeField]
    private float movementSpeed = 1f;

    [SerializeField] private float preemptiveDistance = 0.05f;
    [SerializeField]private int _xIndex, _zIndex;

    private Vector3 _moveToPosition;
    private GridXZ<StackStorageGridItem> _currentGrid;


    [SerializeField] private Transform testDestination;
    
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
        var startCell = _currentGrid.GetItem(transform.position);
        var endCell = _currentGrid.GetItem(testDestination.position);
        
        var nextDestination = MapManager.Instance.RequestPath(startCell, endCell)[1];
        
        
        _moveToPosition = nextDestination.stackStorage.transform.position;
        _xIndex = nextDestination.xIndex;
        _zIndex = nextDestination.zIndex;
    }
}