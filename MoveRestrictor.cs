using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveRestrictor : MonoBehaviour
{

    public float xmax = 0.23f;
    public float xmin = -0.23f;
    public float zmax = 0.58f;
    public float zmin = 0.34f;
    public bool forcebounds = true;

    // Start is called before the first frame update
    // Update is called once per frame
    void Update()
    {
        // force the object to stay in bounds 
        if (forcebounds) {
            Vector3 newposition = this.transform.position;
            if (this.transform.position.x > xmax)
                newposition.x = xmax;
            if (this.transform.position.x < xmin)
                newposition.x = xmin;
            if (this.transform.position.z < zmin)
                newposition.z = zmin;
            if (this.transform.position.z > zmax)
                newposition.z = zmax;
            this.transform.position = newposition;
        }
    }
    void changeKinematic() {
        if (GetComponent<Rigidbody>().isKinematic == true)
            GetComponent<Rigidbody>().isKinematic = false;
        else {
            GetComponent<Rigidbody>().isKinematic = true;               
        }
    }
}
