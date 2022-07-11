using UnityEngine;
using System.Collections.Generic;
using System;
using RosMessageTypes.NiryoMoveit;
using System.Linq;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
public class SimpleFK : MonoBehaviour
{
    private GameObject robot;
    public List<Transform> jointChain;
    public const string k_TagName = "robot";
    public const string joint_name = "link";
    private int framecount = 0;
    private int framecount_lineupdate = 0;
    public TrajectoryPlanner planner;
    public List<float[]> spherecolors;
    public LineRenderer Line;
    public GameObject waypointObject;
    private List<GameObject> waypoints;
    void Start()
    {
        Line = GetComponent<LineRenderer>();
        spherecolors = new List<float[]>();
        for (int i = 0; i < 4; i++)
            spherecolors.Add(new float[] {UnityEngine.Random.Range(0.0f, 1f), UnityEngine.Random.Range(0.0f, 1f),
                                                                                    UnityEngine.Random.Range(0.0f, 1f)});
        jointChain = new List<Transform>();
        robot = FindRobotObject();
        waypoints = new List<GameObject>();
        if (!robot)
        {
            Debug.Log("i didn't find robot");
            return;
        }

        foreach (Transform transformation in robot.GetComponentsInChildren<Transform>())
        {
            
            if (transformation.tag == joint_name) {
                
                jointChain.Add(transformation);
                // angles.Add(transformation.rotation.eulerAngles.y); // adding rotation above y axis 
                // Debug.Log(transformation);
            } 
        }        
    }
    private void Update() {
        framecount_lineupdate++;
        framecount++;
        if (planner.responseforLine.trajectories.Length > 0 & framecount > 10 & 
                                (planner.colorindex != 1 & planner.colorindex != 2)) {
            {
                Debug.Log(planner.colorindex);
                ForwardKinematics();
                // DrawLine();
                framecount = 0; 
            }
        }
        if (framecount_lineupdate > 30 & waypoints.Count > 10) {
            framecount_lineupdate = 0;
            updateLine();
        }
    }

    public static GameObject FindRobotObject()
    {
        try
        {
            GameObject robot = GameObject.FindWithTag(k_TagName);
            if (robot == null)
            {
                Debug.LogWarning($"No GameObject with tag '{k_TagName}' was found.");
            }
            return robot;
        }
        catch (Exception)
        {
            Debug.LogError($"Unable to find tag '{k_TagName}'. " + 
                            $"Add A tag '{k_TagName}' in the Project Settings in Unity Editor.");
        }
        return null;
    }

    public void ForwardKinematics () {
        Vector3 prevPoint = jointChain[0].transform.position;
        Vector3 axis = new Vector3(0, 1, 0); // rotation around y axis 
        Quaternion rotation = Quaternion.identity;
        // Debug.Log(jointChain.Count);
        for (int i = 1; i < jointChain.Count; i++)
        {
            // Rotates around a new axis
            rotation = jointChain[i-1].transform.rotation; // Quaternion.AngleAxis(angles[i - 1], axis);
            Vector3 nextPoint = prevPoint + rotation * jointChain[i].transform.localPosition;
            prevPoint = nextPoint;
        }
        // add way point of the movement 
        waypoints.Add(Instantiate(waypointObject, prevPoint, Quaternion.Euler(0, 90, 0)));
        // here we add the poses of the waypoints to the planner 
        Line.positionCount++;
        Line.SetPosition(Line.positionCount-1,prevPoint);
        // lineposes.Add(sphere.transform.position);

        // }
    }

    public void sendWaypoints() {
        foreach (var wpnt in waypoints) {
            planner.robot_poses.Add(new PoseMsg
            {
                position = wpnt.transform.position.To<FLU>(), // SPHERE position 
                orientation = Quaternion.Euler(90, 0, 0).To<FLU>() // home rotation
                // orientation = rotation.To<FLU>() // SPHERE rotation
            });
        }
    }
    public void updateLine() {
        for (int i = 0; i < waypoints.Count; i ++)
            Line.SetPosition(i,waypoints[i].transform.position);
    }  
}

