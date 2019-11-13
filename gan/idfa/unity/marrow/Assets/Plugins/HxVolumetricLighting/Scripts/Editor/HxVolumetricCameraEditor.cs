#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditor.AnimatedValues;
using UnityEditor.Callbacks;
// Custom Editor using SerializedProperties.
// Automatic handling of multi-object editing, undo, and prefab overrides.


[CustomEditor(typeof(HxVolumetricCamera))]
[CanEditMultipleObjects]

public class HxVolumetricCameraEditor : Editor  
{


    private SerializedProperty m_MapToLDR;
    private SerializedProperty m_resolution;
    private SerializedProperty m_SampleCount;
    private SerializedProperty m_DirectionalSampleCount;
    private SerializedProperty m_MaxDirectionalRayDistance;
    private SerializedProperty m_MaxLightDistance;
    private SerializedProperty m_Density;
    private SerializedProperty m_AmbientLightingStrength;
   

    private SerializedProperty m_NoiseContrast;
    private SerializedProperty m_MieScattering;
    private SerializedProperty m_SunSize;
    private SerializedProperty m_SunBleed;
    private SerializedProperty m_Extinction;
    private SerializedProperty m_ExtinctionEffect;

    private SerializedProperty m_ShadowFix;
    private SerializedProperty m_RenderOrder;

    private SerializedProperty m_FogHeightEnabled;
    private SerializedProperty m_FogHeight;
    private SerializedProperty m_FogTransitionSize;
    private SerializedProperty m_AboveFogPercent;

    private SerializedProperty m_NoiseEnabled;
    private SerializedProperty m_NoiseScale;
    private SerializedProperty m_NoiseVelocity;


    private SerializedProperty m_ParticleDensitySupport;
    private SerializedProperty m_densityResolution;
    private SerializedProperty m_densityDistance;
    private SerializedProperty m_TransparencySupport;
    private SerializedProperty m_transparencyDistance;
    private SerializedProperty m_BlurTransparency;

    private SerializedProperty m_TemporalSampling;
    private SerializedProperty m_DitherSpeed;
    private SerializedProperty m_LuminanceFeedback;
    private SerializedProperty m_MaxFeedback;


    private SerializedProperty m_blurCount;
    private SerializedProperty m_BlurDepthFalloff;
    private SerializedProperty m_DownsampledBlurDepthFalloff;
    private SerializedProperty m_UpSampledblurCount;


    private SerializedProperty m_DepthThreshold;
    private SerializedProperty m_GaussianWeights;
    private SerializedProperty m_RemoveColorBanding;
    // private SerializedProperty m_WarmUpShaders;

    private SerializedProperty m_NoiseTexture;

    private SerializedProperty m_AmbientMode;
    private SerializedProperty m_AmbientSky;
    private SerializedProperty m_AmbientEquator;
    private SerializedProperty m_AmbientGround;
    private SerializedProperty m_AmbientIntesity;
    private SerializedProperty m_TintMode;
    private SerializedProperty m_TintColor;
    private SerializedProperty m_TintColor2;
    private SerializedProperty m_TintIntensity;
    private SerializedProperty m_TintGradient;

    AnimBool m_ShowTemporal;
    AnimBool m_ShowAmbient;
    AnimBool m_ShowGeneral;
    AnimBool m_ShowLighting;
    AnimBool m_ShowFog;
    AnimBool m_ShowNoise;
    AnimBool m_ShowDensity;
    AnimBool m_ShowTransparency;
    AnimBool m_ShowAdvanced;


    static GUIStyle toggles;

