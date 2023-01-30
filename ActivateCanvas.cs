using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using PandaRobot;
public class ActivateCanvas : MonoBehaviour
{
    public GameObject[] objects;
    public GameObject objectCreationCanvas; // add canvas in unity here
    public GameObject objectRobotUICanvas;

    GameObject lastObject;
    public Button ButtonCube;
    public Button ButtonApple;
    public Button ButtonBottle;
    public Button ButtonBanana;    
    public Button ButtonOrange;    
    public Button ButtonHammer;    
    public Button ButtonScrewDriver;    
    public Button ButtonMoveRealRobot;
    public Button ButtonPickObject;
    public Button ButtonNextScene;
    public GameObject buttonX;
    public GameObject buttonY;
    public PandaPlanner planner;
    public SceneSetup scene_controller;
    
    // string canvas_name = "UICanvasPlaceObject";
    int lastobj_id = 0;
    // Start is called before the first frame update

    void Start() {
        // save last button so that we can remove grabbable 
        // lastObject = new GameObject();
        // delegate between different option for different object we want to insert
        ButtonBanana.onClick.AddListener(delegate{InsertObj(0);});
        ButtonBottle.onClick.AddListener(delegate{InsertObj(1);});
        ButtonApple.onClick.AddListener(delegate{InsertObj(2);});
        ButtonCube.onClick.AddListener(delegate{InsertObj(3);});
        ButtonOrange.onClick.AddListener(delegate{InsertObj(4);});   
        ButtonHammer.onClick.AddListener(delegate{InsertObj(5);});
        ButtonScrewDriver.onClick.AddListener(delegate{InsertObj(6);});
        ButtonMoveRealRobot.interactable = false;
        ButtonPickObject.interactable = false;
        ButtonNextScene.interactable = false;

    }
    void Update() {
        // on button x activate objectCreationCanvas on button y activate objectRobotUICanvas
        if (Input.GetKeyDown(KeyCode.X)) {
            Activate();
        } else if (Input.GetKeyDown(KeyCode.Y)) {
            ActivateRobotCanvas();
        }
    }
    public void Activate()
    {
        // activating the canvas on the click of the vr controller
        // objects have to be added in unity, unactive ojbects cannot be found 
        // make a button jump 
        // buttonY.transform.position = new Vector3(buttonY.transform.position.x, buttonY.transform.position.y + 0.01f, buttonY.transform.position.z);
        try {
            buttonX.GetComponentInChildren<ParticleSystem>().Play();
            buttonX.transform.Translate(0, 0.1f, 0);
            buttonX.transform.Translate(0, -0.1f, 0);
        } catch {}
        if (objectCreationCanvas.active == false) {
            objectCreationCanvas.SetActive(true);
            objectRobotUICanvas.SetActive(false);

        } else {
            Deactivate(objectCreationCanvas.name);
        }
        // buttonY.transform.position = new Vector3(buttonY.transform.position.x, buttonY.transform.position.y - 0.01f, buttonY.transform.position.z);

    }

    public void ActivateRobotCanvas()
    {
        // activating the canvas on the click of the vr controller
        // objects have to be added in unity, unactive ojbects cannot be found
        try {
            buttonY.GetComponentInChildren<ParticleSystem>().Play(); 
            buttonY.transform.Translate(0, 0.1f, 0);
            buttonY.transform.Translate(0, -0.1f, 0);

        } catch {}
        if (objectRobotUICanvas.active == false) {
            objectRobotUICanvas.SetActive(true);
            objectCreationCanvas.SetActive(false);
        } else {
            Deactivate(objectRobotUICanvas.name);
        }
    }

    public void Deactivate(string canvasname)
    {
        // deactivating canvas on the clikck 
        // Debug.Log("goidfdfad");
        // objectCreationCanvas = GameObject.Find(canvasname);
        try {
            // problem - if object is grabbed or smt like that, then we cannot use simple iteractor 
            // because collider is assign to the particular iteraction (or smt like that)
            // solution - destroy this object insert the object with the activated simple interactable 
            string name_to_destroy = lastObject.name;
            Transform t = lastObject.transform;
            // Destroy(GameObject.Find(name_to_destroy));
            // InsertObj(lastobj_id, true, false, t);
        } catch {
            // do nothing pass
            Debug.Log("you have not added any object");
        }
        
        // lastObject = new GameObject();
        // lastObject.AddComponent<BoxCollider>();
        if (canvasname == objectRobotUICanvas.name) {
            objectRobotUICanvas.SetActive(false);
        } else {
            objectCreationCanvas.SetActive(false);
        }
    }

    public void InsertObj(int obj_id, bool simple = false, bool grab = true, Transform? t = null, Vector3? p = null, string? name_given = null) {
        // objc id from the list, simple = enable simple interactable, grab - enable grab interactable, p - desire position of the object 
        // choose object
        planner.MoveToRealRobotPose();
        GameObject prefab = objects[obj_id];
        // if object of the prefab name or name_given already exists, then destroy it 
        if (name_given != null) {
            Destroy(GameObject.Find(name_given));
        } else {
            Destroy(GameObject.Find(prefab.name));
        }
        lastobj_id = obj_id;
        // assign middle of the box as new point
        Vector3 position = new Vector3();
        Quaternion rotation = new Quaternion();
        // using the function to randomly insert the objects in the middle of the box or in where they were moved  
        if (t == null && p == null) { 
            position = new Vector3(0.135f, 0.83f, 0.5f);
        } else if (t == null && p != null) {
            position = (Vector3) p;
        } else if (t != null) {
            position = (Vector3) t.position;
            rotation = (Quaternion) t.rotation;
        }
        // create the prefab
        prefab.SetActive(true);
        System.Random rnd = new System.Random();
        // prefab.name = $"cube{name}";
        GameObject newObj = Instantiate(prefab);
        // string rnd_id = rnd.Next(1, 99).ToString().PadLeft(2,'0'); // format 00 
        if (name_given == null) {
            newObj.name = $"{prefab.name}";

        } else {
            newObj.name = $"{name_given}";
        }
        if (newObj.name == scene_controller.missing_class) {
            // activate pick up button if it is the missing class
            ButtonPickObject.interactable = true;
            ButtonMoveRealRobot.interactable = false;
        }
        newObj.transform.position = position;
        newObj.transform.rotation = rotation;
        // if (newObj.name == "banana") {
        //     newObj.transform.rotation = new Quaternion(0,0.7071f,0,0.7071f);
        // } 
        // behaviour - use behavior to enable/disable particular script component in the gameobject
        // https://stackoverflow.com/questions/51736534/component-does-not-contain-a-definition-for-enabled-and-no-extension-method
        Behaviour bhw = (Behaviour) newObj.GetComponent("XRSimpleInteractable");
        bhw.enabled = simple;
        bhw = (Behaviour) newObj.GetComponent("XRGrabInteractable");
        bhw.enabled = grab;
        lastObject = newObj;
    }
}
