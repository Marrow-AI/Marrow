//#define HxApplyDirect
//#define HxApplyQueue
#if UNITY_5_4_OR_NEWER && !UNITY_5_4_0 && (UNITY_STANDALONE_WIN || UNITY_PS4 || UNITY_EDITOR_OSX || UNITY_WEBGL || UNITY_ANDROID)
#define HXVR
#endif
#define HxApplyImageEffect
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;





#if HXVR
using UnityEngine.VR;
#else

#endif
[ExecuteInEditMode]
public class HxVolumetricCamera : MonoBehaviour
{
   
    public enum hxRenderOrder { ImageEffect = 0, ImageEffectOpaque = 1 };
    public hxRenderOrder RenderOrder = hxRenderOrder.ImageEffect;
    public HxVolumetricRenderCallback callBackImageEffect;
    public HxVolumetricRenderCallback callBackImageEffectOpaque;
    public bool ShadowFix = true;
    void SetUpRenderOrder()
    {

        if (callBackImageEffect != null)
        {
            if (TransparencySupport || RenderOrder == hxRenderOrder.ImageEffectOpaque)
            {
                callBackImageEffect.enabled = false;
            }
            else
            {
                callBackImageEffect.enabled = true;
            }
        }

        if (callBackImageEffectOpaque != null)
        {
            if (TransparencySupport || RenderOrder == hxRenderOrder.ImageEffectOpaque)
            {
                callBackImageEffectOpaque.enabled = true;
            }
            else
            {
                callBackImageEffectOpaque.enabled = false;
            }
        }


        if (callBackImageEffectOpaque == null)
        {
            if (TransparencySupport || RenderOrder == hxRenderOrder.ImageEffectOpaque)
            {
                callBackImageEffectOpaque = gameObject.GetComponent<HxVolumetricImageEffectOpaque>();
                if (callBackImageEffectOpaque == null)
                {
                    callBackImageEffectOpaque = gameObject.AddComponent<HxVolumetricImageEffectOpaque>();
                    callBackImageEffectOpaque.RenderOrder = hxRenderOrder.ImageEffectOpaque;
                }
            }
        }

        if (callBackImageEffect == null)
        {
            if (TransparencySupport || RenderOrder == hxRenderOrder.ImageEffectOpaque)
            {

            }
            else
            {
                callBackImageEffect = gameObject.GetComponent<HxVolumetricImageEffect>();
                if (callBackImageEffect == null)
                {
                    callBackImageEffect = gameObject.AddComponent<HxVolumetricImageEffect>();
                    callBackImageEffect.RenderOrder = hxRenderOrder.ImageEffect;
                }
            }
        }
    }

#if HxApplyDirect
    [System.NonSerialized]
        CommandBuffer BufferApply; //apply in render que.
#endif
    bool TemporalFirst = true;
    public bool TemporalSampling = true;
    [Range(0, 1)]
    public float DitherSpeed = 0.625625625f;
    [Range(0, 1)]
    public float LuminanceFeedback = 0.8f; 
    [Range(0, 1)]
    public float MaxFeedback = 0.9f;
    [Range(0, 4)]
    public float NoiseContrast = 1;
    static Shader directionalShader;
    static Shader pointShader;
    static Shader spotShader;
    static Shader ProjectorShader;
    [System.NonSerialized]
    public bool FullUsed = false;
    [System.NonSerialized]
    public bool LowResUsed = false;
    [System.NonSerialized]
    public bool HeightFogUsed = false;
    [System.NonSerialized]
    public bool HeightFogOffUsed = false;
    [System.NonSerialized]
    public bool NoiseUsed = false;
    [System.NonSerialized]
    public bool NoiseOffUsed = false;
    [System.NonSerialized]
    public bool TransparencyUsed = false;
    [System.NonSerialized]
    public bool TransparencyOffUsed = false;
    [System.NonSerialized]
    public bool DensityParticlesUsed = false;
    [System.NonSerialized]
    public bool PointUsed = false;
    [System.NonSerialized]
    public bool SpotUsed = false;
    [System.NonSerialized]
    public bool ProjectorUsed = false;
    [System.NonSerialized]
    public bool DirectionalUsed = false;
    [System.NonSerialized]
    public bool SinglePassStereoUsed = false;

    static public Material GetDirectionalMaterial(int mid)
    {
        Material outMaterial;
        if (!DirectionalMaterial.TryGetValue(mid, out outMaterial))
        {
            if (directionalShader == null)
            { directionalShader = Shader.Find("Hidden/HxVolumetricDirectionalLight"); }
            CreateShader(directionalShader, mid, out outMaterial, false);
            DirectionalMaterial.Add(mid, outMaterial);
        }
        return outMaterial;
    }

    static public Material GetProjectorMaterial(int mid)
    {
        Material outMaterial;
        if (!ProjectorMaterial.TryGetValue(mid, out outMaterial))
        {
            if (ProjectorShader == null)
            { ProjectorShader = Shader.Find("Hidden/HxVolumetricProjector"); }
            CreateShader(ProjectorShader, mid, out outMaterial, false);
            ProjectorMaterial.Add(mid, outMaterial);
        }
        return outMaterial;
    }

    static public Material GetSpotMaterial(int mid)
    {
        Material outMaterial;
        if (!SpotMaterial.TryGetValue(mid, out outMaterial))
        {
            if (spotShader == null)
            { spotShader = Shader.Find("Hidden/HxVolumetricSpotLight"); }
            CreateShader(spotShader, mid, out outMaterial, false);
            SpotMaterial.Add(mid, outMaterial);
        }
        return outMaterial;
    }

    static public Material GetPointMaterial(int mid)
    {
        Material outMaterial;
        if (!PointMaterial.TryGetValue(mid, out outMaterial))
        {
            if (pointShader == null)
            { pointShader = Shader.Find("Hidden/HxVolumetricPointLight"); }
            CreateShader(pointShader, mid, out outMaterial, true);
            PointMaterial.Add(mid, outMaterial);
        }
        return outMaterial;
    }

    //Edit these values to the same values as volumemtricLightCore
    //There is a limit to the amount of textures that the gpu can sample from for each draw call.
    //if you have complicated transparent shaders it can cause compile issues if you have this setting too high.

    //Amount of Transparency slices used in 3D density texture (dx9 will cap at x12)

    public enum TransparencyQualities { Low = 0, Medium = 1, High = 2, VeryHigh = 3 };
    public enum DensityParticleQualities { Low = 0, Medium = 1, High = 2, VeryHigh = 3 };


    public static TransparencyQualities TransparencyBufferDepth = TransparencyQualities.Medium;

    //Amount of Depth slices used in 3D density texture
    public static DensityParticleQualities DensityBufferDepth = DensityParticleQualities.High;
    //end



    int EnumBufferDepthLength = 4;

    public TransparencyQualities compatibleTBuffer()
    {
        if ((int)TransparencyBufferDepth > 1)
        {
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D11 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.PlayStation4)
            {
                return TransparencyQualities.High;
            }
        }
        return TransparencyBufferDepth;
    }

    bool IsRenderBoth()
    {

#if HXVR
        if (Mycamera.stereoTargetEye == StereoTargetEyeMask.Both && Application.isPlaying && UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRDevice.isPresent)
        {
            return true;
        }
#endif
        return false;
    }

    DensityParticleQualities compatibleDBuffer()
    {
        return DensityBufferDepth;
    }

    Matrix4x4 CurrentView;
    Matrix4x4 CurrentProj;
    Matrix4x4 CurrentInvers;

    Matrix4x4 CurrentView2;
    Matrix4x4 CurrentProj2;
    Matrix4x4 CurrentInvers2;

    
 
    RenderTexture TemporalTexture;

    RenderTargetIdentifier TemporalTextureRTID;

    static RenderTexture VolumetricTexture;

    static RenderTexture FullBlurRT;
    static RenderTargetIdentifier FullBlurRTID;

    static RenderTexture downScaledBlurRT;
    static RenderTargetIdentifier downScaledBlurRTID;

    static RenderTexture FullBlurRT2;
    static RenderTargetIdentifier FullBlurRT2ID;

    static RenderTargetIdentifier[] VolumetricUpsampledBlurTextures = new RenderTargetIdentifier[2];
    static RenderTexture[] VolumetricDensityTextures = new RenderTexture[8];
    static int[] VolumetricDensityPID = new int[4] { 0, 0, 0, 0 };
    static int[] VolumetricTransparencyPID = new int[4] { 0, 0, 0, 0 };
    static RenderTexture[] VolumetricTransparencyTextures = new RenderTexture[8];

    public static RenderTargetIdentifier[][] VolumetricDensity = new RenderTargetIdentifier[][] {
        new RenderTargetIdentifier[1] { new RenderTargetIdentifier()},
        new RenderTargetIdentifier[2] { new RenderTargetIdentifier(), new RenderTargetIdentifier()},
        new RenderTargetIdentifier[3] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[4] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[5] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[6] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[7] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[8] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() }
    };

    public static RenderTargetIdentifier[][] VolumetricTransparency = new RenderTargetIdentifier[][] {
        new RenderTargetIdentifier[2] { new RenderTargetIdentifier(), new RenderTargetIdentifier()},
        new RenderTargetIdentifier[3] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[4] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[5] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[6] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[7] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[8] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[9] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() }
    };

    public static RenderTargetIdentifier[][] VolumetricTransparencyI = new RenderTargetIdentifier[][] {
        new RenderTargetIdentifier[1] { new RenderTargetIdentifier()},
        new RenderTargetIdentifier[2] { new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[3] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[4] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[5] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[6] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[7] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() },
        new RenderTargetIdentifier[8] { new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier(), new RenderTargetIdentifier() }
    };

    static RenderTexture[] ScaledDepthTexture = new RenderTexture[4] { null, null, null, null };

    static ShaderVariantCollection CollectionAll;

    public static Texture2D Tile5x5;

    static int VolumetricTexturePID;
    static int ScaledDepthTexturePID;

    public static int ShadowMapTexturePID;

    public static RenderTargetIdentifier VolumetricTextureRTID;

    public static RenderTargetIdentifier[] ScaledDepthTextureRTID = new RenderTargetIdentifier[4];
    [System.NonSerialized]
    public static Material DownSampleMaterial;
    [System.NonSerialized]
    public static Material VolumeBlurMaterial;
    [System.NonSerialized]
    public static Material TransparencyBlurMaterial;
    [System.NonSerialized]
    public static Material ApplyMaterial;
    [System.NonSerialized]
    public static Material ApplyDirectMaterial;
    [System.NonSerialized]
    public static Material ApplyQueueMaterial;

    public Texture3D NoiseTexture3D = null;

    void MyPreCull(Camera cam) //add a listen event incase there are other camera that dont have this script
    {
        if (cam != ActiveCamera) { ReleaseLightBuffers(); SetUpRenderOrder(); }
    }

    static public Matrix4x4 BlitMatrix;
    static public Matrix4x4 BlitMatrixMV;
    static public Matrix4x4 BlitMatrixMVP;
    static public Vector3 BlitScale;


    [Tooltip("Rending resolution, Lower for more speed, higher for better quality")]
    public Resolution resolution = Resolution.half;
    [Tooltip("How many samples per pixel, Recommended 4-8 for point, 6 - 16 for Directional")]
    [Range(2, 64)]
    public int SampleCount = 4;
    [Tooltip("How many samples per pixel, Recommended 4-8 for point, 6 - 16 for Directional")]
    [Range(2, 64)]
    public int DirectionalSampleCount = 8;
    [Tooltip("Max distance the directional light gets raymarched.")]
    public float MaxDirectionalRayDistance = 128;
    [Tooltip("Any point of spot lights passed this point will not render.")]
    public float MaxLightDistance = 128;


    [Range(0.0f, 1f)]
    [Tooltip("Density of air")]
    public float Density = 0.05f;
    [Range(0.0f, 2f)]
    public float AmbientLightingStrength = 0.5f;


    [Tooltip("0 for even scattering, 1 for forward scattering")]
    [Range(0f, 0.995f)]
    public float MieScattering = 0.4f;
    [Range(0.0f, 1f)]
    [Tooltip("Create a sun using mie Scattering")]
    public float SunSize = 0f;
    [Tooltip("Allows the sun to bleed over the edge of objects (recommend using bloom)")]
    public bool SunBleed = true;
    [Range(0.0f, 0.5f)]
    [Tooltip("dimms results over distance")]
    public float Extinction = 0.05f;
    [Tooltip("Tone down Extinction effect on FinalColor")]
    [Range(0, 1)]
    public float ExtinctionEffect = 0f;

    public bool renderDensityParticleCheck()
    {
        return ParticleDensityRenderCount > 0;// if nothing getting rendered. return false.. todo
    }


    public bool FogHeightEnabled = false;
    public float FogHeight = 5;
    public float FogTransitionSize = 5;

    public float AboveFogPercent = 0.1f;

    public enum HxAmbientMode { UseRenderSettings = 0, Color = 1, Gradient = 2 };
    public enum HxTintMode { Off = 0, Color = 1, Edge = 2, Gradient = 3 };

    [Tooltip("Ambient Mode - Use unitys or overide your own")]
    public HxAmbientMode Ambient = HxAmbientMode.UseRenderSettings;
    public Color AmbientSky = Color.white;
    public Color AmbientEquator = Color.white;
    public Color AmbientGround = Color.white;
    [Range(0, 1)]
    public float AmbientIntensity = 1;

    public HxTintMode TintMode = HxTintMode.Off;
    public Color TintColor = Color.red;
    public Color TintColor2 = Color.blue;
    public float TintIntensity = 0.2f;
    [Range(0, 1)]
    public float TintGradient = 0.2f;

    public Vector3 CurrentTint;
    public Vector3 CurrentTintEdge;
    [Tooltip("Use 3D noise")]
    public bool NoiseEnabled = false;
    [Tooltip("The scale of the noise texture")]
    public Vector3 NoiseScale = new Vector3(0.1f, 0.1f, 0.1f);
    [Tooltip("Used to simulate some wind")]
    public Vector3 NoiseVelocity = new Vector3(1, 0, 1);


    public enum Resolution { full = 0, half = 1, quarter = 2 };
    public enum DensityResolution { full = 0, half = 1, quarter = 2, eighth = 3 };


    [Tooltip("Allows particles to modulate the air density")]
    public bool ParticleDensitySupport = false;
    [Tooltip("Rending resolution of density, Lower for more speed, higher for more detailed dust")]
    public DensityResolution densityResolution = DensityResolution.eighth;

    [Tooltip("Max Distance of density particles")]
    public float densityDistance = 64;
    float densityBias = 1.7f;


    [Tooltip("Enabling Transparency support has a cost - disable if you dont need it")]
    public bool TransparencySupport = false;

    [Tooltip("Max Distance for transparency Support - lower distance will give greater resilts")]
    public float transparencyDistance = 64;
    [Tooltip("Cost a little extra but can remove the grainy look on Transparent objects when sample count is low")]
    [Range(0, 4)]
    public int BlurTransparency = 1;
    float transparencyBias = 1.5f;


    [Range(0, 4)]
    [Tooltip("Blur results of volumetric pass")]
    public int blurCount = 1;
    [Tooltip("Used in final blur pass, Higher number will retain silhouette")]
    public float BlurDepthFalloff = 5f;
    [Tooltip("Used in Downsample blur pass, Higher number will retain silhouette")]
    public float DownsampledBlurDepthFalloff = 5f;
    [Range(0, 4)]
    [Tooltip("Blur bad results after upscaling")]
    public int UpSampledblurCount = 0;

    [Tooltip("If depth is with-in this threshold, bilinearly sample result")]
    public float DepthThreshold = 0.06f;
    [Tooltip("Use gaussian weights - makes blur less blurry but can make it more splotchy")]
    public bool GaussianWeights = false;
    [HideInInspector]
    [Tooltip("Only enable if you arnt using tonemapping and HDR mode")]
    public bool MapToLDR = false;
    [Tooltip("A small amount of noise can be added to remove and color banding from the volumetric effect")]
    public bool RemoveColorBanding = true;


    [System.NonSerialized]
    public Vector3 Offset = Vector3.zero;

    static int DepthThresholdPID;
    static int BlurDepthFalloffPID;

    static int VolumeScalePID;
    static int InverseViewMatrixPID;
    static int InverseProjectionMatrixPID;

