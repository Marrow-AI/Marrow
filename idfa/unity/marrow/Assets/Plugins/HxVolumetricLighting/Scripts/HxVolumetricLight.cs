using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Reflection;
[ExecuteInEditMode]

public class HxVolumetricLight : MonoBehaviour
{
    static float ShadowDistanceExtra = 0.75f;
    Light myLight;
    HxDummyLight myDummyLight;
    public Texture3D NoiseTexture3D;
    public Light LightSafe()
    {
        if (myLight == null)
        {
            myLight = GetComponent<Light>();
        }
        return myLight;
    }

    public HxDummyLight DummyLightSafe()
    {
        if (myDummyLight == null)
        {
            myDummyLight = GetComponent<HxDummyLight>();
        }
        return myDummyLight;
    }

    CommandBuffer BufferRender;
    CommandBuffer BufferCopy;
    Projector myProjector;
    public Vector3 NoiseScale = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 NoiseVelocity = new Vector3(1, 1, 0);
    bool dirty = true;

    public Texture LightFalloff;
   


    public float NearPlane = 0;
    public bool NoiseEnabled = false;
    public bool CustomMieScatter = false;
    public bool CustomExtinction = false;
    public bool CustomExtinctionEffect = false;
    public bool CustomDensity = false;
    public bool CustomSampleCount = false;
    public bool CustomColor = false;
    public bool CustomNoiseEnabled = false;
    public bool CustomNoiseTexture = false;
    public bool CustomNoiseScale = false;
    public bool CustomNoiseVelocity = false;
    public bool CustomNoiseContrast = false;

    public bool CustomFogHeightEnabled = false;
    public bool CustomFogHeight = false;
    public bool CustomFogTransitionSize = false;
    public bool CustomAboveFogPercent = false;


    public bool CustomSunSize = false;
    public bool CustomSunBleed = false;
    public bool ShadowCasting = true;
    public bool CustomStrength = false;
    public bool CustomIntensity = false;
    public bool CustomTintMode = false;
    public bool CustomTintColor = false;
    public bool CustomTintColor2 = false;
    public bool CustomTintGradient = false;
    public bool CustomTintIntensity = false;
    public bool CustomMaxLightDistance = false;
    [Range(0, 4)]
    public float NoiseContrast = 1;
    public HxVolumetricCamera.HxTintMode TintMode = HxVolumetricCamera.HxTintMode.Off;
    public Color TintColor = Color.red;
    public Color TintColor2 = Color.blue;
    public float TintIntensity = 0.2f;
    [Range(0, 1)]
    public float TintGradient = 0.2f;

    [Range(0, 8)]
    public float Intensity = 1;
    [Range(0, 1)]
    public float Strength = 1;
    public Color Color = Color.white;
    [Range(0.0f, 0.9999f)]
    [Tooltip("0 for even scattering, 1 for forward scattering")]
    public float MieScattering = 0.05f;
    [Range(0.0f, 1)]
    [Tooltip("Create a sun using mie scattering")]
    public float SunSize = 0f;
    [Tooltip("Allows the sun to bleed over the edge of objects (recommend using bloom)")]
    public bool SunBleed = true;
    [Range(0.0f, 10f)]
    [Tooltip("dimms results over distance")]
    public float Extinction = 0.01f;
    [Range(0.0f, 1f)]
    [Tooltip("Density of air")]
    public float Density = 0.2f;
    [Range(0.0f, 1.0f)]
    [Tooltip("Useful when you want a light to have slightly more density")]
    public float ExtraDensity = 0;
    [Range(2, 64)]
    [Tooltip("How many samples per pixel, Recommended 4-8 for point, 6 - 16 for Directional")]
    public int SampleCount = 4;


    [Tooltip("Ray marching Shadows can be expensive, save some frames by not marching shadows")]
    public bool Shadows = true;
    public bool FogHeightEnabled = false;
    public float FogHeight = 5;
    public float FogTransitionSize = 5;
    public float MaxLightDistance = 128;

    public float AboveFogPercent = 0.1f;

    bool OffsetUpdated = false;

    public Vector3 Offset = Vector3.zero;

    static MaterialPropertyBlock propertyBlock;

    LightType GetLightType()
    {
        if (myLight != null)
        {
            return myLight.type;
        }
        if (myDummyLight != null)
        {
            return myDummyLight.type;
        }

        return LightType.Area;
    }

    LightShadows LightShadow()
    {
        if (myLight != null) { return myLight.shadows; }
        return LightShadows.None;
    }

    bool HasLight()
    {
        if (myLight != null) { return true; }
        if (myDummyLight != null) { return true; }
        return false;
    }

    Texture LightCookie()
    {
        if (myLight != null) { return myLight.cookie; }
        if (myDummyLight != null) { return myDummyLight.cookie; }
        return null;
    }

    Texture LightFalloffTexture()
    {
        if (LightFalloff != null)
        {
            return LightFalloff;
        }
        else
        {
            return HxVolumetricCamera.Active.LightFalloff;
        }
    }

    float LightShadowBias()
    {
        if (myLight != null) { return myLight.shadowBias * 1.05f; }
        return 0.1f;
    }

    Color LightColor()
    {
        if (myLight != null) { return (QualitySettings.activeColorSpace == ColorSpace.Gamma ? myLight.color : myLight.color.linear); }
        if (myDummyLight != null) { return (QualitySettings.activeColorSpace == ColorSpace.Gamma ? myDummyLight.color : myDummyLight.color.linear); }
        if (myProjector != null) { return (QualitySettings.activeColorSpace == ColorSpace.Gamma ? myProjector.material.GetColor("_Color") : myProjector.material.GetColor("_Color").linear); }
        return Color.white;
    }

    float LightSpotAngle()
    {
        if (myLight != null) { return myLight.spotAngle; }
        if (myDummyLight != null) { return myDummyLight.spotAngle; }
        if (myProjector != null) { return myProjector.fieldOfView; }
        return 1;
    }

    bool LightEnabled()
    {
        if (myLight != null) { return myLight.enabled; }
        if (myDummyLight != null) { return myDummyLight.enabled; }
        if (myProjector != null) { return myProjector.enabled; }
        myLight = GetComponent<Light>();
        if (myLight != null) { return myLight.enabled; }
        myDummyLight = GetComponent<HxDummyLight>();
        if (myDummyLight != null) { return myDummyLight.enabled; }

        myProjector = GetComponent<Projector>();
        if (myProjector != null) { return myProjector.enabled; }
        return false;
    }

    float LightRange()
    {
        if (myLight != null) { return myLight.range; }
        if (myDummyLight != null) { return myDummyLight.range; }
        if (myProjector != null) { return myProjector.farClipPlane; }
        return 0;
    }

    float LightShadowStrength()
    {
        if (myLight != null) { return myLight.shadowStrength; }
        return 1;
    }

    float LightIntensity()
    {
        if (myLight != null) { return myLight.intensity; }
        if (myDummyLight != null) { return myDummyLight.intensity; }
        if (myProjector != null) { return 1; }
        return 0;
    }

    

    void OnEnable()
    {
   
        myLight = GetComponent<Light>();
        myDummyLight = GetComponent<HxDummyLight>();
        myProjector = GetComponent<Projector>();

        HxVolumetricCamera.AllVolumetricLight.Add(this);
        UpdatePosition(true);

        if (GetLightType() != LightType.Directional)
        {
            octreeNode = HxVolumetricCamera.AddLightOctree(this, minBounds, maxBounds);
        }
        else
        {
           
            HxVolumetricCamera.ActiveDirectionalLights.Add(this);
        }

        //if (!HasLight())
        //{
        //    enabled = false;
        //}
    }

    void OnDisable()
    {
        HxVolumetricCamera.AllVolumetricLight.Remove(this);
        if (GetLightType() != LightType.Directional)
        {
            HxVolumetricCamera.RemoveLightOctree(this);
            octreeNode = null;
        }
        else
        {
            HxVolumetricCamera.ActiveDirectionalLights.Remove(this);
        }
    }

    void OnDestroy()
    {
        HxVolumetricCamera.AllVolumetricLight.Remove(this);
        if (lastType == LightType.Directional)
        {
            HxVolumetricCamera.ActiveDirectionalLights.Remove(this);
        }
        else
        {
            HxVolumetricCamera.RemoveLightOctree(this);
            octreeNode = null;
        }

    }

    void Start()
    {
        myLight = GetComponent<Light>();
        myDummyLight = GetComponent<HxDummyLight>();
    }

    public void BuildBuffer(CommandBuffer CameraBuffer)
    {
        //if (myLight == null) { myLight = GetComponent<Light>(); if (myLight == null) { enabled = false; Debug.LogWarning("No light attached"); return; } }

        if (LightEnabled() && LightIntensity() > 0)
        {



            switch (GetLightType())
            {
                case LightType.Directional:
                    BuildDirectionalBuffer(CameraBuffer); LastBufferDirectional = true;
                    break;
                case LightType.Spot:
                    BuildSpotLightBuffer(CameraBuffer); LastBufferDirectional = false;
                    break;
                case LightType.Point:
                    BuildPointBuffer(CameraBuffer); LastBufferDirectional = false;
                    break;
                case LightType.Area:
                    if (myProjector != null)
                    {
                        BuildProjectorLightBuffer(CameraBuffer); LastBufferDirectional = false;
                    }
                    break;
                default:
                    break;
            }

        }
       //else
       //{
       //    Debug.Log("" + LightEnabled() + " " +  LightIntensity());
       //}
    }

