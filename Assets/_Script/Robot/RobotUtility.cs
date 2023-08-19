using Shun_Grid_System;
using UnityEngine;

namespace _Script.Robot
{
    public static class RobotUtility
    {
        public static bool CheckRobotBlockAHead(Robot detectedRobot, Vector3 checkPosition)
        {
            return (checkPosition == detectedRobot.NextCellPosition && detectedRobot.IsMidwayMove) ||
                   checkPosition == detectedRobot.LastCellPosition; // definitely being block by detected robot's last cell or next cell
        }
        
        public static bool CheckRobotBlockGoal(Robot robot, RobotMovingTask robotMovingTask)
        {
            return robotMovingTask != null && 
                   (robot.NextCellPosition == robotMovingTask.GoalCellPosition || robot.LastCellPosition == robotMovingTask.GoalCellPosition);
        }
        
        public static bool CheckArriveOnNextCell(Robot robot)
        {
            return Vector3.Distance(robot.transform.position, robot.NextCellPosition) == 0;
        }
        
        public static bool CheckRobotOnGoal(GridXZ<CellItem> grid,Robot robot, RobotMovingTask robotMovingTask)
        {
            return robotMovingTask != null && grid.GetIndex(robot.transform.position) == grid.GetIndex(robotMovingTask.GoalCellPosition);
        }
        
        public static float DotOf2RobotMovingDirection(Robot robot1, Robot robot2)
        {
            return Vector3.Dot(robot1.NextCellPosition - robot1.LastCellPosition,robot2.NextCellPosition - robot2.LastCellPosition);
        }


        
    }
}