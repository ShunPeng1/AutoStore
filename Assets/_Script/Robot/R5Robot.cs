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

    protected override void CreatePathFinding(Vector3 startPosition, Vector3 endPosition)
    {
        throw new NotImplementedException();
    }

    protected override void DetectNearByRobot()
    {
        if (CurrentRobotState is RobotStateEnum.Idle or RobotStateEnum.Jamming) return;
        var deltaCastPosition = tailCast.position - headCast.position;
        var hits = Physics.SphereCastAll(headCast.position, castRadius, deltaCastPosition, deltaCastPosition.magnitude,robotLayerMask);


        List<GridXZCell<StackStorage>> dynamicObstacle = new(); 
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
        var startCell = CurrentGrid.GetItem(NextCellPosition);
        var endCell = CurrentGrid.GetItem(GoalCellPosition);
        
        MovingPath = PathfindingAlgorithm.FirstTimeFindPath(startCell, endCell);

        
        if (MovingPath == null || MovingPath.Count <= 1) return;
        
        ExtractNextCellInPath();
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }
    
    /// <summary>
    /// Make the robot go to the last Cell and find new path with the new obstacle
    /// </summary>
    /// <param name="dynamicObstacle"> List of cell that the obstacle is on </param>
    protected override void UpdatePathFinding(List<GridXZCell<StackStorage>> dynamicObstacle)
    {
        var currentStartCell = CurrentGrid.GetItem(LastCellPosition);
         
        MovingPath = PathfindingAlgorithm.UpdatePathWithDynamicObstacle(currentStartCell, dynamicObstacle);
        
        if (MovingPath == null || MovingPath.Count <= 1) return;
        
        //MovingPath.RemoveFirst(); // the current standing node
        
        ExtractNextCellInPath();
        //Debug.Log("Move to "+ _xIndex + " "+ _zIndex);
    }


    void ShowPath()
    {
        if (CurrentRobotState == RobotStateEnum.Idle || MovingPath == null) return;
        
        _debugLineRenderer.positionCount = MovingPath.Count + 1;
        _debugLineRenderer.SetPosition(0,  transform.position);

        int itr = 1;
        foreach (var cell in MovingPath)
        {
            _debugLineRenderer.SetPosition(itr, CurrentGrid.GetWorldPosition(cell.XIndex, cell.ZIndex));
            itr++;
        }
    }

    public override void RedirectOrthogonal(Robot requestedRobot)
    {
        throw new NotImplementedException();
    }

    public override void ApproachCrate(Crate crate)
    {
        HoldingCrate = crate;
        GoalCellPosition = crate.transform.position;
        CurrentRobotState = RobotStateEnum.Approaching;

        CreatePathFinding();
    }

    protected override void PickUpCrate()
    {
        if (CurrentRobotState != RobotStateEnum.Approaching || CurrentGrid.GetXZ(transform.position) !=
            CurrentGrid.GetXZ(HoldingCrate.transform.position)) return;
        GoalCellPosition = CurrentGrid.GetWorldPosition(HoldingCrate.storingX, HoldingCrate.storingZ);
        HoldingCrate.transform.SetParent(transform);
        CurrentRobotState = RobotStateEnum.Delivering;
            
        CreatePathFinding();
    }

    protected override void DropDownCrate()
    {
        if (CurrentRobotState != RobotStateEnum.Delivering ||
            CurrentGrid.GetXZ(transform.position) != (HoldingCrate.storingX, HoldingCrate.storingZ)) return;
        Destroy(HoldingCrate.gameObject);
        HoldingCrate = null;
        CurrentRobotState = RobotStateEnum.Idle;
        
    }
}