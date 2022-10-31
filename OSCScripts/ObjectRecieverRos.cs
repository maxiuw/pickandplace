// using System.Diagnostics;
// using System.Threading.Tasks.Dataflow;
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
using RosMessageTypes.Std;
using Newtonsoft.Json.Linq;

public class ObjectRecieverRos : MonoBehaviour {
    // public OSC osc;
    // Add the objects in the organized way with predefined id's
    // TODO: this should be stored somewhere else (maybe)
    public GameObject[] prefabs;
    public Camera cam;
    [HideInInspector]
    public Stack<Vector3> positions;
    [HideInInspector]
    public Stack<Quaternion> rotations;
    [HideInInspector]
    public List<int> ids;
    ROSConnection m_Ros;
    public float table_y = 0.8f;
    public float camera_height = 475; // Z of the camera in the world 
    public float camera_focal_length_logi = 378; //4 378 actually seems more possible, https://horus.readthedocs.io/en/release-0.2/source/scanner-components/camera.html
    public Dictionary<int,int> label_mapping;
    int best_n_detectableobject = 0;
    JObject lastmsg;

    // labels 
    // bannana 52
    // apple 53
    // orange 55 
    // baseball bat 39 - pen 
    // vase 86 - bana

    void Start () {
        m_Ros = ROSConnection.GetOrCreateInstance();
        label_mapping = new Dictionary<int, int>();
        label_mapping[46] = 0; // org bammaa 
        label_mapping[75] = 0; // org vase, bananna
        label_mapping[34] = 1; // org bat, pen // 34 baseball bat
        label_mapping[47] = 2; // org appleselected
        label_mapping[39] = 3; // bottle  
        m_Ros.Subscribe<StringMsg>("/predictedObjects", DoStuff);
        // osc.SetAddressHandler("/objReciever", OnObjectRecieved);
        // positions = new Stack<Vector3>();
        // rotations = new Stack<Quaternion>();
        // ids = new  List<int>();
    }
    // void Update() {
    //     var mousePos = Input.mousePosition;
    //     Debug.Log($"on the screen {Input.mousePosition}");
    //     mousePos.z = 0.63f; // select distance = 10 units from the camera
    //     Debug.Log(cam.ScreenToWorldPoint(mousePos));
    // }
	void DoStuff(StringMsg msg) {
        
        JObject json = JObject.Parse(msg.data);
        int[] classes = json["classes"].ToObject<int[]>();
        int number_detectableobjects = 0;
        // count the detectable objects, if there are more then last time 
        foreach (int c in classes) {
            try {
                int _ = label_mapping[c];
                number_detectableobjects++; // in this message 
            } catch {
                Debug.Log("I was not trained on that class");
                // return;
            }
        }
        // Debug.Log($"length {classes} length {classes.Length}");
        // check if new msg has more instances than the old one and if yes, it's a new msg 
        if (lastmsg is null | number_detectableobjects > best_n_detectableobject) {
            lastmsg = json;
            best_n_detectableobject = number_detectableobjects;
            Debug.Log(best_n_detectableobject);

        }
        // lastmsg = JObject.Parse(msg.data);
    }
	public void create_env_OnClick() {
        int[] classes = lastmsg["classes"].ToObject<int[]>();
        // Debug.Log(msg.data);
        // double[] bb = json["bb"].ToObject<double[]>();
        for (int i = 0; i < classes.Length; i++) {
            OnObjectRecieved(classes[i],lastmsg["bb"][i].ToObject<double[]>());
                // Debug.Log($"I recieved {json["bb"][0].ToObject<double[]>()}");
        }
    }
    
    // // messages are send and recieved in the following manner: 
    // // 0 - class of the object
    // // 1,2,3 - transform
    // // 4,5,6 - rotation (?)
    // // 7 - scale
    // // 8,9,10 - rgb
    // // 11 - instance idx(?)
	public void OnObjectRecieved(int objclass, double[] bb) {        

        try {
            objclass = label_mapping[objclass];
        } catch {
            Debug.Log("I was not trained on that class");
            return;
        }
        // bb format is  x1,y1,x2,y2 (top left corner, bottom right corner in cv2 python coordinate) 
        // float x_cam = (float) (cam.pixelWidth - bb[2] + ((cam.pixelWidth - bb[0]) - (cam.pixelWidth - bb[2]))/2);
        // float y_cam = (float) (cam.pixelHeight - bb[3] + ((cam.pixelHeight - bb[1]) - (cam.pixelHeight - bb[3]))/2);
        float x_cam = (float) (256 - bb[2] + ((256 - bb[0]) - (256 - bb[2]))/2);
        float y_cam = (float) (256 - bb[3] + ((256 - bb[1]) - (256 - bb[3]))/2);
        // Debug.Log($"boxes {cam.pixelWidth - bb[0]}, {cam.pixelHeight - bb[1]}, {cam.pixelWidth - bb[2]}, {cam.pixelHeight - bb[3]}");
        Debug.Log($"coor {x_cam}, {y_cam}. class {objclass}");

        // Quaternion rot = Quaternion.Euler(0, roty, 0); // rot just around y axis are allowed
        // // scale 
        // float scaleFactor = message.GetInt(7);
        // Vector3 newScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        // // color 
        // int newR = message.GetInt(8);
        // int newB = message.GetInt(9);
        // int newG = message.GetInt(10);
        // // id 
        // int id = message.GetInt(11);
        // ids.Add(id);
        // Color newColor = new Color(newR, newG, newB);
        // // get the prefab of id and assign its properities 
        GameObject prefab = prefabs[objclass];
        // from image to the world coordinate, px -> mm
        float x_world = 0.00001f * (x_cam * 0.264f * 475f) / camera_focal_length_logi;
        float y_world = 0.00001f * (y_cam * 0.264f * 475f) / camera_focal_length_logi;

        // Debug.Log($"camera props {cam.pixelHeight}, {cam.pixelWidth}");
        // Vector3 position = cam.ScreenToWorldPoint(new Vector3(x_world, y_cam, table_y));
        Vector3 position = new Vector3(x_world, table_y, y_world);
        position.y = table_y;
        
        Debug.Log($"new pose {position}");
        // prefab.transform.position = position; // y = 0 so everything is on the same plane
        // prefab.transform.rotation =  rot;
        // transform.localScale = newScale;
        // adding the position and rotation to the list so robot can grab it 
        // positions.Push(prefab.transform.position);
        // rotations.Push(prefab.transform.rotation);
        prefab.SetActive(true);
        prefab.name = $"cube{objclass}";
        
        GameObject newObj = Instantiate(prefab);
        newObj.transform.position = position;
        // prefab.GetComponent<Renderer>().material.color = newColor;
	}
}
