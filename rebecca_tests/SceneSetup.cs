using System;
// using System.Diagnostics;
using System.Security.AccessControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO; 
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
// using RosMessageTypes.NiryoMoveit;
using RosMessageTypes.Panda;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using PandaRobot;
using System.Linq; 
public class SceneSetup : MonoBehaviour
{
    // Start is called before the first frame update
    public Dictionary<string,Vector2> detected_objects;
    public GameObject[] detectable;
    public ActivateCanvas object_inserter;
    [HideInInspector]
    public float timeRemaining = 800f;
    [HideInInspector]
    public float maxtime = 800f;
    bool timerIsRunning = true;
    public Text timeText;
    public Vector2 missing_position;
    public Vector3 missin_position3d;
    string[] object_names = {"Banana", "CubeDetected", "Food_Apple_Red"};
    string scene_name;
    [HideInInspector]
    public GameObject missing_obj;
    public string missing_class = "";
    public Dictionary<string,float> time_logs = new Dictionary<string,float>();
    // ROS
    ROSConnection m_Ros;
    public PandaPlanner planner;
    public Vector3 final_missing_position;  
    public double final_distance = 100;
    double? added_object = null;
    public double? object_placed = null;
    void Start()
    {
        scene_name = SceneManager.GetActiveScene().name;
        // Debug.Log(scene_name);
        detected_objects = new Dictionary<string, Vector2>();
        // this comes as a ROS message in a final version  
        // string path = $"/media/raghav/m2/VRHRI_Rebecca/Assets/Scripts/rebecca_tests/setup_folder/setup_{int.Parse(scene_name[scene_name.Length - 1].ToString())}.txt"; // filename 
        // string path_missing = $"/media/raghav/m2/VRHRI_Rebecca/Assets/Scripts/rebecca_tests/setup_folder/missing_{int.Parse(scene_name[scene_name.Length - 1].ToString())}.txt"; // missing obj
        // GetSetup();
        // foreach (string key in detected_objects.Keys) {
        //     AddObjects(key, detected_objects[key]);
        // }
        // maxtime = timeRemaining;
        // init ros 
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.Subscribe<StringMsg>("/detected_classes", Save_Detected_Objects);
        m_Ros.Subscribe<StringMsg>("/missing_class", Save_Missing_Objects);
        m_Ros.RegisterPublisher<Int16Msg>("/scene_idx");
        m_Ros.RegisterPublisher<FloatListMsg>("/time_logs_scene");
 
    }
    
