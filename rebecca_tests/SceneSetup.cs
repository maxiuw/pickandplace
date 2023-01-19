// using System.Diagnostics;
using System.Security.AccessControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO; 
using UnityEngine.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSetup : MonoBehaviour
{
    // Start is called before the first frame update
    public Dictionary<string,Vector2> ObjectSetup;
    public GameObject[] detectable;
    public ActivateCanvas object_inserter;
    [HideInInspector]
    public float timeRemaining = 80f;
    public float maxtime;
    bool timerIsRunning = true;
    public Text timeText;
    public Vector2 missing_position = new Vector2(0,0);
    string[] object_names = {"banana", "CubeDetected", "Food_Apple_Red"};
    string scene_name;
    public GameObject m_MyGameObject;
    GameObject missing_obj;
    string missing_class = "";
    public Dictionary<string,float> time_logs = new Dictionary<string,float>();

    void Start()
    {
        scene_name = SceneManager.GetActiveScene().name;
        // Debug.Log(scene_name);
        ObjectSetup = new Dictionary<string, Vector2>();
        // this comes as a ROS message in a final version  
        string path = $"/media/raghav/m2/VRHRI_Rebecca/Assets/Scripts/rebecca_tests/setup_folder/setup_{int.Parse(scene_name[scene_name.Length - 1].ToString())}.txt"; // filename 
        string path_missing = $"/media/raghav/m2/VRHRI_Rebecca/Assets/Scripts/rebecca_tests/setup_folder/missing_{int.Parse(scene_name[scene_name.Length - 1].ToString())}.txt"; // missing obj
        GetSetup(path, path_missing);
        foreach (string key in ObjectSetup.Keys) {
            AddObjects(key, ObjectSetup[key]);
        }
        maxtime = timeRemaining;
    }
    
    void Update()
    {
        if (timerIsRunning)
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
                string newscenename = $"FrankaScene{int.Parse(scene_name[scene_name.Length - 1].ToString()) + 1}"; 
                Debug.Log(newscenename);
                UnityEngine.SceneManagement.SceneManager.LoadScene(newscenename);      
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene_name); 
            }
            if (missing_obj != null) {
                CalculateDistanceBetweenTheObjecs2D(missing_obj, missing_position);
            } else {
                 findObject(missing_class);
            }
        }

    }
    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60); 
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds); // to display it at the 
        Debug.Log(string.Format("{0:00}:{1:00}", minutes, seconds));
    }


    void GetSetup(string path, string path_missing) {
        // reads set up and transforms it to the dic
        // next the env is created 
        using(StreamReader readtext = new StreamReader(path))
        using(StreamReader missingobj = new StreamReader(path_missing))
        {   
            // read line by line and split it on location and key
            string line;
            while ((line = readtext.ReadLine()) != null) {            
                Debug.Log(line);
                char[] splitters = {',',':'};
                string[] splitted = line.Split(splitters);
                ObjectSetup[splitted[0]] = new Vector2(float.Parse(splitted[1]), float.Parse(splitted[2]));
            }
            // read line from the path missing file 
            string line_missing;
            while ((line_missing = missingobj.ReadLine()) != null) {            
                Debug.Log(line_missing);
                char[] splitters = {',',':'};
                string[] splitted = line_missing.Split(splitters);
                missing_class = splitted[0];
                missing_position = new Vector2(float.Parse(splitted[1]), float.Parse(splitted[2]));
            }           
        }
    }

    public void AddObjects(string key, Vector2 obj_position) {
        // objc id from the list, simple = enable simple interactable, grab - enable grab interactable, p - desire position of the object 
        // choose object
        GameObject prefab = new GameObject();
        for (int i = 0; i < object_inserter.objects.Length; i ++) {
            if (object_inserter.objects[i].name.ToLower().Contains(key.ToLower())) {
                Vector3 position = new Vector3(obj_position.x, 0.85f ,obj_position.y);
                object_inserter.InsertObj(i, false, true, null, position, key);
            }
        }
    }
    public void findObject(string name) { 
        // get the substring without two last characters
        // find the object of the same name
        missing_obj = GameObject.Find(name);
        // calculate distance between the object and the ground truth
        if (missing_obj != null) {
            SaveTime("Added_object");
            float distance = CalculateDistanceBetweenTheObjecs2D(missing_obj, ObjectSetup[name]);
        }
        else {
            Debug.Log("Object not found");
        }  
    }
    public float CalculateDistanceBetweenTheObjecs2D(GameObject obj1, Vector2 gt) {
        // calculate distance between the object and the ground truth
        float distance = Mathf.Sqrt(Mathf.Pow((obj1.transform.position.x - gt.x), 2) + Mathf.Pow((obj1.transform.position.z - gt.y), 2));
        Debug.Log(distance);
        return distance;
    }
    public void SaveTime(string option) {
        // save time logs
        time_logs[option] = maxtime - timeRemaining;
        Debug.Log(time_logs[option]);
    }
}
