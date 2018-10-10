#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditor.AnimatedValues;
using UnityEditor.Callbacks;
// Custom Editor using SerializedProperties.
// Automatic handling of multi-object editing, undo, and prefab overrides.

[CustomEditor(typeof(HxVolumetricLight))]
[CanEditMultipleObjects]

public class HxVolumetricLightEditor : Editor  
{

    [MenuItem("Help/HxVolumetric Documentation")]
    private static void OpenHelp()
    {
        Application.OpenURL("http://hitboxteam.com/HxVolumetricLighting/#documentation");
    }

    [MenuItem("GameObject/Light/Volumetric Directional")]
    private static void createDirectional()
    {
        GameObject t = new GameObject("Volumetric Directional Light");
        Light l = t.AddComponent<Light>();
        l.type = LightType.Directional;
        t.AddComponent<HxVolumetricLight>();
        Selection.activeGameObject = t;

    }

    [MenuItem("GameObject/Light/Volumetric Point")]
    private static void createPoint()
    {
        GameObject t = new GameObject("Volumetric Point Light");
        Light l = t.AddComponent<Light>();
        l.type = LightType.Point;
        t.AddComponent<HxVolumetricLight>();
        Selection.activeGameObject = t;
    }

    [MenuItem("GameObject/Light/Volumetric Spot")]
    private static void createSpot()
    {
        GameObject t = new GameObject("Volumetric Spot Light");
        Light l = t.AddComponent<Light>();
        l.type = LightType.Spot;
        t.AddComponent<HxVolumetricLight>();
        Selection.activeGameObject = t;
    }


    [MenuItem("GameObject/Light/Volumetric Density Area")]
    private static void createDensityArea()
    {
        GameObject t = new GameObject("Volumetric Density Area");

        t.AddComponent<HxDensityVolume>();
        Selection.activeGameObject = t;

    }

    SerializedProperty LightFalloff;
    SerializedProperty NearPlane;
    SerializedProperty CustomNoiseContrast;
    SerializedProperty CustomMieScatter;
    SerializedProperty CustomExtinction;
    SerializedProperty CustomDensity;
    SerializedProperty CustomSampleCount;
    SerializedProperty CustomColor;
    SerializedProperty CustomNoiseEnabled;
    SerializedProperty CustomNoiseScale;
    SerializedProperty CustomNoiseVelocity;


    SerializedProperty CustomSunSize;
    SerializedProperty CustomSunBleed;
    SerializedProperty CustomIntensity;
    SerializedProperty CustomStrength;
    SerializedProperty CustomNoiseTexture;
    SerializedProperty CustomTintMode;
    SerializedProperty CustomTintColor;
    SerializedProperty CustomTintColor2;
    SerializedProperty CustomTintIntensity;
    SerializedProperty CustomTintGradient;


    SerializedProperty CustomFogHeightEnabled;
    SerializedProperty CustomFogHeight;
    SerializedProperty CustomFogTransitionSize;
    SerializedProperty CustomAboveFogPercent;

    SerializedProperty FogHeightEnabled;
    SerializedProperty FogHeight;
    SerializedProperty FogTransitionSize;
    SerializedProperty AboveFogPercent;

    SerializedProperty NoiseContrast;
    SerializedProperty noiseTexture;
    SerializedProperty Color;
    SerializedProperty MieScattering;
    SerializedProperty Extinction;
    SerializedProperty Density;
    SerializedProperty ExtraDensity;
    SerializedProperty SampleCount;
    SerializedProperty NoiseEnabled;
    SerializedProperty NoiseScale;
    SerializedProperty NoiseVelocity;
    SerializedProperty Shadows;
    SerializedProperty SunSize;
    SerializedProperty SunBleed;

    SerializedProperty Intensity;
    SerializedProperty Strength;

    SerializedProperty TintMode;
    SerializedProperty TintColor;
    SerializedProperty TintColor2;
    SerializedProperty TintIntensity;
    SerializedProperty TintGradient;
    SerializedProperty MaxLightDistance;
    SerializedProperty CustomMaxLightDistance;
    AnimBool m_ShowLightSettings;
    AnimBool m_ShowAmbient;
    AnimBool m_ShowGeneral;
    AnimBool m_ShowLighting;
    AnimBool m_ShowFog;
    AnimBool m_ShowNoise;
    AnimBool m_ShowDensity;
    AnimBool m_ShowTransparency;
    AnimBool m_ShowAdvanced;
    static GUIStyle toggles;
    public static int ExtraRenderFrames = 0;


