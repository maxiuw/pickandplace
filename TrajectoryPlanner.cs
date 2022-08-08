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
// using Mov

public class TrajectoryPlanner : MonoBehaviour
{
    
    // Hardcoded variables
    int k_NumRobotJoints = SourceDestinationPublisher.LinkNames.Length;
    const float k_JointAssignmentWait = 0.1f;
    const float k_PoseAssignmentWait = 0.5f;

    // Variables required for ROS communication
    [SerializeField]
    string m_RosServiceName = "niryo_moveit";
    public string RosServiceName { get => m_RosServiceName; set => m_RosServiceName = value; }
    // string m_RosServiceNameHome = "niryo_moveit_home";
    // public string RosServiceNameHome { get => m_RosServiceNameHome; set => m_RosServiceNameHome = value; }
    string m_simplemoves = "moveit_no_picking";
    public string simplemoves { get => m_simplemoves; set => m_simplemoves = value; }

    [SerializeField]
    GameObject m_NiryoOne;
    public GameObject NiryoOne { get => m_NiryoOne; set => m_NiryoOne = value; }
    [HideInInspector]
    public GameObject m_Target;
    public GameObject Target { get => m_Target; set => m_Target = value; }
    [SerializeField]
    GameObject m_TargetPlacement;
    public GameObject TargetPlacement { get => m_TargetPlacement; set => m_TargetPlacement = value; }

    // Assures that the gripper is always positioned above the m_Target cube before grasping.
    readonly Quaternion m_PickOrientation = Quaternion.Euler(90, 90, 0);
    readonly Vector3 m_PickPoseOffset = Vector3.up * 0.125f;
    // Articulation Bodies
    ArticulationBody[] m_JointArticulationBodies;
    ArticulationBody m_LeftGripper;
    ArticulationBody m_RightGripper;

    // ROS Connector
    ROSConnection m_Ros;

    /// added by maciej
    public ObjReciever Reciever;
    public GameObject[] prefabs;
    [SerializeField]
    private Vector3 homePose;
    [HideInInspector]
    public Stack<string> messagestoshow;
    [HideInInspector]
    public MoverServiceResponse responseforLine;
    public List<Vector3> vectors_for_lines;
    // public string simplemoves = "moveit_no_picking";
    [HideInInspector] 
    public int colorindex = 0;
    public List<PoseMsg> robot_poses;


    /// <summary>
    ///     Find all robot joints in Awake() and add them to the jointArticulationBodies array.
    ///     Find left and right finger joints and assign them to their respective articulation body objects.
    /// </summary>
    void Start()
    {
        // initiate all the variables and find the robot on start 
        messagestoshow = new Stack<string>();
        robot_poses = new List<PoseMsg>();
        // Get ROS connection static instance
        m_Ros = ROSConnection.GetOrCreateInstance();
        // with and without picking responses 

        m_Ros.RegisterRosService<MoverServiceRequest, MoverServiceResponse>(m_RosServiceName);
        m_Ros.RegisterRosService<MoverManyPosesRequest, MoverManyPosesResponse>(m_simplemoves);


        m_JointArticulationBodies = new ArticulationBody[k_NumRobotJoints];

        var linkName = string.Empty;
        for (var i = 0; i < k_NumRobotJoints; i++)
        {
            Debug.Log($"{i}, {linkName}");
            linkName += SourceDestinationPublisher.LinkNames[i];
            m_JointArticulationBodies[i] = m_NiryoOne.transform.Find(linkName).GetComponent<ArticulationBody>();
        }

        // Find left and right fingers
        var rightGripper = linkName + "/tool_link/gripper_base/servo_head/control_rod_right/right_gripper"; // for niryo
        var leftGripper = linkName + "/tool_link/gripper_base/servo_head/control_rod_left/left_gripper";
        // var rightGripper = linkName + "/panda_hand/panda_rightfinger"; // for panda 
        // var leftGripper = linkName + "/panda_hand/panda_leftfinger";
        Debug.Log(leftGripper);
        m_RightGripper = m_NiryoOne.transform.Find(rightGripper).GetComponent<ArticulationBody>();
        m_LeftGripper = m_NiryoOne.transform.Find(leftGripper).GetComponent<ArticulationBody>();
    }

    //     }
    /// <summary>
    ///     Close the gripper
    /// </summary>
    void CloseGripper()
    {
        var leftDrive = m_LeftGripper.xDrive;
        var rightDrive = m_RightGripper.xDrive;

        leftDrive.target = -0.01f;
        rightDrive.target = 0.01f;

        m_LeftGripper.xDrive = leftDrive;
        m_RightGripper.xDrive = rightDrive;
    }

    /// <summary>
    ///     Open the gripper
    /// </summary>
    void OpenGripper()
    {
        var leftDrive = m_LeftGripper.xDrive;
        var rightDrive = m_RightGripper.xDrive;

        leftDrive.target = 0.01f;
        rightDrive.target = -0.01f;

        m_LeftGripper.xDrive = leftDrive;
        m_RightGripper.xDrive = rightDrive;
    }

