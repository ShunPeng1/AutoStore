using System.Collections;
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
            public string RobotState;
            public bool IsBetween2Cell;
            public RobotRecord(float currentTime, Vector3 lastCellPosition, Vector3 goalCellPosition, string robotState, bool isBetween2Cell)
            {
                CurrentTime = currentTime;
                LastCellPosition = lastCellPosition;
                GoalCellPosition = goalCellPosition;
                RobotState = robotState;
                IsBetween2Cell = isBetween2Cell;
            }
        }
        public class ResultRecord
        {
            public readonly float StartTime;
            public readonly float ActualTime;
            public readonly float AssumptionTime;
            public readonly float WaitForGoalTime;
            public readonly float JammingTime;
            
            
            public readonly int StartX;
            public readonly int StartZ;
            public readonly int PickUpX;
            public readonly int PickUpZ;
            public readonly int DropDownX;
            public readonly int DropDownZ;
            
            public readonly int MainStateChangeCount;
            public readonly int RedirectStateChangeCount;
            public readonly int JamStateChangeCount;
    
            public readonly int PathChangeCount;
            public readonly int PathUpdateCount;
            public readonly int PathTurnCount;
            
            public readonly Vector3 TotalDistance;
            
            
            public ResultRecord(
                float startTime, float actualTime, float waitForGoalTime, float jammingTime, float assumptionTime, 
                int startX, int startZ, int pickUpX, int pickUpZ, int dropDownX, int dropDownZ, 
                int mainStateChangeCount, int redirectStateChangeCount, int jamStateChangeCount, int pathChangeCount, int pathUpdateCount,int pathTurnCount, 
                Vector3 totalDistance)
            {
                
                StartTime = startTime;
                ActualTime = actualTime;
                WaitForGoalTime = waitForGoalTime;
                JammingTime = jammingTime;
                AssumptionTime = assumptionTime;
                
                
                StartX = startX;
                StartZ = startZ;
                PickUpX = pickUpX;
                PickUpZ = pickUpZ;
                DropDownX = dropDownX;
                DropDownZ = dropDownZ;
                
                MainStateChangeCount = mainStateChangeCount;
                RedirectStateChangeCount = redirectStateChangeCount;
                JamStateChangeCount = jamStateChangeCount;
                
                PathChangeCount = pathChangeCount;
                PathUpdateCount = pathUpdateCount;
                PathTurnCount = pathTurnCount;
                
                TotalDistance = totalDistance;
            }
        }

        public List<ResultRecord> ResultRecords = new List<ResultRecord>();


        // Start is called before the first frame update
        private void Start()
        {
            _robots = FindObjectsOfType<Robot.Robot>();
            _robotRecordsDictionary = new Dictionary<Robot.Robot, List<RobotRecord>>();
        
            foreach (var robot in _robots)
            {
                _robotRecordsDictionary[robot] = new List<RobotRecord>();
            }

            StartCoroutine(nameof(MyUpdate));
        }

        // Update is called once per frame
        IEnumerator MyUpdate()
        {
            yield return null;
            while (true)
            {
                foreach (var robot in _robots)
                {
                    var record = new RobotRecord(
                        Time.time,
                        robot.LastCellPosition,
                        Vector3.zero, // Have changed
                        robot.CurrentRobotState.ToString(), robot.IsMidwayMove);
                    _robotRecordsDictionary[robot].Add(record);
                }

                yield return new WaitForSeconds(1f);
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
                    writer.WriteLine($"Time: {robotRecord.CurrentTime} CurrentCell: ({robotRecord.LastCellPosition.x},{robotRecord.LastCellPosition.z}) GoalCell: ({robotRecord.GoalCellPosition.x},{robotRecord.GoalCellPosition.z}) CurrentState: {robotRecord.RobotState.ToString()}");
                }
                writer.WriteLine();
            }
        
            writer.Close();
            Debug.Log("Input data exported to file: " + filePath);
        }
    
        private void OnApplicationQuit()
        {
            WriteRobotCSV();
            WriteResultCSV();
        }
        
        public void WriteRobotCSV()
        {   
            string filePath = Application.dataPath + "/OutputRecord/" + SceneManager.GetActiveScene().name + ".csv";
            // neu chua co file thi tao cac cot
            TextWriter tw = new StreamWriter(filePath, false);
            tw.WriteLine("Id, Time, LastX, LastZ, GoalX, GoalZ, RobotState, IsBetween2Cell ");
            tw.Close();

            tw = new StreamWriter(filePath, true);
            //Lay thong tin moi lan duoc thay doi
            foreach (var (robot, robotRecords) in _robotRecordsDictionary)
            {
                foreach (var robotRecord in robotRecords)
                {
                    tw.WriteLine( robot.Id  +","+ robotRecord.CurrentTime.ToString() + "," +  robotRecord.LastCellPosition.x + "," + robotRecord.LastCellPosition.z + "," + 
                    robotRecord.GoalCellPosition.x +"," +  robotRecord.GoalCellPosition.z + "," + robotRecord.RobotState.ToString() + "," + robotRecord.IsBetween2Cell);
                }
                tw.WriteLine();
            }
            tw.Close();
        }
        public void WriteResultCSV()
        {   
            string filePath = Application.dataPath + "/OutputRecord/" + SceneManager.GetActiveScene().name + ".csv";
            // neu chua co file thi tao cac cot
            TextWriter tw = new StreamWriter(filePath, false);
            tw.WriteLine("StartX, StartZ, PickUpX, PickUpZ, DropDownX, DropDownZ, StartTime, ActualTime, AssumptionTime, WaitForGoalTime, JammingTime, MainStateChangeCount, RedirectStateChangeCount, JamStateChangeCount, PathChangeCount, PathUpdateCount, PathTurnCount, TotalDistanceX, TotalDistanceZ");
            tw.Close();

            tw = new StreamWriter(filePath, true);
            //Lay thong tin moi lan duoc thay doi
            
            foreach (var resultRecord in ResultRecords)
            {
                tw.WriteLine(
                    resultRecord.StartX + "," + resultRecord.StartZ + "," +
                    resultRecord.PickUpX + "," + resultRecord.PickUpZ + "," + 
                    resultRecord.DropDownX + "," + resultRecord.DropDownZ + "," +
                    resultRecord.StartTime + "," + resultRecord.ActualTime + "," + resultRecord.AssumptionTime + "," + 
                    resultRecord.WaitForGoalTime + "," + resultRecord.JammingTime + "," + 
                    resultRecord.MainStateChangeCount + "," + resultRecord.RedirectStateChangeCount + "," + resultRecord.JamStateChangeCount + "," + 
                    resultRecord.PathChangeCount + "," + resultRecord.PathUpdateCount + "," + resultRecord.PathTurnCount + "," +
                    resultRecord.TotalDistance.x + "," + resultRecord.TotalDistance.z
                    );
            }

            tw.Close();
        }
    }
}