    void Update()
    {
        if (timerIsRunning && planner.robot_on_the_position)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else if (timeRemaining <= 0) // || or user started moving the robot 
            {
                
                Debug.Log("Time has run out!");
                timeRemaining = 0;
                timerIsRunning = false;
                LoadNewScene();
            }
            if (missing_obj == null) {
                findObject(missing_class);
            } 
            // add objects to the scene if they are not there
            foreach (string key in detected_objects.Keys) {
                if (GameObject.Find(key) == null) {
                    AddObjects(key, detected_objects[key]);
                }
            }
        }

    }
    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60); 
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds); // to display it at the 
        // Debug.Log(string.Format("{0:00}:{1:00}", minutes, seconds));
    }

    void Save_Detected_Objects(StringMsg msg) {
        // remove '{' and '}' from the string
        string line = msg.data.Replace("{", "").Replace("}", "").Replace('"',' ').Replace(" ", "");
        // Debug.Log(line);
        char[] splitters = {',',':'};
        string[] splitted = line.Split(splitters);
        detected_objects[splitted[0]] = new Vector2(float.Parse(splitted[1]), float.Parse(splitted[2]));
        detected_objects[splitted[3]] = new Vector2(float.Parse(splitted[4]), float.Parse(splitted[5]));
        // detected_objects[splitted[6]] = new Vector2(float.Parse(splitted[7]), float.Parse(splitted[8]));
    }
    void Save_Missing_Objects(StringMsg msg) {
        // Debug.Log(msg.data);
        string line = msg.data.Replace("{", "").Replace("}", "").Replace('"',' ').Replace(" ", "");
        char[] splitters = {',',':'};
        // Debug.Log(line);
        string[] splitted = line.Split(splitters);
        missing_class = splitted[0];
        missing_position = new Vector2(float.Parse(splitted[1]), float.Parse(splitted[2]));
        missin_position3d = new Vector3(float.Parse(splitted[1]), 0.85f, float.Parse(splitted[2]));
    }


    public void AddObjects(string key, Vector2 obj_position) {
        // objc id from the list, simple = enable simple interactable, grab - enable grab interactable, p - desire position of the object 
        // choose object
        for (int i = 0; i < object_inserter.objects.Length; i ++) {
            if (object_inserter.objects[i].name.ToLower().Contains(key.ToLower())) {
                Vector3 position = new Vector3(obj_position.x, 0.85f ,obj_position.y);
                object_inserter.InsertObj(i, true, false, null, position, key);  // object_inserter.InsertObj(i, false, false, null, position, key);

            }
        }
    }

    public void findObject(string name) { 
        // get the substring without two last characters
        // find the object of the same name
        missing_obj = GameObject.Find(name);
        // calculate distance between the object and the ground truth
        if (missing_obj != null & added_object == null) {
            // save time at the moment when the object was added
                added_object =  timeRemaining - maxtime;                     
        }
        else {
            Debug.Log("Object not found");
        }  
    }
    public float CalculateDistanceBetweenTheObjecs2D(GameObject obj1, Vector2 gt) {
        // calculate distance between the object and the ground truth
        float distance = Mathf.Sqrt(Mathf.Pow((obj1.transform.position.x - gt.x), 2) + Mathf.Pow((obj1.transform.position.z - gt.y), 2));
        // Debug.Log(distance);
        return distance;
    }
    public float CalculateDistanceBetweenFinalMissing() {
        // final_missing_position and gt
        return Mathf.Sqrt(Mathf.Pow((final_missing_position.x - missing_position.x), 2) + Mathf.Pow((final_missing_position.z - missing_position.y), 2));
    }
    public void LoadNewScene() {
        // iterate over time_logs and publish it to the ros topic
        // yield return planner.SendMeHome();
        // yield return new WaitForSeconds(4);
        // planner.MoveRealRobot();
        // yield return new WaitForSeconds(4);
        // wait 3 seconds for the robot 
        
        FloatListMsg msg1 = new FloatListMsg();
        msg1.joints = new double[3];
        // time logs and distances to publish 
        try {
            msg1.joints[0] = (double) added_object;
        } catch {
            msg1.joints[0] = 0;
        }
        try {
            msg1.joints[1] = (double) object_placed;
        } catch {
            msg1.joints[2] = 0;
        }
        msg1.joints[2] = final_distance;
        // publish to time_logs_scene 
        m_Ros.Publish("/time_logs_scene", msg1);
        
        // get the current scene name
        int sceneidx = int.Parse(scene_name[scene_name.Length - 1].ToString()) + 1;
        if (sceneidx >= 4)
            // quit the applciation 
            Application.Quit();
        string newscenename = $"FrankaScene{sceneidx}"; 
        Debug.Log(newscenename);
        // publish the scene idx
        Int16Msg msg = new Int16Msg();
        msg.data = (short) sceneidx;
        m_Ros.Publish("/scene_idx", msg);
        UnityEngine.SceneManagement.SceneManager.LoadScene(newscenename);      
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene_name); 
    }
 
     // void GetSetup(string path, string path_missing) {
    //     // getset up from the file
    //     // reads set up and transforms it to the dic
    //     // next the env is created 
    //     using(StreamReader readtext = new StreamReader(path))
    //     using(StreamReader missingobj = new StreamReader(path_missing))
    //     {   
    //         // read line by line and split it on location and key
    //         string line;
    //         while ((line = readtext.ReadLine()) != null) {            
    //             Debug.Log(line);
    //             char[] splitters = {',',':'};
    //             string[] splitted = line.Split(splitters);
    //             detected_objects[splitted[0]] = new Vector2(float.Parse(splitted[1]), float.Parse(splitted[2]));
    //         }
    //         // read line from the path missing file 
    //         string line_missing;
    //         while ((line_missing = missingobj.ReadLine()) != null) {            
    //             Debug.Log(line_missing);
    //             char[] splitters = {',',':'};
    //             string[] splitted = line_missing.Split(splitters);
    //             missing_class = splitted[0];
    //             missing_position = new Vector2(float.Parse(splitted[1]), float.Parse(splitted[2]));
    //         }           
    //     }
    // }
}
