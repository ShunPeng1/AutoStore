using UnityEngine;

namespace _Script.Robot
{
    public class RobotPathRenderer : MonoBehaviour
    {
        [SerializeField] private Robot _robot;
        [SerializeField] private LineRenderer _lineRenderer;
        
        
        
        private void Update()
        {
            ShowPath();
        }
        
        void ShowPath()
        {
            if (_robot.CurrentRobotState is Robot.RobotIdlingState || _robot.MovingPath == null)
            {
                _lineRenderer.positionCount = 0;
                return;
            }
        
            _lineRenderer.positionCount = _robot.MovingPath.Count + 2;
            _lineRenderer.SetPosition(0, _robot.transform.position);
            _lineRenderer.SetPosition(1, _robot.NextCellPosition);
            int itr = 2;
            foreach (var cell in _robot.MovingPath)
            {
                _lineRenderer.SetPosition(itr, _robot.CurrentGrid.GetWorldPositionOfNearestCell(cell.XIndex,cell.YIndex));
                itr++;
            }
        }
    }
}