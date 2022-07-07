using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class sceneController : MonoBehaviour
{
    // synth is going to be a camera
    public ImageSynthesis synth;
    public GameObject[] prefabs;
    [Range(0.0f, 100.0f)]
    public int maxObjects = 50;
    float maxX = 0.35f;
    float maxZ = 0.35f; 
    float minX = -0.35f; 
    float minZ= -0.35f;

    // Start is called before the first frame update
    private GameObject[] created;
    private shapePool pool;
    private int frameCount = 0;
    private int valimages;
    public int trainimages = 10;
    public BoundingBoxer boxer;
    public string path;
    void Start()
    {
        pool = shapePool.Create(prefabs);
        boxer.classes = new Dictionary<string, int>();
        boxer.classes["can"] = 7; // can
        boxer.classes["fruit"] = 8; // fruit
        boxer.classes["torch"] = 9; // torch
        boxer.classes["robot"] = 10; // torch
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }  
        boxer.path = path;
        // GetNiryoBounds();
        
    }
    // Update is called once per frame
    void Update()
    {
        if (frameCount % 2 == 0)
        {
            GenerateRandom();
            boxer.framecount = frameCount;
            boxer.images.Add(frameCount);
            boxer.getbb();
            // Debug.Log(boxer.annotations.Count); // JsonUtility.ToJson(boxer.annotations))
            string filename = $"{frameCount.ToString()}"; // .PadLeft(5,'0')
            
            synth.Save(filename, 512,512, path, 0);
            // GenerateRandom();
            // synth.OnSceneChange(); // it will synth on the scene chance
            // it will synth on the scene chance
            // frameCount = 0;
        }
        frameCount++;
        if (frameCount/2 > trainimages) 
            Debug.Break();
        // we just wamt tp save display 1 and 2         
       
    }
   

    void GenerateRandom() {
        // first it will destroy all the objects in the scene 
        // for (int j = 0; j < created.Length; j++){
        //     if(created[j] != null) {
        //         Destroy(created[j]);
        //     }
        // }
        pool.ReclaimAll();
        // and now generate new ones 
        int objnumber = Random.Range(1, maxObjects);
        for (int i = 0; i < objnumber; i++) {
            // Debug.Log(objnumber);

            int prefabIdx = Random.Range(0, prefabs.Length); //);
            GameObject prefab = prefabs[prefabIdx]; 
            // random position, rotation and scale 
            float newX = 0;
            float newZ = 0;
            while (newX < 0.125 & newX > -0.125)
            {
                newX = Random.Range(minX, maxX);
                // result = random.Next(minN, maxN + 1);
            }    
            while (newZ < 0.125 & newZ > -0.125)
            {
                newZ = Random.Range(minZ, maxZ);                
                // result = random.Next(minN, maxN + 1);
            }              
            Vector3 newPos = new Vector3(newX, 0.6f, newZ);
            Quaternion newRot = Random.rotation;
            // newRot.x = 0;
            // newRot.z = 0;
            var newObj = pool.Get((shapeLabel)prefabIdx);
            newObj.obj.transform.position = newPos;
            // newObj.obj.transform.rotation = newRot;
            // GameObject newObj = Instantiate(prefab, newPos, newRot);
            float scaleFactor = Random.Range(0.5f, 3);
            // Vector3 newScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            // newObj.obj.transform.localScale = newScale;
            float newR, newG, newB;
            newR = Random.Range(0.0f, 0.3f);
            newG = Random.Range(0.0f, 0.3f);
            newB = Random.Range(0.0f, 0.3f);
            Color newColor = new Color(newR, newG, newB);
            newObj.obj.GetComponent<Renderer>().material.color = newColor;
            boxer.objects.Add(newObj.obj);
            // Destroy(GetComponent<TheComponentYouWantToDestroy>());            


            // created[i] = newObj; // this commented out creates a cool waterfall (if we dont use shape pool ) :)
        }
        synth.OnSceneChange();
    }
    private void OnDestroy() {
        boxer.createText();
    }
}
