using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class R5Robot : Robot
{
    [Header("Movement")] [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private Transform pickUpPlace;

    [SerializeField] private float preemptiveDistance = 0.05f;
    private int _xIndex, _zIndex;

    private Vector3 _nextCellPosition;
    private GridXZ<StackStorageGridItem> _currentGrid;


    [Header("PathFinding")] [SerializeField]
    private LineRenderer debugLineRenderer;
    private Vector3 _destination;
    private List<StackStorageGridItem> _path;

    IEnumerator Start()
    {
        _destination = _nextCellPosition = transform.position;
        yield return null;
        _currentGrid = MapManager.Instance.storageGrid;
        (_xIndex, _zIndex) = _currentGrid.GetXZ(transform.position);
         
    }

    // Update is called once per frame
    void Update()
    {
        if (_currentGrid == null) return;
        MoveAlongGrid();
        ShowPath();
        
    }


    private void MoveAlongGrid()
    {
        transform.position = Vector3.MoveTowards(transform.position, _nextCellPosition, movementSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, _nextCellPosition) <= preemptiveDistance)
        {
            PathFinding();
            PickUpCrate();
            DropDownCrate();
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
                _nextCellPosition = _currentGrid.GetWorldPosition(_xIndex, _zIndex) +
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
                _nextCellPosition = _currentGrid.GetWorldPosition(_xIndex, _zIndex) +
                                  Vector3.up * transform.position.y;
            }
        }
    }

    private void PathFinding()
    {
        var startCell = _currentGrid.GetItem(_xIndex, _zIndex);
        var endCell = _currentGrid.GetItem(_destination);

        _path = MapManager.Instance.RequestPath(startCell, endCell);
        if (_path == null || _path.Count <= 1) return;

        var nextDestination = _path[1];

        _xIndex = nextDestination.xIndex;
        _zIndex = nextDestination.zIndex;
        _nextCellPosition = _currentGrid.GetWorldPosition(_xIndex, _zIndex) + Vector3.up * transform.position.y;

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

    public override void TransportCrate(Crate crate)
    {
        holdingCrate = crate;
        _destination = crate.transform.position;
        robotState = RobotState.Retrieving;
    }

    public override void PickUpCrate()
    {
        if (robotState == RobotState.Retrieving && _currentGrid.GetXZ(transform.position) == _currentGrid.GetXZ(holdingCrate.transform.position))
        {
            _destination = _currentGrid.GetWorldPosition(holdingCrate.storingX, holdingCrate.storingZ);
            holdingCrate.transform.SetParent(transform);
            robotState = RobotState.Delivering;
        }
    }

    public override void DropDownCrate()
    {
        if (robotState == RobotState.Delivering && _currentGrid.GetXZ(transform.position) == (holdingCrate.storingX, holdingCrate.storingZ))
        {
            Destroy(holdingCrate.gameObject);
            holdingCrate = null;
            robotState = RobotState.Idle;
            
        }
    }
}