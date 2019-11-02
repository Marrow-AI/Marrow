using UnityEditor;
using UnityEditor.Callbacks;
using System;

public static class KinectVisualGestureBuilderPostBuildCopyPluginData
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        KinectCopyPluginDataHelper.CopyPluginData (target, path, "vgbtechs");
    }
}
