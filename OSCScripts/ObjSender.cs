// using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjSender : MonoBehaviour
{
    public OSC osc; 
    // private int[,] objectsToAdd; 
    private int fixedUpdateCount = 0;
    private int n_properites = 10;
    public int n_objects = 5; 
    void Start() {
     
    }
    void FixedUpdate()
    {
        Debug.Log($"I update {fixedUpdateCount}");
        fixedUpdateCount += 1;
        if (fixedUpdateCount >= 100) {
            // n objects added every x seconds
            creatRandomObjects(n_objects,n_properites);
            fixedUpdateCount = 0;
        }
    }

    void creatRandomObjects(int n_objects, int n_properites) { 
           // testing if it works 
        for (int i = 0; i < n_objects; i++) {
            OscMessage message = new OscMessage();
            message.address = "/objReciever";
            // class 
            message.values.Add(Random.Range(0,n_objects));
            // transform, scale, rot 
            for (int j = 1; j < 8; j++) {
                message.values.Add(Random.Range(-10,10));
            }
            // color 
            for (int j = 8; j < 11; j++) {
                message.values.Add(Random.Range(0,255));
            }
            osc.Send(message);
        }
    }
}
