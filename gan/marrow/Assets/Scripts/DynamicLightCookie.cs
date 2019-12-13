using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

public class DynamicLightCookie : MonoBehaviour {

	public MediaTrigger mediaTrigger;
	public Light _light;
	public Material maskSourceMaterial;

	void Start ()
	{
		mediaTrigger.OnAllPlay += OnVideosPlay;
	}

	private void OnDestroy()
	{
		mediaTrigger.OnAllPlay -= OnVideosPlay;
	}
    
	void OnVideosPlay()
	{
		mediaTrigger._slavePlayers[0].Control.SetTextureProperties(FilterMode.Bilinear, TextureWrapMode.Clamp, 0);
		_light.cookie = maskSourceMaterial.mainTexture;
	}
}
