using System;
using System.Collections;
using System.Collections.Generic;
using _Script.StateMachine;
using UnityEngine;

namespace _Script.Robot
{
    public enum RobotStateEnum
    {
        Idle,
        Delivering,
        Retrieving,
        Approaching,
        Jamming,
        Redirecting
    }
    
    public abstract class Robot : BaseStateMachine<RobotStateEnum>
    {
        [Header("Stat")] 
        public int Id;
        
        [Header("Grid")]
        protected GridXZ<GridXZCell<StackStorage>> CurrentGrid;
        protected int XIndex, ZIndex;
        public Vector3 NextCellPosition;
        public Vector3 LastCellPosition;
        protected LinkedList<GridXZCell<StackStorage>> MovingPath;
        
        [Header("Movement")] 
        [SerializeField] protected float MaxMovementSpeed = 1f;
        [SerializeField] protected float PreemptiveDistance = 0.05f;
        [SerializeField] protected float JamWaitTime = 5f;
        
        [Header("Pathfinding")]
        protected IPathfindingAlgorithm<GridXZCell<StackStorage>,StackStorage> PathfindingAlgorithm;
        
        [Header("Task ")]
        [SerializeField] protected Crate HoldingCrate;
        [SerializeField] protected RobotTask CurrentTask;
        
        
        [Header("Components")] 
        protected Rigidbody Rigidbody;

        #region Initial

        private void Start()
        {
            InitializeGrid();
            InitializeStrategy();
            InitializeComponents();
            InitializeState();
        }
        
        private void InitializeGrid()
        {
            CurrentGrid = MapManager.Instance.WorldGrid;
            (XIndex, ZIndex) = CurrentGrid.GetXZ(transform.position);
            LastCellPosition = NextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(XIndex, ZIndex) + Vector3.up * transform.position.y;
        }

        private void InitializeStrategy()
        {
            PathfindingAlgorithm = MapManager.Instance.GetPathFindingAlgorithm();
            StateHistoryStrategy = new RobotStateHistoryStrategy();
        }

        private void InitializeComponents()
        {
            Rigidbody = GetComponent<Rigidbody>();
        }
        
        private void InitializeState()
        {
            BaseState<RobotStateEnum> idleState = new(RobotStateEnum.Idle, null, null, AssignTask);
            BaseState<RobotStateEnum> approachingState = new(RobotStateEnum.Approaching,
                (myStateEnum, objects) =>
                {
                    DetectNearByRobot(myStateEnum, objects);
                    MoveAlongGrid(myStateEnum, objects);
                }, null, AssignTask);
            BaseState<RobotStateEnum> retrievingState = new(RobotStateEnum.Retrieving, null, null, AssignTask);
            BaseState<RobotStateEnum> deliveringState = new(RobotStateEnum.Delivering,
                (myStateEnum, objects) =>
                {
                    DetectNearByRobot(myStateEnum, objects);
                    MoveAlongGrid(myStateEnum, objects);
                }, null, AssignTask);
            
            BaseState<RobotStateEnum> jammingState = new(RobotStateEnum.Jamming, null, null, null);
            BaseState<RobotStateEnum> redirectingState = new(RobotStateEnum.Redirecting,
                (myStateEnum, objects) =>
                {
                    DetectNearByRobot(myStateEnum, objects);
                    MoveAlongGrid(myStateEnum, objects);
                }, null, AssignTask);
            
            AddState(idleState);
            AddState(approachingState);
            AddState(retrievingState);
            AddState(deliveringState);
            AddState(jammingState);
            AddState(redirectingState);

            CurrentBaseState = idleState;
        }

        #endregion

        /// <summary>
        /// Using the Template Method Pattern to store the function that the different type of robot can override for its own implementation
        /// </summary>
        /// <param name="requestedRobot"></param>

        #region AssignTask

