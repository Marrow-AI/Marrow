using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class HxVolumetricImageEffectOpaque : HxVolumetricRenderCallback
{

    void OnEnable()
    {
        RenderOrder = HxVolumetricCamera.hxRenderOrder.ImageEffectOpaque;
        if (volumetricCamera == null) { volumetricCamera = GetComponent<HxVolumetricCamera>(); }
        
    }

    [ImageEffectOpaque]
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
