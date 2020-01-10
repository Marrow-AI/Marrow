using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoSelector : MonoBehaviour
{
    public UnityEngine.Video.VideoClip mother;
    public UnityEngine.Video.VideoClip father;
    public UnityEngine.Video.VideoClip sister;
    public UnityEngine.Video.VideoClip brother;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playVideo(UnityEngine.Video.VideoClip clip) {
        GetComponent<UnityEngine.Video.VideoPlayer>().clip = clip;
        GetComponent<UnityEngine.Video.VideoPlayer>().frame = 0;
        GetComponent<UnityEngine.Video.VideoPlayer>().Play();
    }
}