    bool bufferBuilt = false;
    public void ReleaseBuffer()
    {
        if (myLight != null && bufferBuilt)
        {
            if (LastBufferDirectional)
            {

                myLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, BufferCopy);
                myLight.RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, BufferRender);
            }
            else
            {
                myLight.RemoveCommandBuffer(LightEvent.AfterShadowMap, BufferRender);
            }
            bufferBuilt = false;

        }
    }

    static public int VolumetricBMVPPID;
    static public int VolumetricMVPPID;
    static public int VolumetricMVP2PID;
    static public int VolumetricMVPID;
    static int LightColourPID;
    static int LightColour2PID;
    static int FogHeightsPID;
    static int PhasePID;
    static int _LightParamsPID;
    static int DensityPID;
    static int ShadowBiasPID;
    static int _CustomLightPositionPID;
    static int hxNearPlanePID;
    static int NoiseScalePID;
    static int NoiseOffsetPID;
    static int _SpotLightParamsPID;
    static int _LightTexture0PID;

    static int _hxProjectorTexturePID;
    static int _hxProjectorFalloffTexturePID;
    public static void CreatePID()
    {
        _hxProjectorTexturePID = Shader.PropertyToID("_ShadowTex");
        _hxProjectorFalloffTexturePID = Shader.PropertyToID("_FalloffTex");
        hxNearPlanePID = Shader.PropertyToID("hxNearPlane");
        VolumetricBMVPPID = Shader.PropertyToID("VolumetricBMVP");
        VolumetricMVPPID = Shader.PropertyToID("VolumetricMVP");
        VolumetricMVP2PID = Shader.PropertyToID("VolumetricMVP2");
        LightColourPID = Shader.PropertyToID("LightColour");
        LightColour2PID = Shader.PropertyToID("LightColour2");
        VolumetricMVPID = Shader.PropertyToID("VolumetricMV");
        FogHeightsPID = Shader.PropertyToID("FogHeights");
        PhasePID = Shader.PropertyToID("Phase");
        _LightParamsPID = Shader.PropertyToID("_LightParams");
        DensityPID = Shader.PropertyToID("Density");
        ShadowBiasPID = Shader.PropertyToID("ShadowBias");
        _CustomLightPositionPID = Shader.PropertyToID("_CustomLightPosition");
        NoiseScalePID = Shader.PropertyToID("NoiseScale");
        NoiseOffsetPID = Shader.PropertyToID("NoiseOffset");
        _SpotLightParamsPID = Shader.PropertyToID("_SpotLightParams");
        _LightTexture0PID = Shader.PropertyToID("_LightTexture0");

    }

    float LightNearPlane()
    {

#if UNITY_5_3_OR_NEWER
        if (myLight != null)
        {return myLight.shadowNearPlane;}


        return 0.1f;     
#else
#if UNITY_5_3
        if (myLight != null)
        {return myLight.shadowNearPlane;}


        return 0.1f;
#else
        if (myLight != null)
        { return LightRange() * 0.03987963438034f; }

        return 0.1f;
#endif

#endif
    }

    int DirectionalPass(CommandBuffer buffer)
    {
        if (HxVolumetricCamera.Active.Ambient == HxVolumetricCamera.HxAmbientMode.UseRenderSettings)
        {
            if (RenderSettings.ambientMode == AmbientMode.Flat)
            {
                buffer.SetGlobalVector("AmbientSkyColor", RenderSettings.ambientSkyColor * RenderSettings.ambientIntensity);
                return 0;
            }

            if (RenderSettings.ambientMode == AmbientMode.Trilight)
            {

                buffer.SetGlobalVector("AmbientSkyColor", RenderSettings.ambientSkyColor * RenderSettings.ambientIntensity);
                buffer.SetGlobalVector("AmbientEquatorColor", RenderSettings.ambientEquatorColor * RenderSettings.ambientIntensity);
                buffer.SetGlobalVector("AmbientGroundColor", RenderSettings.ambientGroundColor * RenderSettings.ambientIntensity);

                return 1;
            }

            return 2;
        }
        else if (HxVolumetricCamera.Active.Ambient == HxVolumetricCamera.HxAmbientMode.Color)
        {
            buffer.SetGlobalVector("AmbientSkyColor", HxVolumetricCamera.Active.AmbientSky * HxVolumetricCamera.Active.AmbientIntensity);
            return 0;
        }
        else if (HxVolumetricCamera.Active.Ambient == HxVolumetricCamera.HxAmbientMode.Gradient)
        {
            buffer.SetGlobalVector("AmbientSkyColor", HxVolumetricCamera.Active.AmbientSky * HxVolumetricCamera.Active.AmbientIntensity);
            buffer.SetGlobalVector("AmbientEquatorColor", HxVolumetricCamera.Active.AmbientEquator * HxVolumetricCamera.Active.AmbientIntensity);
            buffer.SetGlobalVector("AmbientGroundColor", HxVolumetricCamera.Active.AmbientGround * HxVolumetricCamera.Active.AmbientIntensity);
            return 1;
        }
        return 2;
        //if (RenderSettings.ambientMode == AmbientMode.Skybox)
        //{
        //    return 2;
        //}
        //
        //if (RenderSettings.ambientMode == AmbientMode.Custom)
        //{
        //    return 3;
        //}

    }

    float getContrast()
    {
        if (CustomNoiseContrast) { return NoiseContrast; }
        return HxVolumetricCamera.Active.NoiseContrast;
    }

    bool LastBufferDirectional = false;

    bool ShaderModel4()
    {
       
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D9 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
        {
            return false;


        }
            return false;
    }

    void BuildDirectionalBuffer(CommandBuffer CameraBuffer)
    {
#if UNITY_EDITOR
        HxVolumetricCamera.Active.DirectionalUsed = true;
#endif
   
        bool RenderShadows = LightShadow() != LightShadows.None && Shadows;

        if (dirty)
        {
            if (RenderShadows)
            {
                if (BufferCopy == null) { BufferCopy = new CommandBuffer(); BufferCopy.name = "ShadowCopy"; BufferCopy.SetGlobalTexture(HxVolumetricCamera.ShadowMapTexturePID, BuiltinRenderTextureType.CurrentActive); }
                if (BufferRender == null) { BufferRender = new CommandBuffer(); BufferRender.name = "VolumetricRender"; }
                bufferBuilt = true;
                CameraBuffer = BufferRender;
                BufferRender.Clear();
            }
            if (RenderShadows && HxVolumetricCamera.Active.ShadowFix) { Graphics.DrawMesh(HxVolumetricCamera.BoxMesh, HxVolumetricCamera.Active.transform.position, HxVolumetricCamera.Active.transform.rotation, HxVolumetricCamera.ShadowMaterial, 0, HxVolumetricCamera.ActiveCamera, 0, null, true); }

            Vector3 forward = transform.forward;

            if (CustomFogHeightEnabled ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled)
            {
                CameraBuffer.SetGlobalVector(FogHeightsPID, new Vector3((CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight) - (CustomFogTransitionSize ? FogTransitionSize : HxVolumetricCamera.Active.FogTransitionSize), (CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight), (CustomAboveFogPercent ? AboveFogPercent : HxVolumetricCamera.Active.AboveFogPercent)));
            }
            float d = GetFogDensity();
            CameraBuffer.SetGlobalVector("MaxRayDistance", new Vector2(Mathf.Min(QualitySettings.shadowDistance, (CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxDirectionalRayDistance)), (CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxDirectionalRayDistance)));
            float phaseG = (CustomMieScatter ? MieScattering : HxVolumetricCamera.Active.MieScattering);
            Vector4 phase = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG * phaseG), 1.0f + (phaseG * phaseG), 2.0f * phaseG);
            float phaseG2 = (CustomSunSize ? SunSize : HxVolumetricCamera.Active.SunSize);
            CameraBuffer.SetGlobalVector("SunSize", new Vector2((phaseG2 == 0 ? 0 : 1), ((CustomSunBleed ? SunBleed : HxVolumetricCamera.Active.SunBleed) ? 1 : 0)));
            phaseG2 = Mathf.Lerp(0.9999f, 0.995f, Mathf.Pow(phaseG2, 4));

           // if (RenderShadows) { Graphics.DrawMesh(HxVolumetricCamera.BoxMesh, Matrix4x4.TRS(HxVolumetricCamera.Active.transform.position, Quaternion.identity, new Vector3(HxVolumetricCamera.Active.MaxDirectionalRayDistance, HxVolumetricCamera.Active.MaxDirectionalRayDistance, HxVolumetricCamera.Active.MaxDirectionalRayDistance) * 2), HxVolumetricCamera.ShadowMaterial, 0, HxVolumetricCamera.ActiveCamera, 0, null, ShadowCastingMode.ShadowsOnly); }

#if UNITY_X_Y_OR_NEWER
            LoadVolumeData();