#if HXVR
   // bool warned = false;
    static int InverseProjectionMatrix2PID;
#endif
    static int NoiseOffsetPID;
    static int ShadowDistancePID;

    static HxVolumetricShadersUsed UsedShaderSettings;

    void WarmUp()
    {
        //if (WarmUpShaders == ShaderWarm.All)
        //{
        if (CollectionAll == null)
        {
            Object[] x = ((Object[])Resources.LoadAll("HxUsedShaderVariantCollection"));
            for (int i = 0; i < x.Length; i++) { if (x[i] as ShaderVariantCollection != null) { CollectionAll = x[i] as ShaderVariantCollection; break; } }
            if (CollectionAll != null) { CollectionAll.WarmUp(); }
        }

        if (UsedShaderSettings == null)
        {
            UsedShaderSettings = (HxVolumetricShadersUsed)Resources.Load("HxUsedShaders");
            if (UsedShaderSettings != null)
            {
                TransparencyBufferDepth = UsedShaderSettings.LastTransperencyQuality;
                DensityBufferDepth = UsedShaderSettings.LastDensityParticleQuality;
            }
        }
        //}

        //if (WarmUpShaders == ShaderWarm.Scene)
        //{
        //    HxVolumetricCamera.Active = this;
        //    ShaderVariantCollection vc = new ShaderVariantCollection();
        //    HxVolumetricLight[] allLights = Resources.FindObjectsOfTypeAll(typeof(HxVolumetricLight)) as HxVolumetricLight[];
        //    for (int i = 0; i < allLights.Length; i++)
        //    {
        //        Light l = allLights[i].LightSafe();
        //        if (l != null)
        //        {
        //
        //            switch (l.type)
        //            {
        //                case LightType.Directional:
        //                    vc.Add(DirectionalVariant[allLights[i].MID(true,true)]);
        //                    vc.Add(DirectionalVariant[allLights[i].MID(false, true)]);
        //                    vc.Add(DirectionalVariant[allLights[i].MID(true, false)]);
        //                    vc.Add(DirectionalVariant[allLights[i].MID(false, false)]);
        //
        //                    break;
        //                case LightType.Point:
        //                    vc.Add(PointVariant[allLights[i].MID(true, true)]);
        //                    vc.Add(PointVariant[allLights[i].MID(false, true)]);
        //                    vc.Add(PointVariant[allLights[i].MID(true, false)]);
        //                    vc.Add(PointVariant[allLights[i].MID(false, false)]);
        //                    break;
        //                case LightType.Spot:
        //                    vc.Add(SpotVariant[allLights[i].MID(true, true)]);
        //                    vc.Add(SpotVariant[allLights[i].MID(false, true)]);
        //                    vc.Add(SpotVariant[allLights[i].MID(true, false)]);
        //                    vc.Add(SpotVariant[allLights[i].MID(false, false)]);
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }
        //    }
        //    vc.WarmUp();
        //    if (Application.isPlaying)
        //    {
        //        GameObject.Destroy(vc);
        //    }
        //    else
        //    {
        //        GameObject.DestroyImmediate(vc);
        //    }
        //    
        //}
    }

    static List<string> ShaderVariantList = new List<string>(10);
    void CreateShaderVariant(Shader source, int i, ref Material[] material, ref ShaderVariantCollection.ShaderVariant[] Variant, bool point = true)
    {

        ShaderVariantList.Clear();
        //material[i] = new Material(source);

        int v = i;
        int vc = 0;
        if (v >= 64) { material[i].EnableKeyword("FULL_ON"); ShaderVariantList.Add("FULL_ON"); v -= 64; vc++; }
        if (v >= 32) { material[i].EnableKeyword("VTRANSPARENCY_ON"); ShaderVariantList.Add("VTRANSPARENCY_ON"); v -= 32; vc++; }
        if (v >= 16) { material[i].EnableKeyword("DENSITYPARTICLES_ON"); ShaderVariantList.Add("DENSITYPARTICLES_ON"); v -= 16; vc++; }
        if (v >= 8) { material[i].EnableKeyword("HEIGHTFOG_ON"); ShaderVariantList.Add("HEIGHTFOG_ON"); v -= 8; vc++; }
        if (v >= 4) { material[i].EnableKeyword("NOISE_ON"); ShaderVariantList.Add("NOISE_ON"); v -= 4; vc++; }
        if (v >= 2) { if (point) { material[i].EnableKeyword("POINT_COOKIE"); ShaderVariantList.Add("POINT_COOKIE"); vc++; } v -= 2; }
        if (v >= 1) { v -= 1; } else { material[i].EnableKeyword("SHADOWS_OFF"); ShaderVariantList.Add("SHADOWS_OFF"); vc++; };

        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap))
        {
            material[i].EnableKeyword("SHADOWS_NATIVE");
        }

        string[] fv = new string[vc];
        ShaderVariantList.CopyTo(fv);

        //string Final = "";
        //
        //for (int t = 0; t < vc; t++)
        //{
        //    Final += fv[t] + " "; 
        //}
        //Debug.Log(Final);

        Variant[i] = new ShaderVariantCollection.ShaderVariant(source, PassType.Normal, fv);

    }

    static void CreateShader(Shader source, int i, out Material outMaterial, bool point = true)
    {

        // ShaderVariantList.Clear();
        outMaterial = new Material(source);
        outMaterial.hideFlags = HideFlags.DontSave;
        bool WillRenderShadows = false;
        int v = i;
        int vc = 0;
        if (v >= 64) { outMaterial.EnableKeyword("FULL_ON"); v -= 64; vc++; }
        if (v >= 32) { outMaterial.EnableKeyword("VTRANSPARENCY_ON"); v -= 32; vc++; }
        if (v >= 16) { outMaterial.EnableKeyword("DENSITYPARTICLES_ON"); v -= 16; vc++; }
        if (v >= 8) { outMaterial.EnableKeyword("HEIGHTFOG_ON"); v -= 8; vc++; }
        if (v >= 4) { outMaterial.EnableKeyword("NOISE_ON"); v -= 4; vc++; }
        if (v >= 2) { if (point) { outMaterial.EnableKeyword("POINT_COOKIE"); vc++; } v -= 2; }
        if (v >= 1) { v -= 1; WillRenderShadows = true; } else { outMaterial.EnableKeyword("SHADOWS_OFF"); vc++; };

        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Shadowmap) && WillRenderShadows)
        {

            outMaterial.EnableKeyword("SHADOWS_NATIVE");
        }



        // string[] fv = new string[vc];
        // ShaderVariantList.CopyTo(fv);
        //
        // //string Final = "";
        // //
        // //for (int t = 0; t < vc; t++)
        // //{
        // //    Final += fv[t] + " "; 
        // //}
        // //Debug.Log(Final);
        //
        // Variant[i] = new ShaderVariantCollection.ShaderVariant(source, PassType.Normal, fv);

    }


    //static int LastLevelID = -100000;
    //void OnLevelWasLoaded(int level)
    //{
    //    if (LastLevelID != level)
    //    {
    //        LastLevelID = level;
    //        if (!PIDCreated)
    //        {
    //            CreatePIDs();
    //        }
    //        else
    //        {
    //            WarmUp();
    //        }
    //    }
    //}





    void CreatePIDs()
    {
        if (NoiseTexture3D == null) { Create3DNoiseTexture(); }
        bool warmup = false;
        if (!PIDCreated)
        {



            warmup = true;
            

            PIDCreated = true;
            VolumetricTexturePID = Shader.PropertyToID("VolumetricTexture");

            ScaledDepthTexturePID = Shader.PropertyToID("VolumetricDepth");
            ShadowMapTexturePID = Shader.PropertyToID("_ShadowMapTexture");

            DepthThresholdPID = Shader.PropertyToID("DepthThreshold");
            BlurDepthFalloffPID = Shader.PropertyToID("BlurDepthFalloff");

            VolumeScalePID = Shader.PropertyToID("VolumeScale");
            InverseViewMatrixPID = Shader.PropertyToID("InverseViewMatrix");
            InverseProjectionMatrixPID = Shader.PropertyToID("InverseProjectionMatrix");
#if HXVR
            InverseProjectionMatrix2PID = Shader.PropertyToID("InverseProjectionMatrix2");
#endif
            NoiseOffsetPID = Shader.PropertyToID("NoiseOffset");
            ShadowDistancePID = Shader.PropertyToID("ShadowDistance");

            for (int i = 0; i < EnumBufferDepthLength; i++)
            {
                VolumetricDensityPID[i] = Shader.PropertyToID("VolumetricDensityTexture" + i);
                VolumetricTransparencyPID[i] = Shader.PropertyToID("VolumetricTransparencyTexture" + i);
            }

            HxVolumetricLight.CreatePID();

        }
        if (Tile5x5 == null) { CreateTileTexture(); }
        if (DownSampleMaterial == null) { DownSampleMaterial = new Material(Shader.Find("Hidden/HxVolumetricDownscaleDepth")); DownSampleMaterial.hideFlags = HideFlags.DontSave; }
        if (TransparencyBlurMaterial == null) { TransparencyBlurMaterial = new Material(Shader.Find("Hidden/HxTransparencyBlur")); TransparencyBlurMaterial.hideFlags = HideFlags.DontSave; }
        if (DensityMaterial == null) { DensityMaterial = new Material(Shader.Find("Hidden/HxDensityShader")); DensityMaterial.hideFlags = HideFlags.DontSave; }

        if (VolumeBlurMaterial == null) { VolumeBlurMaterial = new Material(Shader.Find("Hidden/HxVolumetricDepthAwareBlur")); VolumeBlurMaterial.hideFlags = HideFlags.DontSave; }
        if (ApplyMaterial == null) { ApplyMaterial = new Material(Shader.Find("Hidden/HxVolumetricApply")); ApplyMaterial.hideFlags = HideFlags.DontSave; }
        if (ApplyDirectMaterial == null) { ApplyDirectMaterial = new Material(Shader.Find("Hidden/HxVolumetricApplyDirect")); ApplyDirectMaterial.hideFlags = HideFlags.DontSave; }
        if (ApplyQueueMaterial == null) { ApplyQueueMaterial = new Material(Shader.Find("Hidden/HxVolumetricApplyRenderQueue")); ApplyQueueMaterial.hideFlags = HideFlags.DontSave; }
        if (QuadMesh == null) { QuadMesh = CreateQuad(); QuadMesh.hideFlags = HideFlags.DontSave; }
        if (BoxMesh == null) { BoxMesh = CreateBox(); }
        if (SphereMesh == null) { SphereMesh = CreateIcoSphere(1, 0.56f); SphereMesh.hideFlags = HideFlags.DontSave; }
        if (SpotLightMesh == null) { SpotLightMesh = CreateCone(4, false); SpotLightMesh.hideFlags = HideFlags.DontSave; }
        if (OrthoProjectorMesh == null) { OrthoProjectorMesh = CreateOrtho(4, false); OrthoProjectorMesh.hideFlags = HideFlags.DontSave; }
        if (directionalShader == null)
        { directionalShader = Shader.Find("Hidden/HxVolumetricDirectionalLight"); }

        if (pointShader == null)
        { pointShader = Shader.Find("Hidden/HxVolumetricPointLight"); }

        if (spotShader == null)
        { spotShader = Shader.Find("Hidden/HxVolumetricSpotLight"); }
        //if (DirectionalMaterial[0] == null)
        //{
        //    
        //    for (int i = 0; i < 128; i++)
        //    {
        //        CreateShaderVariant(directionalShader, i, ref DirectionalMaterial, ref DirectionalVariant,false);
        //    }
        //}
        //
        //if (PointMaterial[0] == null)
        //{
        //    Shader pointShader = Shader.Find("Hidden/HxVolumetricPointLight");
        //    for (int i = 0; i < 128; i++)
        //    {
        //        CreateShaderVariant(pointShader, i, ref PointMaterial, ref PointVariant,true);
        //    }
        //}
        //
        //
        //if (SpotMaterial[0] == null)
        //{
        //    Shader spotShader = Shader.Find("Hidden/HxVolumetricSpotLight");
        //    for (int i = 0; i < 128; i++)
        //    {
        //        CreateShaderVariant(spotShader, i, ref SpotMaterial, ref SpotVariant, false);
        //    }
        //}

        if (warmup) WarmUp();

        if (ShadowMaterial == null)
        {

            ShadowMaterial = new Material(Shader.Find("Hidden/HxShadowCasterFix"));
            ShadowMaterial.hideFlags = HideFlags.DontSave;
        }
    }

    public static bool ActiveFull()
    {
        return Active.resolution == Resolution.full;
    }
    void DefineFull()
    {
        // if (LastRes != (int)resolution)
        // {
        //     LastRes = (int)resolution;
        //     if (resolution == Resolution.full)
        //     {
        //         for (int i = 0; i < SpotMaterial.Length; i++)
        //         {
        //             SpotMaterial[i].EnableKeyword("FULL_ON");
        //
        //         }
        //
        //         for (int i = 0; i < PointMaterial.Length; i++)
        //         {
        //             PointMaterial[i].EnableKeyword("FULL_ON");
        //
        //         }
        //
        //         for (int i = 0; i < DirectionalMaterial.Length; i++)
        //         {
        //             DirectionalMaterial[i].EnableKeyword("FULL_ON");
        //
        //         }
        //     }
        //     else
        //     {
        //         for (int i = 0; i < SpotMaterial.Length; i++)
        //         {
        //
        //             SpotMaterial[i].DisableKeyword("FULL_ON");
        //         }
        //
        //         for (int i = 0; i < PointMaterial.Length; i++)
        //         {
        //
        //             PointMaterial[i].DisableKeyword("FULL_ON");
        //         }
        //
        //         for (int i = 0; i < DirectionalMaterial.Length; i++)
        //         {
        //
        //             DirectionalMaterial[i].DisableKeyword("FULL_ON");
        //         }
        //     }
        // }
    }
    [HideInInspector]
    public static List<HxDensityVolume> ActiveVolumes = new List<HxDensityVolume>();
    public static List<HxVolumetricLight> ActiveLights = new List<HxVolumetricLight>();
    public static List<HxVolumetricParticleSystem> ActiveParticleSystems = new List<HxVolumetricParticleSystem>();

    public static HxOctree<HxVolumetricLight> LightOctree;
    public static HxOctree<HxVolumetricParticleSystem> ParticleOctree;

    static void UpdateLight(HxOctreeNode<HxVolumetricLight>.NodeObject node, Vector3 boundsMin, Vector3 boundsMax)
    {
        LightOctree.Move(node, boundsMin, boundsMax);
    }

    public static HxOctreeNode<HxVolumetricLight>.NodeObject AddLightOctree(HxVolumetricLight light, Vector3 boundsMin, Vector3 boundsMax)
    {
        if (LightOctree == null) { LightOctree = new HxOctree<HxVolumetricLight>(Vector3.zero, 100, 0.1f, 10); }

        return LightOctree.Add(light, boundsMin, boundsMax);
    }


    public static HxOctreeNode<HxVolumetricParticleSystem>.NodeObject AddParticleOctree(HxVolumetricParticleSystem particle, Vector3 boundsMin, Vector3 boundsMax)
    {
        if (ParticleOctree == null) { ParticleOctree = new HxOctree<HxVolumetricParticleSystem>(Vector3.zero, 100, 0.1f, 10); }

        return ParticleOctree.Add(particle, boundsMin, boundsMax);
    }

    public static void RemoveLightOctree(HxVolumetricLight light)
    {
        if (LightOctree != null)
        {
            LightOctree.Remove(light);
        }
    }

    public static void RemoveParticletOctree(HxVolumetricParticleSystem Particle)
    {
        if (ParticleOctree != null)
        {
            ParticleOctree.Remove(Particle);
        }
    }


    void OnApplicationQuit()
    {
        PIDCreated = false;
        //if (SpotMaterial != null)
        //{
        //    for (int i = 0; i < SpotMaterial.Length; i++)
        //    {
        //        if (SpotMaterial[i] != null)
        //        {
        //            GameObject.Destroy(SpotMaterial[i]);
        //            SpotMaterial[i] = null;
        //        }
        //    }
        //}
        //
        //if (PointMaterial != null)
        //{
        //    for (int i = 0; i < PointMaterial.Length; i++)
        //    {
        //        if (PointMaterial[i] != null)
        //        {
        //            GameObject.Destroy(PointMaterial[i]);
        //            PointMaterial[i] = null;
        //        }
        //    }
        //}
        //
        //if (DirectionalMaterial != null)
        //{
        //    for (int i = 0; i < DirectionalMaterial.Length; i++)
        //    {
        //        if (DirectionalMaterial[i] != null)
        //        {
        //            GameObject.Destroy(DirectionalMaterial[i]);
        //            DirectionalMaterial[i] = null;
        //        }
        //    }
        //}

        //if (VolumeBlurMaterial != null) { GameObject.Destroy(VolumeBlurMaterial); VolumeBlurMaterial = null; }
        //if (ApplyQueueMaterial != null) { GameObject.Destroy(ApplyQueueMaterial); ApplyQueueMaterial = null; }
        //if (ApplyMaterial != null) { GameObject.Destroy(ApplyMaterial); ApplyMaterial = null; }
        //if (ApplyDirectMaterial != null) { GameObject.Destroy(ApplyDirectMaterial); ApplyDirectMaterial = null; }
        //if (DownSampleMaterial != null) { GameObject.Destroy(DownSampleMaterial); DownSampleMaterial = null; }
        //if (TransparencyBlurMaterial != null) { GameObject.Destroy(TransparencyBlurMaterial); TransparencyBlurMaterial = null; }
        //if (DensityMaterial != null) { GameObject.Destroy(DensityMaterial); DensityMaterial = null; }
        //if (ShadowMaterial != null) { GameObject.Destroy(ShadowMaterial); ShadowMaterial = null; }
        //if (SpotLightMesh != null) { GameObject.Destroy(SpotLightMesh); SpotLightMesh = null; }
        //if (SphereMesh != null) { GameObject.Destroy(SphereMesh); SphereMesh = null; }
        //if (NoiseTexture3D != null) { GameObject.Destroy(NoiseTexture3D); NoiseTexture3D = null; }


    }

    static public HashSet<HxDensityVolume> AllDensityVolumes = new HashSet<HxDensityVolume>();
    static public HashSet<HxVolumetricLight> AllVolumetricLight = new HashSet<HxVolumetricLight>();
    static public HashSet<HxVolumetricParticleSystem> AllParticleSystems = new HashSet<HxVolumetricParticleSystem>();
    bool test;
    public static Mesh QuadMesh;
    public static Mesh BoxMesh;
    public static Mesh SphereMesh;
    public static Mesh SpotLightMesh;
    public static Mesh OrthoProjectorMesh;
    [HideInInspector]
    Camera Mycamera;

    public Camera GetCamera()
    {
        if (Mycamera == null) { Mycamera = GetComponent<Camera>(); }
        return Mycamera;
    }

    static float[] ResolutionScale = new float[4] { 1, 0.5f, 0.25f, 0.125f };
    public static float[] SampleScale = new float[4] { 1, 4, 16, 32 };

    CommandBuffer BufferSetup; //clear textures and downsample
    CommandBuffer BufferRender; //push camera settings, render nonshadow casting lights, render particles (rebuilt every frame)
    CommandBuffer BufferRenderLights;
    CommandBuffer BufferFinalize; //blur upsample pass



    bool dirty = true;
    [System.NonSerialized]
    public static bool PIDCreated = false;

