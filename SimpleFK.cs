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
    void Start()
    {

        spherecolors = new List<float[]>();
        for (int i = 0; i < 4; i++)
            spherecolors.Add(new float[] {UnityEngine.Random.Range(0.0f, 1f), UnityEngine.Random.Range(0.0f, 1f),
                                                                                    UnityEngine.Random.Range(0.0f, 1f)});
        jointChain = new List<Transform>();

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
                planner.robot_poses.Add(ForwardKinematics());
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

    public PoseMsg ForwardKinematics () {
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
        sphere.GetComponent<Renderer>().material.color = newColor;
        sphere.transform.position = prevPoint;
        sphere.transform.rotation = rotation;
        sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        return new PoseMsg
        {
            position = prevPoint.To<FLU>(), // SPHERE position 
            orientation = Quaternion.Euler(90, 0, 0).To<FLU>() // home rotation
            // orientation = rotation.To<FLU>() // SPHERE rotation
        };
    }

    // void DrawLine() {
    //     // linepoints = new List<Vector3>();
    //     // LineRenderer Line = GetComponent<LineRenderer>();
    //     int j = 0;
    //     int color = 0;
    //     for (var poseIndex = 0; poseIndex < planner.responseforLine.trajectories.Length; poseIndex++)
    //     {
    //         // For every robot pose in trajectory plan
    //         foreach (var angs in  planner.responseforLine.trajectories[poseIndex].joint_trajectory.points) {
                
    //             var jointPositions = angs.positions;
                    
    //             var result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
    //             // one point is a 6 angles of the joints in this particular positon 
    //             string printing = "";
    //             for (int i = 1; i < jointChain.Count; i++) {
    //                 printing += result[i-1].ToString() + " next ";

    //                 Vector3 anglesEuler = jointChain[i].transform.rotation.eulerAngles;
    //                 anglesEuler.y = result[i-1];// * (180/(float) Math.PI); // in rad so we have to translate it to deg
    //                 jointChain[i].transform.rotation = Quaternion.Euler(anglesEuler);
    //                 // Debug.Log(anglesEuler);
    //             }
    //             j++;
    //             Debug.Log($" {j} Fk: {printing}");

    //             // get the position of the end effector and add it to the line
    //             // add a point to the line 
    //             // Line.positionCount = j;
    //             ForwardKinematics();
    //             // Line.SetPosition(j-1,ForwardKinematics());
    //         }
    //         color++;
    //     }

        // reset the line to prevet creating inf numbers of lines
    //     planner.responseforLine = new MoverServiceResponse();
    // }
        

}

