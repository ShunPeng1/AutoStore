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
            public RobotHandlingState(Robot robot, RobotStateEnum myStateEnum, Action<RobotStateEnum, IStateParameter> executeEvents = null, Action<RobotStateEnum, IStateParameter> exitEvents = null, Action<RobotStateEnum, IStateParameter> enterEvents = null) : base(robot, myStateEnum, executeEvents, exitEvents, enterEvents)
            {
                EnterEvents += BeginHandling;
                ExecuteEvents += HandleCable;
            }

            private void BeginHandling(RobotStateEnum arg1, IStateParameter arg2)
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
            
            private void HandleCable(RobotStateEnum arg1, IStateParameter arg2)
            {
                Robot.MoveCable();
            }
            
            private void HookBin()
            {
                var item = Grid.GetCell(RobotTransform.position).Item;
                Robot.HoldingBin = item.RemoveTopBinFromStack();
                
                Robot.HoldingBin.transform.SetParent(Robot.BinHookPlaceTransform);
                Robot.HoldingBin.transform.localPosition = Vector3.zero;
                
                Robot.ContractCable(SetToDeliveringState);
            }
            
            private void UnhookBin()
            {
                var item = Grid.GetCell(RobotTransform.position).Item;
                
                Robot.HoldingBin.transform.parent = null;
                Robot.HoldingBin = null;
                
                Robot.ContractCable(SetToIdlingState);
            }

            private void SetToIdlingState()
            {
                Robot.CurrentBinTransportTask = null;
                Robot.RobotStateMachine.SetToState(RobotStateEnum.Idling);
            }

            private void SetToDeliveringState()
            {
                Robot.CurrentBinTransportTask.PickUpBin(Robot.HoldingBin);

                var goalCellPosition = Robot.CurrentGrid.GetWorldPositionOfNearestCell(Robot.CurrentBinTransportTask.TargetBinDestination);
                
                RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NextCell, goalCellPosition, Robot.ArriveBinDestination, 0);
                
                Robot.RobotStateMachine.SetToState(RobotStateEnum.Delivering, null, robotMovingTask);

            }
            
            
        }
    }
}   