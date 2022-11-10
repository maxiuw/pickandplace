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
    // Start is called before the first frame update
    void Start() {
        // save last button so that we can remove grabbable 
        lastObject = new GameObject();
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
        // uicanv = canv.GetComponent<Canvas>();
        
    }

    // Update is called once per frame
    public void Deactivate()
    {
        // deactivating canvas on the clikck 
    
        objectCreationCanvas = GameObject.Find("UICanvasObject");
        try {
            Behaviour bhw = (Behaviour) lastObject.GetComponent("XRSimpleInteractable");
            bhw.enabled = true;
            bhw = (Behaviour) lastObject.GetComponent("XRGrabInteractable");
            bhw.enabled = false;
        } catch {
            // do nothing pass
            Debug.Log("you have not added any object");
        }
        Destroy(lastObject.GetComponent<BoxCollider>());
        lastObject.AddComponent<BoxCollider>();
        objectCreationCanvas.SetActive(false);
        // another way 
        // destroy this object insert the object with the activated simple interactable 
    }

    public void InsertObj(int obj_id) {
        // choose object
        GameObject prefab = objects[obj_id];
        // assign middle of the box as new point 
        Vector3 position = new Vector3(0.135f, 0.83f, 0.5f);
        prefab.SetActive(true);
        prefab.name = $"cube{obj_id}";
        GameObject newObj = Instantiate(prefab);
        newObj.transform.position = position;
        // behaviour 
        // https://stackoverflow.com/questions/51736534/component-does-not-contain-a-definition-for-enabled-and-no-extension-method
        Behaviour bhw = (Behaviour) newObj.GetComponent("XRSimpleInteractable");
        bhw.enabled = false;
        bhw = (Behaviour) newObj.GetComponent("XRGrabInteractable");
        bhw.enabled = true;
        lastObject = newObj;
    }
}
