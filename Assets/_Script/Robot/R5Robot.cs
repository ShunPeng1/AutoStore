using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using UnityEngine;
using UnityEngine.Video;

public class R5Robot : Robot
{
    [Header("Pathfinder")]
    [SerializeField] private LineRenderer _debugLineRenderer;
    private DStarLitePathFinding _dStarLitePathFinding;
    
    [Header("Casting")] 
    [SerializeField] private Transform headCast;
    [SerializeField] private Transform tailCast;
    [SerializeField] private float castRadius;
    [SerializeField] private LayerMask robotLayerMask;
    
    
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

    protected override void DetectNearByRobot()
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
        if (dynamicObstacle.Count != 0) UpdatePathFinding(dynamicObstacle);
    }

    bool IsDirectionHeading(Vector3 hitPosition, float thresholdAngle)
    {
        //Debug.Log("Angle "+Vector3.Angle(hitPosition - transform.position, NextCellPosition - transform.position));
        return (Vector3.Angle(hitPosition - transform.position, NextCellPosition - transform.position) < thresholdAngle);
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
      
        ExtractNextCellInPath();
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }
    
    /// <summary>
    /// Make the robot go to the last Cell and find new path with the new obstacle
    /// </summary>
    /// <param name="dynamicObstacle"> List of cell that the obstacle is on </param>
    private void UpdatePathFinding(List<GridXZCell> dynamicObstacle)
    {
        var currentStartCell = CurrentGrid.GetItem(LastCellPosition);
         
        MovingPath = _dStarLitePathFinding.UpdatePathDynamicObstacle(currentStartCell, dynamicObstacle);
        
        if (MovingPath == null || MovingPath.Count <= 1) return;
        
        //MovingPath.RemoveFirst(); // the current standing node
        
        ExtractNextCellInPath();
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }


    void ShowPath()
    {
        if (RobotState == RobotStateEnum.Idle || MovingPath == null) return;
        
        _debugLineRenderer.positionCount = MovingPath.Count + 1;
        _debugLineRenderer.SetPosition(0,  transform.position);

        int itr = 1;
        foreach (var cell in MovingPath)
        {
            _debugLineRenderer.SetPosition(itr, cell.StackStorage.transform.position);
            itr++;
        }
    }

    public override void IdleRedirect(Robot requestedRobot)
    {
        throw new NotImplementedException();
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