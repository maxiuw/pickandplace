using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UnityEditor;

public class MoveCam : MonoBehaviour
{
    
    // Start is called before the first frame update
    int direction;
    Vector3 initialpoisition;
    Quaternion initialrotation; 
    int frames = 120;
    int currentf = 0;
    int currentseq = 0;
    public ImageSynthesis cam;
    int height;
    int width; 
    public BoundingBoxer annotator;
    public sceneController controller;
    // Dictionary<string,Transform> cameraposes;
    Dictionary<string,Quaternion> cameraRotations;
    Dictionary<string,Vector3>  cameraPositions;
    void Start()
    {
        
        // save init pose 
        initialpoisition = this.transform.position;
        initialrotation = this.transform.rotation;
        // make sure that it's -1 or 1
        direction = Random.Range(-1,2);
        while (direction == 0)
            direction = Random.Range(-1,2); 
        Debug.Log(direction);
        // var labels = cam.capturePasses[2];
        // var depth = cam.capturePasses[3];
        var mainCamera = GetComponent<Camera>();
        height = mainCamera.pixelHeight;
        width = mainCamera.pixelWidth;
        // Matrix4x4 camMatrix = mainCamera.projectionMatrix;
        Debug.Log(mainCamera.fieldOfView);
        cameraPositions = new Dictionary<string, Vector3>();
        cameraRotations = new Dictionary<string, Quaternion>();
        
    }

    // Update is called once per frame
    void Update() {
        if (currentf < frames) { // we run 50fps 
            MoveAndRecord();
            currentf++;
        }
        else {
            // reset the frames
            currentf = 0;
            ResteCam();
            annotator.staticSceneAnnotator($"{string.Format("{0:000000}", currentseq)}labels");
            controller.GenerateRandom();
            // increase the seq
            currentseq++;
        }
        
    }
    
    void MoveAndRecord() { 
        transform.Translate(new Vector3(direction * 0.01f, 0.001f, 0));
        transform.Rotate(new Vector3(0, -direction * 0.1f, 0.05f));
        if (currentf % 10 == 0 & currentf > 0) {
            string seq = string.Format("{0:000000}", currentseq);
            string frame = string.Format("{0:000000}", currentf);
            cam.Save($"{seq}_{frame}", width, height, "DanielLeoMaciej/images", 2); 
            cam.Save($"{seq}_{frame}", width, height, "DanielLeoMaciej/images", 3);
            cameraRotations.Add($"{seq}_{frame}", cam.transform.rotation);
            cameraPositions.Add($"{seq}_{frame}", cam.transform.position);
            // Debug.Log(cam.transform.position);
        }
        
    }

    // void Record() {
    //     var labels = cam.capturePasses[2].camera;
    //     var cameraTexture = labels.targetTexture;
    //     // var depth = cam.capturePasses[3];

    // }
    void ResteCam() {
        transform.position = initialpoisition;
        transform.rotation = initialrotation;
        direction = Random.Range(-1,2);
        while (direction == 0)
            direction = Random.Range(-1,2); 
        WriteCameraPoses();
        cameraPositions = new Dictionary<string, Vector3>();
        cameraRotations = new Dictionary<string, Quaternion>();    }
    void WriteCameraPoses() {
        using (StreamWriter writer = new StreamWriter($"DanielLeoMaciej/cameraposes/{string.Format("{0:000000}", currentseq)}Poses.json")) {
            writer.WriteLine("{");
            foreach (KeyValuePair<string,Vector3> item in cameraPositions) {
                writer.WriteLine($"\"{item.Key}\":");
                writer.WriteLine("{");
                writer.WriteLine($"\"position\": \"[{item.Value.x.ToString("f3")},{item.Value.y.ToString("f3")},{item.Value.z.ToString("f3")}]\",");
                writer.WriteLine($"\"rotation\": \"[{cameraRotations[item.Key].x.ToString("f3")},{cameraRotations[item.Key].y.ToString("f3")},{cameraRotations[item.Key].z.ToString("f3")},{cameraRotations[item.Key].w.ToString("f3")}]\"");
                if (item.Key == cameraPositions.Keys.Last()) {
                    writer.WriteLine("}");
                } else {
                    writer.WriteLine("},");
                }
                
            }
            writer.WriteLine("}");
        }
    }


}
