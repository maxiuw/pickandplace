using System;
using System.Collections;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.NiryoMoveit;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;
using System.Collections.Generic;

public class TrajectoryVizualizer : MonoBehaviour
{
    public TrajectoryPlanner planner;
    // private MoverServiceResponse responses;  
    List<Vector3> linepoints;
    // Start is called before the first frame update
    void Start()
    {
        // this.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (planner.responseforLine.trajectories.Length >= 0) {
            // this.gameObject.SetActive(false);
            DrawLine();
        }
    }

    void DrawLine() {
        linepoints = new List<Vector3>();
        LineRenderer Line = GetComponent<LineRenderer>();
        int j = 0;
        for (var poseIndex = 0; poseIndex < planner.responseforLine.trajectories.Length; poseIndex++)
            {
                // For every robot pose in trajectory plan
                foreach (var t in planner.responseforLine.trajectories[poseIndex].joint_trajectory.points)
                {
                    var jointPositions = t.positions;
                    // linepoints.Add(new Vector3((float) jointPositions[0], (float) jointPositions[1], (float) jointPositions[2]));
                    // linepoints.Add(new Vector3((float) jointPositions[3], (float) jointPositions[4], (float) jointPositions[5]));

                    Line.positionCount = j+2;
                    Line.SetPosition(j, new Vector3((float) jointPositions[0], (float) jointPositions[1], (float) jointPositions[2]));
                    Line.SetPosition(j+1, new Vector3((float) jointPositions[3], (float) jointPositions[4], (float) jointPositions[5]));
                    j += 2;
                    
                    // float x1 = (float) jointPositions[0];
                    // float y1=  (float) jointPositions[1];
                    // float z1 = (float) jointPositions[2];
                    // float x2 = (float) jointPositions[3];
                    // float y2 = (float) jointPositions[4];
                    // float z2 = (float) jointPositions[5];
                    // // var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
                }
            }
        
        planner.responseforLine = new MoverServiceResponse();
    }
    void DestroyLine() {
    }
}