#if UNITY_EDITOR
    [System.NonSerialized]
    public static bool[] DirectionalMaterialUsed = new bool[128];
    [System.NonSerialized]
    public static bool[] PointMaterialUsed = new bool[128];
    [System.NonSerialized]
    public static bool[] SpotMaterialUsed = new bool[128];
#endif
    [System.NonSerialized]
    static Dictionary<int, Material> DirectionalMaterial = new Dictionary<int, Material>();
    //public static Material[] DirectionalMaterial = new Material[128] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
    [System.NonSerialized]
    static Dictionary<int, Material> PointMaterial = new Dictionary<int, Material>();
    //public static Material[] PointMaterial = new Material[128] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };
    [System.NonSerialized]
    static Dictionary<int, Material> SpotMaterial = new Dictionary<int, Material>();
    [System.NonSerialized]
    static Dictionary<int, Material> ProjectorMaterial = new Dictionary<int, Material>();
    //public static Material[] SpotMaterial = new Material[128] { null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null };

    public static ShaderVariantCollection.ShaderVariant[] DirectionalVariant = new ShaderVariantCollection.ShaderVariant[128];
    public static ShaderVariantCollection.ShaderVariant[] PointVariant = new ShaderVariantCollection.ShaderVariant[128];
    public static ShaderVariantCollection.ShaderVariant[] SpotVariant = new ShaderVariantCollection.ShaderVariant[128];

    public static Material ShadowMaterial;
    public static Material DensityMaterial;

    [HideInInspector]
    public Matrix4x4 MatrixVP;
    public Matrix4x4 LastMatrixVP;
    public Matrix4x4 LastMatrixVPInv;
    public Matrix4x4 LastMatrixVP2;
    public Matrix4x4 LastMatrixVPInv2;
    [HideInInspector]
    public Matrix4x4 MatrixV;
    bool OffsetUpdated = false;

    [HideInInspector]
    public Texture2D SpotLightCookie { get { if (_SpotLightCookie == null) { _SpotLightCookie = (Texture2D)Resources.Load("LightSoftCookie"); if (_SpotLightCookie == null) { Debug.Log("couldnt find default cookie"); } } return _SpotLightCookie; } set { _SpotLightCookie = value; } }

    [HideInInspector]
    public Texture2D LightFalloff { get { if (_LightFalloff == null) { _LightFalloff = (Texture2D)Resources.Load("HxFallOff"); if (_LightFalloff == null) { Debug.Log("couldnt find default Falloff"); } } return _LightFalloff; } set { _LightFalloff = value; } }

    [HideInInspector]
    static Texture2D _SpotLightCookie;
    [HideInInspector]
    static Texture2D _LightFalloff;

    Vector4 CalculateDensityDistance(int i)
    {
        float slices = (((int)compatibleDBuffer() + 1) * 4) - 1;
        return new Vector4(
            densityDistance * Mathf.Pow((i + 1) / slices, densityBias) - densityDistance * Mathf.Pow(i / slices, densityBias),
            densityDistance * Mathf.Pow((i + 2) / slices, densityBias) - densityDistance * Mathf.Pow((i + 1) / slices, densityBias),
            densityDistance * Mathf.Pow((i + 3) / slices, densityBias) - densityDistance * Mathf.Pow((i + 2) / slices, densityBias),
            densityDistance * Mathf.Pow((i + 4) / slices, densityBias) - densityDistance * Mathf.Pow((i + 3) / slices, densityBias)
            );
    }

    Vector4 CalculateTransparencyDistance(int i)
    {

        float slices = (((int)compatibleTBuffer() + 1) * 4) - 1;
        return new Vector4(
            transparencyDistance * Mathf.Pow((i + 1) / slices, transparencyBias) - transparencyDistance * Mathf.Pow(i / slices, transparencyBias),
            transparencyDistance * Mathf.Pow((i + 2) / slices, transparencyBias) - transparencyDistance * Mathf.Pow((i + 1) / slices, transparencyBias),
            transparencyDistance * Mathf.Pow((i + 3) / slices, transparencyBias) - transparencyDistance * Mathf.Pow((i + 2) / slices, transparencyBias),
            transparencyDistance * Mathf.Pow((i + 4) / slices, transparencyBias) - transparencyDistance * Mathf.Pow((i + 3) / slices, transparencyBias)
            );
    }
    int ParticleDensityRenderCount = 0;

    void RenderParticles()
    {
        ParticleDensityRenderCount = 0;
        if (ParticleDensitySupport)
        {
            BufferRender.SetGlobalVector("DensitySliceDistance0", CalculateDensityDistance(0));
            BufferRender.SetGlobalVector("DensitySliceDistance1", CalculateDensityDistance(1));
            BufferRender.SetGlobalVector("DensitySliceDistance2", CalculateDensityDistance(2));
            BufferRender.SetGlobalVector("DensitySliceDistance3", CalculateDensityDistance(3));

            ConstructPlanes(Mycamera, 0, Mathf.Max(MaxDirectionalRayDistance, MaxLightDistanceUsed));

            FindActiveParticleSystems();
            ParticleDensityRenderCount += RenderSlices();
            if (ParticleDensityRenderCount > 0)
            {

                Shader.EnableKeyword("DENSITYPARTICLES_ON");


                BufferRender.SetGlobalVector("SliceSettings", new Vector4(densityDistance, 1f / densityBias, ((int)compatibleDBuffer() + 1) * 4, 0));
              
                for (int i = 0; i < (int)compatibleDBuffer() + 1; i++)
                {

                    BufferRender.SetGlobalTexture(VolumetricDensityPID[i], VolumetricDensity[(int)compatibleDBuffer()][i]);
                }
            }
            else
            {
                Shader.DisableKeyword("DENSITYPARTICLES_ON");
            }

        }
        else
        {
            Shader.DisableKeyword("DENSITYPARTICLES_ON");

        }

        //move this shit
        if (TransparencySupport)
        {

            Shader.EnableKeyword("VTRANSPARENCY_ON");

            BufferRender.SetGlobalVector("TransparencySliceSettings", new Vector4(transparencyDistance, 1f / transparencyBias, ((int)compatibleTBuffer() + 1) * 4, 1f / transparencyDistance)); //transparent settings...  

            for (int i = 0; i < (int)compatibleTBuffer() + 1; i++)
            {
                BufferRender.SetGlobalTexture(VolumetricTransparencyPID[i], VolumetricTransparencyI[(int)compatibleTBuffer()][i]);
            }
        }
        else
        {
            Shader.DisableKeyword("VTRANSPARENCY_ON");

        }

    }

    void OnPostRender()
    {
        Shader.DisableKeyword("VTRANSPARENCY_ON");

    }
    static Matrix4x4 particleMatrix;

    int RenderSlices()
    {
        //change thiks to support more slices?
        //calculate view frustum
        //set active texture

        BufferRender.SetRenderTarget(VolumetricDensity[(int)compatibleDBuffer()], VolumetricDensity[(int)compatibleDBuffer()][0]);
        BufferRender.ClearRenderTarget(false, true, new Color(0.5f, 0.5f, 0.5f, 0.5f));

        BufferRender.SetGlobalVector("SliceSettings", new Vector4(densityDistance, 1f / densityBias, ((int)compatibleDBuffer() + 1) * 4, 0));
        int count = 0;


        for (int i = 0; i < ActiveParticleSystems.Count; i++)
        {
            if (ActiveParticleSystems[i].BlendMode == HxVolumetricParticleSystem.ParticleBlendMode.Max)
            {
                BufferRender.SetGlobalFloat("particleDensity", ActiveParticleSystems[i].DensityStrength);
                DensityMaterial.CopyPropertiesFromMaterial(ActiveParticleSystems[i].particleRenderer.sharedMaterial);
      
                BufferRender.DrawRenderer(ActiveParticleSystems[i].particleRenderer, DensityMaterial, 0, (int)ActiveParticleSystems[i].BlendMode);
                count++;
            }
        }

        for (int i = 0; i < ActiveParticleSystems.Count; i++)
        {
            if (ActiveParticleSystems[i].BlendMode == HxVolumetricParticleSystem.ParticleBlendMode.Add)
            {
                BufferRender.SetGlobalFloat("particleDensity", ActiveParticleSystems[i].DensityStrength);
                DensityMaterial.CopyPropertiesFromMaterial(ActiveParticleSystems[i].particleRenderer.sharedMaterial);
                BufferRender.DrawRenderer(ActiveParticleSystems[i].particleRenderer, DensityMaterial, 0, (int)ActiveParticleSystems[i].BlendMode);
                count++;
            }
        }

        for (int i = 0; i < ActiveParticleSystems.Count; i++)
        {
            if (ActiveParticleSystems[i].BlendMode == HxVolumetricParticleSystem.ParticleBlendMode.Min)
            {
                BufferRender.SetGlobalFloat("particleDensity", ActiveParticleSystems[i].DensityStrength);
                DensityMaterial.CopyPropertiesFromMaterial(ActiveParticleSystems[i].particleRenderer.sharedMaterial);
                BufferRender.DrawRenderer(ActiveParticleSystems[i].particleRenderer, DensityMaterial, 0, (int)ActiveParticleSystems[i].BlendMode);
                count++;
            }
        }

        for (int i = 0; i < ActiveParticleSystems.Count; i++)
        {
            if (ActiveParticleSystems[i].BlendMode == HxVolumetricParticleSystem.ParticleBlendMode.Sub)
            {
                BufferRender.SetGlobalFloat("particleDensity", ActiveParticleSystems[i].DensityStrength);
                DensityMaterial.CopyPropertiesFromMaterial(ActiveParticleSystems[i].particleRenderer.sharedMaterial);
                BufferRender.DrawRenderer(ActiveParticleSystems[i].particleRenderer, DensityMaterial, 0, (int)ActiveParticleSystems[i].BlendMode);
                count++;
            }
        }
        // BufferSetup.SetGlobalVector("TexelSize", new Vector2(1.0f/VolumetricDensity.width, 1.0f / VolumetricDensity.height));
        // BufferSetup.SetGlobalVector("offset", new Vector3((slice % 4) / 4f, Mathf.Floor(slice / 4) / 4f,4));
        // BufferSetup.Blit(VolumetricDensitySmallRTID, VolumetricDensityRTID, BlitDensityMaterial);



        //load ortho camera matrix.

        //render mesh into scene using the above texture.

        return count;
    }

    int GetCamPixelHeight()
    {
#if HXVR
        if (Mycamera.stereoTargetEye != StereoTargetEyeMask.None && Application.isPlaying && UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRDevice.isPresent)
        {
            return UnityEngine.XR.XRSettings.eyeTextureHeight;
        }
#endif

        return Mycamera.pixelHeight;  //ehh not sure why its + 32 power of 2?
    }

    int GetCamPixelWidth()
    {
#if HXVR
        if (Mycamera.stereoTargetEye != StereoTargetEyeMask.None && Application.isPlaying && UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRDevice.isPresent)
        {
            return UnityEngine.XR.XRSettings.eyeTextureWidth + (Mycamera.stereoTargetEye == StereoTargetEyeMask.Both ? UnityEngine.XR.XRSettings.eyeTextureWidth +  Mathf.CeilToInt(48 * UnityEngine.XR.XRSettings.eyeTextureResolutionScale) : 0);
        }
#endif

        return Mycamera.pixelWidth; //ehh not sure why its + 32 power of 2?
    }
    

    void CreateTempTextures()
    {

        int w = Mathf.CeilToInt(GetCamPixelWidth() * ResolutionScale[(int)resolution]);
        int h = Mathf.CeilToInt(GetCamPixelHeight() * ResolutionScale[(int)resolution]);


        //Mycamera.depthTextureMode = DepthTextureMode.Depth;
        if (resolution != Resolution.full && FullBlurRT == null) //TODO remove 16bit depth on this texture, might not need it
        {
            FullBlurRT = RenderTexture.GetTemporary(GetCamPixelWidth(), GetCamPixelHeight(), 16, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            FullBlurRTID = new RenderTargetIdentifier(FullBlurRT);
            FullBlurRT.filterMode = FilterMode.Bilinear;
            FullBlurRT.hideFlags = HideFlags.DontSave;
        }

        if (VolumetricTexture == null) //needs 16bit depth for fullscreen stencil
        {
            VolumetricTexture = RenderTexture.GetTemporary(w, h, 16, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            VolumetricTexture.filterMode = FilterMode.Bilinear;
            VolumetricTexture.hideFlags = HideFlags.DontSave;
            VolumetricTextureRTID = new RenderTargetIdentifier(VolumetricTexture);
        }

        if (ScaledDepthTexture[(int)resolution] == null)
        {
            //depth stored in RG, uv offset stored in BA
            ScaledDepthTexture[(int)resolution] = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear); //Need 4 channels for the upsampling.
            ScaledDepthTexture[(int)resolution].filterMode = FilterMode.Point;
            ScaledDepthTextureRTID[(int)resolution] = new RenderTargetIdentifier(ScaledDepthTexture[(int)resolution]);
            ScaledDepthTexture[(int)resolution].hideFlags = HideFlags.DontSave;
        }

        if (TransparencySupport)
        {
            for (int b = 0; b < EnumBufferDepthLength; b++)
            {
                VolumetricTransparency[b][0] = VolumetricTextureRTID;
            }
            for (int i = 0; i < (int)compatibleTBuffer() + 1; i++)
            {
                if (VolumetricTransparencyTextures[i] == null)
                {
                    VolumetricTransparencyTextures[i] = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear); //this might actually be fine as an 888 texture?
                    VolumetricTransparencyTextures[i].hideFlags = HideFlags.DontSave;

                    VolumetricTransparencyTextures[i].filterMode = FilterMode.Bilinear;
                    RenderTargetIdentifier rti = new RenderTargetIdentifier(VolumetricTransparencyTextures[i]);
                    for (int b = Mathf.Max(i, 0); b < EnumBufferDepthLength; b++)
                    {
                        VolumetricTransparency[b][i + 1] = rti;
                        VolumetricTransparencyI[b][i] = rti;
                    }
                }
            }
        }

        if (downScaledBlurRT == null && ((blurCount > 0 || ((BlurTransparency > 0 || (MapToLDR == true)) && TransparencySupport)) && resolution != Resolution.full)) // || Mycamera.hdr == false
        {
            downScaledBlurRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            downScaledBlurRT.filterMode = FilterMode.Bilinear;
            downScaledBlurRTID = new RenderTargetIdentifier(downScaledBlurRT);
            downScaledBlurRT.hideFlags = HideFlags.DontSave;
        }

        if (FullBlurRT2 == null && ((resolution != Resolution.full && UpSampledblurCount > 0) || (resolution == Resolution.full && (blurCount > 0 || ((BlurTransparency > 0 || (MapToLDR == true)) && TransparencySupport) || TemporalSampling)) || (MapToLDR))) // || Mycamera.hdr == false
        {


            FullBlurRT2 = RenderTexture.GetTemporary(GetCamPixelWidth(), GetCamPixelHeight(), 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            FullBlurRT2.hideFlags = HideFlags.DontSave;
            FullBlurRT2.filterMode = FilterMode.Bilinear;
            FullBlurRT2ID = new RenderTargetIdentifier(FullBlurRT2);
            if (resolution != Resolution.full)
            {
                VolumetricUpsampledBlurTextures[0] = FullBlurRTID;
                VolumetricUpsampledBlurTextures[1] = FullBlurRT2ID;
            }

        }

        w = Mathf.CeilToInt(GetCamPixelWidth() * ResolutionScale[Mathf.Max((int)resolution, (int)densityResolution)]);
        h = Mathf.CeilToInt(GetCamPixelHeight() * ResolutionScale[Mathf.Max((int)resolution, (int)densityResolution)]);


        if (ParticleDensitySupport)
        {
            for (int i = 0; i < (int)compatibleDBuffer() + 1; i++)
            {
                if (VolumetricDensityTextures[i] == null)
                {
                    //might be fine with 8888 texture.
                    VolumetricDensityTextures[i] = RenderTexture.GetTemporary(w, h, (i == 0 ? 16 : 0), RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);//need to create a depth for no reason
                    VolumetricDensityTextures[i].hideFlags = HideFlags.DontSave;
                    VolumetricDensityTextures[i].filterMode = FilterMode.Bilinear;
                    RenderTargetIdentifier rti = new RenderTargetIdentifier(VolumetricDensityTextures[i]);
                    for (int b = Mathf.Max(i, 0); b < EnumBufferDepthLength; b++)
                    {
                        VolumetricDensity[b][i] = rti;
                    }
                }
            }
        }



    }


    //public RenderTargetIdentifier FullBlurRTID;
    //public static RenderTexture FullBlurRT;
    public static HxVolumetricCamera Active;
    public static Camera ActiveCamera;

    CameraEvent LightRenderEvent = CameraEvent.AfterLighting;
    CameraEvent SetupEvent = CameraEvent.AfterDepthNormalsTexture; //downsample and clear textures (dont rebuild)
    CameraEvent RenderEvent = CameraEvent.BeforeLighting; //push camera matrix, render nonshadow casting lights, particles
    CameraEvent FinalizeEvent = CameraEvent.AfterLighting; //upsample and blur (dont rebuild)
#if HxApplyDirect
    CameraEvent ApplyEvent = CameraEvent.BeforeForwardAlpha; //apply in render que (dont rebuild)
#endif
    public static void ConstructPlanes(Camera cam, float near, float far)
    {
        
        Vector3 pos = cam.transform.position;
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;
        Vector3 up = cam.transform.up;
        // Vector3 nearCenter = pos + forward * cam.nearClipPlane;
        Vector3 farCenter = pos + forward * far;
        Vector3 nearCenter = pos + forward * near;

        float farHeight = Mathf.Tan((cam.fieldOfView * Mathf.Deg2Rad) / 2f) * far;

        float farWidth = farHeight * cam.aspect;

        float nearHeight = Mathf.Tan((cam.fieldOfView * Mathf.Deg2Rad) / 2f) * near;

        float nearWidth = farHeight * cam.aspect;

        Vector3 farTopLeft = farCenter + up * (farHeight) - right * (farWidth);
        Vector3 farTopRight = farCenter + up * (farHeight) + right * (farWidth);
        Vector3 farBottomLeft = farCenter - up * (farHeight) - right * (farWidth);
        Vector3 farBottomRight = farCenter - up * (farHeight) + right * (farWidth);

        Vector3 nearTopLeft = nearCenter + up * (nearHeight) - right * (nearWidth);
        Vector3 nearTopRight = nearCenter + up * (nearHeight) + right * (nearWidth);
        Vector3 nearBottomLeft = nearCenter - up * (nearHeight) - right * (nearWidth);
        // Vector3 nearBottomRight = nearCenter - up * (nearHeight) + right * (nearWidth); //dont need all of them

        CameraPlanes[0] = new Plane(farBottomLeft, farTopLeft, farTopRight);   //far

        CameraPlanes[1] = new Plane(nearTopLeft, nearTopRight, nearBottomLeft); //near

        CameraPlanes[2] = new Plane(pos, farTopLeft, farBottomLeft);

        CameraPlanes[3] = new Plane(pos, farBottomRight, farTopRight);

        CameraPlanes[4] = new Plane(pos, farBottomLeft, farBottomRight);

        CameraPlanes[5] = new Plane(pos, farTopRight, farTopLeft);

        MinBounds = new Vector3(Mathf.Min(farTopLeft.x, Mathf.Min(farTopRight.x, Mathf.Min(farBottomLeft.x, Mathf.Min(farBottomRight.x, pos.x)))), Mathf.Min(farTopLeft.y, Mathf.Min(farTopRight.y, Mathf.Min(farBottomLeft.y, Mathf.Min(farBottomRight.y, pos.y)))), Mathf.Min(farTopLeft.z, Mathf.Min(farTopRight.z, Mathf.Min(farBottomLeft.z, Mathf.Min(farBottomRight.z, pos.z)))));
        MaxBounds = new Vector3(Mathf.Max(farTopLeft.x, Mathf.Max(farTopRight.x, Mathf.Max(farBottomLeft.x, Mathf.Max(farBottomRight.x, pos.x)))), Mathf.Max(farTopLeft.y, Mathf.Max(farTopRight.y, Mathf.Max(farBottomLeft.y, Mathf.Max(farBottomRight.y, pos.y)))), Mathf.Max(farTopLeft.z, Mathf.Max(farTopRight.z, Mathf.Max(farBottomLeft.z, Mathf.Max(farBottomRight.z, pos.z)))));
    }

    public static List<HxVolumetricLight> ActiveDirectionalLights = new List<HxVolumetricLight>();
    static Vector3 MinBounds;
    static Vector3 MaxBounds;
    static Plane[] CameraPlanes = new Plane[6] { new Plane(), new Plane(), new Plane(), new Plane(), new Plane(), new Plane() };


    void FindActiveLights()
    {
        ActiveLights.Clear();
        ActiveVolumes.Clear();
        if (LightOctree != null)
        {
            LightOctree.GetObjectsBoundsPlane(ref CameraPlanes, MinBounds, MaxBounds, ActiveLights);
        
            //LightOctree.GetObjects(MinBounds, MaxBounds, ActiveLights);
        }
        for (int i = 0; i < ActiveDirectionalLights.Count; i++)
        {
            ActiveLights.Add(ActiveDirectionalLights[i]);
        }


        if (HxDensityVolume.DensityOctree != null)
        {
            HxDensityVolume.DensityOctree.GetObjectsBoundsPlane(ref CameraPlanes, MinBounds, MaxBounds, ActiveVolumes);


            ActiveVolumes.Sort(delegate (HxDensityVolume a, HxDensityVolume b) {
                    return ((int)a.BlendMode).CompareTo(((int)b.BlendMode));
                });
       

        }

    }

    

    void FindActiveParticleSystems()
    {
        ActiveParticleSystems.Clear();
        if (ParticleOctree != null)
        { ParticleOctree.GetObjectsBoundsPlane(ref CameraPlanes, MinBounds, MaxBounds, ActiveParticleSystems); }
    }

    public void Update()
    {
 
        OffsetUpdated = false;
        if (Mycamera == null) { Mycamera = GetComponent<Camera>(); }

        if (Mycamera != null)
        {

            if (HxVolumetricCamera.BoxMesh == null) { HxVolumetricCamera.BoxMesh = HxVolumetricCamera.CreateBox(); }
            if (ShadowMaterial == null)
            {

                ShadowMaterial = new Material(Shader.Find("Hidden/HxShadowCasterFix"));
                ShadowMaterial.hideFlags = HideFlags.DontSave;
            }

           if(ShadowFix) Graphics.DrawMesh(HxVolumetricCamera.BoxMesh, Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(Mathf.Max(MaxDirectionalRayDistance, MaxLightDistance), Mathf.Max(MaxDirectionalRayDistance, MaxLightDistance), Mathf.Max(MaxDirectionalRayDistance, MaxLightDistance)) * 2), HxVolumetricCamera.ShadowMaterial, 0);
        }
        else
        {
            enabled = false;
        }
    }

    void Start()
    {



        FinalizeBufferDirty = true;
#if HxApplyDirect
        ApplyBufferDirty = true;
#endif
        SetupBufferDirty = true;
        //CreateTempTextures();
        if (preCullEventAdded)
        { Camera.onPreCull += MyPreCull; preCullEventAdded = true; }
    }


    bool preCullEventAdded = false;
    void OnEnable()
    {
        FinalizeBufferDirty = true;
#if HxApplyDirect
        ApplyBufferDirty = true;
#endif
        SetupBufferDirty = true;

        if (preCullEventAdded)
        { Camera.onPreCull += MyPreCull; preCullEventAdded = true; }
    }


    bool BuffersBuilt = false;
    bool LightBufferAdded = false;
    bool SetupBufferAdded = false;
    bool SetupBufferDirty = false;
