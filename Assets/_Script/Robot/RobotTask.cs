using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using Priority_Queue;
using Shun_State_Machine;
using UnityEngine;


[RequireComponent(typeof(Robot))]
public class RobotTask : IStateParameter
{
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

    public RobotTask(StartPosition startCellPosition,Vector3 goalCellPosition, Action goalArrivalAction = null, int priority = 0)
    {
        StartCellPosition = startCellPosition;
        GoalCellPosition = goalCellPosition;
        GoalArrivalAction = goalArrivalAction;
        Priority = priority;
    }
    
}
