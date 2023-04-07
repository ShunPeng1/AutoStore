using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;
using UnityEngine.Video;

public class R5Robot : Robot
{

    [Header("Casting")] 
    [SerializeField] private Transform headCast;
    [SerializeField] private Transform tailCast;
    [SerializeField] private float castRadius;
    [SerializeField] private LayerMask robotLayerMask;
    [SerializeField, Range(0.5f,5f)] private float jamWait = 1f;
    
    IEnumerator Start()
    {
        _destinationPosition = _nextCellPosition = transform.position;
        yield return null;
        _currentGrid = MapManager.Instance.storageGrid;
        (_xIndex, _zIndex) = _currentGrid.GetXZ(transform.position);
         
    }

    // Update is called once per frame
    void Update()
    {
        if (_currentGrid == null) return;
        DetectNearByRobot();
        MoveAlongGrid();
        ShowPath();
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(headCast.position, castRadius);
        Gizmos.DrawWireSphere(tailCast.position, castRadius);
    }

    private void DetectNearByRobot()
    {
        if (robotState is RobotState.Idle or RobotState.Jamming) return;
        var deltaCastPosition = tailCast.position - headCast.position;
        var hits = Physics.SphereCastAll(headCast.position, castRadius, deltaCastPosition, deltaCastPosition.magnitude,robotLayerMask);

        foreach (var hit in hits)
        { 
            var robotHit = hit.collider.gameObject.GetComponent<Robot>();
            if (robotHit == this)
            {
                continue;
            }
            /*if (id < robotHit.id && robotHit.robotState != RobotState.Idle)
            {
                Debug.Log(name+" Jamming with "+ robotHit.gameObject.name);
                
                return;
            }*/

            if (IsDirectionHeading(hit.transform.position, 45))
            {
                Debug.Log(name+" Jamming with "+ robotHit.gameObject.name);
                StartCoroutine(nameof(Jamming));
            }
            
        }
    }

    bool IsDirectionHeading(Vector3 hitPosition, float thresholdAngle)
    {
        Debug.Log("Angle "+Vector3.Angle(hitPosition - transform.position, _nextCellPosition - transform.position));
        return (Vector3.Angle(hitPosition - transform.position, _nextCellPosition - transform.position) < thresholdAngle);
    }
    
    IEnumerator Jamming()
    {
        RobotState lastRobotState = robotState;
        robotState = RobotState.Jamming;
        yield return new WaitForSeconds(jamWait);
        robotState = lastRobotState;
    }

    private void MoveAlongGrid()
    {
        if (robotState is RobotState.Jamming) return;
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
            if (item != default(StackStorageGridCell))
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
            if (item != default(StackStorageGridCell))
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
        var endCell = _currentGrid.GetItem(_destinationPosition);

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
        _destinationPosition = crate.transform.position;
        robotState = RobotState.Retrieving;
    }

    public override void PickUpCrate()
    {
        if (robotState == RobotState.Retrieving && _currentGrid.GetXZ(transform.position) == _currentGrid.GetXZ(holdingCrate.transform.position))
        {
            _destinationPosition = _currentGrid.GetWorldPosition(holdingCrate.storingX, holdingCrate.storingZ);
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