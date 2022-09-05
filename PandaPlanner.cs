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

namespace PandaRobot
{

    public class PandaPlanner : MonoBehaviour
    {

        // Hardcoded variables
        int k_NumRobotJoints = SourceDestinationPublisher.LinkNames.Length; // has to be adj in the SDP file for diff robots 
        const float k_JointAssignmentWait = 0.08f;
        const float k_PoseAssignmentWait = 0.05f;

        // Variables required for ROS communication
        [SerializeField]
        string m_RosServiceName = "panda_move";
        public string RosServiceName { get => m_RosServiceName; set => m_RosServiceName = value; }
        string m_simplemoves = "moveit_many";
        string waypoints_service = "waypoints_service";
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
        readonly Vector3 m_PickPoseOffset = Vector3.up * 0.01f;
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
        public PandaPickUpResponse responseforLine;
        public List<Vector3> vectors_for_lines;
        [HideInInspector]
        public int colorindex = 0;
        public List<PoseMsg> robot_poses;
        public string subscriber_topicname = "/joint_state_unity";
        string movit_results = "/move_group/fake_controller_joint_states"; //"/move_group/result"; 
        Quaternion newObjRotation;
        [HideInInspector]
        public float panda_y_offset = 0.64f; // table height, in ros it is set to 0

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
            m_Ros.RegisterRosService<PandaPickUpRequest, PandaPickUpRequest>(m_RosServiceName);
            // m_Ros.RegisterRosService<PandaMoverManyPosesRequest, PandaMoverManyPosesResponse>(simplemoves);
            m_Ros.RegisterRosService<PandaSimpleServiceRequest, PandaSimpleServiceResponse>(m_simplemoves);
            m_Ros.RegisterRosService<PandaManyPosesRequest, PandaManyPosesResponse>(waypoints_service);

            // initiate subscriber 
            m_Ros.Subscribe<FloatListMsg>(subscriber_topicname, ExecuteTrajectoriesJointState);
            // m_Ros.Subscribe<MoveGroupActionResult>(movit_results, ExectuteMoverResults);
            // m_Ros.Subscribe<JointStateMsg>(movit_results, ExectuteMoverResults); 
            // get robot's joints 
            m_JointArticulationBodies = new ArticulationBody[k_NumRobotJoints];
            // rotation of the object that was detected, "global" to "hack" :) 
            newObjRotation = new Quaternion();
            var linkName = string.Empty;
            for (var i = 0; i < k_NumRobotJoints; i++)
            {
                linkName += SourceDestinationPublisher.LinkNames[i];
                m_JointArticulationBodies[i] = Panda.transform.Find(linkName).GetComponent<ArticulationBody>();
                // Debug.Log($"{i}, {linkName}");
            }
            // Find left and right fingers
            // var rightGripper = linkName + "/tool_link/gripper_base/servo_head/control_rod_right/right_gripper"; // for niryo
            // var leftGripper = linkName + "/tool_link/gripper_base/servo_head/control_rod_left/left_gripper";
            var rightGripper = linkName + "/panda_link8/panda_hand/panda_rightfinger"; // for panda 
            var leftGripper = linkName + "/panda_link8/panda_hand/panda_leftfinger";
            m_RightGripper = Panda.transform.Find(rightGripper).GetComponent<ArticulationBody>();
            m_LeftGripper = Panda.transform.Find(leftGripper).GetComponent<ArticulationBody>();
        }
        void CloseGripper()
        {
            Debug.Log("Closed Gripper");
            var leftDrive = m_LeftGripper.xDrive;
            var rightDrive = m_RightGripper.xDrive;

            leftDrive.target = -0.05f;
            rightDrive.target = -0.05f;

            m_LeftGripper.xDrive = leftDrive;
            m_RightGripper.xDrive = rightDrive;
        }

        /// <summary>
        ///     Open the gripper
        /// </summary>
        void OpenGripper()
        {
            Debug.Log("Opened Gripper");
            var leftDrive = m_LeftGripper.xDrive;
            var rightDrive = m_RightGripper.xDrive;

            leftDrive.target = 0.05f;
            rightDrive.target = 0.05f;

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
                // Debug.Log(m_JointArticulationBodies[i].jointPosition[0]);
            }

