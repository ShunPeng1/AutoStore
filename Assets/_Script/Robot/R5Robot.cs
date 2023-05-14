using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;
using UnityEngine.Video;

public class R5Robot : Robot
{
    [Header("Pathfinder")] 
    private DStarLitePathFinding _dStarLitePathFinding;
    
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
        CurrentGrid = MapManager.Instance.StorageGrid;
        (XIndex, ZIndex) = CurrentGrid.GetXZ(transform.position);

        //_dStarLitePathFinding = new DStarLitePathFinding(CurrentGrid);
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


        List<GridXZCell> dynamicObstacle = new(); 
        foreach (var hit in hits)
        { 
            var robotHit = hit.collider.gameObject.GetComponent<Robot>();
            if (robotHit == this)
            {
                continue;
            }
            
            if (IsDirectionHeading(hit.transform.position, 45))
            {
                Debug.Log(name+" Jamming with "+ robotHit.gameObject.name);
                
                // TODO avoidance
                //StartCoroutine(nameof(Jamming));

                // Use the current and next cell to be a obstacle
                dynamicObstacle.Add(CurrentGrid.GetItem(robotHit.transform.position));
                dynamicObstacle.Add(CurrentGrid.GetItem(robotHit.NextCellPosition));
                
            }
            
        }
        
        // Update Path base on obstacle
        if (dynamicObstacle.Count == 0) return; 
        MovingPath = _dStarLitePathFinding.UpdatePathDynamicObstacle(
            CurrentGrid.GetItem(transform.position),
            CurrentGrid.GetItem(NextCellPosition),
            dynamicObstacle
        );
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
        if (MovingPath == null ||MovingPath.Count == 0) return;
        var nextDestination = MovingPath.First.Value;
        MovingPath.RemoveFirst(); // the next standing node
        
        XIndex = nextDestination.XIndex;
        ZIndex = nextDestination.ZIndex;
        NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) + Vector3.up * transform.position.y;

    }

    private void CreatePathFinding()
    {
        var startCell = CurrentGrid.GetItem(XIndex, ZIndex);
        var endCell = CurrentGrid.GetItem(GoalCellPosition);

        // TODO Choose a path finding 
        //MovingPath = MapManager.Instance.RequestPath(startCell, endCell);
        _dStarLitePathFinding = new DStarLitePathFinding(CurrentGrid);
        MovingPath = _dStarLitePathFinding.InitializePathFinding(startCell, endCell);

        
        if (MovingPath == null || MovingPath.Count <= 1) return;

        MovingPath.RemoveFirst(); // the current standing node
      
        GetNextCellInPath();
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }


    void ShowPath()
    {
        if (RobotState == RobotStateEnum.Idle || MovingPath == null) return;
        
        DebugLineRenderer.positionCount = MovingPath.Count + 1;
        DebugLineRenderer.SetPosition(0, transform.position);

        int itr = 1;
        foreach (var cell in MovingPath)
        {
            DebugLineRenderer.SetPosition(itr, cell.StackStorage.transform.position);
            itr++;
        }
    }

    public override void ApproachCrate(Crate crate)
    {
        HoldingCrate = crate;
        GoalCellPosition = crate.transform.position;
        RobotState = RobotStateEnum.Retrieving;

        CreatePathFinding();
    }

    public override void PickUpCrate()
    {
        if (RobotState == RobotStateEnum.Retrieving && CurrentGrid.GetXZ(transform.position) == CurrentGrid.GetXZ(HoldingCrate.transform.position))
        {
            GoalCellPosition = CurrentGrid.GetWorldPosition(HoldingCrate.storingX, HoldingCrate.storingZ);
            HoldingCrate.transform.SetParent(transform);
            RobotState = RobotStateEnum.Delivering;
            
            CreatePathFinding();
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