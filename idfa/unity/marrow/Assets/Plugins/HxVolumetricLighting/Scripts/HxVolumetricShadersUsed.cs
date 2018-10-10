
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HxVolumetricShadersUsed : ScriptableObject
{



   
    public HxVolumetricCamera.TransparencyQualities TransperencyQuality = HxVolumetricCamera.TransparencyQualities.Medium;
    public HxVolumetricCamera.DensityParticleQualities DensityParticleQuality = HxVolumetricCamera.DensityParticleQualities.High;
    [HideInInspector]
    public HxVolumetricCamera.DensityParticleQualities LastDensityParticleQuality = HxVolumetricCamera.DensityParticleQualities.High;
    [HideInInspector]
    public HxVolumetricCamera.TransparencyQualities LastTransperencyQuality = HxVolumetricCamera.TransparencyQualities.Medium;

    static HxVolumetricShadersUsed instance;
    public bool Full;
    public bool LowRes;
    public bool HeightFog;
    public bool HeightFogOff;
    public bool Noise;
    public bool NoiseOff;
    public bool Transparency;
    public bool TransparencyOff;
    public bool DensityParticles;
    public bool Point;
    public bool Spot;
    public bool Directional;
    public bool SinglePassStereo;
    public bool Projector;

    [HideInInspector]
    public bool FullLast;
    [HideInInspector]
    public bool LowResLast;
    [HideInInspector]
    public bool HeightFogLast;
    [HideInInspector]
    public bool HeightFogOffLast;
    [HideInInspector]
    public bool NoiseLast;
    [HideInInspector]
    public bool NoiseOffLast;
    [HideInInspector]
    public bool TransparencyLast;
    [HideInInspector]
    public bool TransparencyOffLast;
    [HideInInspector]
    public bool DensityParticlesLast;
    [HideInInspector]
    public bool PointLast;
    [HideInInspector]
    public bool SpotLast;
    [HideInInspector]
    public bool DirectionalLast;
    [HideInInspector]
    public bool SinglePassStereoLast;
    [HideInInspector]
    public bool ProjectorLast;

    bool CheckDirty()
    {
        bool dirty = false;
        if (Resources.Load("HxUsedShaders") == null) { dirty = true;  }
        if (Resources.Load("HxUsedShaderVariantCollection") == null) { dirty = true;}

        if (Full != FullLast) { dirty = true; FullLast = Full;  }
        if (LowRes != LowResLast) { dirty = true; LowResLast = LowRes; }
        if (HeightFog != HeightFogLast) { dirty = true; HeightFogLast = HeightFog; }
        if (HeightFogOff != HeightFogOffLast) { dirty = true; HeightFogOffLast = HeightFogOff;}
        if (Noise != NoiseLast) { dirty = true; NoiseLast = Noise;  }
        if (NoiseOff != NoiseOffLast) { dirty = true; NoiseOffLast = NoiseOff;  }
        if (Transparency != TransparencyLast) { dirty = true; TransparencyLast = Transparency;  }
        if (TransparencyOff != TransparencyOffLast) { dirty = true; TransparencyOffLast = TransparencyOff; }
        if (DensityParticles != DensityParticlesLast) { dirty = true; DensityParticlesLast = DensityParticles;  }      
        if (Point != PointLast) { dirty = true; PointLast = Point; }
        if (Spot != SpotLast) { dirty = true; SpotLast = Spot; }
        if (Directional != DirectionalLast) { dirty = true; DirectionalLast = Directional;  }
        if (SinglePassStereo != SinglePassStereoLast) { dirty = true; SinglePassStereoLast = SinglePassStereo;  }
        if (Projector != ProjectorLast) { dirty = true; ProjectorLast = Projector; }
        return dirty;
    }
    // public List<Material> PointMaterials = new List<Material>();
    // public List<Material> SpotMaterials = new List<Material>();
    // public List<Material> DirectionMaterials = new List<Material>();
#if UNITY_EDITOR
    public static void SetVolumetricValues(bool full,bool lowRes, bool heightFog, bool heightFogOff, bool noise, bool noiseOff, bool transparency, bool transparencyOff, bool densityParticles, bool point, bool spot, bool directional, bool singlePass,bool projector)
    {




        if (!BuildPipeline.isBuildingPlayer)
        {
            if (instance == null)
            {
                instance = AssetDatabase.LoadAssetAtPath("Assets/Plugins/HxVolumetricLighting/Resources/HxUsedShaders.prefab", typeof(HxVolumetricShadersUsed)) as HxVolumetricShadersUsed;

                if (instance == null)
                {
                    instance = ScriptableObject.CreateInstance<HxVolumetricShadersUsed>();
                    AssetDatabase.CreateAsset(instance, "Assets/Plugins/HxVolumetricLighting/Resources/HxUsedShaders.prefab");
                    AssetDatabase.SaveAssets();
                    // instance = AssetDatabase.LoadAssetAtPath("Assets/Plugins/HxVolumetricLighting/HxUsedShaders.prefab", typeof(HxVolumetricShadersUsed)) as GameObject;
                }
            }

            if (instance != null)
            {
                if (full) { instance.Full = full; }
                if (lowRes) { instance.LowRes = lowRes; }
                if (heightFog) { instance.HeightFog = heightFog; }
                if (heightFogOff) { instance.HeightFogOff = heightFogOff; }
                if (noise) { instance.Noise = noise; }
                if (noiseOff) { instance.NoiseOff = noiseOff; }
                if (transparency) { instance.Transparency = transparency; }
                if (transparencyOff) { instance.TransparencyOff = transparencyOff; }
                if (densityParticles) { instance.DensityParticles = densityParticles; }               
                if (point) { instance.Point = point; }
                if (spot) { instance.Spot = spot; }
                if (directional) { instance.Directional = directional; }
                if (singlePass) { instance.SinglePassStereo = singlePass; }       
                if (projector) { instance.Projector = projector; }
                if (instance.CheckDirty())
                {
                    instance.BuildShaders();
                }
            }
        }
    }


    static public void ForceBuild()
    {
        if (instance == null)
        {
            instance = AssetDatabase.LoadAssetAtPath("Assets/Plugins/HxVolumetricLighting/Resources/HxUsedShaders.prefab", typeof(HxVolumetricShadersUsed)) as HxVolumetricShadersUsed;

            if (instance == null)
            {
                instance = ScriptableObject.CreateInstance<HxVolumetricShadersUsed>();
                AssetDatabase.CreateAsset(instance, "Assets/Plugins/HxVolumetricLighting/Resources/HxUsedShaders.prefab");
                AssetDatabase.SaveAssets();
                // instance = AssetDatabase.LoadAssetAtPath("Assets/Plugins/HxVolumetricLighting/HxUsedShaders.prefab", typeof(HxVolumetricShadersUsed)) as GameObject;
            }
        }

        instance.CheckDirty();

        if (instance.LastDensityParticleQuality != instance.DensityParticleQuality || instance.LastTransperencyQuality != instance.TransperencyQuality)
        {
            instance.RebuildQualityFile();
            instance.LastDensityParticleQuality = instance.DensityParticleQuality;
            instance.LastTransperencyQuality = instance.TransperencyQuality;
            HxVolumetricCamera.TransparencyBufferDepth = instance.LastTransperencyQuality;
            HxVolumetricCamera.DensityBufferDepth = instance.LastDensityParticleQuality;
        }

            instance.BuildShaders();
    }

    void RebuildQualityFile()
    {

        string ShaderCode = "";
        switch (instance.DensityParticleQuality)
        {
            case HxVolumetricCamera.DensityParticleQualities.Low :
                {
                    ShaderCode += "#define DS_4\n";
                    break;
                }
            case HxVolumetricCamera.DensityParticleQualities.Medium:
                {
                    ShaderCode += "#define DS_8\n";
                    break;
                }
            case HxVolumetricCamera.DensityParticleQualities.High:
                {
                    ShaderCode += "#define DS_12\n";
                    break;
                }
            case HxVolumetricCamera.DensityParticleQualities.VeryHigh:
                {
                    ShaderCode += "#define DS_16\n";
                    break;
                }
        }

        switch (instance.TransperencyQuality)
        {
            case HxVolumetricCamera.TransparencyQualities.Low:
                {
                    ShaderCode += "#define TS_4";
                    break;
                }
            case HxVolumetricCamera.TransparencyQualities.Medium:
                {
                    ShaderCode += "#define TS_8";
                    break;
                }
            case HxVolumetricCamera.TransparencyQualities.High:
                {
                    ShaderCode += "#define TS_12";
                    break;
                }
            case HxVolumetricCamera.TransparencyQualities.VeryHigh:
                {
                    ShaderCode += "#define TS_16";
                    break;
                }
        }

        //Debug.Log("Path = " + Application.dataPath + "/Plugins/HxVolumetricLighting/Resources/Shaders/hxQualitySettings.cginc");
        File.WriteAllText(Application.dataPath + "/Plugins/HxVolumetricLighting/Resources/Shaders/hxQualitySettings.cginc", ShaderCode);
        AssetDatabase.Refresh();
    }

    void BuildShaders()
    {

        ShaderVariantCollection collection = new ShaderVariantCollection();

 
        AssetDatabase.CreateAsset(collection, "Assets/Plugins/HxVolumetricLighting/Resources/HxUsedShaderVariantCollection.shadervariants");
        
        collection.Clear();



        Shader pointShader = Shader.Find("Hidden/HxVolumetricPointLight");
        Shader directionalShader = Shader.Find("Hidden/HxVolumetricDirectionalLight");
        Shader spotShader = Shader.Find("Hidden/HxVolumetricSpotLight");
        Shader projectorShader = Shader.Find("Hidden/HxVolumetricProjector");
        for (int i = 0; i < 128; i++)
        {
            this.CheckShaderVariant(collection, pointShader, i, true);
            this.CheckShaderVariant(collection, directionalShader, i, false);
            this.CheckShaderVariant(collection, spotShader, i, false);
            this.CheckShaderVariant(collection, projectorShader, i, false,true);
        }

        // AssetDatabase.ImportAsset()
        EditorUtility.SetDirty(collection);
        EditorUtility.SetDirty(this);
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(this));
        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(collection));

        AssetDatabase.SaveAssets();
    }

    static List<string> ShaderVariantList = new List<string>(10);

    void CheckShaderVariant(ShaderVariantCollection collection, Shader source, int i, bool point = true,bool projector = false)
    {

        ShaderVariantList.Clear();
       

        int v = i;
        int vc = 0;
        if (v >= 64) { if (!Full) { return; } ShaderVariantList.Add("FULL_ON"); v -= 64; vc++; } else { if(!LowRes){ return; } }
        if (v >= 32) { if (!Transparency) { return; } ShaderVariantList.Add("VTRANSPARENCY_ON"); v -= 32; vc++; } else { if (!TransparencyOff) { return; } }
        if (v >= 16) { if (!DensityParticles) { return; } ShaderVariantList.Add("DENSITYPARTICLES_ON"); v -= 16; vc++; }
        if (v >= 8) { if (!HeightFog) { return; } ShaderVariantList.Add("HEIGHTFOG_ON"); v -= 8; vc++; } else { if (!HeightFogOff) { return; } }
        if (v >= 4) { if (!Noise) { return; } ShaderVariantList.Add("NOISE_ON"); v -= 4; vc++; } else { if (!NoiseOff) { return; } }
        if (v >= 2) { if (point) { ShaderVariantList.Add("POINT_COOKIE"); vc++; } v -= 2; }
        if (v >= 1) { v -= 1; } else { if (!projector) { ShaderVariantList.Add("SHADOWS_OFF"); } vc++; };


        string[] fv = new string[vc];
        ShaderVariantList.CopyTo(fv);
        Material m = new Material(source);
        m.name = "";
        EnableKeywordList(m);
    
        AssetDatabase.AddObjectToAsset(m, collection);
     
       //string final = source.name + " ";
       //for (int s = 0; s < fv.Length; s++)
       //{
       //    final += fv[s] + " ";
       //}
       // Debug.Log(final);
        ShaderVariantCollection.ShaderVariant varient = new ShaderVariantCollection.ShaderVariant(source, PassType.Normal, fv);
        if (!collection.Contains(varient)) { collection.Add(varient); }
        //if (shadows && !point && !projector)
        //{
        //    ShaderVariantList.Add("SHADOWS_NATIVE");
        //    string[] fv2 = new string[vc + 1];
        //    ShaderVariantList.CopyTo(fv2);
        //
        //    ShaderVariantCollection.ShaderVariant varient2 = new ShaderVariantCollection.ShaderVariant(source, PassType.Normal, fv2);
        //    if (!collection.Contains(varient2)) { collection.Add(varient2); }
        //
        //    Material m2 = new Material(source);
        //    EnableKeywordList(m2);
        //    m2.name = "";
        //    AssetDatabase.AddObjectToAsset(m2, collection);
        //    ShaderVariantList.RemoveAt(ShaderVariantList.Count - 1);
        //
        //
        //}

        if (SinglePassStereo)
        {
            ShaderVariantList.Add("UNITY_SINGLE_PASS_STEREO");
            string[] fvssp2 = new string[vc + 1];
            ShaderVariantList.CopyTo(fvssp2);

            ShaderVariantCollection.ShaderVariant varientSP = new ShaderVariantCollection.ShaderVariant(source, PassType.Normal, fvssp2);
            if (!collection.Contains(varientSP)) { collection.Add(varientSP); }

            Material m2 = new Material(source);
            EnableKeywordList(m2);
            m2.name = "";
            AssetDatabase.AddObjectToAsset(m2, collection);

           // if (shadows && !point && !projector)
           // {
           //     ShaderVariantList.Add("SHADOWS_NATIVE");
           //     string[] fv2 = new string[vc + 2];
           //     ShaderVariantList.CopyTo(fv2);
           //
           //     ShaderVariantCollection.ShaderVariant varient2 = new ShaderVariantCollection.ShaderVariant(source, PassType.Normal, fv2);
           //     if (!collection.Contains(varient2)) { collection.Add(varient2); }
           //
           //     Material m3 = new Material(source);
           //     EnableKeywordList(m3);
           //     m3.name = "";
           //     AssetDatabase.AddObjectToAsset(m3, collection);
           //
           // }
        }
    }


    /*void RemoveOldMaterials()
    {
        if (PointMaterials != null)
        {
            for (int i = 0; i < PointMaterials.Count; i++)
            {
                if (PointMaterials[i] != null)
                {
                    if (Application.isPlaying)
                    { GameObject.Destroy(PointMaterials[i]); }
                    else
                    { GameObject.DestroyImmediate(PointMaterials[i]); }
                }
            }
        }

        if (SpotMaterials != null)
        {
            for (int i = 0; i < SpotMaterials.Count; i++)
            {
                if (SpotMaterials[i] != null)
                {
                    if (Application.isPlaying)
                    { GameObject.Destroy(SpotMaterials[i]); }
                    else
                    { GameObject.DestroyImmediate(SpotMaterials[i]); }
                }
            }
        }

        if (DirectionMaterials != null)
        {
            for (int i = 0; i < DirectionMaterials.Count; i++)
            {
                if (DirectionMaterials[i] != null)
                {
                    if (Application.isPlaying)
                    { GameObject.Destroy(DirectionMaterials[i]); }
                    else
                    { GameObject.DestroyImmediate(DirectionMaterials[i]); }
                }
            }
        }
    }*/

    //void OnValidate()
    //{
    //    if (CheckDirty())
    //    {
    //        BuildShaders();
    //    }
    //}

    void EnableKeywordList(Material material)
    {
        for (int i = 0; i < ShaderVariantList.Count; i++)
        {

            material.EnableKeyword(ShaderVariantList[i]);
        }
    }

#endif
}
