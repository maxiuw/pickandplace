using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json.Converters;
public class BoundingBoxer : MonoBehaviour
{
    public Camera cam;
    public List<GameObject> objects;
    [HideInInspector]
    public Dictionary<string, int> classes;
    private List<Dictionary<string,string>> categories;
    [HideInInspector]
    public List<Dictionary<string,float>> annotations;
    [HideInInspector]
    public List<int> images;
    [HideInInspector]
    public int framecount;
    [HideInInspector]
    public int id = 0;
    // Dictionary<string,float> niryoannot;
    public string path; 
    // Start is called before the first frame update
    void Start()
    {
        classes = new Dictionary<string, int>();
        images = new List<int>();
        annotations = new List<Dictionary<string, float>>();
        // niryoannot = GetNiryoBounds();
        Debug.Log($"cam dims {cam.pixelHeight}, {cam.pixelWidth}");
    }

    // Update is called once per frame
    
    public void getbb() {
        
        foreach (GameObject obj in objects) {
            var bds = obj.GetComponent<Renderer>().bounds;
            // getiing max and min point of the bb
            Vector3 min_pt = bds.min;
            Vector3 max_pt = bds.max;
            // Debug.Log($"in world {min_pt} {max_pt}");
            // transfering them to the camera coordinates 
            max_pt = cam.WorldToScreenPoint(max_pt);
            min_pt = cam.WorldToScreenPoint(min_pt);
            Debug.Log($"original {bds.min}, on the screen {max_pt}, backtoorgin {cam.ScreenToWorldPoint(max_pt)}");
            // bb coordinates for the coco annot 
            Dictionary<string,float> newannot = new Dictionary<string,float>();
            
            newannot["x"] = min_pt.x;
            newannot["y"] = cam.pixelHeight - max_pt.y; 
            newannot["width"] = max_pt.x - min_pt.x;
            Debug.Log($"in canm {min_pt} {max_pt}");
            // SleepTimeout(15);
            newannot["height"] = max_pt.y - min_pt.y;
            newannot["category_id"] = ((float)obj.layer); 
            newannot["image_id"] = ((float)framecount);
            // Debug.Log($"{min_pt.x}, {newannot["y"]} {newannot["height"]} {newannot["width"]}");
            // float image_id = 
            annotations.Add(newannot);
        }
        annotations.Add(GetNiryoBounds());
        // after all the annotations were assign we have to zeroout the list 
        objects = new List<GameObject>(); 
    }
    Dictionary<string,float> GetNiryoBounds() {

       
        GameObject niryo = GameObject.Find("niryo_one");
    //     Bounds bds =  niryo.GetComponent<Renderer>().bounds
    //     // created = new GameObject[maxObjects];
        Collider m_Collider = niryo.GetComponent<Collider>();
        //Fetch the center of the Collider volume
        // Vector3  m_Center = m_Collider.bounds.center;
        // //Fetch the size of the Collider volume
        // Vector3 m_Size = m_Collider.bounds.size;
        //Fetch the minimum and maximum bounds of the Collider volume
        Vector3 min_pt = m_Collider.bounds.min;
        Vector3 max_pt = m_Collider.bounds.max;
        //Output this data into the console
        Dictionary<string,float> newannot = new Dictionary<string,float>();
        max_pt = cam.WorldToScreenPoint(max_pt);
        min_pt = cam.WorldToScreenPoint(min_pt);
        // hardcoded - > change to more felx v
        if (max_pt.y > cam.pixelHeight)
            max_pt.y = 512f;
        newannot["x"] = min_pt.x;
        newannot["y"] = cam.pixelHeight - max_pt.y; 
        newannot["width"] = max_pt.x - min_pt.x;
        Debug.Log($"in canm {min_pt} {max_pt}");
        // SleepTimeout(15);
        newannot["height"] = max_pt.y - min_pt.y;
        newannot["category_id"] = ((float)niryo.layer); 
        newannot["image_id"] = ((float)framecount);
        return newannot;
        // Debug.Log("Collider Center : " + m_Center);
        // Debug.Log("Collider Size : " + m_Size);
        // Debug.Log("Collider bound Minimum : " + m_Min);
        // Debug.Log("Collider bound Maximum : " + m_Max);
        // Debug.Log($" robot min {min_pt} ,robot max {max_pt}");

    } 
    public void createText() {
        using (StreamWriter writer = new StreamWriter($"{path}/labels.json"))  
        // creating annotation in coco format https://gist.github.com/akTwelve/c7039506130be7c0ad340e9e862b78d9
            {  
                writer.WriteLine("{");
                writer.WriteLine("\"images\": [ ");
                foreach (int im in images) { 
                    writer.WriteLine("{");
                    writer.WriteLine("\"id\": "+ $"{im},");
                    writer.WriteLine("\"file_name\": "+ $"\"{im}_img.png\",");
                    writer.WriteLine("\"height\": 512,");
                    writer.WriteLine("\"width\": 512");
                    if (im == images[images.Count-1]) {
                        writer.WriteLine("}");
                    } else {
                        writer.WriteLine("},");
                    }
                }
                    writer.WriteLine("],");
                writer.WriteLine("\"annotations\": [");
                foreach (Dictionary<string,float> annot in annotations) { 
                    writer.WriteLine("{");  
                    writer.WriteLine($"\"id\": {id},");
                    id++;
                    writer.WriteLine($"\"category_id\": {annot["category_id"]},"); // po prostu id not category_id
                    writer.WriteLine($"\"image_id\": {annot["image_id"]},");
                    writer.WriteLine($"\"bbox\": [{annot["x"]},{annot["y"]},{annot["width"]},{annot["height"]}]");
                    if (annot == annotations[annotations.Count-1]) {
                        writer.WriteLine("}");
                    } else {
                        writer.WriteLine("},");
                    }
                }
                writer.WriteLine("],");
                writer.WriteLine("\"categories\": [");

                writer.WriteLine("{");    
                writer.WriteLine($"\"id\": {6},");
                writer.WriteLine($"\"name\": \"bottle\"");
                writer.WriteLine("},");

                writer.WriteLine("{");    
                writer.WriteLine($"\"id\": {8},");
                writer.WriteLine($"\"name\": \"fruit\"");
                writer.WriteLine("},");


                writer.WriteLine("{");    
                writer.WriteLine($"\"id\": {10},");
                writer.WriteLine($"\"name\": \"robot\"");
                writer.WriteLine("},");

                writer.WriteLine("{");    
                writer.WriteLine($"\"id\": {9},");
                writer.WriteLine($"\"name\": \"torch\"");
                writer.WriteLine("}");
                writer.WriteLine("]");

                // the end of the file 
                writer.WriteLine("}");
            } 
    }
}