            return joints;
        }
        /// <summary>
        /// as name says, it publishes many states. Publish joints is used for pick and 
        /// place (2 intermediate position it has to pass). Publish many is used to pass as many 
        /// positions as user asks. Used mainly following the trajectory with necessary waypoints 
        /// </summary>
        public void Publish_many()
        {
            colorindex = -1;
            if (robot_poses.Count > 0) {
                var request = new PandaManyPosesRequest(); // this is where you edited a lot of shit
                request.current_joints = CurrentJointConfig().joints;

                Vector3 newObjTransformation = homePose;
                // PoseMsg[] poses_to_sent = new PoseMsg[robot_poses.Count];
                // Quaternion newObjRotation = Quaternion.Euler(0, 0, 0).To<FLU>();
                // for (int i = 0; i < robot_poses.Count; i++) {
                //     poses_to_sent[i] = 
                // }
                
                request.poses = robot_poses.ToArray();
                Debug.Log("I send the service msg");
                m_Ros.SendServiceMessage<PandaManyPosesResponse>(waypoints_service, request, PandaTrajectoryResponse);
                robot_poses = new List<PoseMsg>(); // remove all the poses after request was sent 
            } else {
                Debug.Log("Dont have any poses to publish");
            }
            
        }
        /// <summary>
        ///     Create a new MoverServiceRequest with the current values of the robot's joint angles,
        ///     the target cube's current position and rotation, and the targetPlacement position and rotation.
        ///     Call the MoverService using the ROSConnection and if a trajectory is successfully planned,
        ///     execute the trajectories in a coroutine.
        /// </summary>
        public void SendMeHome()
        {
            colorindex = -1;
            PandaSimpleServiceRequest request = new PandaSimpleServiceRequest();
            double[] joints = new double[k_NumRobotJoints];
            for (var i = 0; i < k_NumRobotJoints; i++)
            {
                // Debug.Log(i);
                joints[i] = m_JointArticulationBodies[i].jointPosition[0];
                // Debug.Log(m_JointArticulationBodies[i].jointPosition[0]);
            }
            request.current_joints = joints;
            Quaternion orientation = Quaternion.Euler(180, 0, 0);
            request.targetpose = new PoseMsg
            {

                position = (homePose + m_PickPoseOffset).To<FLU>(), // m_Target.transform.position

                // The hardcoded x/z angles assure that the gripper is always positioned above the target cube before grasping.
                orientation = orientation.To<FLU>() //m_Target.transform
            };
            // for console canvas 
            messagestoshow.Push($"position {homePose} ort {0}");
            Debug.Log($"position {homePose} ort {0}");
            m_Ros.SendServiceMessage<PandaSimpleServiceResponse>(m_simplemoves, request, SimpleResponse);
        }
        void SimpleResponse(PandaSimpleServiceResponse traj)
        {
            Debug.Log(traj.trajectories);
            StartCoroutine(ExecuteTrajectories(traj.trajectories));
        }

        // sending robot to the predefined home pose 
        public void PublishJoints()
        {
            // dealing with target placement 
            // m_Target.transform.position = new Vector3(m_Target.transform.position.x, 0.63f, m_Target.transform.position.z);
            m_TargetPlacement.GetComponent<Rigidbody>().useGravity = false;
            m_TargetPlacement.GetComponent<BoxCollider>().enabled = false; // so we can move it aroudn in vr but when robot moves the cube ther e it doesnt collide\
            var request = new PandaPickUpRequest();
            // getting current joint state
            double[] joints = new double[k_NumRobotJoints];
            for (var i = 0; i < k_NumRobotJoints; i++)
            {
                joints[i] = m_JointArticulationBodies[i].jointPosition[0];
            }
            request.current_joints = joints;
            Vector3 newObjTransformation = Reciever.positions.Pop();
            newObjRotation = Reciever.rotations.Pop();
            Quaternion hand_orientation = Quaternion.Euler(180, newObjRotation.eulerAngles.y, 0); // roty = newObjRotation.eulerAngles.y
            newObjTransformation.y = 0.7f;
            // newObjTransformation.y = 0.2f;
            // Debug.Log($"offset {m_PickPoseOffset}");
            request.pick_pose = new PoseMsg
            {
                position = (newObjTransformation).To<FLU>(), // m_Target.transform.position
                                                             // The hardcoded x/z angles assure that the gripper is always positioned above the target cube before grasping.
                orientation = hand_orientation.To<FLU>() //m_Target.transform
            };
            // for console canvas 
            // messagestoshow.Push($"position {newObjTransformation} ort {newObjRotation.eulerAngles.y}");
            Debug.Log($"position {request.pick_pose.position} ort {request.pick_pose.orientation}");
            // Place Pose
            Vector3 placepose = m_TargetPlacement.transform.position;
            placepose.y = 0.7f;
            request.place_pose = new PoseMsg
            {
                position = (placepose).To<FLU>(),
                orientation = hand_orientation.To<FLU>()
            };
            Debug.Log($"position place {placepose}");
            m_Ros.SendServiceMessage<PandaPickUpResponse>(m_RosServiceName, request, PandaTrajectoryResponse);
        }

        void PandaTrajectoryResponse(PandaPickUpResponse response)
        {
            // Debug.Log(response);
            if (response.trajectories.Length > 0)
            {
                Debug.Log("Trajectory returned.");
                messagestoshow.Push("Trajectory returned.");
                Debug.Log(response);
                responseforLine = response;
                StartCoroutine(ExecuteTrajectories(response.trajectories));
            }
            else
            {

                // if cannot find the path - remove the cube from the game a remove the id 
                messagestoshow.Push("I could not find the trajectory. Move your cube or the target placement. Automatically destroying the obj");
                int id = Reciever.ids[Reciever.ids.Count - 1];
                Destroy(GameObject.Find("cube" + id.ToString() + "(Clone)"));
                Debug.LogError("No trajectory returned from MoverService.");
                Reciever.ids.RemoveAt(Reciever.ids.Count - 1);
            }
        }

        void PandaTrajectoryResponse(PandaManyPosesResponse response)
        {
            // Debug.Log(response);
            if (response.trajectories.Length > 0)
            {
                Debug.Log("Trajectory returned.");
                messagestoshow.Push("Trajectory returned.");
                Debug.Log(response);
                // responseforLine = response;
                StartCoroutine(ExecuteTrajectories(response.trajectories));
            }
            else
            {

                // if cannot find the path - remove the cube from the game a remove the id 
                messagestoshow.Push("I could not find the trajectory. Move your cube or the target placement. Automatically destroying the obj");
                int id = Reciever.ids[Reciever.ids.Count - 1];
                Destroy(GameObject.Find("cube" + id.ToString() + "(Clone)"));
                Debug.LogError("No trajectory returned from MoverService.");
                Reciever.ids.RemoveAt(Reciever.ids.Count - 1);
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
        IEnumerator ExecuteTrajectories(RobotTrajectoryMsg[] response)
        {
            OpenGripper();
            colorindex = 0;
            Debug.Log($"I will got to {response.Length} poses");
            if (response != null)
            {
                // int j = 0;          
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
                    // yield return new WaitForSeconds(k_JointAssignmentWait);
                    if (colorindex == 1)
                    {
                        CloseGripper();
                        // yield return new WaitForSeconds(k_PoseAssignmentWait);
                    }
                    colorindex++;
                    yield return new WaitForSeconds(k_PoseAssignmentWait);
                }
                // All trajectories have been executed, open the gripper to place the target cube
                OpenGripper();
            }
            colorindex = -1; // added to stop generating waypoints 
        }

        void ExecuteTrajectoriesJointState(FloatListMsg response)
        {
            // For every trajectory plan returned
            Debug.Log("I got joint states");
            Debug.Log(response.joints.ToArray());
            // Debug.Log(response.ToString);
            Debug.Log("end of message");
            // RunTrajectories(FloatListMsg response)
            StartCoroutine(RunTrajectories(response));

        }
        IEnumerator RunTrajectories(FloatListMsg response)
        {
            Debug.Log("I executed runtraj");
            Debug.Log($"JOINTS LENGHT {m_JointArticulationBodies.Length}");
            float[] response_array = response.joints.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
            // double[] response_array = response.joints.ToArray();
            string printing = "";
            // response.data[1]
            for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++)
            {
                // Debug.Log($"joint {joint} position {response_array[joint]}");
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
        void ExectuteMoverResults(JointStateMsg result)
        {
            // more details and message structure in the message file
            // getting initial joints state (start) and the joint_trajectory points
            // float[] trajectory_start = result.result.trajectory_start.joint_state.position.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
            // RobotTrajectoryMsg[] joint_states = new RobotTrajectoryMsg[1];
            // joint_states[0] = result.result.planned_trajectory;
            StartCoroutine(RunTrajectories(result.position));


            IEnumerator RunTrajectories(double[] response)
            {
                Debug.Log("I executed runtraj");
                Debug.Log($"JOINTS LENGHT {m_JointArticulationBodies.Length}");
                float[] response_array = response.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
                // double[] response_array = response.joints.ToArray();
                string printing = "";
                // response.data[1]
                for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++)
                {
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

        void SendTrajectoriesToRealRobot() {
            
        }


        enum Poses
        {
            PreGrasp,
            Grasp,
            PickUp,
            Place
        }
    }
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