    void OnLostFocus()
    {
        EditorPrefs.SetBool("Hxl_ShowTemporal", m_ShowTemporal.target); 
        EditorPrefs.SetBool("Hxl_ShowAmbient", m_ShowAmbient.target);
        EditorPrefs.SetBool("Hxl_ShowGeneral", m_ShowGeneral.target);
        EditorPrefs.SetBool("Hxl_ShowLighting", m_ShowLighting.target);
        EditorPrefs.SetBool("Hxl_ShowFog", m_ShowFog.target);
        EditorPrefs.SetBool("Hxl_ShowNoise", m_ShowNoise.target);
        EditorPrefs.SetBool("Hxl_ShowDensity", m_ShowDensity.target);
        EditorPrefs.SetBool("Hxl_ShowTransparency", m_ShowTransparency.target);
        EditorPrefs.SetBool("Hxl_ShowAdvanced", m_ShowAdvanced.target);

    }
    void OnDestroy()
    {
        EditorPrefs.SetBool("Hxl_ShowTemporal", m_ShowTemporal.target);
        EditorPrefs.SetBool("Hxl_ShowAmbient", m_ShowAmbient.target);
        EditorPrefs.SetBool("Hxl_ShowGeneral", m_ShowGeneral.target);
        EditorPrefs.SetBool("Hxl_ShowLighting", m_ShowLighting.target);
        EditorPrefs.SetBool("Hxl_ShowFog", m_ShowFog.target);
        EditorPrefs.SetBool("Hxl_ShowNoise", m_ShowNoise.target);
        EditorPrefs.SetBool("Hxl_ShowDensity", m_ShowDensity.target);
        EditorPrefs.SetBool("Hxl_ShowTransparency", m_ShowTransparency.target);
        EditorPrefs.SetBool("Hxl_ShowAdvanced", m_ShowAdvanced.target);
    }


    private void OnEnable()
    {
   
        if (m_ShowTemporal == null)
        {
            m_ShowTemporal = new AnimBool(EditorPrefs.GetBool("Hxl_ShowTemporal", true));
            m_ShowTemporal.valueChanged.AddListener(Repaint);
        }

        if (m_ShowAmbient == null)
        {
            m_ShowAmbient = new AnimBool(EditorPrefs.GetBool("Hxl_ShowAmbient", true));
            m_ShowAmbient.valueChanged.AddListener(Repaint);
        }

        if (m_ShowGeneral == null)
        {
            m_ShowGeneral = new AnimBool(EditorPrefs.GetBool("Hxl_ShowGeneral", false));
            m_ShowGeneral.valueChanged.AddListener(Repaint);
        }

        if (m_ShowLighting == null)
        {
            m_ShowLighting = new AnimBool(EditorPrefs.GetBool("Hxl_ShowLighting", true));
            m_ShowLighting.valueChanged.AddListener(Repaint);
        }

        if (m_ShowFog == null)
        {
            m_ShowFog = new AnimBool(EditorPrefs.GetBool("Hxl_ShowFog", false));
            m_ShowFog.valueChanged.AddListener(Repaint);
        }


        if (m_ShowNoise == null)
        {
            m_ShowNoise = new AnimBool(EditorPrefs.GetBool("Hxl_ShowNoise", false));
            m_ShowNoise.valueChanged.AddListener(Repaint);
        }

        if (m_ShowDensity == null)
        {
            m_ShowDensity = new AnimBool(EditorPrefs.GetBool("Hxl_ShowDensity", false));
            m_ShowDensity.valueChanged.AddListener(Repaint);
        }

        if (m_ShowTransparency == null)
        {
            m_ShowTransparency = new AnimBool(EditorPrefs.GetBool("Hxl_ShowTransparency", false));
            m_ShowTransparency.valueChanged.AddListener(Repaint);
        }

        if (m_ShowAdvanced == null)
        {
            m_ShowAdvanced = new AnimBool(EditorPrefs.GetBool("Hxl_ShowAdvanced", false));
            m_ShowAdvanced.valueChanged.AddListener(Repaint);
        }
        m_ShadowFix = serializedObject.FindProperty("ShadowFix");
        m_NoiseTexture = serializedObject.FindProperty("NoiseTexture3D");
        m_RenderOrder = serializedObject.FindProperty("RenderOrder");
        m_NoiseContrast = serializedObject.FindProperty("NoiseContrast");
        m_AmbientMode = serializedObject.FindProperty("Ambient");
        m_AmbientSky = serializedObject.FindProperty("AmbientSky");
        m_AmbientEquator = serializedObject.FindProperty("AmbientEquator");
        m_AmbientGround = serializedObject.FindProperty("AmbientGround");
        m_AmbientIntesity = serializedObject.FindProperty("AmbientIntensity");
        m_TintColor = serializedObject.FindProperty("TintColor");
        m_TintColor2 = serializedObject.FindProperty("TintColor2");
        m_TintIntensity = serializedObject.FindProperty("TintIntensity");
        m_TintMode = serializedObject.FindProperty("TintMode");
        m_TintGradient = serializedObject.FindProperty("TintGradient");


        m_MapToLDR = serializedObject.FindProperty("MapToLDR");
        m_resolution = serializedObject.FindProperty("resolution");
        m_SampleCount = serializedObject.FindProperty("SampleCount");
        m_DirectionalSampleCount = serializedObject.FindProperty("DirectionalSampleCount");
        m_MaxDirectionalRayDistance = serializedObject.FindProperty("MaxDirectionalRayDistance");
        m_MaxLightDistance = serializedObject.FindProperty("MaxLightDistance");
        m_Density = serializedObject.FindProperty("Density");
        m_AmbientLightingStrength = serializedObject.FindProperty("AmbientLightingStrength");


        m_MieScattering = serializedObject.FindProperty("MieScattering");
        m_SunSize = serializedObject.FindProperty("SunSize");
        m_SunBleed = serializedObject.FindProperty("SunBleed");
        m_Extinction = serializedObject.FindProperty("Extinction");
        m_ExtinctionEffect = serializedObject.FindProperty("ExtinctionEffect");


        m_FogHeightEnabled = serializedObject.FindProperty("FogHeightEnabled");
        m_FogHeight = serializedObject.FindProperty("FogHeight");
        m_FogTransitionSize = serializedObject.FindProperty("FogTransitionSize");
        m_AboveFogPercent = serializedObject.FindProperty("AboveFogPercent");

        m_NoiseEnabled = serializedObject.FindProperty("NoiseEnabled");
        m_NoiseScale = serializedObject.FindProperty("NoiseScale");
        m_NoiseVelocity = serializedObject.FindProperty("NoiseVelocity");

   

        m_TemporalSampling = serializedObject.FindProperty("TemporalSampling");
        m_DitherSpeed = serializedObject.FindProperty("DitherSpeed");
        m_LuminanceFeedback = serializedObject.FindProperty("LuminanceFeedback");
        m_MaxFeedback = serializedObject.FindProperty("MaxFeedback");


    m_ParticleDensitySupport = serializedObject.FindProperty("ParticleDensitySupport");
        m_densityResolution = serializedObject.FindProperty("densityResolution");
        m_densityDistance = serializedObject.FindProperty("densityDistance");
        m_TransparencySupport = serializedObject.FindProperty("TransparencySupport");
        m_transparencyDistance = serializedObject.FindProperty("transparencyDistance");
        m_BlurTransparency = serializedObject.FindProperty("BlurTransparency");



        m_blurCount = serializedObject.FindProperty("blurCount");
        m_BlurDepthFalloff = serializedObject.FindProperty("BlurDepthFalloff");
        m_DownsampledBlurDepthFalloff = serializedObject.FindProperty("DownsampledBlurDepthFalloff");
        m_UpSampledblurCount = serializedObject.FindProperty("UpSampledblurCount");


        m_DepthThreshold = serializedObject.FindProperty("DepthThreshold");
        m_GaussianWeights = serializedObject.FindProperty("GaussianWeights");
        m_RemoveColorBanding = serializedObject.FindProperty("RemoveColorBanding");
        //m_WarmUpShaders = serializedObject.FindProperty("WarmUpShaders");
        
    }

  
    public static int ExtraRenderFrames = 0;

