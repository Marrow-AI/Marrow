using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PostProcessing_Sobel : MonoBehaviour {

    private Material sobelMat;

    [Range(0, 3)]
    public float SobelResolution = 1;

    public Color outlineColor;


    void Start () {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        sobelMat = new Material(Shader.Find("Nasty-Screen/SobelOutline"));
    }
	
	void OnRenderImage (RenderTexture source, RenderTexture destination)
    {
        sobelMat.SetFloat("_ResX", Screen.width * SobelResolution);
        sobelMat.SetFloat("_ResY", Screen.height * SobelResolution);
        sobelMat.SetColor("_Outline", outlineColor);
        Graphics.Blit(source, destination, sobelMat);
    }
}
