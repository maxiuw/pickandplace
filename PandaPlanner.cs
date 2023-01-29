using System;
using System.Collections;
using System.Linq;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
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
        // -------------------------------------------
        // Variables required for ROS communication
        // -------------------------------------------
        [SerializeField]
        string m_RosServiceName = "panda_move";
        public string RosServiceName { get => m_RosServiceName; set => m_RosServiceName = value; }
        string m_simplemoves = "moveit_many";
        string waypoints_service = "waypoints_service";
        public string simplemoves { get => m_simplemoves; set => m_simplemoves = value; }
        public string real_robot_state_topic = "/joint_state_unity";
        string movit_results = "/move_group/fake_controller_joint_states"; //"/move_group/result"; 
        Quaternion newObjRotation;
        string realrobot_move_topic = "realrobot_publisher"; 
        string pub_number_of_poses = "total_poses_n";
        string reset_n_poses = "reset_n_poses";
        string gripperAction = "gripperAction";
        // ROS Connector
        ROSConnection m_Ros;
        // -------------------------------------------
        // Robot stuff
        // -------------------------------------------
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

        // -------------------------------------------
        // other Unity in the scene connected with PandaPlanner 
        // -------------------------------------------
        /// added by maciej
        public FloatListMsg real_robot_position;
        // public ObjReciever Reciever;
        public ObjectRecieverRos Reciever;
        public GameObject[] prefabs;
        [SerializeField]
        private Vector3 homePose;
        [HideInInspector]
        public Stack<string> messagestoshow;
        [HideInInspector]
        public PandaManyPosesResponse responseforLine;
        public List<Vector3> vectors_for_lines;
        [HideInInspector]
        public int colorindex = 0;
        public List<PoseMsg> robot_poses; // prepick up
        public List<PoseMsg> robot_postpick_pose; //
        public PoseMsg lastpickup_pose;
        public PoseMsg lastplace_pose;
        int opengripper = 0;
        int closegripper = 0;
        [HideInInspector]
        public float panda_y_offset = 0.64f; // table height, in ros it is set to 0
        List<RobotTrajectoryMsg> trajectoriesForRobot;
        public bool robot_on_the_position = false;
        public SceneSetup scenesetup_tool;
        public ActivateCanvas button_controller;
        // double[] joints;
        void Start()
        {
            // initiate all the variables and find the robot on start 
            messagestoshow = new Stack<string>();
            robot_poses = new List<PoseMsg>();
            robot_postpick_pose = new List<PoseMsg>();
            // Get ROS connection static instance
            m_Ros = ROSConnection.GetOrCreateInstance();
            // with and without picking responses 
            // initiate services
            m_Ros.RegisterRosService<PandaPickUpRequest, PandaPickUpRequest>(m_RosServiceName);
            m_Ros.RegisterRosService<PandaSimpleServiceRequest, PandaSimpleServiceResponse>(m_simplemoves);
            m_Ros.RegisterRosService<PandaManyPosesRequest, PandaManyPosesResponse>(waypoints_service);
            // initiate subscriber 
            m_Ros.Subscribe<FloatListMsg>(real_robot_state_topic, ExecuteTrajectoriesJointState);
            m_Ros.RegisterPublisher<RobotTrajectoryMsg>(realrobot_move_topic);
            m_Ros.RegisterPublisher<Int16Msg>(pub_number_of_poses);
            m_Ros.RegisterPublisher<Int16Msg>(reset_n_poses);
            m_Ros.RegisterPublisher<FloatListMsg>(gripperAction);
            responseforLine = new PandaManyPosesResponse();

            // get robot's joints 
            m_JointArticulationBodies = new ArticulationBody[k_NumRobotJoints];
            // rotation of the object that was detected, "global" to "hack" :) 
            newObjRotation = new Quaternion();
            trajectoriesForRobot = new List<RobotTrajectoryMsg>();
            var linkName = string.Empty;
            for (var i = 0; i < k_NumRobotJoints; i++)
            {
                linkName += SourceDestinationPublisher.LinkNames[i];
                m_JointArticulationBodies[i] = Panda.transform.Find(linkName).GetComponent<ArticulationBody>();
            }
            // Find left and right fingers
            var rightGripper = linkName + "/panda_link8/panda_hand/panda_rightfinger"; // for panda 
            var leftGripper = linkName + "/panda_link8/panda_hand/panda_leftfinger";
            m_RightGripper = Panda.transform.Find(rightGripper).GetComponent<ArticulationBody>();
            m_LeftGripper = Panda.transform.Find(leftGripper).GetComponent<ArticulationBody>();
            // MoveToRealRobotPose();
        }
        void Update() {
            // try to move the robot to the real pose and change robot_on_the_position
            if (!robot_on_the_position) {
                MoveToRealRobotPose();
                // robot_on_the_position = true;
            }
        }

        
        IEnumerator CloseGripper()
        {
            Debug.Log("Closed Gripper");
            var leftDrive = m_LeftGripper.xDrive;
            var rightDrive = m_RightGripper.xDrive;

            leftDrive.target = -0.05f;
            rightDrive.target = -0.05f;

            m_LeftGripper.xDrive = leftDrive;
            m_RightGripper.xDrive = rightDrive;
            // so that the robot waits until finish execution
            yield return new WaitForSeconds(k_PoseAssignmentWait);
        }

        /// <summary>
        ///     Open the gripper
        /// </summary>
        IEnumerator OpenGripper()
        {
            Debug.Log("Opened Gripper");
            var leftDrive = m_LeftGripper.xDrive;
            var rightDrive = m_RightGripper.xDrive;

            leftDrive.target = 0.05f;
            rightDrive.target = 0.05f;

            m_LeftGripper.xDrive = leftDrive;
            m_RightGripper.xDrive = rightDrive;
            // so that the robot waits until finish execution
            yield return new WaitForSeconds(k_PoseAssignmentWait);
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
                Debug.Log($" join {i}: {m_JointArticulationBodies[i].jointPosition[0]}");
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
            // used to publish the movement along the waypoints
            colorindex = 0;
            if (robot_poses.Count > 0) {
                // all the necceaasy information for the request 
                var request = new PandaManyPosesRequest(); 
                // last pick and place poses plus the array of waypoitns 
                request.current_joints = CurrentJointConfig().joints;
                request.pre_pick_poses = robot_poses.ToArray();
                request.pick_pose = lastpickup_pose;
                request.post_pick_poses = robot_postpick_pose.ToArray();
                request.place_pose = lastplace_pose;
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

        public void PublishJoints()
        {
            // get the position of the real robot 
            // FloatListMsg response = real_robot_position;
            // float[] response_array = real_robot_position.joints.Select(r => (float)r * Mathf.Rad2Deg).ToArray();

            // for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++)
            // {
            //     // each join jumps to the position of the real robot
            //     var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
            //     joint1XDrive.target = response_array[joint];
            //     m_JointArticulationBodies[joint].xDrive = joint1XDrive;
            //     // save the joint position to the buffer for planning 

            // }
            
            // print joints and response array 
            // Debug.Log($"{joints} {response_array}");
            // calculate time taken to move to the robot 
            button_controller.ButtonMoveRealRobot.interactable = true;
            button_controller.ButtonPickObject.interactable = false;
            scenesetup_tool.object_placed = scenesetup_tool.timeRemaining - scenesetup_tool.maxtime;
            // reset n poses for the real robot 
            Int16Msg pose_n = new Int16Msg();
            pose_n.data =  (short) 0;
            m_Ros.Publish(reset_n_poses, pose_n);
            // dealing with target placement 
            m_TargetPlacement.GetComponent<Rigidbody>().useGravity = false;
            m_TargetPlacement.GetComponent<BoxCollider>().enabled = false; // so we can move it aroudn in vr but when robot moves the cube ther e it doesnt collide\
            var request = new PandaManyPosesRequest();     
            request.current_joints = CurrentJointConfig().joints;
            // while (CurrentJointConfig().joints != real_robot_position.joints.Select(r => (double)r * Mathf.Rad2Deg).ToArray()) {
            //     r
            // }

            Vector3 newObjTransformation = new Vector3();
            try {
                newObjTransformation = scenesetup_tool.missin_position3d;
                scenesetup_tool.final_missing_position = scenesetup_tool.missing_obj.transform.position;
                scenesetup_tool.final_distance = (double) scenesetup_tool.CalculateDistanceBetweenFinalMissing();

            } catch {
                Debug.Log("Could not get the position of the object");
                return;
            }
            Quaternion hand_orientation = Quaternion.Euler(180, 0, 0); // roty = newObjRotation.eulerAngles.y

            request.pick_pose = new PoseMsg
            {
                position = (newObjTransformation).To<FLU>(),
                // The hardcoded x/z angles assure that the gripper is always positioned above the target cube before grasping.
                orientation = hand_orientation.To<FLU>() //m_Target.transform
            };
            // for console canvas 
            Debug.Log($"position {request.pick_pose.position} ort {request.pick_pose.orientation}");
            // Place Pose
            Vector3 placepose = m_TargetPlacement.transform.position;
            placepose.y = 0.7f;
            request.place_pose = new PoseMsg
            {
                position = (placepose).To<FLU>(),
                orientation = hand_orientation.To<FLU>()
            };
            // save last pick up and place poses
            lastpickup_pose = request.pick_pose;
            lastplace_pose = request.place_pose;
            // this are 2 empty poses since we are not using waypoints
            request.pre_pick_poses = new PoseMsg[0];
            request.post_pick_poses = new PoseMsg[0];
            // pose to come back to home position
            request.post_place_pose = new PoseMsg
            {
                position = (homePose + m_PickPoseOffset).To<FLU>(), // m_Target.transform.position

                // The hardcoded x/z angles assure that the gripper is always positioned above the target cube before grasping.
                orientation = hand_orientation.To<FLU>() //m_Target.transform
            };
            Debug.Log($"position place {placepose}");
            m_Ros.SendServiceMessage<PandaManyPosesResponse>(waypoints_service, request, PandaTrajectoryResponse);
        }

        IEnumerator ExecuteTrajectories(RobotTrajectoryMsg[] response)
        {
            OpenGripper();
            colorindex = 0;
            Debug.Log($"I will got to {response.Length} poses");
            if (response != null)
            {
                // For every trajectory plan returned
                for (var poseIndex = 0; poseIndex < response.Length; poseIndex++)
                {
                    // adding the trajectories so they can be later on pulished for the robot 
                    trajectoriesForRobot.Add(response[poseIndex]);
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
                            var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
                            joint1XDrive.target = result[joint];
                            m_JointArticulationBodies[joint].xDrive = joint1XDrive;
                        }
                        // Wait for robot to achieve pose for all joint assignments
                        yield return new WaitForSeconds(k_JointAssignmentWait);
                    }
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

        void PandaTrajectoryResponse(PandaManyPosesResponse response)
        {
            // Debug.Log(response);
            if (response.trajecotry_list.trajectories_pick.Length > 0)
            {
                trajectoriesForRobot = new List<RobotTrajectoryMsg>();
                Debug.Log("Trajectory returned.");
                messagestoshow.Push("Trajectory returned.");
                Debug.Log(response);
                responseforLine = response;
                StartCoroutine(RunAllTrajectories(response));
               
            } else {
                // if cannot find the path - remove the cube from the game a remove the id 
                messagestoshow.Push("I could not find the trajectory. Move your cube or the target placement. Automatically destroying the obj");
                // int id = Reciever.ids[Reciever.ids.Count - 1];
                // Destroy(GameObject.Find("cube" + id.ToString() + "(Clone)"));
                // Debug.LogError("No trajectory returned from MoverService.");
                // Reciever.ids.RemoveAt(Reciever.ids.Count - 1);
            }
        }
        IEnumerator RunAllTrajectories(PandaManyPosesResponse response) {
            // neccessary, otherwise breakes
            // 3 different coroutines, must yield return to complete one before starting next one
            colorindex = 0;
            yield return StartCoroutine(OpenGripper());
            if (response.trajecotry_list.trajectories_prepick.Length > 0)
                yield return StartCoroutine(ExecuteTrajectoriesWaypoints(response.trajecotry_list.trajectories_prepick));
                yield return new WaitForSeconds(k_JointAssignmentWait);
            yield return StartCoroutine(ExecuteTrajectoriesWaypoints(response.trajecotry_list.trajectories_pick));
            yield return new WaitForSeconds(k_JointAssignmentWait);
            closegripper = 3;
            yield return StartCoroutine(CloseGripper());
            yield return new WaitForSeconds(k_JointAssignmentWait);
            yield return StartCoroutine(ExecuteTrajectoriesWaypoints(response.trajecotry_list.trajectories_postpick));
            yield return new WaitForSeconds(k_JointAssignmentWait);
            opengripper = 5;
            yield return StartCoroutine(OpenGripper());
            yield return new WaitForSeconds(k_JointAssignmentWait);
            // go home 
            yield return StartCoroutine(ExecuteTrajectoriesWaypoints(response.trajecotry_list.trajectories_postplace));
            Debug.Log($"close {closegripper} open {opengripper}");
            colorindex = -1; // added to stop generating waypoints 
        }

        IEnumerator ExecuteTrajectoriesWaypoints(RobotTrajectoryMsg[] response)
        {
            // for the real robot to know when to close/open gripper
            // also TODO, must be adjusted, maybe by sending the msg like this pre,pick,post,place
            Int16Msg msg = new Int16Msg();
            msg.data =  (short) response.Length;
            m_Ros.Publish(pub_number_of_poses, msg);
            Debug.Log($"I will got to {response.Length} poses");
            if (response != null)
            {
                for (var poseIndex = 0; poseIndex < response.Length; poseIndex++)
                {
                    // adding the trajectories so they can be later on pulished for the robot 
                    trajectoriesForRobot.Add(response[poseIndex]);
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
                            var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
                            joint1XDrive.target = result[joint];
                            m_JointArticulationBodies[joint].xDrive = joint1XDrive;
                        }
                        // Wait for robot to achieve pose for all joint assignments
                        yield return new WaitForSeconds(k_JointAssignmentWait);
                    }
                    // color idx to open/close gripper - not neccassary here
                    colorindex++;
                    yield return new WaitForSeconds(k_PoseAssignmentWait);
                }
                // All trajectories have been executed, open the gripper to place the target cube
            }
        }
        void ExecuteTrajectoriesJointState(FloatListMsg response)
        {
            // For every trajectory plan returned
            if (this.real_robot_position is null) {
                Debug.Log("I got joint states");
                Debug.Log(response.joints.ToArray());
                // Debug.Log(response.ToString);
                Debug.Log("end of message");
            }
            // save the real robot pose to the buffer
            this.real_robot_position = response;
        }

        public void MoveToRealRobotPose()
        {
            double[] joints = new double[k_NumRobotJoints];

            try {
                FloatListMsg response = real_robot_position;
                float[] response_array = response.joints.Select(r => (float)r * Mathf.Rad2Deg).ToArray();
                string printing = "";
                for (var joint = 0; joint < m_JointArticulationBodies.Length; joint++)
                {
                    // each join jumps to the position of the real robot
                    printing += response_array[joint].ToString() + " next ";
                    var joint1XDrive = m_JointArticulationBodies[joint].xDrive;
                    joint1XDrive.target = response_array[joint];
                    m_JointArticulationBodies[joint].xDrive = joint1XDrive;
                    // save the joint position to the buffer for planning 
                    joints[joint] = m_JointArticulationBodies[joint].jointPosition[0];

                }
                robot_on_the_position = true;
            } catch {
                Debug.Log("Wait a couple of seconds, I have not recieved any poses yet.");
                robot_on_the_position = false;
            }
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
            StartCoroutine(RunTrajectories(result.position));
        }
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
        public void MoveRealRobot() {
            /// <summary>
            /// Moving real robot, based on the trajectories saved earlier during the exacution inside the unity
            /// trajecotry saved in trajectoriesForRobot, calling in this way so i can wait for the execution 
            /// </summary>
            button_controller.ButtonMoveRealRobot.interactable = false;
            button_controller.ButtonNextScene.interactable = true;
            StartCoroutine(SendTrajectoriesToRealRobot());
        }
        IEnumerator SendTrajectoriesToRealRobot() {
            // proposed traj should be stored in the variable RobotTrajMsg
            // after trajectory are discussed and accepted, this function should publish them to the topic
            // moveit_unity_node exectues them on the real world robot 
            // reset_n_poses to start with 
           
            // yield return new WaitForSeconds(2);
            // start with openning the gripper 
            // sending when to open and close the gripper
            // sendmehome = true;
            FloatListMsg gripper = new FloatListMsg();
            gripper.joints = new double[2];
            gripper.joints[0] = (double) closegripper;
            gripper.joints[1] = (double) opengripper;
            m_Ros.Publish(gripperAction, gripper);
           
            Int16Msg msg = new Int16Msg();
            msg.data =  (short) trajectoriesForRobot.Count;
            m_Ros.Publish(pub_number_of_poses, msg);

            for (int i = 0; i < trajectoriesForRobot.Count; i++) {
                Debug.Log("trajectories were sent");
                m_Ros.Publish(realrobot_move_topic, trajectoriesForRobot[i]);
            }  
            yield return new WaitForSecondsRealtime(2);
            // gripper.data = (short) 0;
            // m_Ros.Publish(gripperAction, gripper);
            // remove old traj, reset open/close gripper poses 
            trajectoriesForRobot = new List<RobotTrajectoryMsg>();
            // if (sendmehome) {
            //     SendMeHome();
            //     sendmehome = false
            // }
            // closegripper = -1;
            // opengripper = -1;
            // gripper.data = (short) 0;
            // m_Ros.Publish(gripperAction, gripper);
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