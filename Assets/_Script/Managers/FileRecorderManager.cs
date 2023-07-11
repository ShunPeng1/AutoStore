using System.Collections.Generic;
using System.IO;
using _Script.Robot;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtilities;

namespace _Script.Managers
{
    public class FileRecorderManager : SingletonMonoBehaviour<FileRecorderManager>
    {
        private Dictionary<Robot.Robot, List<RobotRecord>> _robotRecordsDictionary;
        private Robot.Robot[] _robots;
    
        private class RobotRecord
        {
            public float CurrentTime;
            public Vector3 LastCellPosition;
            public Vector3 GoalCellPosition;
            public RobotStateEnum RobotStateEnum;

            public RobotRecord(float currentTime, Vector3 lastCellPosition, Vector3 goalCellPosition, RobotStateEnum robotStateEnum)
            {
                CurrentTime = currentTime;
                LastCellPosition = lastCellPosition;
                GoalCellPosition = goalCellPosition;
                RobotStateEnum = robotStateEnum;
            }
        }
        
    
        // Start is called before the first frame update
        private void Start()
        {
            _robots = FindObjectsOfType<Robot.Robot>();
            _robotRecordsDictionary = new Dictionary<Robot.Robot, List<RobotRecord>>();
        
            foreach (var robot in _robots)
            {
                _robotRecordsDictionary[robot] = new List<RobotRecord>();
            }  
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            foreach (var robot in _robots)
            {
                var record = new RobotRecord(
                    Time.time, 
                    robot.LastCellPosition, 
                    robot.CurrentTask?.GoalCellPosition ?? robot.LastCellPosition, 
                    robot.CurrentRobotState);
                _robotRecordsDictionary[robot].Add(record);
            }   
        }

        private void ExportToFile()
        {
            string filePath = Application.dataPath + "/OutputRecord/" + SceneManager.GetActiveScene().name + ".txt";
            StreamWriter writer = new StreamWriter(filePath);
            foreach (var (robot, robotRecords) in _robotRecordsDictionary)
            {
                writer.WriteLine("ID: "+ robot.Id);
            
                foreach (var robotRecord in robotRecords)
                {
                    writer.WriteLine($"Time: {robotRecord.CurrentTime} CurrentCell: ({robotRecord.LastCellPosition.x},{robotRecord.LastCellPosition.z}) GoalCell: ({robotRecord.GoalCellPosition.x},{robotRecord.GoalCellPosition.z}) CurrentState: {robotRecord.RobotStateEnum.ToString()}");
                }
                writer.WriteLine();
            }
        
            writer.Close();
            Debug.Log("Input data exported to file: " + filePath);
        }
    
        private void OnApplicationQuit()
        {
            ExportToFile();
        }
    }
}
