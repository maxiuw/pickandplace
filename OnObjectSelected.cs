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
        reciever.ids.Add(int.Parse((this.name.Substring(4)[0]).ToString()));
        Debug.Log(this.name.Substring(4));
    }
}