    static public bool ShowCustom = true;

    void OnEnable()
    {
        // Setup the SerializedProperties.
        LightFalloff = serializedObject.FindProperty("LightFalloff");
        NearPlane = serializedObject.FindProperty("NearPlane");
        NoiseContrast = serializedObject.FindProperty("NoiseContrast");
        CustomNoiseContrast = serializedObject.FindProperty("CustomNoiseContrast");

        MaxLightDistance = serializedObject.FindProperty("MaxLightDistance");
        CustomMaxLightDistance = serializedObject.FindProperty("CustomMaxLightDistance");
        CustomMieScatter = serializedObject.FindProperty("CustomMieScatter");
        CustomExtinction = serializedObject.FindProperty("CustomExtinction");
        CustomDensity = serializedObject.FindProperty("CustomDensity");
        CustomSampleCount = serializedObject.FindProperty("CustomSampleCount");
        CustomIntensity = serializedObject.FindProperty("CustomIntensity");
        CustomColor = serializedObject.FindProperty("CustomColor");
        CustomNoiseEnabled = serializedObject.FindProperty("CustomNoiseEnabled");
        CustomNoiseScale = serializedObject.FindProperty("CustomNoiseScale");
        CustomNoiseVelocity = serializedObject.FindProperty("CustomNoiseVelocity");

        CustomSunSize = serializedObject.FindProperty("CustomSunSize");
        CustomSunBleed = serializedObject.FindProperty("CustomSunBleed");
        CustomStrength = serializedObject.FindProperty("CustomStrength");


        CustomFogHeightEnabled = serializedObject.FindProperty("CustomFogHeightEnabled");
        CustomFogHeight = serializedObject.FindProperty("CustomFogHeight");
        CustomFogTransitionSize = serializedObject.FindProperty("CustomFogTransitionSize");
        CustomAboveFogPercent = serializedObject.FindProperty("CustomAboveFogPercent");

        FogHeightEnabled = serializedObject.FindProperty("FogHeightEnabled");
        FogHeight = serializedObject.FindProperty("FogHeight");
        FogTransitionSize = serializedObject.FindProperty("FogTransitionSize");
        AboveFogPercent = serializedObject.FindProperty("AboveFogPercent");

        NoiseEnabled = serializedObject.FindProperty("NoiseEnabled");
        NoiseScale = serializedObject.FindProperty("NoiseScale");
        NoiseVelocity = serializedObject.FindProperty("NoiseVelocity");
        CustomNoiseTexture = serializedObject.FindProperty("CustomNoiseTexture");



        noiseTexture = serializedObject.FindProperty("NoiseTexture3D");
        Color = serializedObject.FindProperty("Color");
        MieScattering = serializedObject.FindProperty("MieScattering");
        Extinction = serializedObject.FindProperty("Extinction");
        Density = serializedObject.FindProperty("Density");
        ExtraDensity = serializedObject.FindProperty("ExtraDensity");
        SampleCount = serializedObject.FindProperty("SampleCount");
        Shadows = serializedObject.FindProperty("Shadows");
        Strength = serializedObject.FindProperty("Strength");
  
        SunSize = serializedObject.FindProperty("SunSize");
        SunBleed = serializedObject.FindProperty("SunBleed");

        Intensity = serializedObject.FindProperty("Intensity");

        CustomTintMode = serializedObject.FindProperty("CustomTintMode");
        CustomTintColor = serializedObject.FindProperty("CustomTintColor");
        CustomTintColor2 = serializedObject.FindProperty("CustomTintColor2");
        CustomTintIntensity = serializedObject.FindProperty("CustomTintIntensity");
        CustomTintGradient = serializedObject.FindProperty("CustomTintGradient");

        TintMode = serializedObject.FindProperty("TintMode");
        TintColor = serializedObject.FindProperty("TintColor");
        TintColor2 = serializedObject.FindProperty("TintColor2");
        TintIntensity = serializedObject.FindProperty("TintIntensity");
        TintGradient = serializedObject.FindProperty("TintGradient");

        if (m_ShowLightSettings == null)
        {

            m_ShowLightSettings = new AnimBool(EditorPrefs.GetBool("Hxc_ShowLightSettings", false));
            m_ShowLightSettings.valueChanged.AddListener(Repaint);
        }

        if (m_ShowAmbient == null)
        {
            m_ShowAmbient = new AnimBool(EditorPrefs.GetBool("Hxc_ShowAmbient", false));
            m_ShowAmbient.valueChanged.AddListener(Repaint);
        }

        if (m_ShowGeneral == null)
        {
            m_ShowGeneral = new AnimBool(EditorPrefs.GetBool("Hxc_ShowGeneral", false));
            m_ShowGeneral.valueChanged.AddListener(Repaint);
        }

        if (m_ShowLighting == null)
        {
            m_ShowLighting = new AnimBool(EditorPrefs.GetBool("Hxc_ShowLighting", false));
            m_ShowLighting.valueChanged.AddListener(Repaint);
        }

        if (m_ShowFog == null)
        {
            m_ShowFog = new AnimBool(EditorPrefs.GetBool("Hxc_ShowFog", false));
            m_ShowFog.valueChanged.AddListener(Repaint);
        }


        if (m_ShowNoise == null)
        {
            m_ShowNoise = new AnimBool(EditorPrefs.GetBool("Hxc_ShowNoise", false));
            m_ShowNoise.valueChanged.AddListener(Repaint);
        }

        if (m_ShowDensity == null)
        {
            m_ShowDensity = new AnimBool(EditorPrefs.GetBool("Hxc_ShowDensity", false));
            m_ShowDensity.valueChanged.AddListener(Repaint);
        }

        if (m_ShowTransparency == null)
        {
            m_ShowTransparency = new AnimBool(EditorPrefs.GetBool("Hxc_ShowTransparency", false));
            m_ShowTransparency.valueChanged.AddListener(Repaint);
        }

        if (m_ShowAdvanced == null)
        {
            m_ShowAdvanced = new AnimBool(EditorPrefs.GetBool("Hxc_ShowAdvanced", false));
            m_ShowAdvanced.valueChanged.AddListener(Repaint);
        }

    }
    
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

