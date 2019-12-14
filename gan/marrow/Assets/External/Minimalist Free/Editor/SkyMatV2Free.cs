using UnityEngine;
using UnityEditor;

public class SkyMatV2Free : ShaderGUI
{

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty _Color1 = ShaderGUI.FindProperty("_Color1", properties);
        MaterialProperty _Color2 = ShaderGUI.FindProperty("_Color2", properties);

        MaterialProperty _UpVector = ShaderGUI.FindProperty("_UpVector", properties);

        MaterialProperty _Intensity = ShaderGUI.FindProperty("_Intensity", properties);
        MaterialProperty _Exponent = ShaderGUI.FindProperty("_Exponent", properties);

        materialEditor.ShaderProperty(_Color1, _Color1.displayName);
        materialEditor.ShaderProperty(_Color2, _Color2.displayName);

        EditorGUI.BeginDisabledGroup(true);
        materialEditor.ShaderProperty(_UpVector, _UpVector.displayName);
        EditorGUI.EndDisabledGroup();

        materialEditor.ShaderProperty(_Intensity, _Intensity.displayName);
        materialEditor.ShaderProperty(_Exponent, _Exponent.displayName);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Some features are not available in the free edition of Minimalist", MessageType.Warning);
        if (GUILayout.Button("Get full version of Minimalist"))
        {
            Application.OpenURL("http://bit.ly/fr_minimalist");
        }
    }
}