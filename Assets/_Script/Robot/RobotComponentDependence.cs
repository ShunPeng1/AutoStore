using System;
using UnityEngine;

namespace _Script.Robot
{
    [RequireComponent(typeof(Robot))]
    public class RobotComponentDependence : MonoBehaviour
    {
        protected Robot Robot;
        private void Awake()
        {
            Robot = GetComponent<Robot>();
        }
    }
}