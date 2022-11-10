// using System.Diagnostics;
// using System.Threading.Tasks.Dataflow;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjReciever : MonoBehaviour {
    public OSC osc;
    // Add the objects in the organized way with predefined id's
    // TODO: this should be stored somewhere else (maybe)
    public GameObject[] prefabs;
    [HideInInspector]
    public Stack<Vector3> positions;
    [HideInInspector]
    public Stack<Quaternion> rotations;
    [HideInInspector]
    public List<int> ids;
    // Start is called before the first frame update
    List<string> object_names;
    void Start () {
       osc.SetAddressHandler("/objReciever", OnObjectRecieved);
       positions = new Stack<Vector3>();
       rotations = new Stack<Quaternion>();
       object_names = new List<string>();
       ids = new List<int>();
    }
	
	// Update is called once per frame
	// void Update () {
    //     foreach (OscMessage m in osc.messagesReceived) {
    //         if (m.address == "/objReciever") {
    //             Debug.Log($"I recieved {m}");
    //             OnObjectRecieved(m);
    //         }
    //     }
	// }
    public void CreateObject() { 
        foreach (OscMessage m in osc.messagesReceived) {
            // Debug.Log($"I recieveed {m}");
            if (m.address == "/objReciever") {
                OnObjectRecieved(m);
            }
        }
	}
    
    // messages are send and recieved in the following manner: 
    // 0 - class of the object
    // 1,2,3 - transform
    // 4,5,6 - rotation (?)
    // 7 - scale
    // 8,9,10 - rgb
    // 11 - instance idx(?)
	public void OnObjectRecieved(OscMessage message) {
        
        Debug.Log($"I recieved {message}");
        // transform 
        int prefabIdx = message.GetInt(0);
		float x = message.GetFloat(1);
        float y = message.GetFloat(2);
		float z = message.GetFloat(3);
        // rotation 
        int rotx = message.GetInt(4);
        int roty = message.GetInt(5);
        int rotz = message.GetInt(6);
        Quaternion rot = Quaternion.Euler(0, 0, 0); // rot just around y axis are allowed
        // scale 
        float scaleFactor = message.GetInt(7);
        Vector3 newScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        // color 
        int newR = message.GetInt(8);
        int newB = message.GetInt(9);
        int newG = message.GetInt(10);
        // id 
        int id = message.GetInt(11);
        ids.Add(id);
        Color newColor = new Color(newR, newG, newB);
        // get the prefab of id and assign its properities 
        GameObject prefab = prefabs[prefabIdx];
        prefab.transform.position = new Vector3(x,0.87f,z); // y = 0 so everything is on the same plane
        prefab.transform.rotation =  rot;
        transform.localScale = newScale;
        // adding the position and rotation to the list so robot can grab it 
        positions.Push(prefab.transform.position);
        rotations.Push(prefab.transform.rotation);
        prefab.name = $"cube{id}";
        object_names.Add(prefab.name);
        ActivatePrefab(prefab);
        // prefab.GetComponent<Renderer>().material.color = newColor;
	}
    public void ActivatePrefab(GameObject prefab) {
        prefab.SetActive(true);     
        GameObject newObj = Instantiate(prefab);
    }
    public void ResetObject() {
        // 1 find and destroy the object
        Debug.Log(object_names[object_names.Count -1]);
        GameObject cube = GameObject.Find($"{object_names[object_names.Count -1]}(Clone)");
        DestroyImmediate(cube);
        // 2 activate prebaf 
        GameObject prefab = prefabs[0]; // hardcoded 0 - this is the class of the prefabs
        prefab.transform.position = positions.Peek(); // peek does not remove an elemtn
        prefab.transform.rotation =  rotations.Peek();
        ActivatePrefab(prefab);
    }
}
