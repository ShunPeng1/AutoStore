using System;
using DG.Tweening;
using Shun_State_Machine;
using UnityEngine;

namespace _Script.Robot
{
    public abstract partial class Robot
    {
        public class RobotHandlingState : RobotState
        {
            public RobotHandlingState(Robot robot, Action<ITransitionData> executeEvents = null, Action<ITransitionData> exitEvents = null, Action<ITransitionData> enterEvents = null) : base(robot, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += BeginHandling;
                ExecuteEvents += HandleCable;
            }

            private void BeginHandling(ITransitionData arg2)
            {
                if (Robot.HoldingBin == null)
                {
                    Robot.ExtendCable(HookBin);
                }
                else
                {
                    Robot.ExtendCable(UnhookBin);
                }

            }
            
            private void HandleCable(ITransitionData arg2)
            {
                Robot.MoveCable();
            }
            
            private void HookBin()
            {
                var item = Grid.GetCell(RobotTransform.position).Item;
                Robot.HoldingBin = item.RemoveTopBinFromStack();

                if (Robot.HoldingBin != null)
                {
                    Robot.HoldingBin.transform.SetParent(Robot.BinHookPlaceTransform);
                    Robot.HoldingBin.transform.localPosition = Vector3.zero;
                
                    Robot.ContractCable(SetToDeliveringState);

                }
                else
                {
                    Robot.ContractCable(SetToIdlingState);
                }
            }
            
            private void UnhookBin()
            {
                var item = Grid.GetCell(RobotTransform.position).Item;
                if (item.AddToStack(Robot.HoldingBin))
                {
                    Robot.HoldingBin.transform.parent = null;
                    Robot.HoldingBin = null;

                    Robot.ContractCable(SetToIdlingState);
                }
                else
                {
                    Robot.HoldingBin.transform.parent = null;
                    DistributionManager.Instance.ReAddInvalidBin(Robot.HoldingBin);
                    Robot.HoldingBin = null;

                    Robot.ContractCable(SetToIdlingState);
                    
                    
                }
            }

            private void SetToIdlingState()
            {
                Robot.CurrentBinTransportTask = null;
                Robot.RobotStateMachine.SetToState(Robot._idlingState);
            }

            private void SetToDeliveringState()
            {
                Robot.CurrentBinTransportTask.PickUpBin(Robot, Robot.HoldingBin);

                var goalCellPosition = Robot.CurrentGrid.GetWorldPositionOfNearestCell(Robot.CurrentBinTransportTask.TargetBinDestination);
                
                RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NextCell, goalCellPosition, Robot.ArriveBinDestination, 0);
                
                Robot.RobotStateMachine.SetToState(Robot._deliveringState, robotMovingTask);

            }
            
            
        }
    }
}   