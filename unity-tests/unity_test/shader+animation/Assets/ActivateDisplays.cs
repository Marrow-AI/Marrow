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
        /*
        OutputStream = new StreamWriter("log.txt", true);
        Debug.Log("displays connected: " + Display.displays.Length);
        OutputStream.WriteLine("displays connected: " + Display.displays.Length);
        OutputStream.Flush();*/
        if (Display.displays.Length > 1)
            Display.displays[1].Activate();
        if (Display.displays.Length > 2)
            Display.displays[2].Activate();

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
