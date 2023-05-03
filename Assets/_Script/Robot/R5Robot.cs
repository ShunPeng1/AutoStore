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
        GoalCellPosition = NextCellPosition = transform.position;
        yield return null;
        CurrentGrid = MapManager.Instance.storageGrid;
        (XIndex, ZIndex) = CurrentGrid.GetXZ(transform.position);
         
    }

    // Update is called once per frame
    void Update()
    {
        if (CurrentGrid == null) return;
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
        if (RobotState is RobotStateEnum.Idle or RobotStateEnum.Jamming) return;
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
        //Debug.Log("Angle "+Vector3.Angle(hitPosition - transform.position, NextCellPosition - transform.position));
        return (Vector3.Angle(hitPosition - transform.position, NextCellPosition - transform.position) < thresholdAngle);
    }
    
    IEnumerator Jamming()
    {
        RobotStateEnum lastRobotStateEnum = RobotState;
        RobotState = RobotStateEnum.Jamming;
        yield return new WaitForSeconds(jamWait);
        RobotState = lastRobotStateEnum;
    }

    private void MoveAlongGrid()
    {
        if (RobotState is RobotStateEnum.Jamming or RobotStateEnum.Idle) return;
        transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MovementSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, NextCellPosition) <= PreemptiveDistance)
        {
            //PathFinding();
            GetNextCellInPath();
            PickUpCrate();
            DropDownCrate();
        }
    }

    private void PlayerControl()
    {
        float horizontal = Input.GetAxisRaw("Horizontal"), vertical = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(horizontal) == 1f)
        {
            var item = CurrentGrid.GetItem(XIndex + (int)horizontal, ZIndex);
            // If walkable
            if (item != default(GridXZCell))
            {
                XIndex += (int)horizontal;
                NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) +
                                  Vector3.up * transform.position.y;
            }
        }
        else if (Mathf.Abs(vertical) == 1f)
        {
            var item = CurrentGrid.GetItem(XIndex, ZIndex + (int)vertical);
            // If walkable
            if (item != default(GridXZCell))
            {
                ZIndex += (int)vertical;
                NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) +
                                  Vector3.up * transform.position.y;
            }
        }
    }

    private void GetNextCellInPath()
    {
        if (MovingPath.Count == 0) return;
        var nextDestination = MovingPath.First.Value;
        MovingPath.RemoveFirst(); // the next standing node
        
        XIndex = nextDestination.xIndex;
        ZIndex = nextDestination.zIndex;
        NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) + Vector3.up * transform.position.y;

    }

    private void PathFinding()
    {
        var startCell = CurrentGrid.GetItem(XIndex, ZIndex);
        var endCell = CurrentGrid.GetItem(GoalCellPosition);

        MovingPath = MapManager.Instance.RequestPath(startCell, endCell);
        if (MovingPath == null || MovingPath.Count <= 1) return;

        MovingPath.RemoveFirst(); // the current standing node
      
        GetNextCellInPath();
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }


    void ShowPath()
    {
        if (RobotState == RobotStateEnum.Idle) return;
        
        DebugLineRenderer.positionCount = MovingPath.Count + 1;
        DebugLineRenderer.SetPosition(0, transform.position);

        int itr = 1;
        foreach (var cell in MovingPath)
        {
            DebugLineRenderer.SetPosition(itr, cell.stackStorage.transform.position);
            itr++;
        }
    }

    public override void ApproachCrate(Crate crate)
    {
        HoldingCrate = crate;
        GoalCellPosition = crate.transform.position;
        RobotState = RobotStateEnum.Retrieving;

        PathFinding();
    }

    public override void PickUpCrate()
    {
        if (RobotState == RobotStateEnum.Retrieving && CurrentGrid.GetXZ(transform.position) == CurrentGrid.GetXZ(HoldingCrate.transform.position))
        {
            GoalCellPosition = CurrentGrid.GetWorldPosition(HoldingCrate.storingX, HoldingCrate.storingZ);
            HoldingCrate.transform.SetParent(transform);
            RobotState = RobotStateEnum.Delivering;
            
            PathFinding();
        }
    }

    public override void DropDownCrate()
    {
        if (RobotState == RobotStateEnum.Delivering && CurrentGrid.GetXZ(transform.position) == (HoldingCrate.storingX, HoldingCrate.storingZ))
        {
            Destroy(HoldingCrate.gameObject);
            HoldingCrate = null;
            RobotState = RobotStateEnum.Idle;
            
        }
    }
}