using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class HxVolumetricImageEffect : HxVolumetricRenderCallback
{
    void OnEnable()
    {
        RenderOrder = HxVolumetricCamera.hxRenderOrder.ImageEffect;
        if (volumetricCamera == null) { volumetricCamera = GetComponent<HxVolumetricCamera>(); }

    }

  
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        //Graphics.Blit(VolumetricTexture, dest);
        if (volumetricCamera == null) { volumetricCamera = GetComponent<HxVolumetricCamera>(); }
        if (volumetricCamera == null) { Graphics.Blit(src, dest); }
        else
        {          
            volumetricCamera.EventOnRenderImage(src, dest);
        }
    }

}
