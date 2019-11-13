#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEditor.AnimatedValues;
using UnityEditor.Callbacks;
// Custom Editor using SerializedProperties.
// Automatic handling of multi-object editing, undo, and prefab overrides.

[CustomEditor(typeof(HxDensityVolume))]
[CanEditMultipleObjects]

public class HxDensityVolumeEditor : Editor  
{

    static int ExtraRenderFrames;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

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
    }
}
#endif