#endif


            LoadVolumeData();
            LoadVolumeDateIntoBuffer(CameraBuffer);

            Vector4 phase2 = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG2 * phaseG2), 1.0f + (phaseG2 * phaseG2), 2.0f * phaseG2);
            CameraBuffer.SetGlobalVector("Phase2", phase2);
            CameraBuffer.SetGlobalVector(PhasePID, phase);
            SetColors(CameraBuffer);

            CameraBuffer.SetGlobalVector(_LightParamsPID, new Vector4((CustomStrength ? Strength : LightShadowStrength()), 0, 0, (CustomIntensity ? Intensity : LightIntensity())));
            CameraBuffer.SetGlobalVector(DensityPID, new Vector4(d, GetSampleCount(RenderShadows), 0, (CustomExtinction ? Extinction : HxVolumetricCamera.Active.Extinction)));
            CameraBuffer.SetGlobalVector(ShadowBiasPID, new Vector3(LightShadowBias(), LightNearPlane(), (1.0f - (CustomStrength ? Strength : LightShadowStrength())) * phase.x * (phase.y / (Mathf.Pow(phase.z - phase.w * -1, 1.5f)))));
            CameraBuffer.SetGlobalVector(_SpotLightParamsPID, new Vector4(forward.x, forward.y, forward.z, 0));
            Vector3 finalScale = (CustomNoiseScale ? NoiseScale : HxVolumetricCamera.Active.NoiseScale);
            finalScale = new Vector3(1f / finalScale.x, 1f / finalScale.y, 1f / finalScale.z) / 32.0f;
            CameraBuffer.SetGlobalVector(NoiseScalePID, finalScale);
            if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
            CameraBuffer.SetGlobalVector(NoiseOffsetPID, (CustomNoiseVelocity ? Offset : HxVolumetricCamera.Active.Offset));

            CameraBuffer.SetGlobalFloat("FirstLight", (HxVolumetricCamera.FirstDirectional ? 1 : 0));
            CameraBuffer.SetGlobalFloat("AmbientStrength", HxVolumetricCamera.Active.AmbientLightingStrength);

            HxVolumetricCamera.FirstDirectional = false;
            if (RenderShadows) //dont have to switch if rendering none shadow casting lights
            {
                if (HxVolumetricCamera.Active.TransparencySupport)
                {
                    CameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTransparency[(int)HxVolumetricCamera.Active.compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
                }
                else
                {
                    CameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
                }
            }

            CameraBuffer.SetGlobalMatrix(HxVolumetricLight.VolumetricMVPPID, HxVolumetricCamera.BlitMatrixMVP);

            CameraBuffer.SetGlobalFloat("ExtinctionEffect", HxVolumetricCamera.Active.ExtinctionEffect);

            int mid = MID(RenderShadows, HxVolumetricCamera.ActiveFull());


            if (CustomNoiseEnabled ? NoiseEnabled : HxVolumetricCamera.Active.NoiseEnabled)
            {
                if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }
                Texture3D n = GetNoiseTexture();
                if (n != null) { propertyBlock.SetFloat("hxNoiseContrast", getContrast()); propertyBlock.SetTexture("NoiseTexture3D", n); }

            }

            CameraBuffer.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, HxVolumetricCamera.GetDirectionalMaterial(mid), 0, DirectionalPass(CameraBuffer), propertyBlock);

            //need to figure out how cookies work with directional lights.
            // if (LightCookie() != null)
            // {
            //         
            //     propertyBlock.SetTexture(Shader.PropertyToID("DirectionCookieTexture"), LightCookie());
            //     CameraBuffer.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, HxVolumetricCamera.DirectionalMaterial[MID()], 0, 0, //propertyBlock);
            //
            // }
            // else
            // {
            //     CameraBuffer.DrawMesh(HxVolumetricCamera.QuadMesh, HxVolumetricCamera.BlitMatrix, HxVolumetricCamera.DirectionalMaterial[MID()]);
            // }


            if (RenderShadows)
            {
                myLight.AddCommandBuffer(LightEvent.AfterShadowMap, BufferCopy); //have to add again because of bug.
                myLight.AddCommandBuffer(LightEvent.AfterScreenspaceMask, BufferRender);
            }
        }
    }

   
    void LoadVolumeDateIntoBuffer(CommandBuffer buffer)
    {
#if UNITY_5_4_OR_NEWER
                if (ShaderModel4())
        {
        buffer.SetGlobalMatrixArray("hxVolumeMatrix", VolumeMatrixArrays);
        buffer.SetGlobalVectorArray("hxVolumeSettings", VolumeSettingsArrays);
        }
        else
        {
        buffer.SetGlobalMatrixArray("hxVolumeMatrixOld", VolumeMatrixArraysOld);
        buffer.SetGlobalVectorArray("hxVolumeSettingsOld", VolumeSettingsArraysOld);
        }
#else
        //cant use arrays cause unity commands buffers didnt have it

        if (ShaderModel4())
        {
            buffer.SetGlobalMatrix("hxVolumeMatrix0", VolumeMatrixArrays[0]);
            buffer.SetGlobalVector("hxVolumeSettings0", VolumeSettingsArrays[0]);
            buffer.SetGlobalMatrix("hxVolumeMatrix1", VolumeMatrixArrays[1]);
            buffer.SetGlobalVector("hxVolumeSettings1", VolumeSettingsArrays[1]);
            buffer.SetGlobalMatrix("hxVolumeMatrix2", VolumeMatrixArrays[2]);
            buffer.SetGlobalVector("hxVolumeSettings2", VolumeSettingsArrays[2]);
            buffer.SetGlobalMatrix("hxVolumeMatrix3", VolumeMatrixArrays[3]);
            buffer.SetGlobalVector("hxVolumeSettings3", VolumeSettingsArrays[3]);
            buffer.SetGlobalMatrix("hxVolumeMatrix4", VolumeMatrixArrays[4]);
            buffer.SetGlobalVector("hxVolumeSettings4", VolumeSettingsArrays[4]);
            buffer.SetGlobalMatrix("hxVolumeMatrix5", VolumeMatrixArrays[5]);
            buffer.SetGlobalVector("hxVolumeSettings5", VolumeSettingsArrays[5]);
            buffer.SetGlobalMatrix("hxVolumeMatrix6", VolumeMatrixArrays[6]);
            buffer.SetGlobalVector("hxVolumeSettings6", VolumeSettingsArrays[6]);
            buffer.SetGlobalMatrix("hxVolumeMatrix7", VolumeMatrixArrays[7]);
            buffer.SetGlobalVector("hxVolumeSettings7", VolumeSettingsArrays[7]);
            buffer.SetGlobalMatrix("hxVolumeMatrix8", VolumeMatrixArrays[8]);
            buffer.SetGlobalVector("hxVolumeSettings8", VolumeSettingsArrays[8]);
            buffer.SetGlobalMatrix("hxVolumeMatrix9", VolumeMatrixArrays[9]);
            buffer.SetGlobalVector("hxVolumeSettings9", VolumeSettingsArrays[9]);
            buffer.SetGlobalMatrix("hxVolumeMatrix10", VolumeMatrixArrays[10]);
            buffer.SetGlobalVector("hxVolumeSettings10", VolumeSettingsArrays[10]);


            buffer.SetGlobalMatrix("hxVolumeMatrix10", VolumeMatrixArrays[10]);
            buffer.SetGlobalVector("hxVolumeSettings10", VolumeSettingsArrays[10]);
            buffer.SetGlobalMatrix("hxVolumeMatrix11", VolumeMatrixArrays[11]);
            buffer.SetGlobalVector("hxVolumeSettings11", VolumeSettingsArrays[11]);
            buffer.SetGlobalMatrix("hxVolumeMatrix12", VolumeMatrixArrays[12]);
            buffer.SetGlobalVector("hxVolumeSettings12", VolumeSettingsArrays[12]);
            buffer.SetGlobalMatrix("hxVolumeMatrix13", VolumeMatrixArrays[13]);
            buffer.SetGlobalVector("hxVolumeSettings13", VolumeSettingsArrays[13]);
            buffer.SetGlobalMatrix("hxVolumeMatrix14", VolumeMatrixArrays[14]);
            buffer.SetGlobalVector("hxVolumeSettings14", VolumeSettingsArrays[14]);
            buffer.SetGlobalMatrix("hxVolumeMatrix15", VolumeMatrixArrays[15]);
            buffer.SetGlobalVector("hxVolumeSettings15", VolumeSettingsArrays[15]);
            buffer.SetGlobalMatrix("hxVolumeMatrix16", VolumeMatrixArrays[16]);
            buffer.SetGlobalVector("hxVolumeSettings16", VolumeSettingsArrays[16]);
            buffer.SetGlobalMatrix("hxVolumeMatrix17", VolumeMatrixArrays[17]);
            buffer.SetGlobalVector("hxVolumeSettings17", VolumeSettingsArrays[17]);
            buffer.SetGlobalMatrix("hxVolumeMatrix18", VolumeMatrixArrays[18]);
            buffer.SetGlobalVector("hxVolumeSettings18", VolumeSettingsArrays[18]);
            buffer.SetGlobalMatrix("hxVolumeMatrix19", VolumeMatrixArrays[19]);
            buffer.SetGlobalVector("hxVolumeSettings19", VolumeSettingsArrays[19]);

            buffer.SetGlobalMatrix("hxVolumeMatrix20", VolumeMatrixArrays[20]);
            buffer.SetGlobalVector("hxVolumeSettings20", VolumeSettingsArrays[20]);
            buffer.SetGlobalMatrix("hxVolumeMatrix21", VolumeMatrixArrays[21]);
            buffer.SetGlobalVector("hxVolumeSettings21", VolumeSettingsArrays[21]);
            buffer.SetGlobalMatrix("hxVolumeMatrix22", VolumeMatrixArrays[22]);
            buffer.SetGlobalVector("hxVolumeSettings22", VolumeSettingsArrays[22]);
            buffer.SetGlobalMatrix("hxVolumeMatrix23", VolumeMatrixArrays[23]);
            buffer.SetGlobalVector("hxVolumeSettings23", VolumeSettingsArrays[23]);
            buffer.SetGlobalMatrix("hxVolumeMatrix24", VolumeMatrixArrays[24]);
            buffer.SetGlobalVector("hxVolumeSettings24", VolumeSettingsArrays[24]);
            buffer.SetGlobalMatrix("hxVolumeMatrix25", VolumeMatrixArrays[25]);
            buffer.SetGlobalVector("hxVolumeSettings25", VolumeSettingsArrays[25]);
            buffer.SetGlobalMatrix("hxVolumeMatrix26", VolumeMatrixArrays[26]);
            buffer.SetGlobalVector("hxVolumeSettings26", VolumeSettingsArrays[26]);
            buffer.SetGlobalMatrix("hxVolumeMatrix27", VolumeMatrixArrays[27]);
            buffer.SetGlobalVector("hxVolumeSettings27", VolumeSettingsArrays[27]);
            buffer.SetGlobalMatrix("hxVolumeMatrix28", VolumeMatrixArrays[28]);
            buffer.SetGlobalVector("hxVolumeSettings28", VolumeSettingsArrays[28]);
            buffer.SetGlobalMatrix("hxVolumeMatrix29", VolumeMatrixArrays[29]);
            buffer.SetGlobalVector("hxVolumeSettings29", VolumeSettingsArrays[29]);

            buffer.SetGlobalMatrix("hxVolumeMatrix30", VolumeMatrixArrays[30]);
            buffer.SetGlobalVector("hxVolumeSettings30", VolumeSettingsArrays[30]);
            buffer.SetGlobalMatrix("hxVolumeMatrix31", VolumeMatrixArrays[31]);
            buffer.SetGlobalVector("hxVolumeSettings31", VolumeSettingsArrays[31]);
            buffer.SetGlobalMatrix("hxVolumeMatrix32", VolumeMatrixArrays[32]);
            buffer.SetGlobalVector("hxVolumeSettings32", VolumeSettingsArrays[32]);
            buffer.SetGlobalMatrix("hxVolumeMatrix33", VolumeMatrixArrays[33]);
            buffer.SetGlobalVector("hxVolumeSettings33", VolumeSettingsArrays[33]);
            buffer.SetGlobalMatrix("hxVolumeMatrix34", VolumeMatrixArrays[34]);
            buffer.SetGlobalVector("hxVolumeSettings34", VolumeSettingsArrays[34]);
            buffer.SetGlobalMatrix("hxVolumeMatrix35", VolumeMatrixArrays[35]);
            buffer.SetGlobalVector("hxVolumeSettings35", VolumeSettingsArrays[35]);
            buffer.SetGlobalMatrix("hxVolumeMatrix36", VolumeMatrixArrays[36]);
            buffer.SetGlobalVector("hxVolumeSettings36", VolumeSettingsArrays[36]);
            buffer.SetGlobalMatrix("hxVolumeMatrix37", VolumeMatrixArrays[37]);
            buffer.SetGlobalVector("hxVolumeSettings37", VolumeSettingsArrays[37]);
            buffer.SetGlobalMatrix("hxVolumeMatrix38", VolumeMatrixArrays[38]);
            buffer.SetGlobalVector("hxVolumeSettings38", VolumeSettingsArrays[38]);
            buffer.SetGlobalMatrix("hxVolumeMatrix39", VolumeMatrixArrays[39]);
            buffer.SetGlobalVector("hxVolumeSettings39", VolumeSettingsArrays[39]);

            buffer.SetGlobalMatrix("hxVolumeMatrix40", VolumeMatrixArrays[40]);
            buffer.SetGlobalVector("hxVolumeSettings40", VolumeSettingsArrays[40]);
            buffer.SetGlobalMatrix("hxVolumeMatrix41", VolumeMatrixArrays[41]);
            buffer.SetGlobalVector("hxVolumeSettings41", VolumeSettingsArrays[41]);
            buffer.SetGlobalMatrix("hxVolumeMatrix42", VolumeMatrixArrays[42]);
            buffer.SetGlobalVector("hxVolumeSettings42", VolumeSettingsArrays[42]);
            buffer.SetGlobalMatrix("hxVolumeMatrix43", VolumeMatrixArrays[43]);
            buffer.SetGlobalVector("hxVolumeSettings43", VolumeSettingsArrays[43]);
            buffer.SetGlobalMatrix("hxVolumeMatrix44", VolumeMatrixArrays[44]);
            buffer.SetGlobalVector("hxVolumeSettings44", VolumeSettingsArrays[44]);
            buffer.SetGlobalMatrix("hxVolumeMatrix45", VolumeMatrixArrays[45]);
            buffer.SetGlobalVector("hxVolumeSettings45", VolumeSettingsArrays[45]);
            buffer.SetGlobalMatrix("hxVolumeMatrix46", VolumeMatrixArrays[46]);
            buffer.SetGlobalVector("hxVolumeSettings46", VolumeSettingsArrays[46]);
            buffer.SetGlobalMatrix("hxVolumeMatrix47", VolumeMatrixArrays[47]);
            buffer.SetGlobalVector("hxVolumeSettings47", VolumeSettingsArrays[47]);
            buffer.SetGlobalMatrix("hxVolumeMatrix48", VolumeMatrixArrays[48]);
            buffer.SetGlobalVector("hxVolumeSettings48", VolumeSettingsArrays[48]);
            buffer.SetGlobalMatrix("hxVolumeMatrix49", VolumeMatrixArrays[49]);
            buffer.SetGlobalVector("hxVolumeSettings49", VolumeSettingsArrays[49]);




        }
        else
        {
            buffer.SetGlobalMatrix("hxVolumeMatrixOld0", VolumeMatrixArraysOld[0]);
            buffer.SetGlobalVector("hxVolumeSettingsOld0", VolumeSettingsArraysOld[0]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld1", VolumeMatrixArraysOld[1]);
            buffer.SetGlobalVector("hxVolumeSettingsOld1", VolumeSettingsArraysOld[1]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld2", VolumeMatrixArraysOld[2]);
            buffer.SetGlobalVector("hxVolumeSettingsOld2", VolumeSettingsArraysOld[2]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld3", VolumeMatrixArraysOld[3]);
            buffer.SetGlobalVector("hxVolumeSettingsOld3", VolumeSettingsArraysOld[3]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld4", VolumeMatrixArraysOld[4]);
            buffer.SetGlobalVector("hxVolumeSettingsOld4", VolumeSettingsArraysOld[4]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld5", VolumeMatrixArraysOld[5]);
            buffer.SetGlobalVector("hxVolumeSettingsOld5", VolumeSettingsArraysOld[5]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld6", VolumeMatrixArraysOld[6]);
            buffer.SetGlobalVector("hxVolumeSettingsOld6", VolumeSettingsArraysOld[6]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld7", VolumeMatrixArraysOld[7]);
            buffer.SetGlobalVector("hxVolumeSettingsOld7", VolumeSettingsArraysOld[7]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld8", VolumeMatrixArraysOld[8]);
            buffer.SetGlobalVector("hxVolumeSettingsOld8", VolumeSettingsArraysOld[8]);
            buffer.SetGlobalMatrix("hxVolumeMatrixOld9", VolumeMatrixArraysOld[9]);
            buffer.SetGlobalVector("hxVolumeSettingsOld9", VolumeSettingsArraysOld[9]);
            
        }
       

#endif
    }

    float CalcLightInstensityDistance(float distance)
    {
        return Mathf.InverseLerp((CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxLightDistance), (CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxLightDistance) * 0.8f, distance);
        //1.0f - Mathf.Clamp01(1.0f / ((CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxLightDistance) * 0.2f) * (distance - ((CustomMaxLightDistance ? MaxLightDistance : HxVolumetricCamera.Active.MaxLightDistance) * 0.8f)));
    }

    void BuildSpotLightBuffer(CommandBuffer cameraBuffer)
    {
#if UNITY_EDITOR
        HxVolumetricCamera.Active.SpotUsed = true;
#endif

      
        float Distance = ClosestDistanceToCone(HxVolumetricCamera.Active.transform.position);

        float distanceLerp = CalcLightInstensityDistance(Distance);

        if (distanceLerp > 0)
        {
            bool RenderShadows = LightShadow() != LightShadows.None && Shadows;
            if (RenderShadows)
            {

                if (Distance > QualitySettings.shadowDistance - ShadowDistanceExtra)
                {
                    RenderShadows = false;
                }
            }

            //DrawIntersect();


            if (dirty)
            {
                if (RenderShadows)
                {
                    if (BufferRender == null) { BufferRender = new CommandBuffer(); BufferRender.name = "VolumetricRender"; }

                    bufferBuilt = true;
                    cameraBuffer = BufferRender;
                    BufferRender.Clear();
                }

                cameraBuffer.SetGlobalTexture(HxVolumetricCamera.ShadowMapTexturePID, BuiltinRenderTextureType.CurrentActive);
                //if (SystemInfo.supportsRawShadowDepthSampling)
                //{
                ////    cameraBuffer.SetShadowSamplingMode(new RenderTargetIdentifier(BuiltinRenderTextureType.CurrentActive), ShadowSamplingMode.RawDepth);
                //}//set directional color and fog settings
                SetColors(cameraBuffer, distanceLerp);


                if (RenderShadows) //dont have to switch if rendering none shadow casting lights
                {
                    if (HxVolumetricCamera.Active.TransparencySupport)
                    {
                        cameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTransparency[(int)HxVolumetricCamera.Active.compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
                    }
                    else
                    {
                        cameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
                    }
                }

                if (CustomFogHeightEnabled ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled)
                {
                    cameraBuffer.SetGlobalVector(FogHeightsPID, new Vector3((CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight) - (CustomFogTransitionSize ? FogTransitionSize : HxVolumetricCamera.Active.FogTransitionSize), (CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight), (CustomAboveFogPercent ? AboveFogPercent : HxVolumetricCamera.Active.AboveFogPercent)));
                }

                LoadVolumeDataBounds();
                LoadVolumeDateIntoBuffer(cameraBuffer);

                float d = GetFogDensity();
                cameraBuffer.SetGlobalMatrix(VolumetricMVPPID, HxVolumetricCamera.Active.MatrixVP * LightMatrix);
                cameraBuffer.SetGlobalMatrix(VolumetricMVPID, HxVolumetricCamera.Active.MatrixV * LightMatrix);
                float phaseG = (CustomMieScatter ? MieScattering : HxVolumetricCamera.Active.MieScattering);

                Vector4 phase = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG * phaseG), 1.0f + (phaseG * phaseG), 2.0f * phaseG);
                cameraBuffer.SetGlobalVector(PhasePID, phase);
                cameraBuffer.SetGlobalVector(_CustomLightPositionPID, transform.position);

                cameraBuffer.SetGlobalVector(_LightParamsPID, new Vector4((CustomStrength ? Strength : LightShadowStrength()), 1f / LightRange(), LightRange(), (CustomIntensity ? Intensity : LightIntensity())));

                cameraBuffer.SetGlobalVector(DensityPID, new Vector4(d, GetSampleCount(RenderShadows), 0, (CustomExtinction ? Extinction : HxVolumetricCamera.Active.Extinction)));
                if (RenderShadows) { Graphics.DrawMesh(HxVolumetricCamera.SpotLightMesh, LightMatrix, HxVolumetricCamera.ShadowMaterial, 0, HxVolumetricCamera.ActiveCamera, 0, null, ShadowCastingMode.ShadowsOnly); }


                //float a = LightRange() / (LightRange() - (LightNearPlane()));
                //float b = LightRange() * (LightNearPlane()) / (LightNearPlane() - LightRange());

                float a = (1 - LightRange() / LightNearPlane()) / LightRange();
                float b = (LightRange() / LightNearPlane()) / LightRange();



                cameraBuffer.SetGlobalVector(ShadowBiasPID, new Vector4(a, b, (1.0f - (CustomStrength ? Strength : LightShadowStrength())) * phase.x * (phase.y / (Mathf.Pow(phase.z - phase.w * -1, 1.5f))), LightShadowBias()));

                Vector3 finalScale = (CustomNoiseScale ? NoiseScale : HxVolumetricCamera.Active.NoiseScale);
                finalScale = new Vector3(1f / finalScale.x, 1f / finalScale.y, 1f / finalScale.z) / 32.0f;
                cameraBuffer.SetGlobalFloat(hxNearPlanePID, NearPlane);
                cameraBuffer.SetGlobalVector(NoiseScalePID, finalScale);
                if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
                cameraBuffer.SetGlobalVector(NoiseOffsetPID, (CustomNoiseVelocity ? Offset : HxVolumetricCamera.Active.Offset));
                Vector3 forward = transform.forward;
                cameraBuffer.SetGlobalVector(_SpotLightParamsPID, new Vector4(forward.x, forward.y, forward.z, (LightSpotAngle() + 0.01f) / 2f * Mathf.Deg2Rad));
                if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }
                propertyBlock.SetTexture(_LightTexture0PID, (LightCookie() == null ? HxVolumetricCamera.Active.SpotLightCookie : LightCookie()));
                propertyBlock.SetTexture(_hxProjectorFalloffTexturePID, LightFalloffTexture());


                cameraBuffer.SetGlobalVector("TopFrustumNormal", TopFrustumNormal);
                cameraBuffer.SetGlobalVector("BottomFrustumNormal", BottomFrustumNormal);
                cameraBuffer.SetGlobalVector("LeftFrustumNormal", LeftFrustumNormal);
                cameraBuffer.SetGlobalVector("RightFrustumNormal", RightFrustumNormal);



                int mid = MID(RenderShadows, HxVolumetricCamera.ActiveFull());


                if (CustomNoiseEnabled ? NoiseEnabled : HxVolumetricCamera.Active.NoiseEnabled)
                {
                    if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }
                    Texture3D n = GetNoiseTexture();
                    if (n != null) { propertyBlock.SetFloat("hxNoiseContrast", getContrast()); propertyBlock.SetTexture("NoiseTexture3D", n); }
                }


                    cameraBuffer.DrawMesh(HxVolumetricCamera.SpotLightMesh, LightMatrix, HxVolumetricCamera.GetSpotMaterial(mid), 0, (lastBounds.SqrDistance(HxVolumetricCamera.Active.transform.position) < ((HxVolumetricCamera.ActiveCamera.nearClipPlane * 2) * (HxVolumetricCamera.ActiveCamera.nearClipPlane * 2)) ? 0 : 1), propertyBlock);
                

                if (RenderShadows)
                {
                    myLight.AddCommandBuffer(LightEvent.AfterShadowMap, BufferRender);
                }
            }
        }
    }

    void BuildProjectorLightBuffer(CommandBuffer cameraBuffer)
    {
#if UNITY_EDITOR
        HxVolumetricCamera.Active.ProjectorUsed = true;
#endif



        float dis = Mathf.Sqrt(lastBounds.SqrDistance(HxVolumetricCamera.ActiveCamera.transform.position));
        float distanceLerp = CalcLightInstensityDistance(dis);

        //DrawIntersect();


        if (distanceLerp > 0 && dirty)
            {
            SetColors(cameraBuffer, distanceLerp);
            cameraBuffer.SetGlobalTexture(HxVolumetricCamera.ShadowMapTexturePID, BuiltinRenderTextureType.CurrentActive);

                if (CustomFogHeightEnabled ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled)
                {
                    cameraBuffer.SetGlobalVector(FogHeightsPID, new Vector3((CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight) - (CustomFogTransitionSize ? FogTransitionSize : HxVolumetricCamera.Active.FogTransitionSize), (CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight), (CustomAboveFogPercent ? AboveFogPercent : HxVolumetricCamera.Active.AboveFogPercent)));
                }

                LoadVolumeDataBounds();
                LoadVolumeDateIntoBuffer(cameraBuffer);

                float d = GetFogDensity();
                cameraBuffer.SetGlobalMatrix(VolumetricMVPPID, HxVolumetricCamera.Active.MatrixVP * LightMatrix);
                cameraBuffer.SetGlobalMatrix(VolumetricMVPID, HxVolumetricCamera.Active.MatrixV * LightMatrix);
                float phaseG = (CustomMieScatter ? MieScattering : HxVolumetricCamera.Active.MieScattering);

                Vector4 phase = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG * phaseG), 1.0f + (phaseG * phaseG), 2.0f * phaseG);
                cameraBuffer.SetGlobalVector(PhasePID, phase);
                cameraBuffer.SetGlobalVector(_CustomLightPositionPID, transform.position);

                cameraBuffer.SetGlobalVector(_LightParamsPID, new Vector4((CustomStrength ? Strength : LightShadowStrength()), 1f / LightRange(), LightRange(), (CustomIntensity ? Intensity : LightIntensity())));

                cameraBuffer.SetGlobalVector(DensityPID, new Vector4(d, GetSampleCount(false), 0, (CustomExtinction ? Extinction : HxVolumetricCamera.Active.Extinction)));


                Vector3 finalScale = (CustomNoiseScale ? NoiseScale : HxVolumetricCamera.Active.NoiseScale);
                finalScale = new Vector3(1f / finalScale.x, 1f / finalScale.y, 1f / finalScale.z) / 32.0f;
                cameraBuffer.SetGlobalFloat(hxNearPlanePID, NearPlane);
                cameraBuffer.SetGlobalVector(NoiseScalePID, finalScale);
                if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
                cameraBuffer.SetGlobalVector(NoiseOffsetPID, (CustomNoiseVelocity ? Offset : HxVolumetricCamera.Active.Offset));
                Vector3 forward = transform.forward;
                cameraBuffer.SetGlobalVector(_SpotLightParamsPID, new Vector4(forward.x, forward.y, forward.z, (LightSpotAngle() + 0.01f) / 2f * Mathf.Deg2Rad));
                if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }
                propertyBlock.SetTexture(_hxProjectorTexturePID, myProjector.material.GetTexture(_hxProjectorTexturePID));
                propertyBlock.SetTexture(_hxProjectorFalloffTexturePID, (LightFalloff != null ? LightFalloff : myProjector.material.GetTexture(_hxProjectorFalloffTexturePID)));

                /// _Color("Main Color", Color) = (1,1,1,1)

                cameraBuffer.SetGlobalVector("TopFrustumNormal", TopFrustumNormal);
                cameraBuffer.SetGlobalVector("BottomFrustumNormal", BottomFrustumNormal);
                cameraBuffer.SetGlobalVector("LeftFrustumNormal", LeftFrustumNormal);
                cameraBuffer.SetGlobalVector("RightFrustumNormal", RightFrustumNormal);
                cameraBuffer.SetGlobalFloat("OrthoLight", myProjector.orthographic ? 1 : 0);



             cameraBuffer.SetGlobalVector("UpFrustumOffset", transform.up * (myProjector.orthographic ? myProjector.orthographicSize : 0));
    
             cameraBuffer.SetGlobalVector("RightFrustumOffset", transform.right * (myProjector.orthographic ? myProjector.orthographicSize * myProjector.aspectRatio : 0));

            int mid = MID(false, HxVolumetricCamera.ActiveFull());


                if (CustomNoiseEnabled ? NoiseEnabled : HxVolumetricCamera.Active.NoiseEnabled)
                {
                    if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }
                    Texture3D n = GetNoiseTexture();
                    if (n != null) { propertyBlock.SetFloat("hxNoiseContrast", getContrast()); propertyBlock.SetTexture("NoiseTexture3D", n); }
                }

            if (myProjector.orthographic)
            {
                cameraBuffer.DrawMesh(HxVolumetricCamera.OrthoProjectorMesh, LightMatrix, HxVolumetricCamera.GetProjectorMaterial(mid), 0, (dis < HxVolumetricCamera.ActiveCamera.nearClipPlane ? 0 : 1), propertyBlock);
            }
            else
            {

                cameraBuffer.DrawMesh(HxVolumetricCamera.SpotLightMesh, LightMatrix, HxVolumetricCamera.GetProjectorMaterial(mid), 0, (dis < HxVolumetricCamera.ActiveCamera.nearClipPlane ? 0 : 1), propertyBlock);
            }

            }
        
    }

    void SetColors(CommandBuffer buffer, float distanceLerp)
    {
        Vector4 BasedColor = (CustomColor ? Color : LightColor()) * (CustomIntensity ? Intensity : LightIntensity()) * distanceLerp;

        if (CustomTintMode ? TintMode == HxVolumetricCamera.HxTintMode.Off : HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Off)
        {
            buffer.SetGlobalVector(LightColourPID, BasedColor);
            buffer.SetGlobalVector(LightColour2PID, BasedColor);
        }
        else if (CustomTintMode ? TintMode == HxVolumetricCamera.HxTintMode.Color : HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Color)
        {
            buffer.SetGlobalVector(LightColourPID, CalcTintColor(BasedColor));
            buffer.SetGlobalVector(LightColour2PID, CalcTintColor(BasedColor));
        }
        else if (CustomTintMode ? TintMode == HxVolumetricCamera.HxTintMode.Edge : HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Edge)
        {
            buffer.SetGlobalVector(LightColourPID, BasedColor);
            buffer.SetGlobalVector(LightColour2PID, CalcTintColor(BasedColor));
            buffer.SetGlobalFloat("TintPercent", 1f / (CustomTintGradient ? TintGradient : HxVolumetricCamera.Active.TintGradient) / 2f);
        }
        else if (CustomTintMode ? TintMode == HxVolumetricCamera.HxTintMode.Gradient : HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Gradient)
        {
            buffer.SetGlobalVector(LightColourPID, CalcTintColor(BasedColor));
            buffer.SetGlobalVector(LightColour2PID, CalcTintColorEdge(BasedColor));
            buffer.SetGlobalFloat("TintPercent", 1f / (CustomTintGradient ? TintGradient : HxVolumetricCamera.Active.TintGradient) / 2f);
        }

    }

    void SetColors(CommandBuffer buffer)
    {
        Vector4 BasedColor = (CustomColor ? Color : LightColor()) * (CustomIntensity ? Intensity : LightIntensity());

        if (HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Off)
        {
            buffer.SetGlobalVector(LightColourPID, BasedColor);
            buffer.SetGlobalVector(LightColour2PID, BasedColor);
        }
        else if (HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Color)
        {
            buffer.SetGlobalVector(LightColourPID, CalcTintColor(BasedColor));
            buffer.SetGlobalVector(LightColour2PID, CalcTintColor(BasedColor));
        }
        else if (HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Edge)
        {
            buffer.SetGlobalVector(LightColourPID, BasedColor);
            buffer.SetGlobalVector(LightColour2PID, CalcTintColor(BasedColor));
            buffer.SetGlobalFloat("TintPercent", 1f / HxVolumetricCamera.Active.TintGradient / 2f);
        }
        else if (HxVolumetricCamera.Active.TintMode == HxVolumetricCamera.HxTintMode.Gradient)
        {
            buffer.SetGlobalVector(LightColourPID, CalcTintColor(BasedColor));
            buffer.SetGlobalVector(LightColour2PID, CalcTintColorEdge(BasedColor));
            buffer.SetGlobalFloat("TintPercent", 1f / HxVolumetricCamera.Active.TintGradient / 2f);
        }
    }

    Vector3 CalcTintColor(Vector4 c)
    {
        Vector3 old = new Vector3(c.x, c.y, c.z);
        float mag = old.magnitude;
        if (CustomTintColor)
        {
            old += new Vector3((QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor : TintColor.linear).r, (QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor : TintColor.linear).g, (QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor : TintColor.linear).b) * (CustomTintIntensity ? TintIntensity : HxVolumetricCamera.Active.TintIntensity);
        }
        else
        {
            old += HxVolumetricCamera.Active.CurrentTint;
        }


        return old.normalized * mag;
    }

    Vector3 CalcTintColorEdge(Vector4 c)
    {
        Vector3 old = new Vector3(c.x, c.y, c.z);
        float mag = old.magnitude;

        if (CustomTintColor2)
        {
            old += new Vector3((QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor2 : TintColor2.linear).r, (QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor2 : TintColor2.linear).g, (QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor2 : TintColor2.linear).b) * (CustomTintIntensity ? TintIntensity : HxVolumetricCamera.Active.TintIntensity);
        }
        else
        {
            old += HxVolumetricCamera.Active.CurrentTintEdge;
        }
        return old.normalized * mag;
    }

    void BuildPointBuffer(CommandBuffer cameraBuffer)
    {
#if UNITY_EDITOR
        HxVolumetricCamera.Active.PointUsed = true;
#endif
        float distance = Mathf.Max(Vector3.Distance(HxVolumetricCamera.Active.transform.position, transform.position) - LightRange(), 0);
        float distanceLerp = CalcLightInstensityDistance(distance);

        if (distanceLerp > 0)
        {

            bool RenderShadows = LightShadow() != LightShadows.None && Shadows;

            if (RenderShadows)
            {
                if (distance >= QualitySettings.shadowDistance - ShadowDistanceExtra)
                {
                    RenderShadows = false;
                }
            }

            if (dirty)
            {
                if (RenderShadows)
                {
                    if (BufferRender == null) { BufferRender = new CommandBuffer(); BufferRender.name = "VolumetricRender"; }
                    bufferBuilt = true;
                    cameraBuffer = BufferRender;
                    BufferRender.Clear();
                }

                cameraBuffer.SetGlobalTexture(HxVolumetricCamera.ShadowMapTexturePID, BuiltinRenderTextureType.CurrentActive);
                SetColors(cameraBuffer, distanceLerp);

                if (CustomFogHeightEnabled ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled)
                {
                    cameraBuffer.SetGlobalVector(FogHeightsPID, new Vector3((CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight) - (CustomFogTransitionSize ? FogTransitionSize : HxVolumetricCamera.Active.FogTransitionSize), (CustomFogHeight ? FogHeight : HxVolumetricCamera.Active.FogHeight), (CustomAboveFogPercent ? AboveFogPercent : HxVolumetricCamera.Active.AboveFogPercent)));
                }

                if (RenderShadows) //dont have to switch if rendering none shadow casting lights
                {
                    if (HxVolumetricCamera.Active.TransparencySupport)
                    {
                        cameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTransparency[(int)HxVolumetricCamera.Active.compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
                    }
                    else
                    {
                        cameraBuffer.SetRenderTarget(HxVolumetricCamera.VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
                    }
                }

                LoadVolumeDataBounds();
                LoadVolumeDateIntoBuffer(cameraBuffer);
                float d = GetFogDensity();
                cameraBuffer.SetGlobalMatrix(VolumetricMVPPID, HxVolumetricCamera.Active.MatrixVP * LightMatrix);
                cameraBuffer.SetGlobalMatrix(VolumetricMVPID, HxVolumetricCamera.Active.MatrixV * LightMatrix);
                float phaseG = (CustomMieScatter ? MieScattering : HxVolumetricCamera.Active.MieScattering);
                Vector4 phase = new Vector4(1.0f / (4.0f * Mathf.PI), 1.0f - (phaseG * phaseG), 1.0f + (phaseG * phaseG), 2.0f * phaseG);

                cameraBuffer.SetGlobalVector(PhasePID, phase);
                cameraBuffer.SetGlobalVector(_CustomLightPositionPID, transform.position);
                cameraBuffer.SetGlobalVector(_LightParamsPID, new Vector4((CustomStrength ? Strength : LightShadowStrength()), 1f / LightRange(), LightRange(), (CustomIntensity ? Intensity : LightIntensity())));
                cameraBuffer.SetGlobalVector(DensityPID, new Vector4(d, GetSampleCount(RenderShadows), 0, (CustomExtinction ? Extinction : HxVolumetricCamera.Active.Extinction)));

                cameraBuffer.SetGlobalVector(ShadowBiasPID, new Vector3(LightShadowBias(), LightNearPlane(), (1.0f - (CustomStrength ? Strength : LightShadowStrength())) * phase.x * (phase.y / (Mathf.Pow(phase.z - phase.w * -1, 1.5f)))));

                Vector3 finalScale = (CustomNoiseScale ? NoiseScale : HxVolumetricCamera.Active.NoiseScale);
                finalScale = new Vector3(1f / finalScale.x, 1f / finalScale.y, 1f / finalScale.z) / 32.0f;
                cameraBuffer.SetGlobalVector(NoiseScalePID, finalScale);
                if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
                cameraBuffer.SetGlobalVector(NoiseOffsetPID, (CustomNoiseVelocity ? Offset : HxVolumetricCamera.Active.Offset));
                if (RenderShadows && HxVolumetricCamera.Active.ShadowFix) { Graphics.DrawMesh(HxVolumetricCamera.BoxMesh, LightMatrix, HxVolumetricCamera.ShadowMaterial, 0, HxVolumetricCamera.ActiveCamera, 0, null, ShadowCastingMode.ShadowsOnly); }

                int pass = (distance <= LightRange() + LightRange() * 0.09f + HxVolumetricCamera.ActiveCamera.nearClipPlane * 2 ? 0 : 1); //near or far


                if (propertyBlock == null) { propertyBlock = new MaterialPropertyBlock(); }

               

                int mid = MID(RenderShadows, HxVolumetricCamera.ActiveFull());
        
                if (CustomNoiseEnabled ? NoiseEnabled : HxVolumetricCamera.Active.NoiseEnabled)
                {
                   
                    Texture3D n = GetNoiseTexture();
                    if (n != null) { propertyBlock.SetFloat("hxNoiseContrast", getContrast()); propertyBlock.SetTexture("NoiseTexture3D", n); }
                }

                propertyBlock.SetTexture(_hxProjectorFalloffTexturePID, LightFalloffTexture());

                if (LightCookie() != null)
                {
                    //_LightTexture0PID
                    propertyBlock.SetTexture(Shader.PropertyToID("PointCookieTexture"), LightCookie());

                    cameraBuffer.DrawMesh(HxVolumetricCamera.SphereMesh, LightMatrix, HxVolumetricCamera.GetPointMaterial(mid), 0, pass, propertyBlock);
                }
                else
                {
                    cameraBuffer.DrawMesh(HxVolumetricCamera.SphereMesh, LightMatrix, HxVolumetricCamera.GetPointMaterial(mid), 0, pass, propertyBlock);
                }




                if (RenderShadows)
                {
                    myLight.AddCommandBuffer(LightEvent.AfterShadowMap, BufferRender);
                }

            }
        }
    }

    public int MID(bool RenderShadows, bool full)
    {
        int i = 0;
        if (RenderShadows) { i += 1; }
        if (LightCookie() != null) { i += 2; }
        if (CustomNoiseEnabled ? NoiseEnabled : HxVolumetricCamera.Active.NoiseEnabled) { i += 4;
#if UNITY_EDITOR
            HxVolumetricCamera.Active.NoiseUsed = true;
#endif
        }
        else
        {
#if UNITY_EDITOR
            HxVolumetricCamera.Active.NoiseOffUsed = true;
#endif
        }
        if (CustomFogHeight ? FogHeightEnabled : HxVolumetricCamera.Active.FogHeightEnabled)
        {
            i += 8;
#if UNITY_EDITOR
            HxVolumetricCamera.Active.HeightFogUsed = true;
#endif
        }
        else
        {
#if UNITY_EDITOR
            HxVolumetricCamera.Active.HeightFogOffUsed = true;
#endif
        }
        if (HxVolumetricCamera.Active.renderDensityParticleCheck()) { i += 16;
#if UNITY_EDITOR
            HxVolumetricCamera.Active.DensityParticlesUsed = true;
#endif
        }

        if (HxVolumetricCamera.Active.TransparencySupport) { i += 32;
#if UNITY_EDITOR
            HxVolumetricCamera.Active.TransparencyUsed = true;
#endif
        }
        else
        {
#if UNITY_EDITOR
            HxVolumetricCamera.Active.TransparencyOffUsed = true;
#endif
        }
        if (full) { i += 64; }
#if UNITY_EDITOR

#endif

        return i;
    }

    void Update()
    {
        OffsetUpdated = false;

       // DrawBounds();
    }

    float GetFogDensity()
    {
        if (CustomDensity)
        {
            return Density + ExtraDensity;
        }
        return HxVolumetricCamera.Active.Density + ExtraDensity;
    }

    Texture3D GetNoiseTexture()
    {
        if (CustomNoiseTexture)
        {
            if (NoiseTexture3D == null) { return HxVolumetricCamera.Active.NoiseTexture3D; }
            return NoiseTexture3D;
        }
        return HxVolumetricCamera.Active.NoiseTexture3D;
    }


    int GetSampleCount(bool RenderShadows)
    {
        int sample = (CustomSampleCount ? SampleCount : (GetLightType() != LightType.Directional ? HxVolumetricCamera.Active.SampleCount : HxVolumetricCamera.Active.DirectionalSampleCount));

        // if (!RenderShadows)
        // {
        //     sample = Mathf.Max(4, sample/8);
        // }

        return Mathf.Max(2, sample);
    }

    public static Vector3 ClosestPointOnLine(Vector3 vA, Vector3 vB, Vector3 vPoint)
    {
        Vector3 vVector1 = vPoint - vA;
        Vector3 vVector2 = (vB - vA).normalized;

        float d = Vector3.Distance(vA, vB);
        float t = Vector3.Dot(vVector2, vVector1);

        if (t <= 0)
            return vA;

        if (t >= d)
            return vB;

        var vVector3 = vVector2 * t;

        var vClosestPoint = vA + vVector3;

        return vClosestPoint;
    }

    float ClosestDistanceToCone(Vector3 Point)
    {
        //this could be faster. but it works.
        Vector3 Axis = transform.forward * LightRange();
        Vector3 planePosition = (transform.position + Axis);
        float planeDistance = Vector3.Dot(transform.forward, (Point - planePosition));
        if (planeDistance == 0) { return 0; }

        Vector3 closestPoint = Point - planeDistance * transform.forward;
        float s = Mathf.Tan(LightSpotAngle() / 2f * Mathf.Deg2Rad) * LightRange();

        Vector3 dif = (closestPoint - planePosition);

        if (planeDistance > 0)
        {
            closestPoint = planePosition + dif.normalized * Mathf.Min(dif.magnitude, s);
            return Vector3.Distance(Point, closestPoint);
        }

        float a = Mathf.Deg2Rad * LightSpotAngle();

        float c = Mathf.Acos((Vector3.Dot((Point - transform.position), -Axis)) / ((Point - transform.position).magnitude * LightRange()));
        if (Mathf.Abs(c - a) >= Mathf.PI / 2.0f)
        {
            return 0; //inside
        }
        closestPoint = planePosition + dif.normalized * s;


        closestPoint = ClosestPointOnLine(closestPoint, transform.position, Point);



        return Vector3.Distance(Point, closestPoint);

    }

    float LastSpotAngle = 0;
    float LastRange = 0;
    float LastAspect = 0;



    LightType lastType = LightType.Area;
    Matrix4x4 LightMatrix;

    Bounds lastBounds = new Bounds();
    Vector3 minBounds;
    Vector3 maxBounds;
    HxOctreeNode<HxVolumetricLight>.NodeObject octreeNode;

    void UpdateLightMatrix()
    {
        LastRange = LightRange();
        LastSpotAngle = LightSpotAngle();
        lastType = GetLightType();
        if (GetLightType() == LightType.Point)
        {
            LightMatrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(LightRange() * 2f, LightRange() * 2f, LightRange() * 2f)); matrixReconstruct = false;
        }
        else if (GetLightType() == LightType.Spot)
        {

            float s = Mathf.Tan(LightSpotAngle() / 2f * Mathf.Deg2Rad) * LightRange();

            LightMatrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(s * 2f, s * 2f, LightRange()));
        }
        else if (GetLightType() == LightType.Area)
        {
            if (myProjector != null)
            {
               

                if (myProjector.orthographic)
                {
                    LightMatrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(myProjector.orthographicSize * myProjector.aspectRatio * 2, myProjector.orthographicSize * 2, LightRange()));
                }
                else
                {
                    float s = Mathf.Tan(LightSpotAngle() / 2f * Mathf.Deg2Rad) * LightRange();
                    LightMatrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(s * 2f , s * 2f, LightRange()));
                }
            }

        }
        transform.hasChanged = false;
        matrixReconstruct = false;
    }

    void CheckLightType()
    {
        if (lastType != GetLightType())
        {
            if (lastType == LightType.Directional)
            {
             
                octreeNode = HxVolumetricCamera.AddLightOctree(this, minBounds, maxBounds);
                HxVolumetricCamera.ActiveDirectionalLights.Remove(this);
            }
            else if (GetLightType() == LightType.Directional && (lastType != LightType.Directional))
            {
                HxVolumetricCamera.RemoveLightOctree(this);
                octreeNode = null;
                HxVolumetricCamera.ActiveDirectionalLights.Add(this);
            }
        }

        lastType = GetLightType();
    }

    Vector4 TopFrustumNormal;
    Vector4 BottomFrustumNormal;

    Vector4 LeftFrustumNormal;
    Vector4 RightFrustumNormal;

    static Matrix4x4[] VolumeMatrixArrays =  new Matrix4x4[50];
    static Vector4[] VolumeSettingsArrays = new Vector4[50];

    static Matrix4x4[] VolumeMatrixArraysOld = new Matrix4x4[10];
    static Vector4[] VolumeSettingsArraysOld = new Vector4[10];
    void LoadVolumeData()
    {
        if (ShaderModel4())
        {
            int c = 0;
            for (int i = 0; i < HxVolumetricCamera.ActiveVolumes.Count; i++)
            {
                if (HxVolumetricCamera.ActiveVolumes[i].enabled && ((HxVolumetricCamera.ActiveVolumes[i].BlendMode != HxDensityVolume.DensityBlendMode.Add || HxVolumetricCamera.ActiveVolumes[i].BlendMode != HxDensityVolume.DensityBlendMode.Sub) && HxVolumetricCamera.ActiveVolumes[i].Density != 0))
                {
                    VolumeMatrixArrays[c] = HxVolumetricCamera.ActiveVolumes[i].ToLocalSpace;
                    VolumeSettingsArrays[c] = new Vector2(HxVolumetricCamera.ActiveVolumes[i].Density, (int)HxVolumetricCamera.ActiveVolumes[i].BlendMode + ((int)HxVolumetricCamera.ActiveVolumes[i].Shape * 4));
    
                    c++;

                }
                if (c >= 50) { break; }
            }

            if (c < 49)
            {
                VolumeSettingsArrays[c] = new Vector2(0, -1);
            }
        }
        else
        {
            int c = 0;
            for (int i = 0; i < HxVolumetricCamera.ActiveVolumes.Count; i++)
            {
                if (HxVolumetricCamera.ActiveVolumes[i].enabled && ((HxVolumetricCamera.ActiveVolumes[i].BlendMode != HxDensityVolume.DensityBlendMode.Add || HxVolumetricCamera.ActiveVolumes[i].BlendMode != HxDensityVolume.DensityBlendMode.Sub) && HxVolumetricCamera.ActiveVolumes[i].Density != 0))
                {
                    VolumeMatrixArraysOld[c] = HxVolumetricCamera.ActiveVolumes[i].ToLocalSpace;
                    VolumeSettingsArraysOld[c] = new Vector2(HxVolumetricCamera.ActiveVolumes[i].Density, (int)HxVolumetricCamera.ActiveVolumes[i].BlendMode + ((int)HxVolumetricCamera.ActiveVolumes[i].Shape * 4));
             
                    c++;

                }
                if (c >= 10) { break; }
            }

            if (c < 9)
            {
                VolumeSettingsArraysOld[c] = new Vector2(0, -1);
            }
        }
    }

    bool  BoundsIntersect(HxDensityVolume vol)
    {
        return (minBounds.x <= vol.maxBounds.x && maxBounds.x >= vol.minBounds.x) &&
               (minBounds.y <= vol.maxBounds.y && maxBounds.y >= vol.minBounds.y) &&
               (minBounds.z <= vol.maxBounds.z && maxBounds.z >= vol.minBounds.z);
    }

    void LoadVolumeDataBounds()
    {
        if (ShaderModel4())
        {
            int c = 0;
            for (int i = 0; i < HxVolumetricCamera.ActiveVolumes.Count; i++)
            {
                if (HxVolumetricCamera.ActiveVolumes[i].enabled && ((HxVolumetricCamera.ActiveVolumes[i].BlendMode != HxDensityVolume.DensityBlendMode.Add || HxVolumetricCamera.ActiveVolumes[i].BlendMode != HxDensityVolume.DensityBlendMode.Sub) && HxVolumetricCamera.ActiveVolumes[i].Density != 0) && BoundsIntersect(HxVolumetricCamera.ActiveVolumes[i]))
                {
                    VolumeMatrixArrays[c] = HxVolumetricCamera.ActiveVolumes[i].ToLocalSpace;
                    VolumeSettingsArrays[c] = new Vector2(HxVolumetricCamera.ActiveVolumes[i].Density, (int)HxVolumetricCamera.ActiveVolumes[i].BlendMode + ((int)HxVolumetricCamera.ActiveVolumes[i].Shape * 4));
                
                    c++;

                }
                if (c >= 50) { break; }
            }

            if (c < 49)
            {
                VolumeSettingsArrays[c] = new Vector2(0, -1);
            }
        }
        else
        {
            int c = 0;
            for (int i = 0; i < HxVolumetricCamera.ActiveVolumes.Count; i++)
            {
                if (HxVolumetricCamera.ActiveVolumes[i].enabled && ((HxVolumetricCamera.ActiveVolumes[i].BlendMode != HxDensityVolume.DensityBlendMode.Add || HxVolumetricCamera.ActiveVolumes[i].BlendMode != HxDensityVolume.DensityBlendMode.Sub) && HxVolumetricCamera.ActiveVolumes[i].Density != 0) && BoundsIntersect(HxVolumetricCamera.ActiveVolumes[i]))
                {
                    VolumeMatrixArraysOld[c] = HxVolumetricCamera.ActiveVolumes[i].ToLocalSpace;
                    VolumeSettingsArraysOld[c] = new Vector2(HxVolumetricCamera.ActiveVolumes[i].Density, (int)HxVolumetricCamera.ActiveVolumes[i].BlendMode + ((int)HxVolumetricCamera.ActiveVolumes[i].Shape * 4));

                    c++;

                }
                if (c >= 10) { break; }
            }

            if (c < 9)
            {
                VolumeSettingsArraysOld[c] = new Vector2(0, -1);
            }
        }

    }

    Vector4 NormalOfTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 dir = Vector3.Cross(b - a, c - a);
        dir.Normalize();
        return new Vector4(dir.x, dir.y, dir.z, 0);
    }

    void DrawIntersect()
    {

  

            Vector3 topDenom;
           Vector3 top;
           Vector3 bottomDenom;
         Vector3 bottom;

        Vector3 _SpotLightParams = transform.forward;
        Vector3 rayDir = HxVolumetricCamera.Active.transform.forward;
        Vector3 _CustomLightPosition = transform.position;
        Vector3 _WorldSpaceCameraPos = HxVolumetricCamera.Active.transform.position;

            bottomDenom.x = Vector3.Dot(_SpotLightParams, rayDir);
            bottom.x = Vector3.Dot(((_CustomLightPosition + _SpotLightParams * LightRange()) - _WorldSpaceCameraPos), _SpotLightParams);

            bottomDenom.y = Vector3.Dot(BottomFrustumNormal, rayDir);
            bottom.y = Vector3.Dot(_CustomLightPosition - _WorldSpaceCameraPos, (Vector3)BottomFrustumNormal);

            bottomDenom.z = Vector3.Dot(LeftFrustumNormal, rayDir);
            bottom.z = Vector3.Dot(_CustomLightPosition - _WorldSpaceCameraPos, (Vector3)LeftFrustumNormal);

            topDenom.x = Vector3.Dot(-_SpotLightParams, rayDir);
            top.x = Vector3.Dot(((_CustomLightPosition + _SpotLightParams * NearPlane) - _WorldSpaceCameraPos),-_SpotLightParams);

            topDenom.y = Vector3.Dot(TopFrustumNormal, rayDir);
            top.y = Vector3.Dot(_CustomLightPosition - _WorldSpaceCameraPos, (Vector3)TopFrustumNormal);

            topDenom.z = Vector3.Dot(RightFrustumNormal, rayDir);
            top.z = Vector3.Dot(_CustomLightPosition - _WorldSpaceCameraPos, (Vector3)RightFrustumNormal);

            float near = 0;
            float far = 100000;
            if (topDenom.x > 0)
            {
                far = Mathf.Min(far, top.x / topDenom.x);
            }
            else if (topDenom.x < 0)
            {
                near = Mathf.Max(near, top.x / topDenom.x);
            }

            if (topDenom.y > 0)
            {
                far = Mathf.Min(far, top.y / topDenom.y);
            }
            else if (topDenom.y < 0)
            {
                near = Mathf.Max(near, top.y / topDenom.y);
            }

            if (topDenom.z > 0)
            {
                far = Mathf.Min(far, top.z / topDenom.z);
            }
            else if (topDenom.z < 0)
            {
                near = Mathf.Max(near, top.z / topDenom.z);
            }

            if (bottomDenom.x > 0)
            {
                far = Mathf.Min(far, bottom.x / bottomDenom.x);
            }
            else if (bottomDenom.x < 0)
            {
                near = Mathf.Max(near, bottom.x / bottomDenom.x);
            }

            if (bottomDenom.y > 0)
            {
                far = Mathf.Min(far, bottom.y / bottomDenom.y);
            }
            else if (bottomDenom.y < 0)
            {
                near = Mathf.Max(near, bottom.y / bottomDenom.y);
            }

            if (bottomDenom.z > 0)
            {
                far = Mathf.Min(far, bottom.z / bottomDenom.z);
            }
            else if (bottomDenom.z < 0)
            {
                near = Mathf.Max(near, bottom.z / bottomDenom.z);
            }
       

        Debug.DrawLine(_WorldSpaceCameraPos, _WorldSpaceCameraPos + (Vector3)RightFrustumNormal);


        Debug.DrawLine(_WorldSpaceCameraPos + rayDir * near, _WorldSpaceCameraPos + rayDir * near + Vector3.up);
        Debug.DrawLine(_WorldSpaceCameraPos + rayDir * near, _WorldSpaceCameraPos + rayDir * far);
        
    }

    float GetAspect()
    {
        if (myProjector != null)
        {
            return myProjector.aspectRatio;
        }
        return 0;
    }


    float LastOrthoSize;
    bool LastOrtho;
    float GetOrthoSize()
    {
        if (myProjector != null) { return myProjector.orthographicSize; }
        return 0;
    }

    bool GetOrtho()
    {
        if (myProjector != null) { return myProjector.orthographic; }
        return false;
    }

    public void UpdatePosition(bool first = false)
    {

        if (first || transform.hasChanged || matrixReconstruct || LastRange != LightRange() || LastSpotAngle != LightSpotAngle() || lastType != GetLightType() || (GetLightType() == LightType.Area && (LastAspect != GetAspect() || LastOrthoSize == GetOrthoSize() || LastOrtho == GetOrtho() )))
        {
            if (GetLightType() == LightType.Point)
            {
                Vector3 dif = new Vector3(LightRange(), LightRange(), LightRange());
                minBounds = transform.position - dif;
                maxBounds = transform.position + dif;
                lastBounds.SetMinMax(minBounds, maxBounds);

                if (!first) { CheckLightType(); HxVolumetricCamera.LightOctree.Move(octreeNode, minBounds, maxBounds); } else { lastType = GetLightType(); }
                UpdateLightMatrix();
               
            }
            else if (GetLightType() == LightType.Spot)
            {
                Vector3 pos = transform.position;
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                Vector3 up = transform.up;

                Vector3 farCenter = pos + forward * LightRange();

                float farHeight = Mathf.Tan((LightSpotAngle() * Mathf.Deg2Rad) / 2f) * LightRange();

                Vector3 farTopLeft = farCenter + up * (farHeight) - right * (farHeight);
                Vector3 farTopRight = farCenter + up * (farHeight) + right * (farHeight);
                Vector3 farBottomLeft = farCenter - up * (farHeight) - right * (farHeight);
                Vector3 farBottomRight = farCenter - up * (farHeight) + right * (farHeight);


                TopFrustumNormal = NormalOfTriangle(pos, farTopLeft, farTopRight);
                BottomFrustumNormal = NormalOfTriangle(pos, farBottomRight, farBottomLeft);

                LeftFrustumNormal = NormalOfTriangle(pos, farBottomLeft, farTopLeft);
                RightFrustumNormal = NormalOfTriangle(pos, farTopRight, farBottomRight);

                minBounds = new Vector3(Mathf.Min(farTopLeft.x, Mathf.Min(farTopRight.x, Mathf.Min(farBottomLeft.x, Mathf.Min(farBottomRight.x, pos.x)))), Mathf.Min(farTopLeft.y, Mathf.Min(farTopRight.y, Mathf.Min(farBottomLeft.y, Mathf.Min(farBottomRight.y, pos.y)))), Mathf.Min(farTopLeft.z, Mathf.Min(farTopRight.z, Mathf.Min(farBottomLeft.z, Mathf.Min(farBottomRight.z, pos.z)))));
                maxBounds = new Vector3(Mathf.Max(farTopLeft.x, Mathf.Max(farTopRight.x, Mathf.Max(farBottomLeft.x, Mathf.Max(farBottomRight.x, pos.x)))), Mathf.Max(farTopLeft.y, Mathf.Max(farTopRight.y, Mathf.Max(farBottomLeft.y, Mathf.Max(farBottomRight.y, pos.y)))), Mathf.Max(farTopLeft.z, Mathf.Max(farTopRight.z, Mathf.Max(farBottomLeft.z, Mathf.Max(farBottomRight.z, pos.z)))));
                lastBounds.SetMinMax(minBounds, maxBounds);
                if (!first) { CheckLightType(); HxVolumetricCamera.LightOctree.Move(octreeNode, minBounds, maxBounds); } else { lastType = GetLightType(); }
                UpdateLightMatrix();
     
            }
            else
            {
                if (myProjector != null)
                {
                    Vector3 pos = transform.position;
                    Vector3 forward = transform.forward;
                    Vector3 right = transform.right;
                    Vector3 up = transform.up;

                    Vector3 farCenter = pos + forward * LightRange();
                    Vector3 nearCenter = pos + forward * myProjector.nearClipPlane;



                    float farHeight; 
                    float nearHeight; 

                    if (myProjector.orthographic)
                    {
                        farHeight = myProjector.orthographicSize;
                        nearHeight = myProjector.orthographicSize;
                    }
                    else
                    {
                        farHeight = Mathf.Tan((LightSpotAngle() * Mathf.Deg2Rad) / 2f) * LightRange();
                        nearHeight = Mathf.Tan((LightSpotAngle() * Mathf.Deg2Rad) / 2f) * myProjector.nearClipPlane;
                    }

                    Vector3 farTopLeft = farCenter + up * (farHeight) - right * (farHeight * myProjector.aspectRatio);
                    Vector3 farTopRight = farCenter + up * (farHeight) + right * (farHeight * myProjector.aspectRatio);
                    Vector3 farBottomLeft = farCenter - up * (farHeight) - right * (farHeight * myProjector.aspectRatio);
                    Vector3 farBottomRight = farCenter - up * (farHeight) + right * (farHeight * myProjector.aspectRatio);

                    Vector3 nearTopLeft = nearCenter + up * (nearHeight) - right * (nearHeight * myProjector.aspectRatio);
                    Vector3 nearTopRight = nearCenter + up * (nearHeight) + right * (nearHeight * myProjector.aspectRatio);
                    Vector3 nearBottomLeft = nearCenter - up * (nearHeight) - right * (nearHeight * myProjector.aspectRatio);
                    Vector3 nearBottomRight = nearCenter - up * (nearHeight) + right * (nearHeight * myProjector.aspectRatio);


                        TopFrustumNormal = NormalOfTriangle(nearTopLeft, farTopLeft, farTopRight);
                        BottomFrustumNormal = NormalOfTriangle(nearBottomRight, farBottomRight, farBottomLeft);

                        LeftFrustumNormal = NormalOfTriangle(nearBottomLeft, farBottomLeft, farTopLeft);
                        RightFrustumNormal = NormalOfTriangle(nearTopRight, farTopRight, farBottomRight);


                    LastOrtho = GetOrtho();

                    minBounds = Vector3.Min(farTopLeft, Vector3.Min(farTopRight, Vector3.Min(farBottomLeft, Vector3.Min(farBottomRight, Vector3.Min(nearTopLeft, Vector3.Min(nearTopRight, Vector3.Min(nearBottomLeft, nearBottomRight)))))));

                    maxBounds = Vector3.Max(farTopLeft, Vector3.Max(farTopRight, Vector3.Max(farBottomLeft, Vector3.Max(farBottomRight, Vector3.Max(nearTopLeft, Vector3.Max(nearTopRight, Vector3.Max(nearBottomLeft, nearBottomRight)))))));
                    LastOrthoSize = GetOrthoSize();
                    lastBounds.SetMinMax(minBounds, maxBounds);
                    if (!first) { CheckLightType(); HxVolumetricCamera.LightOctree.Move(octreeNode, minBounds, maxBounds); } else { lastType = GetLightType(); }
                    LastAspect = GetAspect();
                    UpdateLightMatrix();
                   
                }

                if (!first) { CheckLightType(); } else { lastType = GetLightType(); }
            }

        }
    }



    public void DrawBounds()
    {
        if (GetLightType() != LightType.Directional)
        {

            Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, minBounds.z), new Vector3(maxBounds.x, minBounds.y, minBounds.z), LightColor());
            Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, minBounds.z), new Vector3(maxBounds.x, minBounds.y, maxBounds.z), LightColor());
            Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, maxBounds.z), new Vector3(minBounds.x, minBounds.y, maxBounds.z), LightColor());
            Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, maxBounds.z), new Vector3(minBounds.x, minBounds.y, minBounds.z), LightColor());
            Debug.DrawLine(new Vector3(minBounds.x, maxBounds.y, minBounds.z), new Vector3(maxBounds.x, maxBounds.y, minBounds.z), LightColor());
            Debug.DrawLine(new Vector3(maxBounds.x, maxBounds.y, minBounds.z), new Vector3(maxBounds.x, maxBounds.y, maxBounds.z), LightColor());
            Debug.DrawLine(new Vector3(maxBounds.x, maxBounds.y, maxBounds.z), new Vector3(minBounds.x, maxBounds.y, maxBounds.z), LightColor());
            Debug.DrawLine(new Vector3(minBounds.x, maxBounds.y, maxBounds.z), new Vector3(minBounds.x, maxBounds.y, minBounds.z), LightColor());

            Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, minBounds.z), new Vector3(minBounds.x, maxBounds.y, minBounds.z), LightColor());
            Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, minBounds.z), new Vector3(maxBounds.x, maxBounds.y, minBounds.z), LightColor());
            Debug.DrawLine(new Vector3(maxBounds.x, minBounds.y, maxBounds.z), new Vector3(maxBounds.x, maxBounds.y, maxBounds.z), LightColor());
            Debug.DrawLine(new Vector3(minBounds.x, minBounds.y, maxBounds.z), new Vector3(minBounds.x, maxBounds.y, maxBounds.z), LightColor());
        }
    }

    
    bool matrixReconstruct = true;

}
