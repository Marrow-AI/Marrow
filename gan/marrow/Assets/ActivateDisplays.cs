using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class ActivateDisplays : MonoBehaviour
{
    private StreamWriter OutputStream;
    // Start is called before the first frame update
    void Start()
    {
        
        Debug.Log("displays connected: " + Display.displays.Length);
        
        if (Display.displays.Length > 1) {
            Debug.Log("Activating display 2");
            Display.displays[1].Activate();
        }
        if (Display.displays.Length > 2) {
            Debug.Log("Activating display 3");
            Display.displays[2].Activate();
        }
        if (Display.displays.Length > 3) {
            Debug.Log("Activating display 4");
            Display.displays[3].Activate();
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnDestory()
    {
        if (OutputStream != null)
        {
            OutputStream.Close();
            OutputStream = null;
        }
    }
}
