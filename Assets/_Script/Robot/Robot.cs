using System.Collections.Generic;
using UnityEngine;

namespace _Script.Robot
{
    public abstract class Robot : MonoBehaviour
    {
        [Header("Stat")] 
        public int id;
    
        [Header("Movement")] [SerializeField] protected float movementSpeed = 1f;
        [SerializeField] protected Transform pickUpPlace;

        [SerializeField] protected float preemptiveDistance = 0.05f;
        protected int _xIndex, _zIndex;

        protected Vector3 _nextCellPosition;
        protected GridXZ<StackStorageGridCell> _currentGrid;


        [Header("PathFinding")] [SerializeField]
        protected LineRenderer debugLineRenderer;
        protected Vector3 _destinationPosition;
        protected List<StackStorageGridCell> _path;

    
        public enum RobotState
        {
            Idle,
            Delivering,
            Retrieving,
            Jamming
        }
    
        [Header("Crate ")]
        public RobotState robotState = RobotState.Idle;
        protected Crate holdingCrate;

    
        public abstract void TransportCrate(Crate crate);
        public abstract void PickUpCrate();
        public abstract void DropDownCrate();

        public StackStorageGridCell getCurrentGridCell()
        {
            return _currentGrid.GetItem(_xIndex, _zIndex);
        }
    }
}