#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditor.AnimatedValues;
using UnityEditor.Callbacks;
// Custom Editor using SerializedProperties.
// Automatic handling of multi-object editing, undo, and prefab overrides.

[CustomEditor(typeof(HxVolumetricShadersUsed))]

public class HxVolumetricShadersUsedEditor : Editor  
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        if (GUILayout.Button("Build Shaders (Might take some time)" ))
        {
            HxVolumetricShadersUsed.ForceBuild();
        }
        //draw button to build shaders.

    }
}
#endif