    /// <summary>
    ///     Get the current values of the robot's joint angles.
    /// </summary>
    /// <returns>NiryoMoveitJoints</returns>
    NiryoMoveitJointsMsg CurrentJointConfig()
    {
        var joints = new NiryoMoveitJointsMsg();

        for (var i = 0; i < k_NumRobotJoints; i++)
        {
            joints.joints[i] = m_JointArticulationBodies[i].jointPosition[0];
        }

        return joints;
    }

    /// <summary>
    ///     Create a new MoverServiceRequest with the current values of the robot's joint angles,
    ///     the target cube's current position and rotation, and the targetPlacement position and rotation.
    ///     Call the MoverService using the ROSConnection and if a trajectory is successfully planned,
    ///     execute the trajectories in a coroutine.
    /// </summary>
    public void SendMeHome()
    {
        var request = new MoverManyPosesRequest(); // this is where you edited a lot of shit
        request.joints_input = CurrentJointConfig();

        Vector3 newObjTransformation = homePose;
        PoseMsg[] poses_to_sent = new PoseMsg[1];
        // Quaternion newObjRotation = Quaternion.Euler(0, 0, 0).To<FLU>();
        poses_to_sent[0] = new PoseMsg
        {
           
            position = (newObjTransformation).To<FLU>(), // home position 
            orientation = Quaternion.Euler(90, 0, 0).To<FLU>() // home rotation
        };
        request.poses = poses_to_sent;
        m_Ros.SendServiceMessage<MoverManyPosesResponse>(simplemoves, request, TrajectoryResponse);
    }
    public void Publish_many()
    {
        colorindex = 10;
        if (robot_poses.Count > 0) {
            var request = new MoverManyPosesRequest(); // this is where you edited a lot of shit
            request.joints_input = CurrentJointConfig();

            Vector3 newObjTransformation = homePose;
            // PoseMsg[] poses_to_sent = new PoseMsg[robot_poses.Count];
            // Quaternion newObjRotation = Quaternion.Euler(0, 0, 0).To<FLU>();
            // for (int i = 0; i < robot_poses.Count; i++) {
            //     poses_to_sent[i] = 
            // }
            
            request.poses = robot_poses.ToArray();
            Debug.Log("I send the service msg");
            m_Ros.SendServiceMessage<MoverServiceResponse>(simplemoves, request, TrajectoryResponse);
            robot_poses = new List<PoseMsg>(); // remove all the poses after request was sent 
        } else {
            Debug.Log("Dont have any poses to publish");
        }
        
    }


    // sending robot to the predefined home pose 
    public void PublishJoints() {
        // dealing with target placement 
        // m_Target.transform.position = new Vector3(m_Target.transform.position.x, 0.63f, m_Target.transform.position.z);
        m_TargetPlacement.GetComponent<Rigidbody>().useGravity = false;
        m_TargetPlacement.GetComponent<BoxCollider>().enabled = false; // so we can move it aroudn in vr but when robot moves the cube ther e it doesnt collide\
        var request = new MoverServiceRequest(); // this is where you edited a lot of shit
        request.joints_input = CurrentJointConfig();
        
        Vector3 newObjTransformation = Reciever.positions.Pop();
        Quaternion newObjRotation = Reciever.rotations.Pop();
        // Vector3 newObjTransformation = m_Target.transform.position;
        // Quaternion newObjRotation = m_Target.transform.rotation;
        // Pick Pose
        newObjTransformation.y = 0.64f;
        request.pick_pose = new PoseMsg
        {
           
            position = (newObjTransformation + m_PickPoseOffset).To<FLU>(), // m_Target.transform.position

            // The hardcoded x/z angles assure that the gripper is always positioned above the target cube before grasping.
            orientation = Quaternion.Euler(90, newObjRotation.eulerAngles.y, 0).To<FLU>() //m_Target.transform
        };
        // for console canvas 
        messagestoshow.Push($"position {newObjTransformation} ort {newObjRotation.eulerAngles.y}");
        Debug.Log($"position {newObjTransformation} ort {newObjRotation.eulerAngles.y}");
        // Place Pose
        Vector3 placepose = m_TargetPlacement.transform.position;
        placepose.y = 0.64f;
        request.place_pose = new PoseMsg
        {
            position = (placepose + m_PickPoseOffset).To<FLU>(),
            orientation = m_PickOrientation.To<FLU>()
        };

        m_Ros.SendServiceMessage<MoverServiceResponse>(m_RosServiceName, request, TrajectoryResponse);
    }

