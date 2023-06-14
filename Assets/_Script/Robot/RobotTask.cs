using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using Priority_Queue;
using UnityEngine;


[RequireComponent(typeof(Robot))]
public class RobotTask
{
    public Vector3 GoalCellPosition;
    public readonly Action GoalArrivalAction;
    public int Priority;

    public RobotTask(Vector3 goalCellPosition, Action goalArrivalAction = null, int priority = 0)
    {
        GoalCellPosition = goalCellPosition;
        GoalArrivalAction = goalArrivalAction;
        Priority = priority;
    }
    
}
