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
    public TrajectoryPlanner planner;
    public List<float[]> spherecolors;
    public List<PoseMsg> poses_to_sent;
    public List<Vector3> lineposes;
    public LineRenderer Line;
    void Start()
    {
        Line = GetComponent<LineRenderer>();
        spherecolors = new List<float[]>();
        for (int i = 0; i < 4; i++)
            spherecolors.Add(new float[] {UnityEngine.Random.Range(0.0f, 1f), UnityEngine.Random.Range(0.0f, 1f),
                                                                                    UnityEngine.Random.Range(0.0f, 1f)});
        jointChain = new List<Transform>();
        lineposes = new List<Vector3>();
        robot = FindRobotObject();
        
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
        framecount++;
        if (planner.responseforLine.trajectories.Length > 0 & framecount > 10 & 
                                (planner.colorindex != 1 & planner.colorindex != 2)) {
            {
                Debug.Log(planner.colorindex);
                ForwardKinematics();
                // DrawLine();
                framecount = 0; 
            }
        // if (planner.colorindex == 0 & poses_to_sent.Count > 10) {
        //     planner.Publish_many(poses_to_sent);
        // }
        }
        // Debug.Log(planner.responseforLine.trajectories.Length);
        // if (planner.responseforLine.trajectories.Length > 0) {
        //     DrawLine();
        // }
        // if (planner.colorindex == 0 & lineposes.Count > 10) {
        // }
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
        // generate sphere with the point
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.GetComponent<SphereCollider>().enabled = false; 
        Color newColor = new Color(spherecolors[planner.colorindex][0], spherecolors[planner.colorindex][1],
                                                                                 spherecolors[planner.colorindex][2]);
        // adding the line 
        // if (lineposes.Count % 5 == 0) {
        // every 5th pose add waypoint to modify
        sphere.GetComponent<Renderer>().material.color = newColor;
        sphere.transform.position = prevPoint;
        sphere.transform.rotation = rotation;
        sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        planner.robot_poses.Add(new PoseMsg
        {
            position = prevPoint.To<FLU>(), // SPHERE position 
            orientation = Quaternion.Euler(90, 0, 0).To<FLU>() // home rotation
            // orientation = rotation.To<FLU>() // SPHERE rotation
        });
        Line.positionCount++;
        Line.SetPosition(Line.positionCount-1,sphere.transform.position);
        // lineposes.Add(sphere.transform.position);

        // }
    }

    // void DrawLine() {
    //     // linepoints = new List<Vector3>();
        
    //     for (int i = Line.positionCount; i < lineposes.Count; i++)
    //     {      
               
    //     }
    // }
        

}

