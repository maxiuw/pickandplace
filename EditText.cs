using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditText : MonoBehaviour
{
    public SceneSetup sceneSetup;
    // Start is called before the first frame update
    bool updateText = true;
    // Update is called once per frame
    void Update()
    {
        // if updated text and missing object if found then update text
        if (updateText && sceneSetup.missing_obj != null) {
            updateText = false;
            // update text
            GetComponent<UnityEngine.UI.Text>().text = "Hej, thanks for \n adding the object! \n Please pick up the " + sceneSetup.missing_obj.name;
        }
      
    }
}
