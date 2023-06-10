using System;
using System.Collections;
using System.Collections.Generic;
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
    
    public abstract class Robot : MonoBehaviour
    {
        [Header("Stat")] 
        public int Id;
        public RobotStateEnum RobotState = RobotStateEnum.Idle;
    
        [Header("Grid")]
        protected GridXZ<GridXZCell<StackStorage>> CurrentGrid;
        protected int XIndex, ZIndex;
        public Vector3 NextCellPosition;
        public Vector3 LastCellPosition;
        public Vector3 GoalCellPosition;
        public Vector3 RedirectGoalCellPosition;

        protected LinkedList<GridXZCell<StackStorage>> MovingPath;
        
        [Header("Movement")] 
        [SerializeField] protected float MaxMovementSpeed = 1f;
        [SerializeField] protected float PreemptiveDistance = 0.05f;
        [SerializeField] protected float JamWaitTime = 5f;
        
        
        [Header("Pathfinding")]
        protected Action ArrivalRedirectGoalAction;
        protected Action ArrivalGoalAction;
        protected IPathfindingAlgorithm<GridXZCell<StackStorage>,StackStorage> PathfindingAlgorithm;


        [Header("Crate ")] 
        protected Crate HoldingCrate;

        [Header("Components")] 
        protected Rigidbody Rigidbody;

        #region Initial

        void Start()
        {
            InitializeGrid();
            InitializePathfinding();
            InitializeComponents();
        }
        
        private void InitializeGrid()
        {
            CurrentGrid = MapManager.Instance.WorldGrid;
            (XIndex, ZIndex) = CurrentGrid.GetXZ(transform.position);
            GoalCellPosition = LastCellPosition = NextCellPosition = transform.position;
        }

        private void InitializePathfinding()
        {
            PathfindingAlgorithm = MapManager.Instance.GetPathFindingAlgorithm();
        }

        private void InitializeComponents()
        {
            Rigidbody = GetComponent<Rigidbody>();
        }

        #endregion
        
        /// <summary>
        /// Using the Template Method Pattern to store the function that the different type of robot can override for its own implementation
        /// </summary>
        /// <param name="requestedRobot"></param>
        #region AssignTask
        
        public abstract void RedirectOrthogonal(Robot requestedRobot);
        
        public abstract void ApproachCrate(Crate crate);
        
        protected abstract void PickUpCrate();
        protected abstract void DropDownCrate();
        
        protected IEnumerator JammingForGoalCell()
        {
            RobotStateEnum lastRobotStateEnum = RobotState;
            RobotState = RobotStateEnum.Jamming;
            
            yield return new WaitForSeconds(JamWaitTime);
            CreatePathFinding(LastCellPosition, GoalCellPosition);
            
            RobotState = lastRobotStateEnum;
        }
        protected IEnumerator Jamming()
        {
            RobotStateEnum lastRobotStateEnum = RobotState;
            RobotState = RobotStateEnum.Jamming;
            yield return new WaitForSeconds(JamWaitTime);
            RobotState = lastRobotStateEnum;
        }

        protected abstract void UpdatePathFinding(List<GridXZCell<StackStorage>> dynamicObstacle);
        protected abstract void CreatePathFinding(Vector3 startPosition, Vector3 endPosition);
        #endregion

        #region Detection
        protected abstract void DetectNearByRobot();

        #endregion

        #region Movement
        protected void MoveAlongGrid()
        {
            if (RobotState is RobotStateEnum.Jamming or RobotStateEnum.Idle) return;

            // Move
            transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);

            // Check Cell
            if (Vector3.Distance(transform.position, NextCellPosition) <= PreemptiveDistance)
            {
                ArriveDestination();
                ExtractNextCellInPath();
            }
        }
        
        /// <summary>
        /// Using a Observer Pattern to store a queue of order and call when the robot reach the goal
        /// ArrivalDestinationFuncs is the queue that store order and Invoke(), then dequeue to get next Function for next goal
        /// </summary>
        /// <returns></returns>
        private void ArriveDestination()
        {
            if (RobotState == RobotStateEnum.Redirecting)
            {
                if (CurrentGrid.GetXZ(transform.position) != CurrentGrid.GetXZ(RedirectGoalCellPosition)) return;
                
                ArrivalRedirectGoalAction?.Invoke();
            }
            else
            {
                if (CurrentGrid.GetXZ(transform.position) != CurrentGrid.GetXZ(GoalCellPosition)) return;
            
                ArrivalGoalAction?.Invoke();
            }
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
                    NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) +
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
                    NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) +
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
            NextCellPosition = CurrentGrid.GetWorldPosition(XIndex, ZIndex) + Vector3.up * transform.position.y;
            //Debug.Log(gameObject.name + " Get Next Cell " + NextCellPosition);
        }
        
        
        public GridXZCell<StackStorage> GetCurrentGridCell()
        {
            return CurrentGrid.GetItem(XIndex, ZIndex);
        }
        
        #endregion
    }
}