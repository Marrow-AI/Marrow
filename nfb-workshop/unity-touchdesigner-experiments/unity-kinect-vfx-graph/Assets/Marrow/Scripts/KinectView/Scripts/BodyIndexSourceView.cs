using UnityEngine;
using System.Collections;

public class BodyIndexSourceView : MonoBehaviour
{
    public GameObject BodyIndexSourceManager;
    private BodyIndexSourceManager _BodyIndexSourceManager;

    // Use this for initialization
    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    // Update is called once per frame
    void Update()
    {
        if (BodyIndexSourceManager == null)
        {
            return;
        }

        _BodyIndexSourceManager = BodyIndexSourceManager.GetComponent<BodyIndexSourceManager>();
        if (_BodyIndexSourceManager == null)
        {
            return;
        }

        //gameObject.GetComponent<Renderer>().material.mainTexture = _BodyIndexSourceManager.GetBodyIndexTexture();
        gameObject.GetComponent<Renderer>().material.SetTexture("_MainTex", _BodyIndexSourceManager.GetBodyIndexTexture());
    }
}