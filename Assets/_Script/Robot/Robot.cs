using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shun_Grid_System;
using Shun_State_Machine;
using Shun_Unity_Editor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Script.Robot
{

    public abstract partial class Robot : MonoBehaviour
    {
        [Header("Stat")] 
        private static int _idCount = 0;
        public int Id;

        [Header("Robot State Machine")] 
        [ShowImmutable, SerializeField] protected BaseStateMachine RobotStateMachine;

        protected RobotIdlingState IdlingState;
        protected RobotMovingState ApproachingState;
        protected RobotHandlingState HandlingState;
        protected RobotMovingState DeliveringState;
        protected RobotJammingState JammingState;
        protected RobotRedirectingState RedirectingState;
        public IState CurrentRobotState => RobotStateMachine.GetCurrentState();

        
        [Header("Grid")]
        protected internal GridXZ<CellItem> CurrentGrid;
        public GridXZCell<CellItem> NextCell;
        public GridXZCell<CellItem> LastCell;
        
        public Vector3 NextCellPosition => CurrentGrid.GetWorldPositionOfNearestCell(NextCell);
        public Vector3 LastCellPosition => CurrentGrid.GetWorldPositionOfNearestCell(LastCell);
        public Vector3 GoalCellPosition
        {
            get
            {
                if (MovingPath == null || MovingPath.Count == 0)
                {
                    return transform.position;
                }
                return CurrentGrid.GetWorldPositionOfNearestCell(MovingPath.Last.Value);
            }
        }

        public bool IsMidwayMove = true;
        public LinkedList<GridXZCell<CellItem>> MovingPath;
        protected GridXZCell<CellItem> GoalCell
        {
            get
            {
                if (MovingPath == null || MovingPath.Count == 0)
                {
                    return null;
                }
                return MovingPath.Last.Value;
            }
        }

        [Header("Movement")] 
        public float MaxMovementSpeed = 1f;
        protected List<Robot> NearbyRobots = new();

        [Header("Task")] 
        public BinTransportTask CurrentBinTransportTask; 
        [ShowImmutable, SerializeField] protected Bin HoldingBin;
        
        [Header("Cable and Hook Transform")]
        [SerializeField] protected Transform CableTransform;
        [SerializeField] protected Transform HookTransform;    
        [SerializeField] protected Transform TopHookCeilingTransform;
        [SerializeField] protected Transform BinHookPlaceTransform;


        #region INITIALIZE

        protected void Awake()
        {
            Id = _idCount;
            _idCount++;
        }
        
        private void Start()
        {
            InitializeGrid();
            InitializeComponents();
            InitializeState();
        }

        private void FixedUpdate()
        {
            RobotStateMachine.FixedUpdate();
        }

        private void InitializeGrid()
        {
            CurrentGrid = MapManager.Instance.WorldGrid;
            
            LastCell = NextCell = CurrentGrid.GetCell(transform.position);
        }

        protected abstract void InitializeComponents();

        protected abstract void InitializeState();

        #endregion

        /// <summary>
        /// Using the Template Method Pattern to store the function that the different type of robot can override for its own implementation
        /// </summary>
        /// <param name="requestedRobot"></param>

        #region REDIRECT_FUNCTIONS
        
        
        public bool RedirectRequest(Robot requestedRobot, Vector3 requestedRobotNextCellPosition, Vector3 requestedRobotGoalCellPosition)
        {
            
            //bool result = RedirectToOrthogonalCell(requestedRobot, requestedRobotNextCellPosition, requestedRobotGoalCellPosition);

            return RedirectToLowestWeightCell(requestedRobot, requestedRobotNextCellPosition, requestedRobotGoalCellPosition);
            
        }
        
        protected void RedirectToNearestCell()
        {
            Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position);
        
            //Debug.Log( gameObject.name+ " Redirect To Nearest Cell " + nearestCellPosition);
            
            RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NearestCell, nearestCellPosition, SetToJam);
            RobotStateMachine.SetToState(RedirectingState, robotMovingTask );
        }

        
        /// <summary>
        /// This function will be requested when the robot is Idle, or standing on others goal. To move to a other direction
        /// The direction is right, left, backward, prefer mostly the direction which is not blocking
        /// </summary>
        private bool RedirectToOrthogonalCell(Robot requestedRobot, Vector3 requestedRobotNextCellPosition, Vector3 requestedRobotGoalCellPosition)
        {
            if (CurrentRobotState is RobotRedirectingState)
            {
                return true;  // Cannot redirect twice, but this is already redirecting
            }
            
            // Calculate the direction
            Vector3 requestedRobotDistance = (CurrentGrid.GetWorldPositionOfNearestCell(requestedRobot.transform.position) - CurrentGrid.GetWorldPositionOfNearestCell(transform.position)).normalized;
            
            Vector3 roundDirection = new Vector3(
                Mathf.FloorToInt(Mathf.Abs(requestedRobotDistance.x)), // -1 or 1, or 0 when -1<x<1 
                0,
                Mathf.Sign(requestedRobotDistance.z) * Mathf.CeilToInt(Mathf.Abs(requestedRobotDistance.z)) // 0 or -1 when -1<=z<0 or -1 when 0<z<=1
            );
            Vector3 orthogonalDirection = Vector3.Cross(Vector3.up, roundDirection).normalized; // find the orthogonal vector
            
            
            // Check validity and detect obstacles for redirecting right, left, and backward
            bool goRightValid = IsValidRedirectPosition(orthogonalDirection * -1, requestedRobotDistance, requestedRobotNextCellPosition, out Vector3 redirectRightGoalCellPosition, out bool isBlockRight);
            bool goLeftValid = IsValidRedirectPosition(orthogonalDirection, requestedRobotDistance, requestedRobotNextCellPosition, out Vector3 redirectLeftGoalCellPosition, out bool isBlockLeft);
            bool goBackwardValid = IsValidRedirectPosition(roundDirection * -1, requestedRobotDistance, requestedRobotNextCellPosition, out Vector3 redirectBackwardGoalCellPosition, out bool isBlockBackward); 
            bool goForwardValid = IsValidRedirectPosition(roundDirection, requestedRobotDistance, requestedRobotNextCellPosition, out Vector3 redirectForwardGoalCellPosition, out bool isBlockForward);
            
            // Determine the final redirect goal position based on validity and obstacles
            Vector3 redirectGoalCellPosition;
            List<Vector3> potentialRedirectGoalCells = new();
            
            // Select randomly no blocking redirect goal
            if (goRightValid && ! isBlockRight)
                potentialRedirectGoalCells.Add(redirectRightGoalCellPosition); 
            if (goLeftValid && ! isBlockLeft)
                potentialRedirectGoalCells.Add(redirectLeftGoalCellPosition);
            if (goBackwardValid && ! isBlockBackward)
                potentialRedirectGoalCells.Add(redirectBackwardGoalCellPosition);
            if (goForwardValid && ! isBlockForward)
                potentialRedirectGoalCells.Add(redirectForwardGoalCellPosition);
            
            if (potentialRedirectGoalCells.Count != 0) // There is a non-blocking redirect goal
            {
                redirectGoalCellPosition = potentialRedirectGoalCells[Random.Range(0, potentialRedirectGoalCells.Count)];
            }
            else // All redirect goal are block, randomly choose a path that is valid and may redirect other if needed
            {
                if (goRightValid) potentialRedirectGoalCells.Add(redirectRightGoalCellPosition);
                if (goLeftValid) potentialRedirectGoalCells.Add( redirectLeftGoalCellPosition);
                if (goBackwardValid) potentialRedirectGoalCells.Add( redirectBackwardGoalCellPosition);
                if (goForwardValid) potentialRedirectGoalCells.Add(redirectForwardGoalCellPosition);
                
                if (potentialRedirectGoalCells.Count == 0) // No valid path was found either (usually at corner)
                {
                    RedirectToNearestCell(); // Redirect to fit the cell and wait 
                    return false;
                }

                redirectGoalCellPosition = potentialRedirectGoalCells[Random.Range(0, potentialRedirectGoalCells.Count)];
            }

            //Debug.Log(requestedRobot.gameObject.name + " requested to move " + gameObject.name + " from " + CurrentGrid.GetIndex(transform.position) + " to " + CurrentGrid.GetIndex(redirectGoalCellPosition));
            RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NearestCell, redirectGoalCellPosition, SetToJam);
            RobotStateMachine.SetToState(RedirectingState, robotMovingTask );
            return true;
        }
        private bool IsValidRedirectPosition(Vector3 direction, Vector3 exceptDirection, Vector3 detectedRobotGoalPosition, out Vector3 redirectGoalCellPosition, out bool isBlockAhead)
        {
            var redirectIndex = CurrentGrid.GetIndex(transform.position + direction * 1);
            redirectGoalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(redirectIndex.x, redirectIndex.y);
            isBlockAhead = false;
            
            foreach (var nearbyRobot in NearbyRobots)
            {
                bool isBlockingGoal = RobotUtility.CheckRobotBlockAHead(nearbyRobot, redirectGoalCellPosition);
                if (isBlockingGoal)
                {
                    switch (nearbyRobot.CurrentRobotState)
                    {
                        case RobotJammingState _:
                            isBlockAhead = false;
                            break;
                        case RobotHandlingState _:
                            isBlockAhead = true;
                            return false;
                        default:
                            isBlockAhead = true;
                            break;
                    }
                }
            }
            
            return CurrentGrid.CheckValidCell(redirectIndex.x, redirectIndex.y) && exceptDirection != direction && detectedRobotGoalPosition != redirectGoalCellPosition;
        }
        
        /// <summary>
        /// This function will be requested when the robot is Idle, or standing on others goal. To move to a other direction
        /// The direction is right, left, backward, prefer mostly the direction which is not blocking
        /// </summary>
        private bool RedirectToLowestWeightCell(Robot requestedRobot, Vector3 requestedRobotNextCellPosition, Vector3 requestedRobotGoalCellPosition)
        {
            if (CurrentRobotState is RobotRedirectingState)
            {
                return true;  // Cannot redirect twice, but this is already redirecting
            }

            Dictionary<GridXZCell<CellItem>, double> weightCellToCosts = new();
            List<GridXZCell<CellItem>> allRobotObstacles = GetAllRobotObstacleCells();
            double weight = 999;
            
            weightCellToCosts[CurrentGrid.GetCell(requestedRobotNextCellPosition)] = weight;
            weightCellToCosts[requestedRobot.NextCell] = weight;

            if (requestedRobot.MovingPath != null)
            {
                foreach (GridXZCell<CellItem> cell in requestedRobot.MovingPath)
                {
                    weightCellToCosts[cell] = weight;
                }
            }
            DijkstraPathFinding<GridXZ<CellItem>, GridXZCell<CellItem>, CellItem> dijkstraPathFinding = new(CurrentGrid);
            List<GridXZCell<CellItem>> candidateCells = dijkstraPathFinding.LowestCostCellWithWeightMap(CurrentGrid.GetCell(transform.position), weightCellToCosts, allRobotObstacles);
            GridXZCell<CellItem> redirectCell = SelectCellNearestToGoal(candidateCells, GoalCell);
            
            if (redirectCell == null || redirectCell == CurrentGrid.GetCell(transform.position))
            {
                List<GridXZCell<CellItem>> nonIdleRobotObstacle = GetNonIdleRobotObstacleCells();
                candidateCells = dijkstraPathFinding.LowestCostCellWithWeightMap(CurrentGrid.GetCell(transform.position), weightCellToCosts, nonIdleRobotObstacle);
                redirectCell = SelectCellNearestToGoal(candidateCells, GoalCell);
                
                if (redirectCell == null || redirectCell == CurrentGrid.GetCell(transform.position))
                {
                    Debug.Log(requestedRobot.gameObject.name + " fail to redirect " + gameObject.name + " from " + CurrentGrid.GetIndex(transform.position) + " to " + CurrentGrid.GetIndex(CurrentGrid.GetWorldPositionOfNearestCell(redirectCell)));
                    return false;
                }
            }
            
            Debug.Log(requestedRobot.gameObject.name + " redirect to move " + gameObject.name + " from " + CurrentGrid.GetIndex(transform.position) + " to " + CurrentGrid.GetIndex(CurrentGrid.GetWorldPositionOfNearestCell(redirectCell)));
            RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NearestCell,CurrentGrid.GetWorldPositionOfNearestCell(redirectCell) , SetToJam);
            RobotStateMachine.SetToState(RedirectingState, robotMovingTask );
            return true;
        }

        private GridXZCell<CellItem> SelectCellNearestToGoal(List<GridXZCell<CellItem>> candidateCells, GridXZCell<CellItem> goalCell)
        {
            if (candidateCells.Count == 0) return null;
            
            if (goalCell == null)
            {
                return candidateCells[Random.Range(0, candidateCells.Count)];
            }
            
            // Choose the lowest D Star Lite G Cost Cell
            double lowestDStarLiteGCost = double.PositiveInfinity;
            GridXZCell<CellItem> lowestDStarLiteGCostCell = candidateCells[0];

            foreach (var currentCell in candidateCells)
            {
                double currentGCost = GetDistanceCost(currentCell, goalCell);

                if (currentGCost < lowestDStarLiteGCost)
                {
                    lowestDStarLiteGCost = currentGCost;
                    lowestDStarLiteGCostCell = currentCell;
                }
            }

            return lowestDStarLiteGCostCell;
        }
        
        protected virtual double GetDistanceCost(GridXZCell<CellItem> start, GridXZCell<CellItem> end)
        {
            var indexDifferenceAbsolute = CurrentGrid.GetIndexDifferenceAbsolute(start, end);
            return indexDifferenceAbsolute.x + indexDifferenceAbsolute.y;
        }

        #endregion

        #region HANDLE_FUNCTIONS

        protected abstract void ExtendCable(Action finishCallback);

        protected abstract void ContractCable(Action finishCallback);

        protected abstract void MoveCable();
        

        #endregion
        
        #region TASK_FUNCTIONS

        
        public void ApproachBin(BinTransportTask binTransportTask)
        {
            CurrentBinTransportTask = binTransportTask;

            Vector3 goalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell( CurrentBinTransportTask.TargetBinSource );
            RobotMovingTask robotMovingTask = new RobotMovingTask(RobotMovingTask.StartPosition.NextCell, goalCellPosition, ArriveBinSource, 0);
        
            RobotStateMachine.SetToState(ApproachingState, robotMovingTask);
        }

        protected void SetToJam()
        {
            RobotStateMachine.SetToState(JammingState,new RobotJammingTask(){IsWaitingForGoal = false});
        }
        protected void SetToJam(bool isWaitingForGoal)
        {
            RobotStateMachine.SetToState(JammingState,new RobotJammingTask(){IsWaitingForGoal = isWaitingForGoal});
        }
        
        protected void ArriveBinSource()
        {
            RobotStateMachine.SetToState(HandlingState);
        }
        
        
        protected void ArriveBinDestination()
        {
            DistributionManager.Instance.ArriveDestination(this, CurrentBinTransportTask);
            
            RobotStateMachine.SetToState(HandlingState);
        }
        

        #endregion

        #region MOVEMENT
        protected virtual Vector3 MoveAlongGrid()
        {
            // Move
            var lastPosition = transform.position;
            var nextPosition = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);
            transform.position = nextPosition;
            IsMidwayMove = true;
            
            return nextPosition - lastPosition;
        }
        
        
        #endregion
        

        #region DETECTION_AND_COLLIDERS
        
        
        protected virtual List<GridXZCell<CellItem>> GetAllRobotObstacleCells()
        {
            List<GridXZCell<CellItem>> dynamicObstacle = new();
            foreach (var detectedRobot in NearbyRobots)
            {
                dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.LastCellPosition));
                if(detectedRobot.IsMidwayMove) dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.NextCellPosition));
            }
            return dynamicObstacle;
        }
        
        protected virtual List<GridXZCell<CellItem>> GetNonIdleRobotObstacleCells()
        {
            List<GridXZCell<CellItem>> dynamicObstacle = new();
            foreach (var detectedRobot in NearbyRobots)
            {
                if (detectedRobot.CurrentRobotState is RobotIdlingState) continue;
                dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.LastCellPosition));
                if(detectedRobot.IsMidwayMove) dynamicObstacle.Add(CurrentGrid.GetCell(detectedRobot.NextCellPosition));
            }
            return dynamicObstacle;
        }
        
        protected abstract void DetectNearByRobot();

        protected abstract bool CheckRobotSafeDistance(Robot checkRobot);
        
        private void OnCollisionEnter(Collision other)
        {
            DebugUIManager.Instance.AddCollision();
            //Debug.Log($"{gameObject.name} Collide with {other.gameObject.name}");
            #if UNITY_EDITOR
            // UnityEditor.EditorApplication.isPaused = true;
            #endif
        }
        
        #endregion

    }
}