#if HxApplyDirect
    bool ApplyBufferAdded = false;
    bool ApplyBufferDirty = false;
#endif

    bool FinalizeBufferAdded = false;

    bool FinalizeBufferDirty = false;

    CameraEvent lastApply;
    CameraEvent lastRender;
    CameraEvent lastSetup;
    CameraEvent lastFinalize;
    CameraEvent lastLightRender;

    void CreateApplyBuffer()
    {
#if HxApplyDirect
       // ApplyBufferDirty = true; //set dirty every frame for now
        if (ApplyBufferDirty && ApplyBufferAdded)
        {

            Mycamera.RemoveCommandBuffer(lastApply, BufferApply);
            ApplyBufferAdded = false;
            ApplyBufferDirty = false;

        }


        if (!ApplyBufferAdded)
        {

            if (BufferApply == null) { BufferApply = new CommandBuffer(); BufferApply.name = "VolumetricApply"; }
            else
            { BufferApply.Clear(); }

            ApplyBufferAdded = true;
            
            BufferApply.Blit(Tile5x5, BuiltinRenderTextureType.CurrentActive, ApplyDirectMaterial, (QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 2) + (RemoveColorBanding ? 0 : 2));
            Mycamera.AddCommandBuffer(ApplyEvent, BufferApply);
            lastApply = ApplyEvent;
        }
#endif
    }

    void CreateSetupBuffer()
    {

        // SetupBufferDirty = true; //set dirty every frame for now
        if (SetupBufferDirty && SetupBufferAdded)
        {

            Mycamera.RemoveCommandBuffer(lastSetup, BufferSetup);
            SetupBufferAdded = false;
            SetupBufferDirty = false;

        }

        if (!SetupBufferAdded)
        {

            if (BufferSetup == null) { BufferSetup = new CommandBuffer(); BufferSetup.name = "VolumetricSetup"; }
            else
            { BufferSetup.Clear(); }

            if (TransparencySupport)
            {

                BufferSetup.SetRenderTarget(HxVolumetricCamera.VolumetricTransparencyI[(int)compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
                BufferSetup.ClearRenderTarget(false, true, new Color32(0, 0, 0, 0));
            }

            BufferSetup.SetRenderTarget(ScaledDepthTextureRTID[(int)resolution]);
            BufferSetup.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, DownSampleMaterial, 0, (int)resolution);

            //  BufferSetup.Blit(Tile5x5, ScaledDepthTextureRTID[(int)resolution], DownSampleMaterial, (int)resolution);
            BufferSetup.SetGlobalTexture(ScaledDepthTexturePID, ScaledDepthTextureRTID[(int)resolution]);
            lastSetup = SetupEvent;
            Mycamera.AddCommandBuffer(SetupEvent, BufferSetup);
            SetupBufferAdded = true;
        }
    }

    bool LastPlaying = false;
    [System.NonSerialized]
    static int lastRes = -1; //shared between all of them.
    [System.NonSerialized]
    int lastBlurCount = -1;
    [System.NonSerialized]
    int lastupSampleBlurCount;
    [System.NonSerialized]
    int lastLDR = -1;
    [System.NonSerialized]
    int lastBanding = -1;
    [System.NonSerialized]
    int lastH = -1;
    [System.NonSerialized]
    int lastW = -1;
    [System.NonSerialized]
    int lastPath = -1;
    [System.NonSerialized]
    int lastGaussian = -1;
    [System.NonSerialized]
    int lastTransparency = -1;
    [System.NonSerialized]
    int lastDensity = -1;
    [System.NonSerialized]
    int lastDensityRes = -1;

    [System.NonSerialized]
    float lastDepthFalloff = -1;

    [System.NonSerialized]
    float lastDownDepthFalloff = -1;


    bool CheckBufferDirty()
    {
        bool dirtyAll = true;

        if (TemporalSampling && TemporalFirst)
        {
            dirtyAll = true;
        }

        if (lastDownDepthFalloff != DownsampledBlurDepthFalloff)
        {
            dirtyAll = true;
            lastDownDepthFalloff = DownsampledBlurDepthFalloff;
        }

        if (lastDepthFalloff != BlurDepthFalloff)
        {
            dirtyAll = true;
            lastDepthFalloff = BlurDepthFalloff;
        }

        if (lastDensityRes != (int)densityResolution)
        {
            dirtyAll = true;
            lastDensityRes = (int)densityResolution;
        }

        if (lastTransparency != (TransparencySupport ? 1 : 0))
        {
            dirtyAll = true;
            lastTransparency = (TransparencySupport ? 1 : 0);
        }

        if (lastDensity != (ParticleDensitySupport ? 1 : 0))
        {
            dirtyAll = true;
            lastDensity = (ParticleDensitySupport ? 1 : 0);
        }
        if (lastGaussian != (GaussianWeights ? 1 : 0))
        {
            dirtyAll = true;
            lastGaussian = (GaussianWeights ? 1 : 0);
        }

        if (lastPath != (int)Mycamera.actualRenderingPath)
        {
            dirtyAll = true;
            lastPath = (int)Mycamera.actualRenderingPath;
        }

        if (lastBanding != (RemoveColorBanding ? 1 : 0))
        {
            dirtyAll = true;
            lastBanding = (RemoveColorBanding ? 1 : 0);
        }

        if (GetCamPixelHeight() != lastH)
        {
            dirtyAll = true;
            lastH = GetCamPixelHeight();
        }

        if (GetCamPixelWidth() != lastW)
        {
            dirtyAll = true;
            lastW = GetCamPixelWidth();
        }

        if (lastLDR != (MapToLDR ? 1 : 0))
        {
            dirtyAll = true;
            lastLDR = (MapToLDR ? 1 : 0);
        }

        if (lastupSampleBlurCount != UpSampledblurCount)
        {
            lastupSampleBlurCount = UpSampledblurCount;
            dirtyAll = true;
        }

        if (lastBlurCount != blurCount)
        {
            lastBlurCount = blurCount;
            dirtyAll = true;
        }

        if (lastRes != (int)resolution)
        {
            lastRes = (int)resolution;
            dirtyAll = true;
        }

        if (Application.isPlaying)
        {
            if (LastPlaying != true)
                dirtyAll = true;
            LastPlaying = true;
        }
        else
        {
            if (LastPlaying != false)
                dirtyAll = true;
            LastPlaying = false;
        }

        if (dirtyAll)
        {
            FinalizeBufferDirty = true;
#if HxApplyDirect
            ApplyBufferDirty = true;
#endif
            SetupBufferDirty = true;
            return true;
        }
        return false;
    }

    void CreateFinalizeBuffer()
    {
        //FinalizeBufferDirty = true; //set dirty every frame for now


       //if (FinalizeBufferDirty && FinalizeBufferAdded)
       //{
       //
       //    Mycamera.RemoveCommandBuffer(lastFinalize, BufferFinalize);
       //    FinalizeBufferAdded = false;
       //    FinalizeBufferDirty = false;
       //
       //}

        if (!FinalizeBufferAdded)
        {

           // if (BufferFinalize == null) { BufferFinalize = new CommandBuffer(); BufferFinalize.name = "VolumetricFinalize"; }
           // else
           // { BufferFinalize.Clear(); }

            bool DownSampleToggle = true;
            bool SampleToggle = true;

            if ((BlurTransparency > 0 || (MapToLDR == true)) && TransparencySupport) // || Mycamera.hdr == fals
            {
                int ctb = (int)compatibleTBuffer();
                int tbc = Mathf.Max(BlurTransparency, 1);
                for (int i = 0; i < ctb + 1; i++)
                {

                    for (int p = 0; p < tbc; p++)
                    {
                        BufferFinalize.SetRenderTarget((resolution == Resolution.full ? FullBlurRT2ID : downScaledBlurRTID));
                        BufferFinalize.SetGlobalTexture("_MainTex", VolumetricTransparencyI[ctb][i]);
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 0);

                        BufferFinalize.SetRenderTarget(VolumetricTransparencyI[ctb][i]);
                        BufferFinalize.SetGlobalTexture("_MainTex", (resolution == Resolution.full ? FullBlurRT2ID : downScaledBlurRTID));
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, (((MapToLDR) && p == tbc - 1) ? 2 : 1)); //Mycamera.hdr == false || 
                    }

                }
            }

            if (blurCount > 0 && resolution != Resolution.full)
            {
                BufferFinalize.SetGlobalFloat(BlurDepthFalloffPID, DownsampledBlurDepthFalloff);

                for (int i = 0; i < blurCount; i++)
                {
                    if (DownSampleToggle)
                    {
                        BufferFinalize.SetRenderTarget(downScaledBlurRTID);
                        BufferFinalize.SetGlobalTexture("_MainTex", VolumetricTextureRTID);
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, VolumeBlurMaterial, 0, (GaussianWeights ? 2 : 0));

                    }
                    else
                    {
                        BufferFinalize.SetRenderTarget(VolumetricTextureRTID);
                        BufferFinalize.SetGlobalTexture("_MainTex", downScaledBlurRTID);
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, VolumeBlurMaterial, 0, (GaussianWeights ? 2 : 0));
                    }
                    DownSampleToggle = !DownSampleToggle;

                }
            }

            if (resolution != Resolution.full)
            {
                if (TemporalSampling)
                {
                    BufferFinalize.SetGlobalTexture("hxLastVolumetric", TemporalTextureRTID);
                    BufferFinalize.SetGlobalVector("hxTemporalSettings", new Vector4(LuminanceFeedback, MaxFeedback, 0, 0));
                }

                if (UpSampledblurCount == 0)
                {
                    BufferFinalize.SetRenderTarget(FullBlurRT);
                    BufferFinalize.SetGlobalTexture("_MainTex", (DownSampleToggle ? VolumetricTextureRTID : downScaledBlurRTID));
                    BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, ApplyDirectMaterial, 0, ((TemporalSampling && !TemporalFirst) ? 6 : 0));
                    // BufferFinalize.Blit((DownSampleToggle ? VolumetricTextureRTID : downScaledBlurRTID), FullBlurRT, ApplyDirectMaterial, ((TemporalSampling && !TemporalFirst) ? 6 : 0));
                }
                else
                {
                    BufferFinalize.SetGlobalTexture("_MainTex", (DownSampleToggle ? VolumetricTextureRTID : downScaledBlurRTID));
                    BufferFinalize.SetRenderTarget(VolumetricUpsampledBlurTextures, FullBlurRT);
                    BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, ApplyDirectMaterial, 0, ((TemporalSampling && !TemporalFirst) ? 7 : 5));
                }



                if (UpSampledblurCount > 0)
                {
                    BufferFinalize.SetGlobalFloat(BlurDepthFalloffPID, BlurDepthFalloff);


                    if (UpSampledblurCount % 2 != 0) { SampleToggle = false; }

                    for (int i = 0; i < UpSampledblurCount; i++)
                    {
                        if (SampleToggle)
                        {
                            BufferFinalize.SetRenderTarget(FullBlurRT2ID);
                            BufferFinalize.SetGlobalTexture("_MainTex", FullBlurRTID);
                            BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, VolumeBlurMaterial, 0, 1);


                        }
                        else
                        {
                            BufferFinalize.SetRenderTarget(FullBlurRTID);
                            BufferFinalize.SetGlobalTexture("_MainTex", FullBlurRT2ID);
                            BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, VolumeBlurMaterial, 0, 1);

                        }
                        SampleToggle = !SampleToggle;
                    }
                }

                if (MapToLDR) // || Mycamera.hdr == false)
                {
                    BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRTID);
           
                    BufferFinalize.SetRenderTarget(TemporalTexture);
                    BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, ApplyDirectMaterial, 0, 8); //copy depth and final effect.



                    BufferFinalize.SetRenderTarget(FullBlurRT2);
                    BufferFinalize.SetGlobalTexture("_MainTex", FullBlurRTID);
                    BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 3);
                    BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
                }
                else
                {
                    BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT);
                }


            }
            else
            {

                if (blurCount > 0 || TemporalSampling)
                {
                    if (TemporalSampling)
                    {
                        BufferFinalize.SetGlobalTexture("hxLastVolumetric", TemporalTextureRTID);
                        BufferFinalize.SetGlobalVector("hxTemporalSettings", new Vector4(LuminanceFeedback, MaxFeedback, 0, 0));
                    }

                    BufferFinalize.SetGlobalFloat(BlurDepthFalloffPID, BlurDepthFalloff);

                    SampleToggle = true;
                    for (int i = 0; i < blurCount; i++)
                    {
                        if (SampleToggle)
                        {
                            BufferFinalize.SetRenderTarget(FullBlurRT2ID);
                            BufferFinalize.SetGlobalTexture("_MainTex", VolumetricTextureRTID);
                            BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, VolumeBlurMaterial, 0, (GaussianWeights ? 5 : 4));
                        }
                        else
                        {
                            BufferFinalize.SetRenderTarget(VolumetricTextureRTID);
                            BufferFinalize.SetGlobalTexture("_MainTex", FullBlurRT2ID);
                            BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, VolumeBlurMaterial, 0, (GaussianWeights ? 5 : 4));
                        }
                        SampleToggle = !SampleToggle;
                    }


                    if (!SampleToggle)
                    {

                        if (MapToLDR)// || Mycamera.hdr == false)
                        {
                            if (TemporalSampling && !TemporalFirst)
                            {
                                BufferFinalize.SetRenderTarget(VolumetricTextureRTID);
                                BufferFinalize.SetGlobalTexture("_MainTex", FullBlurRT2);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 5);
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTextureRTID);

                                TemporalFirst = false;
                                BufferFinalize.SetRenderTarget(TemporalTexture);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, ApplyDirectMaterial, 0, 8); //copy depth and final effect.


                                BufferFinalize.SetRenderTarget(FullBlurRT2);
                                BufferFinalize.SetGlobalTexture("_MainTex", VolumetricTextureRTID);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, ((TemporalSampling && !TemporalFirst) ? 4 : 3));
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);

                            }
                            else
                            {
                                BufferFinalize.SetRenderTarget(VolumetricTextureRTID);
                                BufferFinalize.SetGlobalTexture("_MainTex", FullBlurRT2ID);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 3);
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTexture);
                            }

                        }
                        else
                        {
                            if ((TemporalSampling && !TemporalFirst))
                            {
                                BufferFinalize.SetRenderTarget(VolumetricTextureRTID);
                                BufferFinalize.SetGlobalTexture("_MainTex", FullBlurRT2);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 5);
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTextureRTID);
                            }
                            else
                            {
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
                            }
                        }

                    }
                    else
                    {
                        if (MapToLDR)// || Mycamera.hdr == false)
                        {

                            if (TemporalSampling && !TemporalFirst)
                            {
                                BufferFinalize.SetRenderTarget(FullBlurRT2);
                                BufferFinalize.SetGlobalTexture("_MainTex", VolumetricTextureRTID);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 5);
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);

                              
                                BufferFinalize.SetRenderTarget(TemporalTexture);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, ApplyDirectMaterial, 0, 8); //copy depth and final effect.

                                BufferFinalize.SetRenderTarget(VolumetricTextureRTID);
                                BufferFinalize.SetGlobalTexture("_MainTex", FullBlurRT2ID);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 3);
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTexture);

                            }
                            else
                            {
                                BufferFinalize.SetRenderTarget(FullBlurRT2);
                                BufferFinalize.SetGlobalTexture("_MainTex", VolumetricTextureRTID);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, ((TemporalSampling && !TemporalFirst) ? 4 : 3));

                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
                            }
                        }
                        else
                        {
                            if ((TemporalSampling && !TemporalFirst))
                            {
                                BufferFinalize.SetRenderTarget(FullBlurRT2);
                                BufferFinalize.SetGlobalTexture("_MainTex", VolumetricTextureRTID);
                                BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 5);
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
                            }
                            else
                            {
                                BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTextureRTID);
                            }
                        }
                    }
                }
                else
                {
                    if (MapToLDR)// || Mycamera.hdr == false)
                    {
                        BufferFinalize.SetRenderTarget(FullBlurRT2);
                        BufferFinalize.SetGlobalTexture("_MainTex", VolumetricTextureRTID);
                        BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, TransparencyBlurMaterial, 0, 3);

                        BufferFinalize.SetGlobalTexture(VolumetricTexturePID, FullBlurRT2);
                    }
                    else
                    {
                        BufferFinalize.SetGlobalTexture(VolumetricTexturePID, VolumetricTextureRTID);
                    }
                }
            }


            if (TemporalSampling)
            {
                if (MapToLDR)// || Mycamera.hdr == false)
                {
                    TemporalFirst = false;
                }
                else
                {
                    TemporalFirst = false;
                    BufferFinalize.SetRenderTarget(TemporalTexture);
                    BufferFinalize.DrawMesh(HxVolumetricCamera.QuadMesh, Matrix4x4.identity, ApplyDirectMaterial, 0, 8); //copy depth and final effect.
                }
            }
            else
            {
                TemporalFirst = true;
            }


            lastFinalize = FinalizeEvent;
            lastRender = RenderEvent;
            lastLightRender = LightRenderEvent;
            lastFinalize = FinalizeEvent;
       
            Mycamera.AddCommandBuffer(FinalizeEvent, BufferFinalize);
            FinalizeBufferAdded = true;
        }
    }

    float currentDitherOffset;

    void BuildBuffer()
    {

        if (BuffersBuilt)
        {

            if (BufferRender != null) Mycamera.RemoveCommandBuffer(lastRender, BufferRender);
            BuffersBuilt = false;
        }

       
            CreatePIDs();
        CalculateEvent();
        DefineFull();
        CheckTemporalTextures();
        if (CheckBufferDirty()) { ReleaseTempTextures(); }

        CreateTempTextures();
        Active = this;
        ActiveCamera = Mycamera;
        if (FinalizeBufferDirty && FinalizeBufferAdded)
        {
        
            Mycamera.RemoveCommandBuffer(lastFinalize, BufferFinalize);
            FinalizeBufferAdded = false;
            FinalizeBufferDirty = false;
        
        }

        if (!FinalizeBufferAdded)
        {

            if (BufferFinalize == null) { BufferFinalize = new CommandBuffer(); BufferFinalize.name = "VolumetricFinalize"; }
            else
            { BufferFinalize.Clear(); }
        }

        CreateSetupBuffer();
        CreateApplyBuffer();
      

        if (resolution == Resolution.full) { FullUsed = true; } else { LowResUsed = true; }

        
        CurrentTint = new Vector3((QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor : TintColor.linear).r, (QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor : TintColor.linear).g, (QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor : TintColor.linear).b) * TintIntensity;
        CurrentTintEdge = new Vector3((QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor2 : TintColor2.linear).r, (QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor2 : TintColor2.linear).g, (QualitySettings.activeColorSpace == ColorSpace.Gamma ? TintColor2 : TintColor2.linear).b) * TintIntensity;
        if (dirty) //incase resolution was changed.
        {
            if (BufferRender == null) { BufferRender = new CommandBuffer(); BufferRender.name = "VolumetricRender"; } else { BufferRender.Clear(); }

            if (TemporalSampling)
            {
                Matrix4x4 thisViewMatrix = CurrentView;
                Matrix4x4 thisProjMatrix = GL.GetGPUProjectionMatrix(CurrentProj, false);
                LastMatrixVP = thisProjMatrix * thisViewMatrix;

                BufferRender.SetGlobalMatrix("hxLastVP", LastMatrixVP);

#if HXVR

                if (IsRenderBoth())
                {
                    thisViewMatrix = CurrentView2;
                    thisProjMatrix = GL.GetGPUProjectionMatrix(CurrentProj2, false);

                    LastMatrixVP = thisProjMatrix * thisViewMatrix;
             
                    BufferRender.SetGlobalMatrix("hxLastVP2", LastMatrixVP);
                }
#endif
            }




#if HXVR

            if (Mycamera.stereoTargetEye != StereoTargetEyeMask.None && Application.isPlaying && UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRDevice.isPresent) 
            {

                Camera.StereoscopicEye currentEye = Camera.StereoscopicEye.Right;

                if (IsRenderBoth())
                {
#if UNITY_EDITOR
                    //if (PlayerSettings.singlePassStereoRendering == false && !warned) { warned = true; Debug.LogError("Enable Single pass stero rendering in the player settings if you want to render the volumetric effect in 1 pass"); }
#endif
                    SinglePassStereoUsed = true;
                }
                else
                {
                    if (Mycamera.stereoTargetEye == StereoTargetEyeMask.Right) { currentEye = Camera.StereoscopicEye.Right;}
                    else
                    {
                        currentEye = Camera.StereoscopicEye.Left; 
                     
                    }

                }

                CurrentProj = Mycamera.GetStereoProjectionMatrix(currentEye);
                CurrentView = Mycamera.GetStereoViewMatrix(currentEye);
                CurrentInvers = CurrentProj.inverse;

                if (IsRenderBoth())
                {
                    CurrentProj2 = Mycamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                    CurrentView2 = Mycamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                   
                }

                if (IsRenderBoth())
                {
                    Matrix4x4 camToWorld = Mycamera.worldToCameraMatrix;
                    Matrix4x4 camToWorld2 = Mycamera.worldToCameraMatrix;

                    camToWorld[12] += Mycamera.stereoSeparation / 2.0f;
                    camToWorld2[12] -= Mycamera.stereoSeparation / 2.0f;

                    BufferRender.SetGlobalMatrix("hxCameraToWorld", camToWorld.inverse);
                    BufferRender.SetGlobalMatrix("hxCameraToWorld2", camToWorld2.inverse);
                    //BufferRender.SetGlobalVector("hxCameraPosition", new Vector4(camToWorld[12], camToWorld[13], camToWorld[14], 0));
                    // BufferRender.SetGlobalVector("hxCameraPosition2", new Vector4(camToWorld2[12], camToWorld2[13], camToWorld2[14], 0));
                }
                else
                {
                    Matrix4x4 camToWorld = Mycamera.worldToCameraMatrix;
                    camToWorld[12] += Mycamera.stereoSeparation / 2.0f * (currentEye == Camera.StereoscopicEye.Left ? -1 : 1);
                    BufferRender.SetGlobalMatrix("hxCameraToWorld", camToWorld.inverse);
                    // BufferRender.SetGlobalVector("hxCameraPosition", new Vector4(camToWorld[12], camToWorld[13], camToWorld[14], 0));
                }
            }
            else
            {

                CurrentView = Mycamera.worldToCameraMatrix;
                BufferRender.SetGlobalMatrix("hxCameraToWorld", Mycamera.cameraToWorldMatrix);
                CurrentProj = Mycamera.projectionMatrix;
                CurrentInvers = Mycamera.projectionMatrix.inverse;
            }


#else
            CurrentView = Mycamera.worldToCameraMatrix;
            BufferRender.SetGlobalMatrix("hxCameraToWorld", Mycamera.cameraToWorldMatrix);
            CurrentProj = Mycamera.projectionMatrix;
            CurrentInvers = Mycamera.projectionMatrix.inverse;
#endif



         
            


            Matrix4x4 proj = GL.GetGPUProjectionMatrix(CurrentProj, true);
            MatrixVP = proj * CurrentView;
            MatrixV = CurrentView;


            Matrix4x4 m_view = CurrentView;
            Matrix4x4 m_proj = GL.GetGPUProjectionMatrix(CurrentProj, false); //was false
            Matrix4x4 m_viewproj = m_proj * m_view;
            Matrix4x4 m_inv_viewproj = m_viewproj.inverse;


            BufferRender.SetGlobalMatrix("_InvViewProj", m_inv_viewproj);

            BlitScale.z = HxVolumetricCamera.ActiveCamera.nearClipPlane + 1f;
            BlitScale.y = (HxVolumetricCamera.ActiveCamera.nearClipPlane + 1f) * Mathf.Tan(Mathf.Deg2Rad * HxVolumetricCamera.ActiveCamera.fieldOfView * 0.51f);
            BlitScale.x = BlitScale.y * HxVolumetricCamera.ActiveCamera.aspect;
            BlitMatrix = Matrix4x4.TRS(HxVolumetricCamera.Active.transform.position, HxVolumetricCamera.Active.transform.rotation, BlitScale);


            BlitMatrixMVP = HxVolumetricCamera.Active.MatrixVP * BlitMatrix;
            BlitMatrixMV = HxVolumetricCamera.Active.MatrixV * BlitMatrix;

            if (TemporalSampling)
            {
                currentDitherOffset += DitherSpeed;
                if (currentDitherOffset > 1) { currentDitherOffset -= 1; }
                BufferRender.SetGlobalFloat("hxRayOffset", currentDitherOffset);
            }

            BufferRender.SetGlobalMatrix(HxVolumetricLight.VolumetricMVPPID, HxVolumetricCamera.BlitMatrixMVP);
            BufferRender.SetGlobalMatrix(HxVolumetricLight.VolumetricMVPID, HxVolumetricCamera.BlitMatrixMV);

#if HXVR
            Matrix4x4 rightEyeProj = GL.GetGPUProjectionMatrix(Mycamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), true); 
            Matrix4x4 rightView = Mycamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
        
            BufferRender.SetGlobalMatrix(HxVolumetricLight.VolumetricMVP2PID, (rightEyeProj * rightView) * BlitMatrix);

            BufferRender.SetGlobalMatrix(InverseProjectionMatrix2PID, Mycamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left).inverse);
            BufferRender.SetGlobalMatrix("InverseProjectionMatrix1", Mycamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right).inverse);

            BufferRender.SetGlobalMatrix("hxInverseP1", GL.GetGPUProjectionMatrix(Mycamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), false).inverse);
            BufferRender.SetGlobalMatrix("hxInverseP2", GL.GetGPUProjectionMatrix(Mycamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), false).inverse);
