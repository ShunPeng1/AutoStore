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
        
        public static bool CheckRobotBlockGoal(Robot robot, RobotTask robotTask)
        {
            return robotTask != null && 
                   (robot.NextCellPosition == robotTask.GoalCellPosition || robot.LastCellPosition == robotTask.GoalCellPosition);
        }
        
        
        public static bool CheckRobotOnGoal(Robot robot, RobotTask robotTask)
        {
            return robotTask != null && 
                   robot.NextCellPosition == robotTask.GoalCellPosition &&
                   robot.LastCellPosition == robotTask.GoalCellPosition;
        }
        
        public static float DotOf2RobotMovingDirection(Robot robot1, Robot robot2)
        {
            return Vector3.Dot(robot1.NextCellPosition - robot1.LastCellPosition,robot2.NextCellPosition - robot2.LastCellPosition);
        }


        
    }
}