        private void AssignTask(RobotStateEnum lastRobotState, object [] enterParameters)
        {
            if (enterParameters == null) return;
            
            CurrentTask = enterParameters[0] as RobotTask;

            switch (CurrentTask.StartCellPosition)
            {
                case RobotTask.StartPosition.LastCell:
                    CreatePathFinding(LastCellPosition, CurrentTask.GoalCellPosition);
                    ExtractNextCellInPath();
                    break;
                case RobotTask.StartPosition.NextCell:
                    CreatePathFinding(NextCellPosition, CurrentTask.GoalCellPosition);
                    break;
                case RobotTask.StartPosition.NearestCell:
                    Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position);
                    CreatePathFinding(nearestCellPosition, CurrentTask.GoalCellPosition);
                    ExtractNextCellInPath();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void RestoreState()
        {
            var (enterState, exitOldStateParameters,enterNewStateParameters) = StateHistoryStrategy.Restore();
            if(enterState!= null) SetToState(enterState.MyStateEnum, exitOldStateParameters, enterNewStateParameters);
            else SetToState(RobotStateEnum.Idle);
        }  
        
        public abstract void RedirectOrthogonal(Robot requestedRobot);
        
        public abstract void ApproachCrate(Crate crate);
        
        protected abstract void PickUpCrate();
        protected abstract void DropDownCrate();
        
        
        protected IEnumerator JammingForGoalCell()
        {
            RobotTask newTask = new RobotTask(RobotTask.StartPosition.LastCell, CurrentGrid.GetWorldPositionOfNearestCell(transform.position));

            SetToState(RobotStateEnum.Jamming, new object[]{CurrentTask}, new object[]{newTask});

            yield return new WaitForSeconds(JamWaitTime);
            
            RestoreState();
        }
        protected IEnumerator Jamming()
        {
            RobotTask newTask = new RobotTask(RobotTask.StartPosition.LastCell, CurrentGrid.GetWorldPositionOfNearestCell(transform.position));

            SetToState(RobotStateEnum.Jamming, new object[]{CurrentTask}, new object[]{newTask});

            yield return new WaitForSeconds(JamWaitTime);
            
            RestoreState();
        }

        protected abstract void UpdatePathFinding(List<GridXZCell<StackStorage>> dynamicObstacle);
        protected abstract void CreatePathFinding(Vector3 startPosition, Vector3 endPosition);
        #endregion

        #region Detection
        protected abstract void DetectNearByRobot(RobotStateEnum currentRobotState, object [] parameters);

        #endregion

        #region Movement
        protected void MoveAlongGrid(RobotStateEnum currentRobotState, object [] parameters)
        {
            if (CurrentBaseState.MyStateEnum is RobotStateEnum.Jamming) return;
            
            // Move
            transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);
            Rigidbody.velocity = Vector3.zero;
            
            // Check Cell
            if (!(Vector3.Distance(transform.position, NextCellPosition) <= PreemptiveDistance)) return;

            if ( CurrentTask != null && CurrentGrid.GetXZ(transform.position) == CurrentGrid.GetXZ(CurrentTask.GoalCellPosition)) 
                CurrentTask.GoalArrivalAction?.Invoke();
                
            ExtractNextCellInPath();
        }

        protected void PlayerControl()
        {
            float horizontal = Input.GetAxisRaw("Horizontal"), vertical = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(horizontal) == 1f)
            {
                var item = CurrentGrid.GetItem(XIndex + (int)horizontal, ZIndex);
                // If walkable
                if (item != default(GridXZCell<StackStorage>))
                {
                    XIndex += (int)horizontal;
                    NextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(XIndex, ZIndex) +
                                       Vector3.up * transform.position.y;
                }
            }
            else if (Mathf.Abs(vertical) == 1f)
            {
                var item = CurrentGrid.GetItem(XIndex, ZIndex + (int)vertical);
                // If walkable
                if (item != default(GridXZCell<StackStorage>))
                {
                    ZIndex += (int)vertical;
                    NextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(XIndex, ZIndex) +
                                       Vector3.up * transform.position.y;
                }
            }
        }
        
        #endregion


        #region GetCells

        
        /// <summary>
        /// Using Iteration Pattern for getting the cell that the robot travelling when getting form the PathFinding Algorithm
        /// NextCellPosition : the next cell the robot going to travel to
        /// LastCellPosition : the cell that the robot is leaving
        /// This guarantee the robot is between the LastCellPosition and NextCellPosition, which is next to each other
        /// </summary>
        /// <returns></returns>
        protected void ExtractNextCellInPath()
        {
            if (MovingPath == null || MovingPath.Count == 0)
            {
                LastCellPosition = NextCellPosition;
                return;
            }
            var nextDestination = MovingPath.First.Value;
            MovingPath.RemoveFirst(); // the next standing node
            
            XIndex = nextDestination.XIndex;
            ZIndex = nextDestination.ZIndex;
            LastCellPosition = NextCellPosition;
            NextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(XIndex, ZIndex) + Vector3.up * transform.position.y;
            //Debug.Log(gameObject.name + " Get Next Cell " + NextCellPosition);
        }
        
        
        public GridXZCell<StackStorage> GetCurrentGridCell()
        {
            return CurrentGrid.GetItem(XIndex, ZIndex);
        }
        
        #endregion
    }
}