#endif

            RenderParticles();

            BufferRender.SetRenderTarget(VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
            BufferRender.ClearRenderTarget(false, true, new Color(0, 0, 0, 0));

            BufferRender.SetGlobalFloat(DepthThresholdPID, DepthThreshold);
            BufferRender.SetGlobalVector("CameraFoward", transform.forward);
            BufferRender.SetGlobalFloat(BlurDepthFalloffPID, BlurDepthFalloff);
            BufferRender.SetGlobalFloat(VolumeScalePID, ResolutionScale[(int)resolution]);
            BufferRender.SetGlobalMatrix(InverseViewMatrixPID, Mycamera.cameraToWorldMatrix);
            BufferRender.SetGlobalMatrix(InverseProjectionMatrixPID, CurrentInvers);
            if (OffsetUpdated == false) { OffsetUpdated = true; Offset += NoiseVelocity * Time.deltaTime; }
            BufferRender.SetGlobalVector(NoiseOffsetPID, Offset);

            BufferRender.SetGlobalFloat(ShadowDistancePID, QualitySettings.shadowDistance);



            CreateLightbuffers(); //his will add buffers to each light or to applystep for nonshadow casting lights       
                       
            CreateFinalizeBuffer();


            BuffersBuilt = true;

            Mycamera.AddCommandBuffer(RenderEvent, BufferRender); //rebuild every frame    
            Mycamera.AddCommandBuffer(LightRenderEvent, BufferRenderLights);
#if HxApplyQueue
            Graphics.DrawMesh(QuadMesh, BlitMatrix, ApplyQueueMaterial, 0, Mycamera, 0);

#endif
        }
    }

    void OnDestroy()
    {
        if (TemporalTexture != null) { RenderTexture.ReleaseTemporary(TemporalTexture); TemporalTexture = null; }
        if (!preCullEventAdded)
        { Camera.onPreCull -= MyPreCull; preCullEventAdded = false; }
        if (Active == this) { Active.ReleaseLightBuffers(); ReleaseTempTextures(); }
        if (BuffersBuilt)
        {
            if (BufferRenderLights != null && LightBufferAdded) { Mycamera.RemoveCommandBuffer(lastLightRender, BufferRenderLights); LightBufferAdded = false; }
            if (BufferSetup != null && SetupBufferAdded) { Mycamera.RemoveCommandBuffer(lastSetup, BufferSetup); SetupBufferAdded = false; }
            if (BufferRender != null) Mycamera.RemoveCommandBuffer(lastRender, BufferRender);
            if (BufferFinalize != null && FinalizeBufferAdded) { Mycamera.RemoveCommandBuffer(lastFinalize, BufferFinalize); FinalizeBufferAdded = false; }
#if HxApplyDirect
            if (BufferApply != null && ApplyBufferAdded) { Mycamera.RemoveCommandBuffer(ApplyEvent, BufferApply); ApplyBufferAdded = false; }
#endif

            BuffersBuilt = false;
        }

        SaveUsedShaderVarience();

        if (callBackImageEffect != null) { callBackImageEffect.enabled = false; }
        if (callBackImageEffectOpaque != null) { callBackImageEffectOpaque.enabled = false; }
    }

    void SaveUsedShaderVarience()
    {



#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            HxVolumetricShadersUsed.SetVolumetricValues(FullUsed, LowResUsed, HeightFogUsed, HeightFogOffUsed, NoiseUsed, NoiseOffUsed, TransparencyUsed, TransparencyOffUsed, DensityParticlesUsed, PointUsed, SpotUsed, DirectionalUsed, SinglePassStereoUsed,ProjectorUsed);
        }