    void TrajectoryResponse(MoverServiceResponse response)
    {
        if (response.trajectories.Length > 0)
        {
            Debug.Log("Trajectory returned.");
            messagestoshow.Push("Trajectory returned.");
            responseforLine = response;
            StartCoroutine(ExecuteTrajectories(response));
        }
        else
        {
            
            // if cannot find the path - remove the cube from the game a remove the id 
            messagestoshow.Push("I could not find the trajectory. Move your cube or the target placement. Automatically destroying the obj");
            int id = Reciever.ids[Reciever.ids.Count-1];
            Destroy(GameObject.Find("cube"+id.ToString()+"(Clone)"));
            Debug.LogError("No trajectory returned from MoverService.");
            Reciever.ids.RemoveAt(Reciever.ids.Count-1);
        }
    }
    void TrajectoryResponse(MoverManyPosesResponse response)
    {
        if (response.trajectories.Length > 0)
        {
            Debug.Log("Trajectory returned.");
            messagestoshow.Push("Trajectory returned.");
            // responseforLine = response;
            StartCoroutine(ExecuteTrajectories(response));
        }
        else
        {
            
            // if cannot find the path - remove the cube from the game a remove the id 
            messagestoshow.Push("I could not find the trajectory. Move your cube or the target placement. Automatically destroying the obj");
            int id = Reciever.ids[Reciever.ids.Count-1];
            Destroy(GameObject.Find("cube"+id.ToString()+"(Clone)"));
            Debug.LogError("No trajectory returned from MoverService.");
            Reciever.ids.RemoveAt(Reciever.ids.Count-1);
        }
    }

    /// <summary>
    ///     Execute the returned trajectories from the MoverService.
    ///     The expectation is that the MoverService will return four trajectory plans,
    ///     PreGrasp, Grasp, PickUp, and Place,
    ///     where each plan is an array of robot poses. A robot pose is the joint angle values
    ///     of the six robot joints.
    ///     Executing a single trajectory will iterate through every robot pose in the array while updating the
    ///     joint values on the robot.
    /// </summary>
    /// <param name="response"> MoverServiceResponse received from niryo_moveit mover service running in ROS</param>
    /// <returns></returns>
    IEnumerator ExecuteTrajectories(MoverServiceResponse response)
    {
        if (response.trajectories != null)
        {                
            // For every trajectory plan returned
            for (var poseIndex = 0; poseIndex < response.trajectories.Length; poseIndex++)
            {
                // colorindex = 0;
                // For every robot pose in trajectory plan
                foreach (var t in response.trajectories[poseIndex].joint_trajectory.points)
                {
                    var jointPositions = t.positions;
                    
                    float[] result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
              
                    // Set the joint values for every joint
                    string printing = "";
                    for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++)
                    {
                        printing += result[joint].ToString() + " next ";
                        // Debug.Log($"my name is {m_JointArticulationBodies[joint].name}");
                        var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
                        joint1XDrive.target = result[joint];
                        m_JointArticulationBodies[joint].xDrive = joint1XDrive;
                    }
                    // Wait for robot to achieve pose for all joint assignments
                    yield return new WaitForSeconds(k_JointAssignmentWait);
                }
                
                // Close the gripper if completed executing the trajectory for the Grasp pose
                if (poseIndex == (int)Poses.Grasp)
                {
                    CloseGripper();
                }

                // Wait for the robot to achieve the final pose from joint assignment
                yield return new WaitForSeconds(k_PoseAssignmentWait);
                colorindex++;
                if (colorindex == 11) 
                    colorindex = 0;
            }
            // All trajectories have been executed, open the gripper to place the target cube
            OpenGripper();
        }
        colorindex = 10; // added to stop generating waypoints 
    }

    IEnumerator ExecuteTrajectories(MoverManyPosesResponse response)
    {
        if (response.trajectories != null)
        {                
            // For every trajectory plan returned
            for (var poseIndex = 0; poseIndex < response.trajectories.Length; poseIndex++)
            {
                // colorindex = 0;
                // For every robot pose in trajectory plan
                foreach (var t in response.trajectories[poseIndex].joint_trajectory.points)
                {
                    var jointPositions = t.positions;
                    
                    float[] result = jointPositions.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
              
                    // Set the joint values for every joint
                    string printing = "";
                    for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++)
                    {
                        printing += result[joint].ToString() + " next ";
                        // Debug.Log($"my name is {m_JointArticulationBodies[joint].name}");
                        var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
                        joint1XDrive.target = result[joint];
                        m_JointArticulationBodies[joint].xDrive = joint1XDrive;
                    }
                    // Wait for robot to achieve pose for all joint assignments
                    yield return new WaitForSeconds(k_JointAssignmentWait);
                }
                
                // Close the gripper if completed executing the trajectory for the Grasp pose
                if (poseIndex == (int)Poses.Grasp)
                {
                    CloseGripper();
                }

                // Wait for the robot to achieve the final pose from joint assignment
                yield return new WaitForSeconds(k_PoseAssignmentWait);
                colorindex++;
                if (colorindex == 11) 
                    colorindex = 0;
            }
            // All trajectories have been executed, open the gripper to place the target cube
            OpenGripper();
        }
        colorindex = 10; // added to stop generating waypoints 
    }

    enum Poses
    {
        PreGrasp,
        Grasp,
        PickUp,
        Place
    }
}