        serializedObject.Update();

        EditorGUILayout.HelpBox("You can override the settings for this light by toggling the checkmark next to each variables, It will use that value instead of the one set on the camera.", MessageType.Info);


        if (GUILayout.Button("Light Settings", toggles))
        {
            m_ShowLightSettings.target = !m_ShowLightSettings.target;
        }

        if (EditorGUILayout.BeginFadeGroup(m_ShowLightSettings.faded))
        {
            EditorGUILayout.PropertyField(ExtraDensity);
            EditorGUILayout.PropertyField(LightFalloff);
            
            CustomField(CustomColor, Color);
            CustomField(CustomIntensity, Intensity);
            EditorGUILayout.PropertyField(Shadows);
            CustomField(CustomStrength, Strength);
            EditorGUILayout.PropertyField(NearPlane); 
        }

        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();


        if (GUILayout.Button("Light Scattering", toggles))
        {
            m_ShowLighting.target = !m_ShowLighting.target;
        }

        if (EditorGUILayout.BeginFadeGroup(m_ShowLighting.faded))
        {
            CustomField(CustomDensity, Density);
            CustomField(CustomMieScatter, MieScattering);
            CustomField(CustomExtinction, Extinction);
            CustomField(CustomSunSize, SunSize);
            CustomField(CustomSunBleed, SunBleed);

            EditorGUILayout.HelpBox("Although tempting, Modifying Density and Extintion per light can result in an inaccurate result", MessageType.Info);
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

        if (GUILayout.Button("Tint", toggles))
        {
            m_ShowAmbient.target = !m_ShowAmbient.target;
        }

        if (EditorGUILayout.BeginFadeGroup(m_ShowAmbient.faded))
        {
            CustomField(CustomTintMode, TintMode);
            CustomField(CustomTintColor, TintColor);
            CustomField(CustomTintColor2, TintColor2);
            CustomField(CustomTintGradient, TintGradient);
            CustomField(CustomTintIntensity, TintIntensity);
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

        if (GUILayout.Button("Fog Height", toggles))
        {
            m_ShowFog.target = !m_ShowFog.target;
        }

        if (EditorGUILayout.BeginFadeGroup(m_ShowFog.faded))
        {
            CustomField(CustomFogHeightEnabled, FogHeightEnabled);
            CustomField(CustomFogHeight, FogHeight);
            CustomField(CustomFogTransitionSize, FogTransitionSize);
            CustomField(CustomAboveFogPercent, AboveFogPercent);
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

        if (GUILayout.Button("Noise", toggles))
        {
            m_ShowNoise.target = !m_ShowNoise.target;
        }

        if (EditorGUILayout.BeginFadeGroup(m_ShowNoise.faded))
        {
            CustomField(CustomNoiseEnabled, NoiseEnabled);
            CustomField(CustomNoiseTexture, noiseTexture);
            CustomField(CustomNoiseContrast, NoiseContrast);
            CustomField(CustomNoiseScale, NoiseScale);
            CustomField(CustomNoiseVelocity, NoiseVelocity);

        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

        if (GUILayout.Button("General Settings", toggles))
        {
            m_ShowGeneral.target = !m_ShowGeneral.target;
        }

        if (EditorGUILayout.BeginFadeGroup(m_ShowGeneral.faded))
        {
            CustomField(CustomSampleCount, SampleCount);
            CustomField(CustomMaxLightDistance, MaxLightDistance);
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();

       

        serializedObject.ApplyModifiedProperties();
       
    }

    void OnLostFocus()
    {
        EditorPrefs.SetBool("Hxc_ShowLightSettings", m_ShowLightSettings.target);
        EditorPrefs.SetBool("Hxc_ShowAmbient", m_ShowAmbient.target);
        EditorPrefs.SetBool("Hxc_ShowGeneral", m_ShowGeneral.target);
        EditorPrefs.SetBool("Hxc_ShowLighting", m_ShowLighting.target);
        EditorPrefs.SetBool("Hxc_ShowFog", m_ShowFog.target);
        EditorPrefs.SetBool("Hxc_ShowNoise", m_ShowNoise.target);
        EditorPrefs.SetBool("Hxc_ShowDensity", m_ShowDensity.target);
        EditorPrefs.SetBool("Hxc_ShowTransparency", m_ShowTransparency.target);
        EditorPrefs.SetBool("Hxc_ShowAdvanced", m_ShowAdvanced.target);

    }

    void OnDestroy()
    {
        EditorPrefs.SetBool("Hxc_ShowLightSettings", m_ShowLightSettings.target);
        EditorPrefs.SetBool("Hxc_ShowAmbient", m_ShowAmbient.target);
        EditorPrefs.SetBool("Hxc_ShowGeneral", m_ShowGeneral.target);
        EditorPrefs.SetBool("Hxc_ShowLighting", m_ShowLighting.target);
        EditorPrefs.SetBool("Hxc_ShowFog", m_ShowFog.target);
        EditorPrefs.SetBool("Hxc_ShowNoise", m_ShowNoise.target);
        EditorPrefs.SetBool("Hxc_ShowDensity", m_ShowDensity.target);
        EditorPrefs.SetBool("Hxc_ShowTransparency", m_ShowTransparency.target);
        EditorPrefs.SetBool("Hxc_ShowAdvanced", m_ShowAdvanced.target);
    }

    void FogField(AnimBool anim, SerializedProperty ct, SerializedProperty f1, SerializedProperty f2, SerializedProperty f3,string name)
    {
        bool ShowCustom = false;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(ct, new GUIContent(name));
        if (EditorGUI.EndChangeCheck() || !ct.hasMultipleDifferentValues)
        {
            ShowCustom = ct.boolValue;

        }

        anim.target = ct.boolValue;

        if (ShowCustom)
        {
            if (EditorGUILayout.BeginFadeGroup(anim.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(f1);
                EditorGUILayout.PropertyField(f2);
                EditorGUILayout.PropertyField(f3);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }


    }

    void Vector3Field(AnimBool anim, SerializedProperty ct, SerializedProperty f, string name, string tooltip)
    {
        bool ShowCustom = false;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(ct, new GUIContent(name));
        if (EditorGUI.EndChangeCheck() || !ct.hasMultipleDifferentValues)
        {
            ShowCustom = ct.boolValue;

        }

        anim.target = ct.boolValue;

        if (ShowCustom)
        {
            if (EditorGUILayout.BeginFadeGroup(anim.faded))
            {
                EditorGUI.indentLevel++;
                //EditorGUI.BeginChangeCheck();
                //Vector3 v = EditorGUILayout.Vector3Field(new GUIContent((f.hasMultipleDifferentValues ? "Variation" : " ")), f.vector3Value);
                //if (EditorGUI.EndChangeCheck()) { f.vector3Value = v; }
                EditorGUILayout.PropertyField(f, new GUIContent(" "));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }


    }

    void CustomField(SerializedProperty ct, SerializedProperty f)
    {

        EditorGUILayout.BeginHorizontal();
        ct.boolValue = EditorGUILayout.Toggle(ct.boolValue, GUILayout.Width(20));
        GUI.enabled = ct.boolValue;
        EditorGUILayout.PropertyField(f);
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;



        // bool ShowCustom = false;
        // EditorGUI.BeginChangeCheck();
        // EditorGUILayout.PropertyField(ct, new GUIContent(name));
        // if (EditorGUI.EndChangeCheck() || !ct.hasMultipleDifferentValues)
        // {
        //     ShowCustom = ct.boolValue;
        //
        // }
        //
        // anim.target = ct.boolValue;
        //
        // if (ShowCustom)
        // {
        //     if (EditorGUILayout.BeginFadeGroup(anim.faded))
        //     {
        //         EditorGUI.indentLevel++;
        //         //EditorGUI.BeginChangeCheck();
        //         //float v = EditorGUILayout.Slider(new GUIContent((f.hasMultipleDifferentValues ? "Variation" : " ")), f.floatValue, min, max);
        //         //if (EditorGUI.EndChangeCheck()) { f.floatValue = v; }
        //         EditorGUILayout.PropertyField(f, new GUIContent(" "));
        //         EditorGUI.indentLevel--;
        //     }
        //     EditorGUILayout.EndFadeGroup();
        // }
        //
        //
    }

    void IntField(AnimBool anim, SerializedProperty ct, SerializedProperty f, string name, string tooltip, int min = 0, int max = 5)
    {
        bool ShowCustom = false;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(ct, new GUIContent(name));
        if (EditorGUI.EndChangeCheck() || !ct.hasMultipleDifferentValues)
        {
            ShowCustom = ct.boolValue;

        }

        anim.target = ct.boolValue;

        if (ShowCustom)
        {
            if (EditorGUILayout.BeginFadeGroup(anim.faded))
            {
                EditorGUI.indentLevel++;
                //EditorGUI.BeginChangeCheck();
                //int v = EditorGUILayout.IntSlider(new GUIContent((f.hasMultipleDifferentValues ? "Variation" : " ")), f.intValue, min, max);
                //if (EditorGUI.EndChangeCheck()) { f.intValue = v; }
                EditorGUILayout.PropertyField(f, new GUIContent(" "));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }


    }

    void BoolField(AnimBool anim, SerializedProperty ct, SerializedProperty f, string name, string tooltip)
    {
        bool ShowCustom = false;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(ct, new GUIContent(name));
        

        if (EditorGUI.EndChangeCheck() || !ct.hasMultipleDifferentValues)
        {
            ShowCustom = ct.boolValue;

        }

        anim.target = ct.boolValue;

        if (ShowCustom)
        {
            if (EditorGUILayout.BeginFadeGroup(anim.faded))
            {
                EditorGUI.indentLevel++;
                //EditorGUI.BeginChangeCheck();
                //bool v = EditorGUILayout.Toggle(new GUIContent((f.hasMultipleDifferentValues ? "Variation" : " ")), f.boolValue);
                //if (EditorGUI.EndChangeCheck()) { f.boolValue = v; }
                EditorGUILayout.PropertyField(f, new GUIContent("On/Off"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }


    }

    void ColorField(AnimBool anim, SerializedProperty ct, SerializedProperty f, string name, string tooltip)
    {
        bool ShowCustom = false;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(ct, new GUIContent(name));
        if (EditorGUI.EndChangeCheck() || !ct.hasMultipleDifferentValues)
        {
            ShowCustom = ct.boolValue;

        }

        anim.target = ct.boolValue;

        if (ShowCustom)
        {
            if (EditorGUILayout.BeginFadeGroup(anim.faded))
            {
                EditorGUI.indentLevel++;
                //EditorGUI.BeginChangeCheck();
                //Color v = EditorGUILayout.ColorField(new GUIContent((f.hasMultipleDifferentValues ? "Variation" : " ")), f.colorValue);
                //if (EditorGUI.EndChangeCheck()) { f.colorValue = v; }
                EditorGUILayout.PropertyField(f, new GUIContent(" "));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }


    }
}
#endif
