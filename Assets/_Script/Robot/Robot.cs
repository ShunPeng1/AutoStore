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
        Handling,
        Approaching,
        Jamming,
        Redirecting
    }
    
    public abstract class Robot : BaseStateMachine<RobotStateEnum>
    {
        [Header("Stat")] 
        private static int _idCount = 0;
        public int Id;

        [Header("Grid")]
        public Vector3 NextCellPosition;
        public Vector3 LastCellPosition;
        protected internal GridXZ<GridXZCell<StackStorage>> CurrentGrid;
        protected int XIndex, ZIndex;
        protected internal LinkedList<GridXZCell<StackStorage>> MovingPath;
        
        [Header("Movement")] 
        [SerializeField] protected float MaxMovementSpeed = 1f;
        [SerializeField] protected float PreemptiveDistance = 0.05f;
        [SerializeField] protected float JamWaitTime = 5f;
        protected Coroutine JamCoroutine;
        
        [Header("Pathfinding")]
        protected IPathfindingAlgorithm<GridXZCell<StackStorage>,StackStorage> PathfindingAlgorithm;
        
        [Header("Task ")]
        [SerializeField] protected Crate HoldingCrate;
        [SerializeField] public RobotTask CurrentTask;
        
        [Header("Components")] 
        protected Rigidbody Rigidbody;
        protected BoxCollider BoxCollider;
        protected SphereCollider SphereCollider;
        
        [SerializeField] protected float BoxColliderSize = 0.9f;
        [SerializeField] protected float CastRadius = 1.5f;
        [SerializeField] protected LayerMask RobotLayerMask;

        #region INITIALIZE

        protected override void Awake()
        {
            base.Awake();
            Id = _idCount;
            _idCount++;
        }
        
        private void Start()
        {
            InitializeGrid();
            InitializeStrategy();
            InitializeComponents();
            InitializeState();
        }

        private void FixedUpdate()
        {
            CurrentBaseState.ExecuteState();
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
            BoxCollider = GetComponent<BoxCollider>();
            SphereCollider = GetComponent<SphereCollider>();
            BoxCollider.size = BoxColliderSize * Vector3.one;
            SphereCollider.radius = CastRadius;
        }
        
        private void InitializeState()
        {
            BaseState<RobotStateEnum> idleState = new(RobotStateEnum.Idle, null, null,
                (_, _) =>
                {
                    if (transform.position != LastCellPosition) 
                        RedirectToNearestCell();
                });
            BaseState<RobotStateEnum> approachingState = new(RobotStateEnum.Approaching,
                (myStateEnum, objects) =>
                {
                    CheckArriveCell();
                    DecideFromRobotDetection(myStateEnum, objects);
                    MoveAlongGrid(myStateEnum, objects);
                }, null, AssignTask);
            BaseState<RobotStateEnum> retrievingState = new(RobotStateEnum.Handling, null, null, AssignTask);
            BaseState<RobotStateEnum> deliveringState = new(RobotStateEnum.Delivering,
                (myStateEnum, objects) =>
                {
                    CheckArriveCell();
                    DecideFromRobotDetection(myStateEnum, objects);
                    MoveAlongGrid(myStateEnum, objects);
                }, null, AssignTask);
            
            BaseState<RobotStateEnum> jammingState = new(RobotStateEnum.Jamming, null, null, AssignTask);
            BaseState<RobotStateEnum> redirectingState = new(RobotStateEnum.Redirecting,
                (myStateEnum, objects) =>
                {
                    CheckArriveCell();
                    DecideFromRobotDetection(myStateEnum, objects);
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

        #region TASK_FUNCTIONS

        private void AssignTask(RobotStateEnum lastRobotState, object [] enterParameters)
        {
            if (enterParameters == null) return;
            
            CurrentTask = enterParameters[0] as RobotTask;
            if (CurrentTask == null) return;
            
            switch (CurrentTask.StartCellPosition)
            {
                case RobotTask.StartPosition.LastCell:
                    CreateInitialPath(LastCellPosition, CurrentTask.GoalCellPosition);
                    ExtractNextCellInPath();
                    break;
                
                case RobotTask.StartPosition.NextCell:
                    CreateInitialPath(NextCellPosition, CurrentTask.GoalCellPosition);
                    MovingPath.RemoveFirst();
                    break;
                
                case RobotTask.StartPosition.NearestCell:
                    Vector3 nearestCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(transform.position) + Vector3.up * transform.position.y;

                    if (nearestCellPosition == LastCellPosition)
                    {
                        CreateInitialPath(nearestCellPosition, CurrentTask.GoalCellPosition);
                        ExtractNextCellInPath();
                    }
                    else if (nearestCellPosition == NextCellPosition)
                    {
                        if(CreateInitialPath(NextCellPosition, CurrentTask.GoalCellPosition))
                            MovingPath.RemoveFirst();
                    }
                    else
                    {
                        Debug.LogError( gameObject.name+" THE NEAREST CELL IS NOT LAST OR NEXT CELL "+ nearestCellPosition);
                    }
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            CheckArriveCell();
        }

        protected void RestoreState()
        {
            var (enterState, exitOldStateParameters,enterNewStateParameters) = StateHistoryStrategy.Restore();
            if (enterState != null)
            {
                SetToState(enterState.MyStateEnum, exitOldStateParameters, enterNewStateParameters);
            }
            else SetToState(RobotStateEnum.Idle);
            
        }

        protected abstract void RedirectToNearestCell();        
        public abstract bool RedirectToOrthogonalCell(Robot requestedRobot, Vector3 exceptionPosition);
        public abstract void ApproachCrate(Crate crate);

        protected void SetToJam()
        {
            if (JamCoroutine != null) StopCoroutine(JamCoroutine);

            if (LastCellPosition == transform.position) NextCellPosition = LastCellPosition;
            JamCoroutine = StartCoroutine(nameof(Jamming));
        }
        
        protected IEnumerator Jamming()
        {
            SetToState(RobotStateEnum.Jamming);

            yield return new WaitForSeconds(JamWaitTime);
            
            RestoreState();
            
        }
        
        protected void ArriveCrateSource()
        {
            StartCoroutine(nameof(PullingUp));
        }
        
        protected IEnumerator PullingUp()
        {
            SetToState(RobotStateEnum.Handling);

            yield return new WaitForSeconds(HoldingCrate.PullUpTime);
            
            
            HoldingCrate.transform.SetParent(transform);

            var goalCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(HoldingCrate.StoringX, HoldingCrate.StoringZ) + Vector3.up * transform.position.y;
        
            RobotTask robotTask = new RobotTask(RobotTask.StartPosition.NextCell, goalCellPosition, ArriveCrateDestination, 0);
        
            SetToState(RobotStateEnum.Delivering, 
                new object[]{CurrentTask}, 
                new object[]{robotTask});
            
        }
        
        protected void ArriveCrateDestination()
        {
            StartCoroutine(nameof(DroppingDown));
        }

        protected IEnumerator DroppingDown()
        {
            SetToState(RobotStateEnum.Handling);

            yield return new WaitForSeconds(HoldingCrate.DropDownTime);
            
            Destroy(HoldingCrate.gameObject);
            HoldingCrate = null;
            DebugUIManager.Instance.AddFinish();
        
            SetToState(RobotStateEnum.Idle, new object[]{CurrentTask});
        }

        #endregion

        #region MOVEMENT
        
        protected abstract void DecideFromRobotDetection(RobotStateEnum currentRobotState, object[] parameters);
        
        protected void MoveAlongGrid(RobotStateEnum currentRobotState, object [] parameters)
        {
            if (CurrentBaseState.MyStateEnum != currentRobotState) return;
            
            // Move
            transform.position = Vector3.MoveTowards(transform.position, NextCellPosition, MaxMovementSpeed * Time.fixedDeltaTime);
            Rigidbody.velocity = Vector3.zero;
        }

        void CheckArriveCell()
        {
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
                var item = CurrentGrid.GetCell(XIndex + (int)horizontal, ZIndex);
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
                var item = CurrentGrid.GetCell(XIndex, ZIndex + (int)vertical);
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
        
        #region PATHS_AND_CELLS
        
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
            var nextNextCell = MovingPath.First.Value;
            MovingPath.RemoveFirst(); // the next standing node
            
            XIndex = nextNextCell.XIndex;
            ZIndex = nextNextCell.ZIndex;

            Vector3 nextNextCellPosition = CurrentGrid.GetWorldPositionOfNearestCell(XIndex, ZIndex) +
                                   Vector3.up * transform.position.y;

            LastCellPosition = NextCellPosition;
            NextCellPosition = nextNextCellPosition;
            //Debug.Log(gameObject.name + " Get Next Cell " + NextCellPosition);
        }
        
        
        public GridXZCell<StackStorage> GetCurrentGridCell()
        {
            return CurrentGrid.GetCell(XIndex, ZIndex);
        }
        
        protected abstract bool UpdateInitialPath(List<GridXZCell<StackStorage>> dynamicObstacle);
        protected abstract bool CreateInitialPath(Vector3 startPosition, Vector3 endPosition);

        
        #endregion

        #region DEBUG

        private void OnCollisionEnter(Collision other)
        {
            DebugUIManager.Instance.AddCollision();
            Debug.Log($"{gameObject.name} Collide with {other.gameObject.name}");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = true;
            #endif
        }

        #endregion

    }
}