#endif
    }

    void OnDisable()
    {
    
        if (TemporalTexture != null) { RenderTexture.ReleaseTemporary(TemporalTexture); TemporalTexture = null; TemporalFirst = true; }
        if (!preCullEventAdded)
        { Camera.onPreCull -= MyPreCull; preCullEventAdded = false; }
        if (Active == this) { Active.ReleaseLightBuffers(); ReleaseTempTextures(); }
        if (BuffersBuilt)
        {
            if (BufferRenderLights != null && LightBufferAdded) { Mycamera.RemoveCommandBuffer(lastLightRender, BufferRenderLights); LightBufferAdded = false; }
            if (BufferSetup != null && SetupBufferAdded) { Mycamera.RemoveCommandBuffer(lastSetup, BufferSetup); SetupBufferAdded = false; }
            if (BufferRender != null) Mycamera.RemoveCommandBuffer(lastRender, BufferRender);
            if (BufferFinalize != null && FinalizeBufferAdded) { Mycamera.RemoveCommandBuffer(lastFinalize, BufferFinalize); FinalizeBufferAdded = false; }
#if HxApplyDirect
            if (BufferApply != null && ApplyBufferAdded) { Mycamera.RemoveCommandBuffer(ApplyEvent, BufferApply); ApplyBufferAdded = false; }
#endif
            BuffersBuilt = false;
        }
        if (callBackImageEffect != null) { callBackImageEffect.enabled = false; }
        if (callBackImageEffectOpaque != null) { callBackImageEffectOpaque.enabled = false; }
    }

    void CalculateEvent()
    {
        switch (Mycamera.actualRenderingPath)
        {
            case RenderingPath.DeferredLighting:
                SetupEvent = CameraEvent.BeforeLighting;
                RenderEvent = CameraEvent.BeforeLighting;
                LightRenderEvent = CameraEvent.BeforeLighting;
                FinalizeEvent = CameraEvent.AfterLighting;
                
                break;
            case RenderingPath.DeferredShading:
                SetupEvent = CameraEvent.BeforeLighting;
                RenderEvent = CameraEvent.BeforeLighting;
                LightRenderEvent = CameraEvent.BeforeLighting;
                FinalizeEvent = CameraEvent.AfterLighting;
                break;
            case RenderingPath.Forward:
                if (Mycamera.depthTextureMode == DepthTextureMode.None) { Mycamera.depthTextureMode = DepthTextureMode.Depth; }
                if ((int)Mycamera.depthTextureMode == 1 || (int)Mycamera.depthTextureMode == 5) { RenderEvent = CameraEvent.BeforeDepthTexture; SetupEvent = CameraEvent.AfterDepthTexture; }
                else { RenderEvent = CameraEvent.BeforeDepthNormalsTexture; SetupEvent = CameraEvent.AfterDepthNormalsTexture; }
                FinalizeEvent = CameraEvent.AfterForwardOpaque;
                LightRenderEvent = CameraEvent.BeforeForwardOpaque;
                break;
        }
    }


    public void EventOnRenderImage(RenderTexture src, RenderTexture dest)
    {     
            Graphics.Blit(src, dest, ApplyMaterial, (QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 2) + (RemoveColorBanding ? 0 : 2));
    }


    int ScalePass()
    {
        if (resolution == Resolution.half) { return 0; }
        if (resolution == Resolution.quarter) { return 1; }
        //if (resolution == Resolution.eighth) { return 2; }
        return 2;
    }

    void DownSampledFullBlur(RenderTexture mainColor, RenderBuffer NewColor, RenderBuffer depth, int pass)
    {
        Graphics.SetRenderTarget(NewColor, depth);
        VolumeBlurMaterial.SetTexture("_MainTex", mainColor);
        GL.PushMatrix();
        VolumeBlurMaterial.SetPass(pass);
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);
        GL.Color(Color.red);
        GL.Vertex3(0, 0, 0);
        GL.Vertex3(1, 0, 0);
        GL.Vertex3(1, 1, 0);
        GL.Vertex3(0, 1, 0);
        GL.End();
        GL.PopMatrix();
    }
