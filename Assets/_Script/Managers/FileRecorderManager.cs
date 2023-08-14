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
            public RobotStateEnum RobotStateEnum;
            public bool IsBetween2Cell;
            public RobotRecord(float currentTime, Vector3 lastCellPosition, Vector3 goalCellPosition, RobotStateEnum robotStateEnum, bool isBetween2Cell)
            {
                CurrentTime = currentTime;
                LastCellPosition = lastCellPosition;
                GoalCellPosition = goalCellPosition;
                RobotStateEnum = robotStateEnum;
                IsBetween2Cell = isBetween2Cell;
            }
        }
        public class ResultRecord
        {
            public readonly float ActualTime;
            public readonly float AssumptionTime;

            public int PickUpX;
            public int PickUpZ;
            public int DropDownX;
            public int DropDownZ;
            
            public ResultRecord(float actualTime, float assumptionTime, int pickUpX, int pickUpZ, int dropDownX, int dropDownZ)
            {
                ActualTime = actualTime;
                AssumptionTime = assumptionTime;
                PickUpX = pickUpX;
                PickUpZ = pickUpZ;
                DropDownX = dropDownX;
                DropDownZ = dropDownZ;
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

            StartCoroutine("MyUpdate");
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
                        robot.CurrentTask?.GoalCellPosition ?? robot.LastCellPosition,
                        robot.CurrentRobotState, robot.IsMidwayMove);
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
                    writer.WriteLine($"Time: {robotRecord.CurrentTime} CurrentCell: ({robotRecord.LastCellPosition.x},{robotRecord.LastCellPosition.z}) GoalCell: ({robotRecord.GoalCellPosition.x},{robotRecord.GoalCellPosition.z}) CurrentState: {robotRecord.RobotStateEnum.ToString()}");
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
                    robotRecord.GoalCellPosition.x +"," +  robotRecord.GoalCellPosition.z + "," + robotRecord.RobotStateEnum.ToString() + "," + robotRecord.IsBetween2Cell);
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
            tw.WriteLine("Time, Finish");
            tw.Close();

            tw = new StreamWriter(filePath, true);
            //Lay thong tin moi lan duoc thay doi
            
            foreach (var resultRecord in ResultRecords)
            {
                tw.WriteLine(resultRecord.PickUpX + "," + resultRecord.PickUpZ + "," + 
                             resultRecord.DropDownX + "," + resultRecord.DropDownZ + "," +
                             resultRecord.ActualTime + "," + resultRecord.AssumptionTime );
            }

            tw.Close();
        }
    }
}
