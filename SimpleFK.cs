using UnityEngine;
using System.Collections.Generic;
using System;
using RosMessageTypes.NiryoMoveit;
using System.Linq;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using PandaRobot;

public class SimpleFK : MonoBehaviour
{
    private GameObject robot;
    public List<Transform> jointChain;
    public const string k_TagName = "robot";
    public const string joint_name = "link";
    private int framecount = 0;
    private int framecount_lineupdate = 0;
    public PandaPlanner planner;
    public List<float[]> spherecolors;
    public LineRenderer Line;
    public GameObject waypointObject;
    private List<GameObject> waypoints; // where the waypoints are saved and the position is updated (if they are moved)
    private Dictionary<int,List<Vector3>> posedict; // where initial position of the waypoints is saved
    public Shader shade; 
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
        posedict = new Dictionary<int, List<Vector3>>(); 
        if (!robot)
        {
            Debug.Log("i didn't find robot");
            return;
        }

        foreach (Transform transformation in robot.GetComponentsInChildren<Transform>())
        {   
            if (transformation.tag == joint_name) {              
                jointChain.Add(transformation);
            } 
        }        
    }
    private void Update() {
        framecount_lineupdate++;
        framecount++;
        // dont create a waypoints when picking up the obj or 
        // when going back to the starting point when redoing the trajectory after waypoints were moves 
        if (planner.responseforLine.trajectories.Length > 0 & framecount > 25 & 
                                (planner.colorindex != -1)) { //planner.colorindex != 1 & planner.colorindex != 2
            {
                Debug.Log(planner.colorindex);
                ForwardKinematics();
                // DrawLine();
                framecount = 0; 
            }
        }
        if (framecount_lineupdate > 25 & waypoints.Count > 5) {
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
        // Vector3 axis = new Vector3(0, 1, 0); // rotation around y axis 
        Quaternion rotation = Quaternion.identity;
        // Debug.Log(jointChain.Count);
        for (int i = 1; i < jointChain.Count; i++)
        {
            // Rotates around a new axis
            rotation = jointChain[i-1].transform.rotation; // obtaning rotation quaternion 
            Vector3 nextPoint = prevPoint + rotation * jointChain[i].transform.localPosition;
            prevPoint = nextPoint;
        }
        prevPoint += new Vector3(0, -0.05f, 0); 
        // add way point of the movement dict 
        try {
            posedict[planner.colorindex].Add(prevPoint);
        } 
        catch {
            posedict[planner.colorindex] = new List<Vector3>();
            posedict[planner.colorindex].Add(prevPoint);
        }
        // create a waypoint and disable collider 
        GameObject pt = Instantiate(waypointObject, prevPoint, Quaternion.Euler(0, 90, 0));
        pt.GetComponent<SphereCollider>().enabled = false;
        waypoints.Add(pt);
        // here we add the poses of the waypoints to the planner 
        Line.positionCount++;
        Line.SetPosition(Line.positionCount-1,prevPoint);
    }

    public void sendWaypoints() {
        // TODO MAKE IT WORK WELL :)
         // send waypoints to the puhlisher 
        int k = 0;
        int numofpt = 0;
        foreach (int key in posedict.Keys) {
            foreach (Vector3 vect in posedict[key]) { 
                if (k == 0 | k == waypoints.Count-1 | waypoints[k].transform.position != vect | key == 1 | key == 2) {
                    Vector3 position = waypoints[k].transform.position;
                    position.y -= planner.panda_y_offset; // offset neccessary for the ROS planner 
                    planner.robot_poses.Add(new PoseMsg
                    {
                    position = position.To<FLU>(), // SPHERE position 
                    orientation = Quaternion.Euler(180, 0, 0).To<FLU>() // home rotation
                    // orientation = rotation.To<FLU>() // SPHERE rotation
                    });
                    numofpt++;
                }
                Destroy(waypoints[k]);
                k++;
            }
        }
        Debug.Log($"i sent {numofpt} waypoints");
        waypoints = new List<GameObject>();
        leave_old_line(); // create a copy of a line so that we can see how the trajectory chagned 
        Line.positionCount = 0; // remove all the line verices 
        
    }
        // // send waypoints to the puhlisher 
        // for (int wpnt = 0; wpnt < waypoints.Count; wpnt++) {
        //     // to ensure smoothness of the traj (too many points, too laggy) only include points that changed the position
        //     if (wpnt == 0 | wpnt == waypoints.Count-1 | waypoints[wpnt].transform.position != GetNext(wpnt)) {
        //         planner.robot_poses.Add(new PoseMsg
        //         {
        //             position = waypoints[wpnt].transform.position.To<FLU>(), // SPHERE position 
        //             orientation = Quaternion.Euler(90, 0, 0).To<FLU>() // home rotation
        //             // orientation = rotation.To<FLU>() // SPHERE rotation
        //         });
        //     }
           
        //     // destroy old waypoits
        //     Destroy(waypoints[wpnt]);
        // }
        // waypoints = new List<GameObject>();
        // Line.positionCount = 0; // remove all the line verices 
  
    public void updateLine() {
        // update the lines (since we are moving the waypoints aroind)
        for (int i = 0; i < waypoints.Count; i ++)
            Line.SetPosition(i,waypoints[i].transform.position);
    }  

    public void enable_waypoints() {
        foreach (GameObject waypt in waypoints) {
            // for spehere 
            // enable waypoints in the line so that the user can mvoe them around
            // TODO for the endeff
            try {
                waypt.GetComponent<SphereCollider>().enabled = true;
            } catch {
                Debug.Log("Does not have a collider, wont be able to grab it ");
            }
        }
    }
    public void leave_old_line() { 
        GameObject gObject = new GameObject("OldTrajectory");
        LineRenderer lRend = gObject.AddComponent<LineRenderer>();
        lRend.positionCount = 0;
        for (int i = 0; i < Line.positionCount; i++) {
            lRend.positionCount++;
            lRend.SetPosition(i,Line.GetPosition(i));
        }
        // lRend.SetPosition(Line.Posit());
        
        // lRend.startColor = Color.green;
        // lRend.endColor = Color.green;
        lRend.material = new Material(shade);
        lRend.material.color = Color.green;
        lRend.SetWidth(Line.startWidth, Line.endWidth);
    }
}
    // <!-- <limit effort="1" lower="-3.05433" upper="3.05433" velocity="1.0"/> -->

