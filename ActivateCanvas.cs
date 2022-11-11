using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class ActivateCanvas : MonoBehaviour
{
    public GameObject[] objects;
    public GameObject objectCreationCanvas; // add canvas in unity here
    GameObject lastObject;
    public Button ButtonCube;
    public Button ButtonApple;
    public Button ButtonBottle;
    public Button ButtonBanana;    
    int lastobj_id = 0;
    // Start is called before the first frame update
    void Start() {
        // save last button so that we can remove grabbable 
        // lastObject = new GameObject();
        // delegate between different option for different object we want to insert
        ButtonCube.onClick.AddListener(delegate{InsertObj(3);});
        ButtonApple.onClick.AddListener(delegate{InsertObj(2);});
        ButtonBottle.onClick.AddListener(delegate{InsertObj(1);});
        ButtonBanana.onClick.AddListener(delegate{InsertObj(0);});

    }
    public void Activate()
    {
        // activating the canvas on the click of the vr controller
        // objects have to be added in unity, unactive ojbects cannot be found 
        objectCreationCanvas.SetActive(true);
       
    }

    // Update is called once per frame
    public void Deactivate()
    {
        // deactivating canvas on the clikck 
        Debug.Log("goidfdfad");
        objectCreationCanvas = GameObject.Find("UICanvasObject");
        string name_to_destroy = lastObject.name;
        try {
            // Behaviour bhw = (Behaviour) lastObject.GetComponent("XRSimpleInteractable");
            // bhw.enabled = true;
            // bhw = (Behaviour) lastObject.GetComponent("XRGrabInteractable");
            // bhw.enabled = false
            
            InsertObj(lastobj_id, true, false, lastObject.transform);
        } catch {
            // do nothing pass
            Debug.Log("you have not added any object");
        }

        Destroy(GameObject.Find(name_to_destroy));
        // lastObject = new GameObject();
        // lastObject.AddComponent<BoxCollider>();
        objectCreationCanvas.SetActive(false);


        // another way 
        // destroy this object insert the object with the activated simple interactable 
    }

    public void InsertObj(int obj_id, bool simple = false, bool grab = true, Transform? t = null) {
        ///
        // objc id from the list, simple = enable simple interactable, grab - enable grab interactable, p - desire position of the object 
        ///
        // choose object
        GameObject prefab = objects[obj_id];
        lastobj_id = obj_id;
        // assign middle of the box as new point
        Vector3 position = new Vector3();
        Quaternion rotation = new Quaternion();
        // using the function to randomly insert the objects in the middle of the box or in where they were moved  
        if (t == null){
            position = new Vector3(0.135f, 0.83f, 0.5f);
        } else {
            position = (Vector3) t.position;
            rotation = (Quaternion) t.rotation;
        }
        // create the prefab
        prefab.SetActive(true);
        System.Random rnd = new System.Random();
        string name = rnd.Next(1, 99).ToString().PadLeft(2,'0'); // format 00 
        prefab.name = $"cube{name}";
        GameObject newObj = Instantiate(prefab);
        newObj.transform.position = position;
        newObj.transform.rotation = rotation;
        // behaviour 
        // https://stackoverflow.com/questions/51736534/component-does-not-contain-a-definition-for-enabled-and-no-extension-method
        Behaviour bhw = (Behaviour) newObj.GetComponent("XRSimpleInteractable");
        bhw.enabled = simple;
        bhw = (Behaviour) newObj.GetComponent("XRGrabInteractable");
        bhw.enabled = grab;
        lastObject = newObj;
    }
}
