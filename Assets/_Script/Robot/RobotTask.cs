using System;
using System.Collections;
using System.Collections.Generic;
using _Script.Robot;
using Priority_Queue;
using UnityEngine;


[RequireComponent(typeof(Robot))]
public class RobotTask : MonoBehaviour
{
    public class Task
    {
        public Vector3 GoalCellPosition;
        public Func<IEnumerator> GoalArrivalFunction;
        public int Priority;

        public Task( Vector3 goalCellPosition, Func<IEnumerator> goalArrivalFunction, int priority)
        {
            GoalCellPosition = goalCellPosition;
            GoalArrivalFunction = goalArrivalFunction;
            Priority = priority;
        }
    }

    private SimplePriorityQueue<Task, int> _tasks = new ();

    public void AddTask(Vector3 goalCellPosition, Func<IEnumerator> goalArrivalFunction, int priority )
    {
        
    }

    public void ExecuteTask()
    {
        
    }
}