#if UNITY_EDITOR
    public static void ReleaseShaders()
    {
        HxVolumetricCamera[] cams = GameObject.FindObjectsOfType<HxVolumetricCamera>();

        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] != null)
            {
                cams[i].FinalizeBufferDirty = true;
#if HxApplyDirect
                cams[i].ApplyBufferDirty = true;
#endif
                cams[i].SetupBufferDirty = true;
            }
        }


        if (CollectionAll != null) GameObject.DestroyImmediate(CollectionAll, true); CollectionAll = null;
        HxVolumetricCamera.PIDCreated = false;
        GameObject.DestroyImmediate(ApplyQueueMaterial, true);
        GameObject.DestroyImmediate(ApplyDirectMaterial, true);
        GameObject.DestroyImmediate(ApplyMaterial, true);
        GameObject.DestroyImmediate(DensityMaterial, true);
        GameObject.DestroyImmediate(ShadowMaterial, true);
        GameObject.DestroyImmediate(ApplyMaterial, true);
        GameObject.DestroyImmediate(DensityMaterial, true);
        GameObject.DestroyImmediate(VolumeBlurMaterial, true);
        GameObject.DestroyImmediate(DownSampleMaterial, true);
        GameObject.DestroyImmediate(TransparencyBlurMaterial, true);
        //GameObject.DestroyImmediate(NoiseTexture3D, true);
        GameObject.DestroyImmediate(Tile5x5, true);

        GameObject.DestroyImmediate(SphereMesh, true);
        GameObject.DestroyImmediate(SpotLightMesh, true);
        GameObject.DestroyImmediate(OrthoProjectorMesh, true);
        GameObject.DestroyImmediate(QuadMesh, true);
        CollectionAll = null;
        //if (CollectionAll != null)
        //{
        //    GameObject.DestroyImmediate(CollectionAll);
        //}

        // for (int i = 0; i < DirectionalMaterial.Length; i++)
        // {
        //     GameObject.DestroyImmediate(DirectionalMaterial[i], true);
        // }
        // for (int i = 0; i < SpotMaterial.Length; i++)
        // {
        //     GameObject.DestroyImmediate(SpotMaterial[i], true);
        // }
        //
        // for (int i = 0; i < PointMaterial.Length; i++)
        // {
        //     GameObject.DestroyImmediate(PointMaterial[i], true);
        // }
    }
