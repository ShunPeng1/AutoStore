using System;
using Shun_Grid_System;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{
    
    public class RobotState : IState
    {
        protected readonly Robot Robot;
        protected readonly Transform RobotTransform;
        protected readonly GridXZ<CellItem> Grid;

        public event Action<ITransitionData> EnterEvents;
        public event Action<ITransitionData> ExecuteEvents;
        public event Action<ITransitionData> ExitEvents;
        
        public RobotState(Robot robot, Action<ITransitionData> executeEvents = null, Action<ITransitionData> exitEvents = null, Action<ITransitionData> enterEvents = null)
        {
            Robot = robot;
            RobotTransform = Robot.transform;
            Grid = Robot.CurrentGrid;
            
            EnterEvents += RecordStateChange;
        }
        
        
        protected virtual void RecordStateChange(ITransitionData stateParameter)
        {
            if (Robot.CurrentBinTransportTask == null) return;
                
            if (Robot.CurrentRobotState is Robot.RobotRedirectingState)
            {
                Robot.CurrentBinTransportTask.RedirectStateChangeCount++;    
            }
            else if (Robot.CurrentRobotState is Robot.RobotJammingState)
            {
                Robot.CurrentBinTransportTask.JamStateChangeCount++;
            }
            else
            {
                Robot.CurrentBinTransportTask.MainStateChangeCount++;
            }
        }

        protected void RecordDistance(Vector3 distance)
        {
            if (Robot.CurrentBinTransportTask == null) return;
                
            Robot.CurrentBinTransportTask.TotalDistance += new Vector3(Mathf.Abs(distance.x), Mathf.Abs(distance.y), Mathf.Abs(distance.z));
        }
        
        protected virtual void RecordPathTurn()
        {
            if (Robot.CurrentBinTransportTask == null) return;
            
            Robot.CurrentBinTransportTask.PathTurnCount++;
        }
        
        protected virtual void RecordPathChange()
        {
            if (Robot.CurrentBinTransportTask == null) return;
            
            Robot.CurrentBinTransportTask.PathChangeCount++;
        }
        
        protected virtual void RecordPathUpdate()
        {
            if (Robot.CurrentBinTransportTask == null) return;
            
            Robot.CurrentBinTransportTask.PathUpdateCount++;
        }

        public virtual void OnEnterState(ITransitionData enterTransitionData = null)
        {
            EnterEvents?.Invoke(enterTransitionData);
        }

        public virtual void OnExitState(ITransitionData exitTransitionData = null)
        {
            ExitEvents?.Invoke(exitTransitionData);
        }

        public virtual void UpdateState()
        {
            Debug.Log("Not Implemented");
        }

        public virtual void FixedUpdateState()
        {
            ExecuteEvents?.Invoke(null);
        }
    }
}