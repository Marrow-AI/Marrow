using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[CanEditMultipleObjects]
public class MinimalistStandardMatV2Free : MaterialEditor
{
    #region enums 
    public enum ShadingMode { VertexColor, SolidColor, Gradient_ProOnly }
    public enum GradientSettings { UseGlobalGradientSettings, DefineCustomGradientSettings }
    public enum GradientSpace { WorldSpace, LocalSpace }
    public enum AOuv { uv0, uv1 }
    public enum LightMapBlendingMode { Add, Multiply, UseAsAO }
    public enum HandleProfile { frontGradient, frontRotation }
    #endregion

    #region Constants
    const string FRONT = "Front";
    const string BACK = "Back";
    const string TOP = "Top";
    const string DOWN = "Down";
    const string LEFT = "Left";
    const string RIGHT = "Right";
    #endregion

    #region boleans
    bool showFrontShading;
    bool showBackShading;
    bool showLeftShading;
    bool showRightShading;
    bool showTopShading;
    bool showBottomShading;

    private bool ShowTexture
    {
        get
        {
            float x = _ShowTexture.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _ShowTexture.floatValue = 1;
            }
            else
            {
                _ShowTexture.floatValue = 0;
            }
        }
    }
    private bool ShowCustomShading
    {
        get
        {
            if (_ShowCustomShading.floatValue == 0) return false;
            else return true;
        }
        set
        {
            if (value == true) _ShowCustomShading.floatValue = 1;
            else _ShowCustomShading.floatValue = 0;
        }
    }
    private bool ShowOtherSettings
    {
        get
        {
            float x = _OtherSettings.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _OtherSettings.floatValue = 1;
            }
            else
            {
                _OtherSettings.floatValue = 0;
            }
        }
    }
    private bool ShowGlobalGradientSettings
    {
        get
        {
            float x = _ShowGlobalGradientSettings.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _ShowGlobalGradientSettings.floatValue = 1;
            }
            else
            {
                _ShowGlobalGradientSettings.floatValue = 0;
            }
        }
    }
    private bool ShowAmbientSettings
    {
        get
        {
            float x = _ShowAmbientSettings.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _ShowAmbientSettings.floatValue = 1;
            }
            else
            {
                _ShowAmbientSettings.floatValue = 0;
            }
        }
    }
    private bool realtimeShadow
    {
        get
        {
            float x = _RealtimeShadow.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _RealtimeShadow.floatValue = 1;
            }
            else
            {
                _RealtimeShadow.floatValue = 0;
            }
        }
    }
    private bool dontMix
    {
        get
        {
            float x = _DontMix.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _DontMix.floatValue = 1;
            }
            else
            {
                _DontMix.floatValue = 0;
            }
        }
    }
    private bool ShowAOSettings
    {
        get
        {
            float x = _ShowAO.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _ShowAO.floatValue = 1;
            }
            else
            {
                _ShowAO.floatValue = 0;
            }
        }
    }
    private bool EnableAO
    {
        get
        {
            float x = _AOEnable.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _AOEnable.floatValue = 1;
            }
            else
            {
                _AOEnable.floatValue = 0;
            }
        }
    }
    private bool ShowlMapSettings
    {
        get
        {
            float x = _ShowLMap.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _ShowLMap.floatValue = 1;
            }
            else
            {
                _ShowLMap.floatValue = 0;
            }
        }
    }
    private bool EnableLmap
    {
        get
        {
            float x = _LmapEnable.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _LmapEnable.floatValue = 1;
            }
            else
            {
                _LmapEnable.floatValue = 0;
            }
        }
    }
    private bool ShowFogSettigns
    {
        get
        {
            float x = _ShowFog.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _ShowFog.floatValue = 1;
            }
            else
            {
                _ShowFog.floatValue = 0;
            }
        }
    }
    private bool EnableUnityFog
    {
        get
        {
            float x = _UnityFogEnable.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _UnityFogEnable.floatValue = 1;
            }
            else
            {
                _UnityFogEnable.floatValue = 0;
            }
        }
    }
    private bool EnableHFog
    {
        get
        {
            float x = _HFogEnable.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _HFogEnable.floatValue = 1;
            }
            else
            {
                _HFogEnable.floatValue = 0;
            }
        }
    }
    private bool ColorCorrectionEnable
    {
        get
        {
            float x = _ColorCorrectionEnable.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _ColorCorrectionEnable.floatValue = 1;
            }
            else
            {
                _ColorCorrectionEnable.floatValue = 0;
            }
        }
    }
    private bool RimEnable
    {
        get
        {
            float x = _RimEnable.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _RimEnable.floatValue = 1;
            }
            else
            {
                _RimEnable.floatValue = 0;
            }
        }
    }
    private bool ShowColorCorrectionSettings
    {
        get
        {
            float x = _ShowColorCorrection.floatValue;
            if (x == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        set
        {
            if (value == true)
            {
                _ShowColorCorrection.floatValue = 1;
            }
            else
            {
                _ShowColorCorrection.floatValue = 0;
            }
        }
    }

    #endregion

    #region enum props
    ShadingMode frontShadingMode;
    ShadingMode backShadingMode;
    ShadingMode leftShadingMode;
    ShadingMode rightShadingMode;
    ShadingMode topShadingMode;
    ShadingMode bottomShadingMode;

    GradientSpace gSpace
    {
        get
        {
            return (GradientSpace)_GradientSpace.floatValue;
        }
        set
        {
            if (value == GradientSpace.WorldSpace)
            {
                _GradientSpace.floatValue = 0;
            }
            else
            {
                _GradientSpace.floatValue = 1;
            }
        }
    }
    AOuv aouv
    {
        get
        {
            return (AOuv)_AOuv.floatValue;
        }
        set
        {
            if (value == AOuv.uv0)
            {
                _AOuv.floatValue = 0;
            }
            else
            {
                _AOuv.floatValue = 1;
            }
        }
    }
    LightMapBlendingMode lMapBlendMode
    {
        get
        {
            return (LightMapBlendingMode)_LmapBlendingMode.floatValue;
        }
        set
        {
            if (value == LightMapBlendingMode.Add)
            {
                _LmapBlendingMode.floatValue = 0;
            }
            else if (value == LightMapBlendingMode.Multiply)
            {
                _LmapBlendingMode.floatValue = 1;
            }
            else
            {
                _LmapBlendingMode.floatValue = 2;
            }
        }
    }
    #endregion

    #region gradientSettings
    GradientSettings frontGradientSettings;
    GradientSettings backGradientSettings;
    GradientSettings leftGradientSettings;
    GradientSettings rightGradientSettings;
    GradientSettings topGradientSettings;
    GradientSettings bottomGradientSettings;
    #endregion

    #region  ShaderProperties
    MaterialProperty _ShowTexture, _MainTexture, _MainTexturePower;

    MaterialProperty _ShowCustomShading;
    MaterialProperty _ShowFront, _Shading_F, _Color1_F, _Color2_F, _GradSettings_F, _GradientYStartPos_F, _GradientHeight_F, _Rotation_F, _GizmoPosition_F;
    MaterialProperty _ShowBack, _Shading_B, _Color1_B, _Color2_B, _GradSettings_B, _GradientYStartPos_B, _GradientHeight_B, _Rotation_B, _GizmoPosition_B;
    MaterialProperty _ShowLeft, _Shading_L, _Color1_L, _Color2_L, _GradSettings_L, _GradientYStartPos_L, _GradientHeight_L, _Rotation_L, _GizmoPosition_L;
    MaterialProperty _ShowRight, _Shading_R, _Color1_R, _Color2_R, _GradSettings_R, _GradientYStartPos_R, _GradientHeight_R, _Rotation_R, _GizmoPosition_R;
    MaterialProperty _ShowTop, _Shading_T, _Color1_T, _Color2_T, _GradSettings_T, _GradientXStartPos_T, _GradientHeight_T, _Rotation_T, _GizmoPosition_T;
    MaterialProperty _ShowBottom, _Shading_D, _Color1_D, _Color2_D, _GradSettings_D, _GradientXStartPos_D, _GradientHeight_D, _Rotation_D, _GizmoPosition_D;

    MaterialProperty _ShowAO, _AOEnable, _AOTexture, _AOColor, _AOPower, _AOuv;
    MaterialProperty _ShowLMap, _LmapEnable, _LmapBlendingMode, _LMColor, _LMPower;
    MaterialProperty _ShowFog, _UnityFogEnable, _HFogEnable, _Color_Fog, _FogYStartPos, _FogHeight;
    MaterialProperty _ShowColorCorrection, _ColorCorrectionEnable, _RimEnable, _RimColor, _RimPower, _TintColor, _Saturation, _Brightness;
    MaterialProperty _ShowGlobalGradientSettings, _GradientHeight_G, _GradPivot_G, _Rotation_G, _OtherSettings;
    MaterialProperty _ShowAmbientSettings, _AmbientColor, _AmbientPower;
    MaterialProperty _RealtimeShadow, _ShadowColor;
    MaterialProperty _GradientSpace, _DontMix;
    #endregion

    Dictionary<string, MaterialProperty> RecorderProps;

    #region GUIStyles
    GUIStyle Indented;
    GUIStyle HeaderStyle;
    #endregion

    Material targetMat;
    GradientHandle gradientHandle;
    Transform selectedObject;
    Transform top, bottom;
    bool drawGradintGizmo, drawRotationGizmo, deawFogGizmo;
    bool ortho;
    Event guiEvent;

    public override void OnEnable()
    {
        base.OnEnable();
        targetMat = target as Material;

        InitializeMatProps();
        InitializeHelperVars();
        InitializeGUIStyles();
        Repaint();
    }
    public override void UndoRedoPerformed()
    {
        base.UndoRedoPerformed();
        InitializeMatProps();
        InitializeHelperVars();
        Repaint();
    }

    private void InitializeMatProps()
    {
        _ShowTexture = GetMaterialProperty(targets, "_ShowTexture");
        _MainTexture = GetMaterialProperty(targets, "_MainTexture");
        _MainTexturePower = GetMaterialProperty(targets, "_MainTexturePower");

        _ShowCustomShading = GetMaterialProperty(targets, "_ShowCustomShading");

        _ShowFront = GetMaterialProperty(targets, "_ShowFront");
        _Shading_F = GetMaterialProperty(targets, "_Shading_F");
        _Color1_F = GetMaterialProperty(targets, "_Color1_F");
        _Color2_F = GetMaterialProperty(targets, "_Color2_F");
        _GradSettings_F = GetMaterialProperty(targets, "_GradSettings_F");
        _GradientYStartPos_F = GetMaterialProperty(targets, "_GradientYStartPos_F");
        _GradientHeight_F = GetMaterialProperty(targets, "_GradientHeight_F");
        _Rotation_F = GetMaterialProperty(targets, "_Rotation_F");
        _GizmoPosition_F = GetMaterialProperty(targets, "_GizmoPosition_F");

        _ShowBack = GetMaterialProperty(targets, "_ShowBack");
        _Shading_B = GetMaterialProperty(targets, "_Shading_B");
        _Color1_B = GetMaterialProperty(targets, "_Color1_B");
        _Color2_B = GetMaterialProperty(targets, "_Color2_B");
        _GradSettings_B = GetMaterialProperty(targets, "_GradSettings_B");
        _GradientYStartPos_B = GetMaterialProperty(targets, "_GradientYStartPos_B");
        _GradientHeight_B = GetMaterialProperty(targets, "_GradientHeight_B");
        _Rotation_B = GetMaterialProperty(targets, "_Rotation_B");
        _GizmoPosition_B = GetMaterialProperty(targets, "_GizmoPosition_B");

        _ShowLeft = GetMaterialProperty(targets, "_ShowLeft");
        _Shading_L = GetMaterialProperty(targets, "_Shading_L");
        _Color1_L = GetMaterialProperty(targets, "_Color1_L");
        _Color2_L = GetMaterialProperty(targets, "_Color2_L");
        _GradSettings_L = GetMaterialProperty(targets, "_GradSettings_L");
        _GradientYStartPos_L = GetMaterialProperty(targets, "_GradientYStartPos_L");
        _GradientHeight_L = GetMaterialProperty(targets, "_GradientHeight_L");
        _Rotation_L = GetMaterialProperty(targets, "_Rotation_L");
        _GizmoPosition_L = GetMaterialProperty(targets, "_GizmoPosition_L");

        _ShowRight = GetMaterialProperty(targets, "_ShowRight");
        _Shading_R = GetMaterialProperty(targets, "_Shading_R");
        _Color1_R = GetMaterialProperty(targets, "_Color1_R");
        _Color2_R = GetMaterialProperty(targets, "_Color2_R");
        _GradSettings_R = GetMaterialProperty(targets, "_GradSettings_R");
        _GradientYStartPos_R = GetMaterialProperty(targets, "_GradientYStartPos_R");
        _GradientHeight_R = GetMaterialProperty(targets, "_GradientHeight_R");
        _Rotation_R = GetMaterialProperty(targets, "_Rotation_R");
        _GizmoPosition_R = GetMaterialProperty(targets, "_GizmoPosition_R");


        _ShowTop = GetMaterialProperty(targets, "_ShowTop");
        _Shading_T = GetMaterialProperty(targets, "_Shading_T");
        _Color1_T = GetMaterialProperty(targets, "_Color1_T");
        _Color2_T = GetMaterialProperty(targets, "_Color2_T");
        _GradSettings_T = GetMaterialProperty(targets, "_GradSettings_T");
        _GradientXStartPos_T = GetMaterialProperty(targets, "_GradientXStartPos_T");
        _GradientHeight_T = GetMaterialProperty(targets, "_GradientHeight_T");
        _Rotation_T = GetMaterialProperty(targets, "_Rotation_T");
        _GizmoPosition_T = GetMaterialProperty(targets, "_GizmoPosition_T");

        _ShowBottom = GetMaterialProperty(targets, "_ShowBottom");
        _Shading_D = GetMaterialProperty(targets, "_Shading_D");
        _Color1_D = GetMaterialProperty(targets, "_Color1_D");
        _Color2_D = GetMaterialProperty(targets, "_Color2_D");
        _GradSettings_D = GetMaterialProperty(targets, "_GradSettings_D");
        _GradientXStartPos_D = GetMaterialProperty(targets, "_GradientXStartPos_D");
        _GradientHeight_D = GetMaterialProperty(targets, "_GradientHeight_D");
        _Rotation_D = GetMaterialProperty(targets, "_Rotation_D");
        _GizmoPosition_D = GetMaterialProperty(targets, "_GizmoPosition_D");

        _ShowAO = GetMaterialProperty(targets, "_ShowAO");
        _AOEnable = GetMaterialProperty(targets, "_AOEnable");
        _AOTexture = GetMaterialProperty(targets, "_AOTexture");
        _AOPower = GetMaterialProperty(targets, "_AOPower");
        _AOColor = GetMaterialProperty(targets, "_AOColor");
        _AOuv = GetMaterialProperty(targets, "_AOuv");

        _ShowLMap = GetMaterialProperty(targets, "_ShowLMap");
        _LmapEnable = GetMaterialProperty(targets, "_LmapEnable");
        _LmapBlendingMode = GetMaterialProperty(targets, "_LmapBlendingMode");
        _LMColor = GetMaterialProperty(targets, "_LMColor");
        _LMPower = GetMaterialProperty(targets, "_LMPower");

        _ShowFog = GetMaterialProperty(targets, "_ShowFog");
        _UnityFogEnable = GetMaterialProperty(targets, "_UnityFogEnable");
        _HFogEnable = GetMaterialProperty(targets, "_HFogEnable");
        _Color_Fog = GetMaterialProperty(targets, "_Color_Fog");
        _FogYStartPos = GetMaterialProperty(targets, "_FogYStartPos");
        _FogHeight = GetMaterialProperty(targets, "_FogHeight");

        _ShowColorCorrection = GetMaterialProperty(targets, "_ShowColorCorrection");
        _ColorCorrectionEnable = GetMaterialProperty(targets, "_ColorCorrectionEnable");
        _RimEnable = GetMaterialProperty(targets, "_RimEnable");
        _RimColor = GetMaterialProperty(targets, "_RimColor");
        _RimPower = GetMaterialProperty(targets, "_RimPower");
        _TintColor = GetMaterialProperty(targets, "_TintColor");
        _Saturation = GetMaterialProperty(targets, "_Saturation");
        _Brightness = GetMaterialProperty(targets, "_Brightness");

        _OtherSettings = GetMaterialProperty(targets, "_OtherSettings");

        _ShowGlobalGradientSettings = GetMaterialProperty(targets, "_ShowGlobalGradientSettings");
        _GradientHeight_G = GetMaterialProperty(targets, "_GradientHeight_G");
        _GradPivot_G = GetMaterialProperty(targets, "_GradientYStartPos_G");
        _Rotation_G = GetMaterialProperty(targets, "_Rotation_G");

        _ShowAmbientSettings = GetMaterialProperty(targets, "_ShowAmbientSettings");
        _AmbientColor = GetMaterialProperty(targets, "_AmbientColor");
        _AmbientPower = GetMaterialProperty(targets, "_AmbientPower");

        _RealtimeShadow = GetMaterialProperty(targets, "_RealtimeShadow");
        _ShadowColor = GetMaterialProperty(targets, "_ShadowColor");

        _GradientSpace = GetMaterialProperty(targets, "_GradientSpace");
        _DontMix = GetMaterialProperty(targets, "_DontMix");

        RecorderProps = new Dictionary<string, MaterialProperty>();
        RecorderProps.Add(_GradientHeight_F.name, _GradientHeight_F);
        RecorderProps.Add(_GradientHeight_B.name, _GradientHeight_B);
        RecorderProps.Add(_GradientHeight_L.name, _GradientHeight_L);
        RecorderProps.Add(_GradientHeight_R.name, _GradientHeight_R);
        RecorderProps.Add(_GradientHeight_T.name, _GradientHeight_T);
        RecorderProps.Add(_GradientHeight_D.name, _GradientHeight_D);

        RecorderProps.Add(_GradientYStartPos_F.name, _GradientYStartPos_F);
        RecorderProps.Add(_GradientYStartPos_B.name, _GradientYStartPos_B);
        RecorderProps.Add(_GradientYStartPos_L.name, _GradientYStartPos_L);
        RecorderProps.Add(_GradientYStartPos_R.name, _GradientYStartPos_R);
        RecorderProps.Add(_GradientXStartPos_T.name, _GradientXStartPos_T);
        RecorderProps.Add(_GradientXStartPos_D.name, _GradientXStartPos_D);

        RecorderProps.Add(_GizmoPosition_F.name, _GizmoPosition_F);
        RecorderProps.Add(_GizmoPosition_B.name, _GizmoPosition_B);
        RecorderProps.Add(_GizmoPosition_L.name, _GizmoPosition_L);
        RecorderProps.Add(_GizmoPosition_R.name, _GizmoPosition_R);
        RecorderProps.Add(_GizmoPosition_T.name, _GizmoPosition_T);
        RecorderProps.Add(_GizmoPosition_D.name, _GizmoPosition_D);

        RecorderProps.Add(_Rotation_F.name, _Rotation_F);
        RecorderProps.Add(_Rotation_B.name, _Rotation_B);
        RecorderProps.Add(_Rotation_L.name, _Rotation_L);
        RecorderProps.Add(_Rotation_R.name, _Rotation_R);
        RecorderProps.Add(_Rotation_T.name, _Rotation_T);
        RecorderProps.Add(_Rotation_D.name, _Rotation_D);
    }
    private void InitializeHelperVars()
    {
        if (_ShowFront.floatValue == 0) showFrontShading = false; else if (_ShowFront.floatValue == 1) showFrontShading = true;
        if (_ShowBack.floatValue == 0) showBackShading = false; else if (_ShowBack.floatValue == 1) showBackShading = true;
        if (_ShowLeft.floatValue == 0) showLeftShading = false; else if (_ShowLeft.floatValue == 1) showLeftShading = true;
        if (_ShowRight.floatValue == 0) showRightShading = false; else if (_ShowRight.floatValue == 1) showRightShading = true;
        if (_ShowTop.floatValue == 0) showTopShading = false; else if (_ShowTop.floatValue == 1) showTopShading = true;
        if (_ShowBottom.floatValue == 0) showBottomShading = false; else if (_ShowBottom.floatValue == 1) showBottomShading = true;

        frontShadingMode = (ShadingMode)_Shading_F.floatValue;
        backShadingMode = (ShadingMode)_Shading_B.floatValue;
        leftShadingMode = (ShadingMode)_Shading_L.floatValue;
        rightShadingMode = (ShadingMode)_Shading_R.floatValue;
        topShadingMode = (ShadingMode)_Shading_T.floatValue;
        bottomShadingMode = (ShadingMode)_Shading_D.floatValue;

        frontGradientSettings = (GradientSettings)_GradSettings_F.floatValue;
        backGradientSettings = (GradientSettings)_GradSettings_B.floatValue;
        leftGradientSettings = (GradientSettings)_GradSettings_L.floatValue;
        rightGradientSettings = (GradientSettings)_GradSettings_R.floatValue;
        topGradientSettings = (GradientSettings)_GradSettings_T.floatValue;
        bottomGradientSettings = (GradientSettings)_GradSettings_D.floatValue;
    }
    private void InitializeGUIStyles()
    {
        Indented = new GUIStyle();
        Indented.padding = new RectOffset(10, 0, 0, 0);
    }

    public override void OnInspectorGUI()
    { 
        if (!isVisible)
            return;

        var shdr = serializedObject.FindProperty("m_Shader");
        if (shdr.hasMultipleDifferentValues || shdr.objectReferenceValue == null)
            return;

        serializedObject.Update();
        // Update inspector if user changed shader.
        if (targetMat.shader.name != "MinimalistFree_V2/Standard" && targetMat.shader.name != "MinimalistFree_V2/LightWeight/Standard")
        {
            Repaint();
            EditorUtility.SetDirty(target);
            OnShaderChanged();
            return;
        }
        
        HeaderStyle = new GUIStyle("box")
        {
            fontSize = EditorStyles.boldLabel.fontSize,
            fontStyle = EditorStyles.boldLabel.fontStyle,
            font = EditorStyles.boldLabel.font,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(10, 0, 2, 0),
        };
        HeaderStyle.normal.background = GUI.skin.GetStyle("ShurikenModuleTitle").normal.background;
        

        //Texture Module
        EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width - 34));
        ShowTexture = GUILayout.Toggle(ShowTexture, "Main Texture", HeaderStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
        if (ShowTexture) TextureModule();
        //Shading Module
        ShowCustomShading = GUILayout.Toggle(ShowCustomShading, "Custom Shading", HeaderStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
        if (ShowCustomShading)
        {
            EditorGUILayout.BeginVertical(Indented);
            CustomShadingModule(ref showFrontShading, ref _ShowFront, FRONT, ref frontShadingMode, ref _Shading_F, ref _Color1_F, ref _Color2_F, ref frontGradientSettings, ref _GradSettings_F, ref _GradientHeight_F, ref _GradientYStartPos_F, ref _Rotation_F, "FRONTGRADIENT", "FRONTSOLID", _GizmoPosition_F);
            CustomShadingModule(ref showBackShading, ref _ShowBack, BACK, ref backShadingMode, ref _Shading_B, ref _Color1_B, ref _Color2_B, ref backGradientSettings, ref _GradSettings_B, ref _GradientHeight_B, ref _GradientYStartPos_B, ref _Rotation_B, "BACKGRADIENT", "BACKSOLID", _GizmoPosition_B);
            CustomShadingModule(ref showLeftShading, ref _ShowLeft, LEFT, ref leftShadingMode, ref _Shading_L, ref _Color1_L, ref _Color2_L, ref leftGradientSettings, ref _GradSettings_L, ref _GradientHeight_L, ref _GradientYStartPos_L, ref _Rotation_L, "LEFTGRADIENT", "LEFTSOLID", _GizmoPosition_L);
            CustomShadingModule(ref showRightShading, ref _ShowRight, RIGHT, ref rightShadingMode, ref _Shading_R, ref _Color1_R, ref _Color2_R, ref rightGradientSettings, ref _GradSettings_R, ref _GradientHeight_R, ref _GradientYStartPos_R, ref _Rotation_R, "RIGHTGRADIENT", "RIGHTSOLID", _GizmoPosition_R);
            CustomShadingModule(ref showTopShading, ref _ShowTop, TOP, ref topShadingMode, ref _Shading_T, ref _Color1_T, ref _Color2_T, ref topGradientSettings, ref _GradSettings_T, ref _GradientHeight_T, ref _GradientXStartPos_T, ref _Rotation_T, "TOPGRADIENT", "TOPSOLID", _GizmoPosition_T);
            CustomShadingModule(ref showBottomShading, ref _ShowBottom, DOWN, ref bottomShadingMode, ref _Shading_D, ref _Color1_D, ref _Color2_D, ref bottomGradientSettings, ref _GradSettings_D, ref _GradientHeight_D, ref _GradientXStartPos_D, ref _Rotation_D, "BOTTOMGRADIENT", "BOTTOMSOLID", _GizmoPosition_D);
            EditorGUILayout.EndVertical();
        }

        //Ambient Occlusion
        ShowAOSettings = GUILayout.Toggle(ShowAOSettings, "Ambient Occlusion", HeaderStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
        if (ShowAOSettings) AOModule();

        //Lightmap
        ShowlMapSettings = GUILayout.Toggle(ShowlMapSettings, "Lightmap", HeaderStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
        if (ShowlMapSettings) LightmapModule();

        //Fog
        ShowFogSettigns = GUILayout.Toggle(ShowFogSettigns, "Fog", HeaderStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
        if (ShowFogSettigns) FogModule();

        //Color Correction
        ShowColorCorrectionSettings = GUILayout.Toggle(ShowColorCorrectionSettings, "Color Correction", HeaderStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
        if (ShowColorCorrectionSettings) ColorCorrectionModule();

        //Other Settings
        ShowOtherSettings = GUILayout.Toggle(ShowOtherSettings, "Other Settings", HeaderStyle, GUILayout.Height(20), GUILayout.ExpandWidth(true));
        if (ShowOtherSettings) OtherSettings();

        EditorGUILayout.HelpBox("Some features are not available in the free edition of Minimalist", MessageType.Warning);
        if (GUILayout.Button("Get Full version of Minimalist"))
        {
            Application.OpenURL("http://bit.ly/fr_minimalist");
        }

        EditorGUILayout.EndVertical();
        Repaint();
    }

    private void TextureModule()
    {
        Texture tex = TextureProperty(_MainTexture, "Texture");
        if (tex != null)
        {
            targetMat.EnableKeyword("TEXTUREMODULE_ON");
            EditorGUI.BeginDisabledGroup(true);
            RangeProperty(_MainTexturePower, "Power [Pro only]");
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            targetMat.DisableKeyword("TEXTUREMODULE_ON");
        }
    }
    private void CustomShadingModule(
        ref bool ShowShading, ref MaterialProperty Show, string ShadingSide,
        ref ShadingMode ShadingType, ref MaterialProperty ShadeMode, ref MaterialProperty Color1,
        ref MaterialProperty Color2, ref GradientSettings GradSettings, ref MaterialProperty GradientSettings,
        ref MaterialProperty GradHeight, ref MaterialProperty GradPivot,
        ref MaterialProperty Rotation, string shaderKeywordG, string shaderKeywordS, MaterialProperty Gizmopos)
    {
        EditorGUILayout.BeginVertical();
        {
            ShowShading = EditorGUILayout.Foldout(ShowShading, ShadingSide);
        }
        EditorGUILayout.EndVertical();
        if (ShowShading)
        {
            Show.floatValue = 1;
            ShadingType = (ShadingMode)EditorGUILayout.EnumPopup("Shading Mode", ShadingType);
            ShadeMode.floatValue = (float)ShadingType;
            if (ShadingType == ShadingMode.VertexColor)
            {
                targetMat.DisableKeyword(shaderKeywordS);
            }
            else if (ShadingType == ShadingMode.SolidColor)
            {
                ColorProperty(Color1, "Color");
                targetMat.EnableKeyword(shaderKeywordS);
            }
            else if (ShadingType == ShadingMode.Gradient_ProOnly)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.BeginHorizontal("Box");
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(50));
                    {
                        Color1.colorValue = EditorGUILayout.ColorField(Color1.colorValue);
                        Color2.colorValue = EditorGUILayout.ColorField(Color2.colorValue);
                        if (GUILayout.Button("Swap"))
                        {
                        }
                        Rect R = EditorGUILayout.GetControlRect(GUILayout.Height(50), GUILayout.Width(50));

                        if (ShadingSide == TOP || ShadingSide == DOWN)
                            GUI.DrawTexture(R, GetTexture(Color1.colorValue, Color2.colorValue, true));
                        else
                            GUI.DrawTexture(R, GetTexture(Color1.colorValue, Color2.colorValue, false));
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width - 112));
                    {
                        GradSettings = (GradientSettings)EditorGUILayout.EnumPopup("", GradSettings, GUILayout.Width(Screen.width - 110));
                        GradientSettings.floatValue = (float)GradSettings;
                        EditorGUI.BeginDisabledGroup(IsGlobal(GradSettings));
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.BeginVertical(GUILayout.Width(Screen.width - 142));
                                {
                                    if (IsGlobal(GradSettings))
                                    {
                                        GradHeight.floatValue = CEditor.FloatField("Falloff", 70, _GradientHeight_G.floatValue);
                                        EditorGUILayout.LabelField("Pivot", GUILayout.Width(60));
                                        GradPivot.vectorValue = EditorGUILayout.Vector2Field("", _GradPivot_G.vectorValue, GUILayout.Width(Screen.width - 142));
                                        EditorGUILayout.LabelField("Rotation", GUILayout.Width(60));
                                        Rotation.floatValue = EditorGUILayout.Slider(_Rotation_G.floatValue, 0f, 360f, GUILayout.Width(Screen.width - 142));
                                    }
                                    else
                                    {
                                        GradHeight.floatValue = CEditor.FloatField("Falloff", 70, GradHeight.floatValue);
                                        EditorGUILayout.LabelField("Pivot", GUILayout.Width(60));
                                        GradPivot.vectorValue = EditorGUILayout.Vector2Field("", GradPivot.vectorValue, GUILayout.Width(Screen.width - 142));
                                        EditorGUILayout.LabelField("Rotation", GUILayout.Width(60));
                                        Rotation.floatValue = EditorGUILayout.Slider(Rotation.floatValue, 0f, 360f, GUILayout.Width(Screen.width - 142));
                                    }
                                }
                                EditorGUILayout.EndVertical();

                                EditorGUILayout.BeginVertical();
                                {
                                    EditorGUI.BeginDisabledGroup(!isAnythingSelected());
                                    {
                                        if (GUILayout.Button(EditorGUIUtility.IconContent("EditCollider", "Edit in Scene"), GUILayout.Height(28)))
                                        {
                                            
                                        }
                                        if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Duplicate"), GUILayout.Height(28)))
                                        {
                                        }
                                        if (GUILayout.Button(EditorGUIUtility.IconContent("Clipboard", "Paste" ), GUILayout.Height(28)))
                                        {
                                        }
                                    }
                                    EditorGUI.EndDisabledGroup();
                                }
                                EditorGUILayout.EndVertical();
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                Color1.colorValue = Color.white;
                Color2.colorValue = Color.white;
            }
        }
        else { Show.floatValue = 0; }
    }
    private void AOModule()
    {
        EnableAO = EditorGUILayout.Toggle("Enable", EnableAO);
        if (EnableAO)
        {
            EditorGUI.BeginDisabledGroup(true);
            TexturePropertySingleLine(new GUIContent("AO Map [Pro only]"), _AOTexture, _AOPower);
            EditorGUILayout.BeginHorizontal();
            {
                ColorProperty(_AOColor, "AO Color [Pro only]");
                aouv = (AOuv)EditorGUILayout.EnumPopup(aouv);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }
    }
    private void LightmapModule()
    {
        EnableLmap = EditorGUILayout.Toggle("Enable", EnableLmap);
        if (EnableLmap)
        {
            lMapBlendMode = (LightMapBlendingMode)EditorGUILayout.EnumPopup("Mode [Pro only]", lMapBlendMode);
            if (lMapBlendMode == LightMapBlendingMode.Add || lMapBlendMode == LightMapBlendingMode.Multiply)
            {
                EditorGUI.BeginDisabledGroup(true);
                RangeProperty(_LMPower, "Power");
                EditorGUI.EndDisabledGroup();
            }
            if (lMapBlendMode == LightMapBlendingMode.UseAsAO)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.HelpBox("Turn of all lights in the scene and bake lightmap with AO turned on and ambient color set to WHITE under the environment lighting settings, in the lightmap baking window.", MessageType.Info);
                ColorProperty(_LMColor, "AO Color");
                RangeProperty(_LMPower, "Power");
                EditorGUI.EndDisabledGroup();
            }
        }
    }
    private void GlobalGradientSettingsModule()
    {
        ShowGlobalGradientSettings = EditorGUILayout.Foldout(ShowGlobalGradientSettings, "Global Gradient Settings [Pro only]");
        if (ShowGlobalGradientSettings)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginVertical("box");
            {
                _GradientHeight_G.floatValue = CEditor.FloatField("Height", 80, _GradientHeight_G.floatValue);
                _GradPivot_G.vectorValue = EditorGUILayout.Vector2Field("Pivot", _GradPivot_G.vectorValue);
                EditorGUILayout.LabelField("Rotation");
                _Rotation_G.floatValue = EditorGUILayout.Slider(_Rotation_G.floatValue, 0f, 360f);
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

    }
    private void AmbientSettingsModule()
    {
        ShowAmbientSettings = EditorGUILayout.Foldout(ShowAmbientSettings, "Ambient Settings [Pro only]");
        if (ShowAmbientSettings)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginVertical("box");
            {
                ColorProperty(_AmbientColor, "Color");
                RangeProperty(_AmbientPower, "Power");
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();

        }
    }
    private void OtherSettings()
    {
        EditorGUILayout.BeginVertical(Indented);
        GlobalGradientSettingsModule();
        AmbientSettingsModule();
        EditorGUILayout.EndVertical();

        RimEnable = EditorGUILayout.Toggle("Rim [Pro only]", RimEnable);
        if (RimEnable)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginVertical("box");
            {
                ColorProperty(_RimColor, "Color");
                RangeProperty(_RimPower, "Power");
            }
            EditorGUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();
        }

        realtimeShadow = EditorGUILayout.Toggle("Realtime Shadow [Pro only]", realtimeShadow);
        if (realtimeShadow)
        {
            EditorGUI.BeginDisabledGroup(true);
            ColorProperty(_ShadowColor, "Shadow Color");
            EditorGUI.EndDisabledGroup();
        }

        dontMix = EditorGUILayout.Toggle("Don't mix shadings [Pro only]", dontMix);
    }

    private void FogModule()
    {
        EnableUnityFog = EditorGUILayout.Toggle("Unity fog", EnableUnityFog);
        if (EnableUnityFog) targetMat.EnableKeyword("UNITY_FOG");
        else targetMat.DisableKeyword("UNITY_FOG");

        EnableHFog = EditorGUILayout.Toggle("Height fog [Pro only]", EnableHFog);
        if (EnableHFog)
        {
            EditorGUI.BeginDisabledGroup(true);
            ColorProperty(_Color_Fog, "Color");
            FloatProperty(_FogYStartPos, "Height");
            FloatProperty(_FogHeight, "Falloff");
            EditorGUI.EndDisabledGroup();
        }
    }
    private void ColorCorrectionModule()
    {
        ColorCorrectionEnable = EditorGUILayout.Toggle("Enable", ColorCorrectionEnable);
        if (ColorCorrectionEnable)
        {
            EditorGUI.BeginDisabledGroup(true);
            RangeProperty(_Saturation, "Saturation [Pro only]");
            RangeProperty(_Brightness, "Brightness [Pro only]");
            ColorProperty(_TintColor, "Tint [Pro only]");
            EditorGUI.EndDisabledGroup();
        }
    }
    
    void OnSceneGUI()
    {
        if (gradientHandle != null)
        {
            Tools.hidden = true;
            EditorGUI.BeginChangeCheck();

            Vector3 pivot = new Vector3(), falloff = new Vector3();
            float rotation = 0;
            
            if (gradientHandle.profile == "LeftGradient" || gradientHandle.profile == "RightGradient")
            {
                pivot = Handles.PositionHandle(gradientHandle.pivot, Quaternion.identity);
                gradientHandle.pivot = new Vector3(gradientHandle.pivot.x, pivot.y, pivot.z);
                falloff = Handles.PositionHandle(gradientHandle.falloff, Quaternion.identity);
                gradientHandle.falloff = new Vector3(gradientHandle.falloff.x, falloff.y, falloff.z);
                rotation = Vector3.SignedAngle(falloff - pivot, Vector3.up, Vector3.right);
            }
            else if (gradientHandle.profile == "FrontGradient" || gradientHandle.profile == "BackGradient")
            {
                pivot = Handles.PositionHandle(gradientHandle.pivot, Quaternion.identity);
                gradientHandle.pivot = new Vector3(pivot.x, pivot.y, gradientHandle.pivot.z);
                falloff = Handles.PositionHandle(gradientHandle.falloff, Quaternion.identity);
                gradientHandle.falloff = new Vector3(falloff.x, falloff.y, gradientHandle.falloff.z);
                rotation = Vector3.SignedAngle(falloff - pivot, Vector3.up, Vector3.back);
            }
            else
            {
                pivot = Handles.PositionHandle(gradientHandle.pivot, Quaternion.identity);
                gradientHandle.pivot = new Vector3(pivot.x, gradientHandle.pivot.y, pivot.z);
                falloff = Handles.PositionHandle(gradientHandle.falloff, Quaternion.identity);
                gradientHandle.falloff = new Vector3(falloff.x, gradientHandle.falloff.y, falloff.z);
                rotation = Vector3.SignedAngle(falloff - pivot, Vector3.forward, Vector3.up);
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetMat, "Undo gradient values");
                Undo.RecordObject(gradientHandle, "Undo gradient handles");
                if (gradientHandle.profile == "FrontGradient" || gradientHandle.profile == "BackGradient")
                {
                    RecorderProps[gradientHandle.Ystart].vectorValue = new Vector2(gradientHandle.pivot.x, gradientHandle.pivot.y);
                }
                else if (gradientHandle.profile == "LeftGradient" || gradientHandle.profile == "RightGradient")
                {
                    RecorderProps[gradientHandle.Ystart].vectorValue = new Vector2(gradientHandle.pivot.z, gradientHandle.pivot.y);
                }
                else {
                    RecorderProps[gradientHandle.Ystart].vectorValue = new Vector2(gradientHandle.pivot.z, gradientHandle.pivot.x);
                }
                RecorderProps[gradientHandle.Height].floatValue = Vector3.Distance(gradientHandle.pivot, gradientHandle.falloff);
                RecorderProps[gradientHandle.rotation].floatValue = Remap(rotation, -180, 180, 360, 0);
                Repaint();
            }
            
            //Handles.SphereHandleCap(0, gradientHandle.pivot, Quaternion.identity, 2f, EventType.MouseUp);
            //Handles.SphereCap(0, gradientHandle.pivot, Quaternion.identity, 1f);
            //Handles.SphereHandleCap(0, gradientHandle.falloff, Quaternion.identity, 2f, EventType.Ignore);
            Handles.DrawDottedLine(gradientHandle.pivot, gradientHandle.falloff, 5);

        }
        else
        {
            Tools.hidden = false;
        }
    }

    //Helper Classes and Methods
    bool IsGlobal(GradientSettings settings)
    {
        if (settings == GradientSettings.UseGlobalGradientSettings) return true;
        else return false;
    }
    bool isAnythingSelected()
    {
        if (Selection.activeTransform) return true;
        return false;
    }

    float WrapAngle(float angle)
    {
        if (angle > 360)
        {
            angle = angle - 360;
        }
        else if (angle < 0)
        {
            angle = 360 + angle;
        }
        return angle;
    }
    float Remap(float value, float low1, float high1, float low2, float high2)
    {
        return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
    }
    Texture2D GetTexture(Color color1, Color color2, bool Horizontal)
    {
        Texture2D tex;
        Color[] cols = new Color[64];

        if (Horizontal)
        {
            for (int i = 0; i < 64; i++)
            {
                cols[i] = Color.Lerp(color1, color2, (float)i / 63);
            }
            tex = new Texture2D(64, 1);
        }
        else
        {
            for (int i = 0; i < 64; i++)
            {
                cols[i] = Color.Lerp(color2, color1, (float)i / 63);
            }
            tex = new Texture2D(1, 64);
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }
    class CEditor
    {
        public static float FloatField(string label, float labelWidth, float floatValue, float floatBoxWidth)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            float value = EditorGUILayout.FloatField(floatValue, GUILayout.Width(floatBoxWidth));
            EditorGUILayout.EndHorizontal();
            return value;
        }
        public static float FloatField(string label, float labelWidth, float floatValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            float value = EditorGUILayout.FloatField(floatValue);
            EditorGUILayout.EndHorizontal();
            return value;
        }
    }
    class GradientSettingsHolder
    {
        public ShadingMode Mode;
        public GradientSettings gradSettings;
        public Color color1;
        public Color color2;
        public float gradHeight;
        public float gradYPos;
        public float Rotation;

        public GradientSettingsHolder(ShadingMode _mode, GradientSettings _settings, Color _color1, Color _color2, float _gradHeight, float _gradYPos, float _Rotation)
        {
            Mode = _mode;
            gradSettings = _settings;
            color1 = _color1;
            color2 = _color2;
            gradHeight = _gradHeight;
            gradYPos = _gradYPos;
            Rotation = _Rotation;
        }
    }
    class GradientHandle : ScriptableObject
    {
        public Vector3 pivot;
        public Vector3 falloff;
        public string profile;
        public string Ystart;
        public string Height;
        public string gizmo;
        public string rotation;
    }

    public static MinimalistClipboard MClipboard;

    //[CreateAssetMenu(fileName = "MinimalistClipboard", menuName = "MinimalistClipboard", order = 1)]
    public class MinimalistClipboard : ScriptableObject
    {
        public float falloff;
        public Vector2 pivot;
        public float rotation;
    }
}
