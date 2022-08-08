using System;
using System.Collections;
using System.Linq;
using RosMessageTypes.Geometry;
// using RosMessageTypes.NiryoMoveit;
using RosMessageTypes.Panda;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;
using System.Collections.Generic;
using RosMessageTypes.Moveit;
using RosMessageTypes.Sensor;
public class PandaPlanner : MonoBehaviour
{
    
    // Hardcoded variables
    int k_NumRobotJoints = SourceDestinationPublisher.LinkNames.Length; // has to be adj in the SDP file for diff robots 
    const float k_JointAssignmentWait = 0.1f;
    const float k_PoseAssignmentWait = 0.5f;

    // Variables required for ROS communication
    [SerializeField]
    string m_RosServiceName = "panda_msgs";
    public string RosServiceName { get => m_RosServiceName; set => m_RosServiceName = value; }
    string m_simplemoves = "moveit_many";
    public string simplemoves { get => m_simplemoves; set => m_simplemoves = value; }

    [SerializeField]
    GameObject m_Panda;
    public GameObject Panda { get => m_Panda; set => m_Panda = value; }
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
    public PandaMoverServiceResponse responseforLine;
    public List<Vector3> vectors_for_lines;
    [HideInInspector] 
    public int colorindex = 0;
    public List<PoseMsg> robot_poses;
    public string subscriber_topicname = "/joint_state_unity";
    string movit_results = "/move_group/fake_controller_joint_states"; //"/move_group/result"; 

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
        // initiate services
        // m_Ros.RegisterRosService<PandaMoverServiceRequest, PandaMoverServiceResponse>(m_RosServiceName);
        // m_Ros.RegisterRosService<PandaMoverManyPosesRequest, PandaMoverManyPosesResponse>(simplemoves);
        // initiate subscriber 
        m_Ros.Subscribe<FloatListMsg>(subscriber_topicname, ExecuteTrajectoriesJointState);
        // m_Ros.Subscribe<MoveGroupActionResult>(movit_results, ExectuteMoverResults);
        m_Ros.Subscribe<JointStateMsg>(movit_results, ExectuteMoverResults); 
        // get robot's joints 
        m_JointArticulationBodies = new ArticulationBody[k_NumRobotJoints];

