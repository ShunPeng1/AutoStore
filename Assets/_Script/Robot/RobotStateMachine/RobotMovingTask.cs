using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using Priority_Queue;
using Shun_State_Machine;
using UnityEngine;


public class RobotMovingTask : ITransitionData
{
    public IState FromState { get; set; }
    public ITransition Transition { get; set; }
    
    public StartPosition StartCellPosition;
    public Vector3 GoalCellPosition;
    public readonly Action GoalArrivalAction;
    public int Priority;
    
    public enum StartPosition
    {
        LastCell,
        NextCell,
        NearestCell
    }

    public RobotMovingTask(StartPosition startCellPosition,Vector3 goalCellPosition, Action goalArrivalAction = null, int priority = 0)
    {
        StartCellPosition = startCellPosition;
        GoalCellPosition = goalCellPosition;
        GoalArrivalAction = goalArrivalAction;
        Priority = priority;
    }

}
