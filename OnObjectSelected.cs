using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnObjectSelected : MonoBehaviour
{
    public ObjectRecieverRos reciever;

    // Start is called before the first frame update
    void Start() {
        reciever = FindObjectOfType<ObjectRecieverRos>();
    }
    // Update is called once per frame
    public void OnSelected() {
        reciever.positions.Push(this.transform.position);
        reciever.rotations.Push(this.transform.rotation);
        try {
            reciever.lastobject = GameObject.Find(this.name);
            Debug.Log("found the object");
        } catch {
            Debug.Log("did not find the object");
        }
        reciever.ids.Add(int.Parse((this.name.Substring(4,2)))); // name is cube00clone where 00 is the idx
        this.GetComponent<Rigidbody>().isKinematic = false;
        // Debug.Log($"I will pick {int.Parse((this.name.Substring(4,2)))}");
    }
}