        var linkName = string.Empty;
        for (var i = 0; i < k_NumRobotJoints; i++)
        {
            linkName += SourceDestinationPublisher.LinkNames[i];
            m_JointArticulationBodies[i] = Panda.transform.Find(linkName).GetComponent<ArticulationBody>();
            Debug.Log($"{i}, {linkName}");
        }
        // Find left and right fingers
        // var rightGripper = linkName + "/tool_link/gripper_base/servo_head/control_rod_right/right_gripper"; // for niryo
        // var leftGripper = linkName + "/tool_link/gripper_base/servo_head/control_rod_left/left_gripper";
        var rightGripper = linkName + "/panda_link8/panda_hand/panda_rightfinger"; // for panda 
        var leftGripper = linkName + "/panda_link8/panda_hand/panda_leftfinger";
        m_RightGripper = Panda.transform.Find(rightGripper).GetComponent<ArticulationBody>();
        m_LeftGripper = Panda.transform.Find(leftGripper).GetComponent<ArticulationBody>();
    }
    void Update() {
         
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
    PandaMoveitJointsMsg CurrentJointConfig()
    {
        var joints = new PandaMoveitJointsMsg();

        for (var i = 0; i < k_NumRobotJoints; i++)
        {
            // Debug.Log(i);
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
        var request = new PandaMoverServiceRequest(); 
        request.joints_input = CurrentJointConfig();
        // request.messagename = new RosMessageTypes.Diagnostic.SelfTestResponse {
        //     id = "home"
        // };
        Vector3 newObjTransformation = homePose;
        // Quaternion newObjRotation = Reciever.rotations.Pop();
        // Vector3 newObjTransformation = m_Target.transform.position;
        // Quaternion newObjRotation = m_Target.transform.rotation;
        // Pick Pose
        newObjTransformation.y = 0.64f;
        request.pick_pose = new PoseMsg
        {
           
            position = (newObjTransformation + m_PickPoseOffset).To<FLU>(), // m_Target.transform.position

            // The hardcoded x/z angles assure that the gripper is always positioned above the target cube before grasping.
            orientation = Quaternion.Euler(90, 0, 0).To<FLU>() //m_Target.transform
        };
        // for console canvas 
        messagestoshow.Push($"position {newObjTransformation} ort {0}");
        Debug.Log($"position {newObjTransformation} ort {0}");
        // Place Pose
        Vector3 placepose = m_TargetPlacement.transform.position;
        placepose.y = 0.64f;
        request.place_pose = new PoseMsg
        {
            position = (placepose + m_PickPoseOffset).To<FLU>(),
            orientation = m_PickOrientation.To<FLU>()
        };

        m_Ros.SendServiceMessage<PandaMoverServiceResponse>(m_RosServiceName, request, PandaTrajectoryResponse);
    }
    public void Publish_many()
    {
        colorindex = 10;
        if (robot_poses.Count > 0) {
            var request = new PandaMoverManyPosesRequest(); // this is where you edited a lot of shit
            request.joints_input = CurrentJointConfig();

            // Vector3 newObjTransformation = homePose;
            // PoseMsg[] poses_to_sent = new PoseMsg[robot_poses.Count];
            // Quaternion newObjRotation = Quaternion.Euler(0, 0, 0).To<FLU>();
            // for (int i = 0; i < robot_poses.Count; i++) {
            //     poses_to_sent[i] = 
            // }
            
            request.poses = robot_poses.ToArray();
            Debug.Log("I send the service msg");
            m_Ros.SendServiceMessage<PandaMoverServiceResponse>(simplemoves, request, PandaTrajectoryResponse);
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
        var request = new PandaMoverServiceRequest(); 
        request.joints_input = CurrentJointConfig();
        // request.messagename = new RosMessageTypes.Diagnostic.SelfTestResponse {
        //     id = "home"
        // };
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

        m_Ros.SendServiceMessage<PandaMoverServiceResponse>(m_RosServiceName, request, PandaTrajectoryResponse);
    }

    // void TrajectoryResponseManyPoses(MoverServiceResponse response) {
    //     if (response.trajectories.Length > 0)
    //     {
    //         Debug.Log("Trajectory returned.");
    //         messagestoshow.Push("Trajectory returned.");
    //         responseforLine = response;
    //         StartCoroutine(ExecuteTrajectories(response));
    //     }
    // }


    void PandaTrajectoryResponse(PandaMoverServiceResponse response)
    {
        // Debug.Log(response);
        if (response.trajectories.Length > 0)
        {
            Debug.Log("Trajectory returned.");
            messagestoshow.Push("Trajectory returned.");
            responseforLine = response;
            StartCoroutine(ExecuteTrajectories(response.trajectories));
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
    IEnumerator ExecuteTrajectories(RobotTrajectoryMsg[] response) {
        Debug.Log("YO EXECUTIN MANY TRAJECTORIES");
        if (response != null)
        {                
            // For every trajectory plan returned
            for (var poseIndex = 0; poseIndex < response.Length; poseIndex++)
            {
                // colorindex = 0;
                // For every robot pose in trajectory plan
                foreach (var t in response[poseIndex].joint_trajectory.points)
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

    void ExecuteTrajectoriesJointState(FloatListMsg response) {
        // For every trajectory plan returned
        Debug.Log("I got joint states");
        Debug.Log(response.joints.ToArray());
        // Debug.Log(response.ToString);
        Debug.Log("end of message");
        // RunTrajectories(FloatListMsg response)
        StartCoroutine(RunTrajectories(response));

    }
    IEnumerator RunTrajectories(FloatListMsg response) {
        Debug.Log("I executed runtraj");
        Debug.Log($"JOINTS LENGHT {m_JointArticulationBodies.Length}");
        float[] response_array = response.joints.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
        // double[] response_array = response.joints.ToArray();
        string printing = "";
        // response.data[1]
        for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++) {
            Debug.Log($"joint {joint} position {response_array[joint]}");
            printing += response_array[joint].ToString() + " next ";
            // Debug.Log($"my name is {m_JointArticulationBodies[joint].name}");
            var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
            joint1XDrive.target = response_array[joint];
            m_JointArticulationBodies[joint].xDrive = joint1XDrive;
        }
        yield return new WaitForSeconds(k_JointAssignmentWait);
    }
    /// <summary>
    /// executing the trajectory given by the controller  /move_group/fake_controller_joint_states 
    /// </summary>
    /// <param name="response"> MoverServiceResponse received from niryo_moveit mover service running in ROS</param>
    /// <returns></returns>
    void ExectuteMoverResults(JointStateMsg result) {
        // more details and message structure in the message file
        // getting initial joints state (start) and the joint_trajectory points
        // float[] trajectory_start = result.result.trajectory_start.joint_state.position.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
        // RobotTrajectoryMsg[] joint_states = new RobotTrajectoryMsg[1];
        // joint_states[0] = result.result.planned_trajectory;
        StartCoroutine(RunTrajectories(result.position));


    IEnumerator RunTrajectories(double[] response) {
        Debug.Log("I executed runtraj");
        Debug.Log($"JOINTS LENGHT {m_JointArticulationBodies.Length}");
        float[] response_array = response.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
        // double[] response_array = response.joints.ToArray();
        string printing = "";
        // response.data[1]
        for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++) {
            Debug.Log($"joint {joint} position {response_array[joint]}");
            printing += response_array[joint].ToString() + " next ";
            // Debug.Log($"my name is {m_JointArticulationBodies[joint].name}");
            var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
            joint1XDrive.target = response_array[joint];
            m_JointArticulationBodies[joint].xDrive = joint1XDrive;
        }
        yield return new WaitForSeconds(k_JointAssignmentWait);
    }
    
        // if (result.result.Length > 0)
        // {
        //     Debug.Log("Trajectory returned.");
        //     messagestoshow.Push("Trajectory returned.");
        //     // responseforLine = result;
        //     StartCoroutine(ExecuteTrajectories(result));
        // }
    }
    // void ExectuteMoverResults(MoveGroupActionResult result) {
    //     // more details and message structure in the message file
    //     // getting initial joints state (start) and the joint_trajectory points
    //     float[] trajectory_start = result.result.trajectory_start.joint_state.position.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
    //     RobotTrajectoryMsg[] joint_states = new RobotTrajectoryMsg[1];
    //     joint_states[0] = result.result.planned_trajectory;
    //     StartCoroutine(ExecuteTrajectories(joint_states));
    //     // if (result.result.Length > 0)
    //     // {
    //     //     Debug.Log("Trajectory returned.");
    //     //     messagestoshow.Push("Trajectory returned.");
    //     //     // responseforLine = result;
    //     //     StartCoroutine(ExecuteTrajectories(result));
    //     // }
    // }

    enum Poses
    {
        PreGrasp,
        Grasp,
        PickUp,
        Place
    }
}


// send me home copy 

// public void SendMeHome()
//     {   
//         var request = new PandaMoverManyPosesRequest(); 
//         request.joints_input = CurrentJointConfig();

//         Vector3 newObjTransformation = homePose;
//         PoseMsg[] poses_to_sent = new PoseMsg[1];
//         // Quaternion newObjRotation = Quaternion.Euler(0, 0, 0).To<FLU>();
//         poses_to_sent[0] = new PoseMsg
//         {
           
//             position = (newObjTransformation).To<FLU>(), // home position 
//             orientation = Quaternion.Euler(90, 0, 0).To<FLU>() // home rotation
//         };
//         request.poses = poses_to_sent;
//         Debug.Log($"I requested {newObjTransformation} {poses_to_sent.Length}");
//         m_Ros.SendServiceMessage<PandaMoverServiceResponse>(simplemoves, request, PandaTrajectoryResponse);
//     }