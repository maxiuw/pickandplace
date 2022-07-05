using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum shapeLabel {Cube, Sphere, Collider};

// shape class with info about label and objects 
public class Shape {
    public shapeLabel label;
    public GameObject obj; 

}
public class shapePool : ScriptableObject
{   
    private GameObject[] prefabs;
    // dic with shape lable and lsit of shapes 
    private Dictionary<shapeLabel, List<Shape>> pools;
    private List<Shape> active;


    // creator 
    public static shapePool Create(GameObject[] prefabs) {
        var p = ScriptableObject.CreateInstance<shapePool>();
        p.prefabs = prefabs;
        p.pools = new Dictionary<shapeLabel, List<Shape>>();
        for (int i = 0; i < prefabs.Length; i++) {
            // Debug.Log(prefabs.Length);
            p.pools[(shapeLabel)i] = new List<Shape>();
        }
        p.active = new List<Shape>();
        return p;
    }
    // getter, we get a new object 
    public Shape Get(shapeLabel label) {
        Debug.Log(label);
        var pool = pools[label];
        int lastIdx = pool.Count - 1;
        Shape retshape;
        // if the last idx = 0, we dont have any objs in the pool
        // initiate prefab
        if (lastIdx <= 0) {
            var obj = Instantiate(prefabs[(int)label]); // instintiate one of the prefabs 
            retshape = new Shape() {label = label, obj = obj};
        } else {
            retshape = pool[lastIdx];
            retshape.obj.SetActive(true); // obj on the scene 
            pool.RemoveAt(lastIdx); // important part !
            
        }
        active.Add(retshape); // list of active objct
        return retshape;
    }
    public void ReclaimAll() {
        // look at all the objects, make them inactive and rreturn therm to the pool
        foreach (var shape in active) {
            shape.obj.SetActive(false);
            pools[shape.label].Add(shape);
        }
        active.Clear();
    }

}

