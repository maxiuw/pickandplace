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
    Dictionary<string,Transform> cameraposes;
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
        cameraposes = new Dictionary<string, Transform>();
        controller.GenerateRandom();
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
            annotator.staticSceneAnnotator($"labels{currentseq}");
            controller.GenerateRandom();
            // increase the seq
            currentseq++;
        }
        
    }
    
    void MoveAndRecord() { 
        transform.Translate(new Vector3(direction * 0.01f, 0.001f, 0));
        transform.Rotate(new Vector3(0, -direction * 0.1f, 0.05f));
        if (currentf % 5 == 0) {
            cam.Save($"000{currentseq}_{currentf}", width, height, "DanielLeoMaciej/images", 2); 
            cam.Save($"000{currentseq}_{currentf}", width, height, "DanielLeoMaciej/images", 3);
            cameraposes.Add($"{currentseq}_{currentf}", cam.transform);
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
        cameraposes = new Dictionary<string, Transform>();
    }
    void WriteCameraPoses() {
        using (StreamWriter writer = new StreamWriter($"DanielLeoMaciej/cameraposes/cameraPosesSeq{currentseq}.json")) {
            writer.WriteLine("{");
            foreach (KeyValuePair<string,Transform> item in cameraposes) {
                writer.WriteLine($"\"{item.Key}\":");
                writer.WriteLine("{");
                writer.WriteLine($"\"position\": \"{item.Value.transform.position}\",");
                writer.WriteLine($"\"rotation\": \"{item.Value.transform.rotation}\"");
                if (item.Key == cameraposes.Keys.Last()) {
                    writer.WriteLine("}");
                } else {
                    writer.WriteLine("},");
                }
                
            }
            writer.WriteLine("}");
        }
    }


}