    public override void OnInspectorGUI()
    {
        if (ExtraRenderFrames > 0 && !Application.isPlaying)
        {
            ExtraRenderFrames -= 1;

            if (ExtraRenderFrames > 2)
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
        else
        {
            if (ExtraRenderFrames <= 0)
            {

                ExtraRenderFrames = 70;
            }
        }

        if (toggles == null)
        {      
            toggles = new GUIStyle(EditorStyles.layerMaskField);
            toggles.fontStyle = FontStyle.Bold;
            toggles.alignment = TextAnchor.MiddleCenter;
            toggles.fixedHeight += 5;
            toggles.fontSize = 12;
        }
        HxVolumetricCamera targetCamera = (HxVolumetricCamera)target;

        Camera cam = targetCamera.GetCamera();
    
        EditorGUILayout.Space();

       



        if (GUILayout.Button("Light Scattering", toggles))
        {
            m_ShowLighting.target = !m_ShowLighting.target;
        }


        if (EditorGUILayout.BeginFadeGroup(m_ShowLighting.faded))
        {
    
            EditorGUILayout.PropertyField(m_Density);
            EditorGUILayout.PropertyField(m_MieScattering);
            EditorGUILayout.PropertyField(m_Extinction);
            EditorGUILayout.PropertyField(m_ExtinctionEffect);
            EditorGUILayout.PropertyField(m_SunSize);
            EditorGUILayout.PropertyField(m_SunBleed);
            //EditorGUILayout.PropertyField(m_LightFalloff);           
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

        if (GUILayout.Button("Ambient & Tint", toggles))
        {
            m_ShowAmbient.target = !m_ShowAmbient.target;
        }


        if (EditorGUILayout.BeginFadeGroup(m_ShowAmbient.faded))
        {
            EditorGUILayout.PropertyField(m_AmbientLightingStrength);
            EditorGUILayout.PropertyField(m_AmbientMode);
            if (m_AmbientMode.hasMultipleDifferentValues || targetCamera.Ambient != HxVolumetricCamera.HxAmbientMode.UseRenderSettings)
            {
                if (targetCamera.Ambient == HxVolumetricCamera.HxAmbientMode.Gradient)
                {
                    EditorGUILayout.PropertyField(m_AmbientSky);
                    EditorGUILayout.PropertyField(m_AmbientEquator);
                    EditorGUILayout.PropertyField(m_AmbientGround);
                }
                else
                {
                    EditorGUILayout.PropertyField(m_AmbientSky);
                }
                EditorGUILayout.PropertyField(m_AmbientIntesity);
            }
            EditorGUILayout.PropertyField(m_TintMode);
            if (targetCamera.TintMode != HxVolumetricCamera.HxTintMode.Off)
            {
                EditorGUILayout.PropertyField(m_TintColor);
                if (targetCamera.TintMode == HxVolumetricCamera.HxTintMode.Gradient)
                {
                    EditorGUILayout.PropertyField(m_TintColor2);

                }

                if (targetCamera.TintMode == HxVolumetricCamera.HxTintMode.Gradient || targetCamera.TintMode == HxVolumetricCamera.HxTintMode.Edge)
                {
                    EditorGUILayout.PropertyField(m_TintGradient);
                }
                EditorGUILayout.PropertyField(m_TintIntensity);
            }
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

        if (GUILayout.Button("Fog Height", toggles))
        {
            m_ShowFog.target = !m_ShowFog.target;
        }
        if (EditorGUILayout.BeginFadeGroup(m_ShowFog.faded))
        {

            EditorGUILayout.PropertyField(m_FogHeightEnabled);
            EditorGUILayout.PropertyField(m_FogHeight);
            EditorGUILayout.PropertyField(m_FogTransitionSize);
            EditorGUILayout.PropertyField(m_AboveFogPercent);

        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();


        if (GUILayout.Button("Noise", toggles))
        {
            m_ShowNoise.target = !m_ShowNoise.target;
        }
        if (EditorGUILayout.BeginFadeGroup(m_ShowNoise.faded))
        {
  
            EditorGUILayout.PropertyField(m_NoiseEnabled);
            EditorGUILayout.PropertyField(m_NoiseTexture);
            EditorGUILayout.PropertyField(m_NoiseContrast); 
            EditorGUILayout.PropertyField(m_NoiseScale);
            EditorGUILayout.PropertyField(m_NoiseVelocity);
 
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

   
        if (GUILayout.Button("Particle Density", toggles))
        {
            m_ShowDensity.target = !m_ShowDensity.target;
        }

        if (EditorGUILayout.BeginFadeGroup(m_ShowDensity.faded))
        {
            EditorGUILayout.LabelField(new GUIContent("Quality", "To change quality, modify the density quality in Plugins/HxVolumetricLighting/Resources/HxUsedShaders and hit build shaders"), new GUIContent("" + HxVolumetricCamera.DensityBufferDepth, "To change quality, modify the density quality in Plugins/HxVolumetricLighting/Resources/HxUsedShaders and hit build shaders"));
            EditorGUILayout.PropertyField(m_ParticleDensitySupport);
            EditorGUILayout.PropertyField(m_densityResolution);
            EditorGUILayout.PropertyField(m_densityDistance);
            
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();


        if (GUILayout.Button("Transparency", toggles))
        {
            m_ShowTransparency.target = !m_ShowTransparency.target;
        }
        if (EditorGUILayout.BeginFadeGroup(m_ShowTransparency.faded))
        {

            EditorGUILayout.LabelField(new GUIContent("Quality", "To change quality, modify the transparency quality in Plugins/HxVolumetricLighting/Resources/HxUsedShaders and hit build shaders"), new GUIContent("" + HxVolumetricCamera.TransparencyBufferDepth, "To change quality, modify the transparency quality in Plugins/HxVolumetricLighting/Resources/HxUsedShaders and hit build shaders"));

            EditorGUILayout.PropertyField(m_TransparencySupport);
            EditorGUILayout.PropertyField(m_transparencyDistance);
            EditorGUILayout.PropertyField(m_BlurTransparency);

            
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

        if (GUILayout.Button("General Settings", toggles))
        {
            m_ShowGeneral.target = !m_ShowGeneral.target;
        }



        if (EditorGUILayout.BeginFadeGroup(m_ShowGeneral.faded))
        {


            EditorGUILayout.PropertyField(m_resolution);
            EditorGUILayout.PropertyField(m_SampleCount);
            EditorGUILayout.PropertyField(m_DirectionalSampleCount);
            EditorGUILayout.PropertyField(m_MaxDirectionalRayDistance);
            EditorGUILayout.PropertyField(m_MaxLightDistance);
            

            if (targetCamera.resolution == HxVolumetricCamera.Resolution.full)
            {
                EditorGUILayout.HelpBox("Setting the resolution to half will give you a 4X speed increase!", MessageType.Info);
            }
            else
            if (targetCamera.resolution == HxVolumetricCamera.Resolution.half)
            {
                EditorGUILayout.HelpBox("Setting the resolution to Quarter will give you a 4X speed increase, although it can be artifact heavy", MessageType.Info);
            }

        }
        string Warning = "";
        MessageType mt = MessageType.None;
        EditorGUILayout.EndFadeGroup();
        if (cam != null)
        {

                GUIStyle s = new GUIStyle(EditorStyles.label);
                s.border = new RectOffset(10, 10, 10, 10);
                s.normal.textColor = Color.yellow;
                if (targetCamera.MapToLDR)
                {
                    Warning += "Disable Map to LDR in Advanced settings if you USE tone mapping!";
                    mt = MessageType.Warning;
                    //GUILayout.Label("Disable this if you USE tone mapping!", s);
                }
                else
                {
                    Warning += "Enable Map to LDR in Advanced settings if you do NOT use tone mapping!";
                    mt = MessageType.Warning;

                }
       
         
        }



  

        EditorGUILayout.Space();

        if (GUILayout.Button("Advanced Settings", toggles))
        {
            m_ShowAdvanced.target = !m_ShowAdvanced.target;
        }
        if (EditorGUILayout.BeginFadeGroup(m_ShowAdvanced.faded))
        {          
            EditorGUILayout.PropertyField(m_blurCount);
            EditorGUILayout.PropertyField(m_BlurDepthFalloff);
            EditorGUILayout.PropertyField(m_DownsampledBlurDepthFalloff);
            EditorGUILayout.PropertyField(m_UpSampledblurCount);
            EditorGUILayout.PropertyField(m_DepthThreshold);
            EditorGUILayout.PropertyField(m_GaussianWeights);
            EditorGUILayout.PropertyField(m_MapToLDR);
            EditorGUILayout.PropertyField(m_RemoveColorBanding);
            GUI.enabled = !m_TransparencySupport.boolValue;
            EditorGUILayout.PropertyField(m_RenderOrder);
            EditorGUILayout.PropertyField(m_ShadowFix);
            GUI.enabled = true;
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();
        if (GUILayout.Button("Temporal Settings", toggles))
        {
            m_ShowTemporal.target = !m_ShowTemporal.target;
        }
        if (EditorGUILayout.BeginFadeGroup(m_ShowTemporal.faded))
        {
            EditorGUILayout.PropertyField(m_TemporalSampling);
            EditorGUILayout.PropertyField(m_DitherSpeed);
            EditorGUILayout.PropertyField(m_LuminanceFeedback);
            EditorGUILayout.PropertyField(m_MaxFeedback);
        }
        EditorGUILayout.EndFadeGroup();

        if (mt != MessageType.None)
        {
            EditorGUILayout.HelpBox(Warning, mt);
        }
        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();
       // base.DrawDefaultInspector();
    }

    
}
#endif