#endif


    void CheckTemporalTextures()
    {
        if (TemporalSampling)
        {
            if (TemporalTexture != null && (TemporalTexture.width != GetCamPixelWidth() || TemporalTexture.height != GetCamPixelHeight())) { RenderTexture.ReleaseTemporary(TemporalTexture); TemporalTexture = null; TemporalFirst = true; }



            if (TemporalTexture == null)
            {
                TemporalTexture = RenderTexture.GetTemporary(GetCamPixelWidth(), GetCamPixelHeight(), 16, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

                TemporalTextureRTID = new RenderTargetIdentifier(TemporalTexture);
                TemporalTexture.hideFlags = HideFlags.DontSave;
            }

        }
        else
        {
            if (TemporalTexture != null) { RenderTexture.ReleaseTemporary(TemporalTexture); TemporalTexture = null; TemporalFirst = true; }
        }
    }

    public static void ReleaseTempTextures()
    {


        if (VolumetricTexture != null) { RenderTexture.ReleaseTemporary(VolumetricTexture); VolumetricTexture = null; }
        if (FullBlurRT != null) { RenderTexture.ReleaseTemporary(FullBlurRT); FullBlurRT = null; }

        for (int i = 0; i < VolumetricTransparencyTextures.Length; i++)
        {
            if (VolumetricTransparencyTextures[i] != null) { RenderTexture.ReleaseTemporary(VolumetricTransparencyTextures[i]); VolumetricTransparencyTextures[i] = null; }
        }


        for (int i = 0; i < VolumetricDensityTextures.Length; i++)
        {
            if (VolumetricDensityTextures[i] != null) { RenderTexture.ReleaseTemporary(VolumetricDensityTextures[i]); VolumetricDensityTextures[i] = null; }
        }

        if (downScaledBlurRT != null)
        {
            RenderTexture.ReleaseTemporary(downScaledBlurRT); downScaledBlurRT = null;
        }

        if (FullBlurRT2 != null)
        {
            RenderTexture.ReleaseTemporary(FullBlurRT2); FullBlurRT2 = null;
        }
        //if (resolution != Resolution.full)
        //{
        if (ScaledDepthTexture[0] != null) { RenderTexture.ReleaseTemporary(ScaledDepthTexture[0]); ScaledDepthTexture[0] = null; }
        if (ScaledDepthTexture[1] != null) { RenderTexture.ReleaseTemporary(ScaledDepthTexture[1]); ScaledDepthTexture[1] = null; }
        if (ScaledDepthTexture[2] != null) { RenderTexture.ReleaseTemporary(ScaledDepthTexture[2]); ScaledDepthTexture[2] = null; }
        if (ScaledDepthTexture[3] != null) { RenderTexture.ReleaseTemporary(ScaledDepthTexture[3]); ScaledDepthTexture[3] = null; }

        //}   

    }


    void OnPreCull()
    {
        SetUpRenderOrder();

        ReleaseLightBuffers();
        MaxLightDistanceUsed = MaxLightDistance;
        ConstructPlanes(Mycamera, 0, MaxLightDistanceUsed); //set near to 0 just incase.
       
      
        UpdateLightPoistions();

        UpdateParticlePoistions();
        FindActiveLights();

        BuildBuffer();
    }
    float MaxLightDistanceUsed = 0;
  
    void UpdateLightPoistions()
    {
        MaxLightDistanceUsed = MaxLightDistance;
        for (var e = AllVolumetricLight.GetEnumerator(); e.MoveNext();)
        {
            if (e.Current.CustomMaxLightDistance)
            {
                MaxLightDistanceUsed = Mathf.Max(e.Current.MaxLightDistance,MaxLightDistanceUsed);
            }
          
            e.Current.UpdatePosition();
        }
        if (LightOctree != null) LightOctree.TryShrink();

        for (var e = AllDensityVolumes.GetEnumerator(); e.MoveNext();)
        {
            e.Current.UpdateVolume();
        }
        if (HxDensityVolume.DensityOctree != null) HxDensityVolume.DensityOctree.TryShrink();
    }

    void UpdateParticlePoistions()
    {
        if (ParticleDensitySupport)
        {
            for (var e = AllParticleSystems.GetEnumerator(); e.MoveNext();)
            {
                e.Current.UpdatePosition();
            }
            if (ParticleOctree != null) ParticleOctree.TryShrink();
        }
    }

    void Awake()
    {

        if (_SpotLightCookie == null)
        {
            _SpotLightCookie = (Texture2D)Resources.Load("LightSoftCookie");
        }


        CreatePIDs();


        Mycamera = GetComponent<Camera>();
    }

    void start()
    {
        Mycamera = GetComponent<Camera>();
    }

    public void ReleaseLightBuffers()
    {
        for (int i = 0; i < ActiveLights.Count; i++)
        {
            ActiveLights[i].ReleaseBuffer();
        }

        ActiveLights.Clear();
    }

    public static bool FirstDirectional = true;

    void CreateLightbuffers()
    {
        if (BufferRenderLights == null) { BufferRenderLights = new CommandBuffer(); BufferRenderLights.name = "renderLights"; }
        else
        { BufferRenderLights.Clear(); }

        if (LightBufferAdded)
        {
            ActiveCamera.RemoveCommandBuffer(lastLightRender, BufferRenderLights);
            LightBufferAdded = false;
        }

       

        if (HxVolumetricCamera.Active.TransparencySupport)
        {
            BufferRenderLights.SetRenderTarget(HxVolumetricCamera.VolumetricTransparency[(int)HxVolumetricCamera.Active.compatibleTBuffer()], HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
        }
        else
        {
            BufferRenderLights.SetRenderTarget(HxVolumetricCamera.VolumetricTextureRTID, HxVolumetricCamera.ScaledDepthTextureRTID[(int)HxVolumetricCamera.Active.resolution]);
        }

        FirstDirectional = true;

        for (int i = 0; i < ActiveLights.Count; i++)
        {
        
            ActiveLights[i].BuildBuffer(BufferRenderLights);
        }

    
        LightBufferAdded = true;
    }

    static void CreateTileTexture()
    {
        Tile5x5 = Resources.Load("HxOffsetTile") as Texture2D;
        if (Tile5x5 == null)
        {
            Tile5x5 = new Texture2D(5, 5, TextureFormat.RFloat, false, true);
            Tile5x5.hideFlags = HideFlags.DontSave;
            Tile5x5.filterMode = FilterMode.Point;
            Tile5x5.wrapMode = TextureWrapMode.Repeat;
            Color[] tempc = new Color[25];
            for (int i = 0; i < tempc.Length; i++)
            {
                tempc[i] = new Color(Tile5x5int[i] * 0.04f, 0, 0, 0);
            }

            Tile5x5.SetPixels(tempc);
            Tile5x5.Apply();
            Shader.SetGlobalTexture("Tile5x5", Tile5x5);
            Shader.SetGlobalFloat("HxTileSize", 5);
        }
        else
        {
            Shader.SetGlobalTexture("Tile5x5", Tile5x5);
            Shader.SetGlobalFloat("HxTileSize", Tile5x5.width);

        }

    }

    public static Mesh CreateOrtho(int sides, bool inner = true)
    {

        Vector3[] vertices = {
            new Vector3 (-0.5f, -0.5f, 0),
            new Vector3 (0.5f, -0.5f, 0),
            new Vector3 (0.5f, 0.5f, 0),
            new Vector3 (-0.5f, 0.5f, 0),
            new Vector3 (-0.5f, 0.5f, 1),
            new Vector3 (0.5f, 0.5f, 1),
            new Vector3 (0.5f, -0.5f, 1),
            new Vector3 (-0.5f, -0.5f, 1),
        };

        int[] triangles = {
            0, 2, 1, //face front
			0, 3, 2,
            2, 3, 4, //face top
			2, 4, 5,
            1, 2, 5, //face right
			1, 5, 6,
            0, 7, 4, //face left
			0, 4, 3,
            5, 4, 7, //face back
			5, 7, 6,
            0, 6, 7, //face bottom
			0, 1, 6
        };

        Mesh mesh = new Mesh();
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    
}

    public static Mesh CreateCone(int sides, bool inner = true)
    {
        Mesh newMesh = new Mesh();
        Vector3[] verts = new Vector3[sides + 1];
        int[] tri = new int[(sides * 3) + ((sides - 2) * 3)];

        float r = (inner ? Mathf.Cos(Mathf.PI / (sides)) : 1f);

        float aLength = r * Mathf.Tan(Mathf.PI / (sides));
        Vector3 topCenter = new Vector3(0.5f - ((1f - r) / 2f), 0, 0);
        Vector3 Offset = new Vector3(0, 0, aLength);


        topCenter += new Vector3(0, 0, aLength / 2f);

        //verts
        Quaternion offsetAngle = Quaternion.Euler(new Vector3(0, (360f / (sides)), 0));
        Quaternion Rotation = Quaternion.Euler(new Vector3(-90, 0, 0));

        verts[0] = new Vector3(0f, 0f, 0);

        for (int i = 1; i < sides + 1; i++)
        {
            verts[i] = Rotation * (topCenter - Vector3.up);

            topCenter -= Offset;
            Offset = offsetAngle * Offset;
        };


        //triSides
        int n = 0;
        for (int i = 0; i < sides - 1; i++)
        {
            n = i * 3;
            tri[n] = 0;
            tri[n + 1] = i + 1;
            tri[n + 2] = i + 2;
        }
        n = (sides - 1) * 3; ;
        tri[n] = 0;
        tri[n + 1] = sides;
        tri[n + 2] = 1;
        n += 3;

        for (int i = 0; i < sides - 2; i++)
        {
            tri[n] = 1;
            tri[n + 2] = i + 2;
            tri[n + 1] = i + 3;
            n += 3;
        }


        newMesh.vertices = verts;
        newMesh.triangles = tri;
        newMesh.uv = new Vector2[verts.Length];
        newMesh.colors = new Color[0];
        ;
        newMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
        newMesh.RecalculateNormals();

        return newMesh;
    }

    public static Mesh CreateQuad()
    {
        /*
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Mesh m = t.GetComponent<MeshFilter>().sharedMesh;
        if (Application.isPlaying)
        {
            GameObject.Destroy(t);
        }
        else
        {
            GameObject.DestroyImmediate(t);
        }
        return m;
        */

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];

        vertices[0] = new Vector3(-1, -1, 1);
        vertices[1] = new Vector3(-1, 1, 1);
        vertices[2] = new Vector3(1, -1, 1);
        vertices[3] = new Vector3(1, 1, 1);

        mesh.vertices = vertices;

        int[] indices = new int[6];

        indices[0] = 0;
        indices[1] = 1;
        indices[2] = 2;

        indices[3] = 2;
        indices[4] = 1;
        indices[5] = 3;

        mesh.triangles = indices;
        mesh.RecalculateBounds();

        return mesh;

    }

    public static Mesh CreateBox()
    {
        GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh m = t.GetComponent<MeshFilter>().sharedMesh;
        if (Application.isPlaying)
        {
            GameObject.Destroy(t);
        }
        else
        {
            GameObject.DestroyImmediate(t);
        }
        return m;
    }

    public static Mesh CreateIcoSphere(int recursionLevel, float radius)
    {

        Mesh mesh = new Mesh();
        mesh.Clear();

        List<Vector3> vertList = new List<Vector3>();
        Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();

        // create 12 vertices of a icosahedron
        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        vertList.Add(new Vector3(-1f, t, 0f).normalized * radius);
        vertList.Add(new Vector3(1f, t, 0f).normalized * radius);
        vertList.Add(new Vector3(-1f, -t, 0f).normalized * radius);
        vertList.Add(new Vector3(1f, -t, 0f).normalized * radius);

        vertList.Add(new Vector3(0f, -1f, t).normalized * radius);
        vertList.Add(new Vector3(0f, 1f, t).normalized * radius);
        vertList.Add(new Vector3(0f, -1f, -t).normalized * radius);
        vertList.Add(new Vector3(0f, 1f, -t).normalized * radius);

        vertList.Add(new Vector3(t, 0f, -1f).normalized * radius);
        vertList.Add(new Vector3(t, 0f, 1f).normalized * radius);
        vertList.Add(new Vector3(-t, 0f, -1f).normalized * radius);
        vertList.Add(new Vector3(-t, 0f, 1f).normalized * radius);


        // create 20 triangles of the icosahedron
        List<TriangleIndices> faces = new List<TriangleIndices>();

        // 5 faces around point 0
        faces.Add(new TriangleIndices(0, 11, 5));
        faces.Add(new TriangleIndices(0, 5, 1));
        faces.Add(new TriangleIndices(0, 1, 7));
        faces.Add(new TriangleIndices(0, 7, 10));
        faces.Add(new TriangleIndices(0, 10, 11));

        // 5 adjacent faces 
        faces.Add(new TriangleIndices(1, 5, 9));
        faces.Add(new TriangleIndices(5, 11, 4));
        faces.Add(new TriangleIndices(11, 10, 2));
        faces.Add(new TriangleIndices(10, 7, 6));
        faces.Add(new TriangleIndices(7, 1, 8));

        // 5 faces around point 3
        faces.Add(new TriangleIndices(3, 9, 4));
        faces.Add(new TriangleIndices(3, 4, 2));
        faces.Add(new TriangleIndices(3, 2, 6));
        faces.Add(new TriangleIndices(3, 6, 8));
        faces.Add(new TriangleIndices(3, 8, 9));

        // 5 adjacent faces 
        faces.Add(new TriangleIndices(4, 9, 5));
        faces.Add(new TriangleIndices(2, 4, 11));
        faces.Add(new TriangleIndices(6, 2, 10));
        faces.Add(new TriangleIndices(8, 6, 7));
        faces.Add(new TriangleIndices(9, 8, 1));


        // refine triangles
        for (int i = 0; i < recursionLevel; i++)
        {
            List<TriangleIndices> faces2 = new List<TriangleIndices>();
            foreach (var tri in faces)
            {
                // replace triangle by 4 triangles
                int a = getMiddlePoint(tri.v1, tri.v2, ref vertList, ref middlePointIndexCache, radius);
                int b = getMiddlePoint(tri.v2, tri.v3, ref vertList, ref middlePointIndexCache, radius);
                int c = getMiddlePoint(tri.v3, tri.v1, ref vertList, ref middlePointIndexCache, radius);

                faces2.Add(new TriangleIndices(tri.v1, a, c));
                faces2.Add(new TriangleIndices(tri.v2, b, a));
                faces2.Add(new TriangleIndices(tri.v3, c, b));
                faces2.Add(new TriangleIndices(a, b, c));
            }
            faces = faces2;
        }

        mesh.vertices = vertList.ToArray();

        List<int> triList = new List<int>();
        for (int i = 0; i < faces.Count; i++)
        {
            triList.Add(faces[i].v1);
            triList.Add(faces[i].v2);
            triList.Add(faces[i].v3);
        }

        mesh.triangles = triList.ToArray();
        mesh.uv = new Vector2[vertList.Count];

        Vector3[] normales = new Vector3[vertList.Count];
        for (int i = 0; i < normales.Length; i++)
            normales[i] = vertList[i].normalized;


        mesh.normals = normales;

        mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
        ;

        return mesh;

    }

    private struct TriangleIndices
    {
        public int v1;
        public int v2;
        public int v3;

        public TriangleIndices(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }

    private static int getMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache, float radius)
    {
        // first check if we have it already
        bool firstIsSmaller = p1 < p2;
        long smallerIndex = firstIsSmaller ? p1 : p2;
        long greaterIndex = firstIsSmaller ? p2 : p1;
        long key = (smallerIndex << 32) + greaterIndex;

        int ret;
        if (cache.TryGetValue(key, out ret))
        {
            return ret;
        }

        // not in cache, calculate it
        Vector3 point1 = vertices[p1];
        Vector3 point2 = vertices[p2];
        Vector3 middle = new Vector3
        (
            (point1.x + point2.x) / 2f,
            (point1.y + point2.y) / 2f,
            (point1.z + point2.z) / 2f
        );

        // add vertex makes sure point is on unit sphere
        int i = vertices.Count;
        vertices.Add(middle.normalized * radius);

        // store it, return index
        cache.Add(key, i);

        return i;
    }

    public void Create3DNoiseTexture()
    {
        NoiseTexture3D = Resources.Load("NoiseTexture") as Texture3D;
        
        /*
        int size = 32;
        NoiseTexture3D = new Texture3D(size, size, size, TextureFormat.Alpha8, false);
        NoiseTexture3D.hideFlags = HideFlags.DontSave;
        NoiseTexture3D.filterMode = FilterMode.Bilinear;
        NoiseTexture3D.wrapMode = TextureWrapMode.Repeat;
        Tileable3DNoise sn = new Tileable3DNoise();
        Color[] tempc = new Color[size * size * size];

        int idx = 0;

        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++, ++idx)
                {
                    tempc[idx].a = sn.noiseArray[idx] / 255.0f;
                }
            }
        }

        //Debug.Log("min = " + min + " Max = " + max + "Average = " + (average/(32*32*32)));

        NoiseTexture3D.SetPixels(tempc);
        NoiseTexture3D.Apply();
        */
        Shader.SetGlobalTexture("NoiseTexture3D", NoiseTexture3D);
    }

    int PostoIndex(Vector3 pos)
    {
        if (pos.x >= 32) { pos.x = 0; } else if (pos.x < 0) { pos.x = 31; }
        if (pos.y >= 32) { pos.y = 0; } else if (pos.y < 0) { pos.y = 31; }
        if (pos.z >= 32) { pos.z = 0; } else if (pos.z < 0) { pos.z = 31; }

        return (int)(pos.z * 32 * 32 + pos.y * 32 + pos.x);
    }
    //float SampleNoise(Tileable3DNoise sn,Vector3 pos)
    //{
    //    float amount = 0.5f + sn.noiseArray[PostoIndex(pos)];
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.left)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.right)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.up)]) * 0.7f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.down)]) * 0.7f;
    //               
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.right)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.left)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.right)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.left)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.up + Vector3.forward)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.up + Vector3.back)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.down + Vector3.forward)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.down + Vector3.back)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.right + Vector3.down)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.right + Vector3.back)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.left + Vector3.up)]) * 0.5f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.left + Vector3.down)]) * 0.5f;
    //               
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.up + Vector3.right)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.up + Vector3.left)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.up + Vector3.right)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.up + Vector3.left)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.down + Vector3.right)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.forward + Vector3.down + Vector3.left)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.down + Vector3.right)]) * 0.3f;
    //     amount += (0.5f + sn.noiseArray[PostoIndex(pos + Vector3.back + Vector3.down + Vector3.left)]) * 0.3f;
    //     float f = Mathf.Clamp01((amount / 13.6f) * 1.4285714285714285714285714285714f);
    //     return f * f;
    //}


    //    static int[] Tile10x10int = new int[100] //{ 50, 28, 14, 8, 45, 63, 20, 5, 39, 11, 33, 12, 23, 30, 29, 17, 36, 56, 22, 59, 0, 4, 60, 35, 2, 57, 47, 21, 6, 13, 38, 1, 52, 42, 19, 16, 62, 44, 31, 15, 58, 40, 53, 43, 61, 3, 48, 24, 18, 41, 32, 26, 37, 27, 46, 54, 55, 7, 9, 25, 34, 51, 49, 10 };
    //{35,	67,	48,	0,	82,	24,	76,	45,	8,	97,
    //5,	85,	39,	68,	40,	2,	95,	22,	77,	50,
    //64,	42,	13,	81,	21,	72,	56,	7,	88,	34,
    //90,	30,	75,	52,	12,	93,	29,	62,	55,	17,
    //51,	19,	96,	36,	69,	44,	18,	84,	32,	71,
    //33,	66,	53,	11,	87,	23,	64,	41,	10,	92,
    //15,	86,	26,	74,	46,	16,	83,	25,	60,	54,
    //79,	58,	3,	80,	38,	78,	59,	6,	99,	37,
    //94,	31,	73,	43,	9,	91,	28,	61,	57,	4 ,
    //49,	14,	98,	27,	63,	47,	1,	89,	20,	70};
    //
    //    
    //}

    //{ 50, 28, 14, 8, 45, 63, 20, 5, 39, 11, 33, 12, 23, 30, 29, 17, 36, 56, 22, 59, 0, 4, 60, 35, 2, 57, 47, 21, 6, 13, 38, 1, 52, 42, 19, 16, 62, 44, 31, 15, 58, 40, 53, 43, 61, 3, 48, 24, 18, 41, 32, 26, 37, 27, 46, 54, 55, 7, 9, 25, 34, 51, 49, 10 };   
    static int[] Tile5x5int = new int[25]{
8,  18, 22, 0,  13,
4,  14, 9,  19, 21,
16, 23, 1,  12, 6,
10, 7,  15, 24, 3,
20, 2,  11, 5,  17};
}


