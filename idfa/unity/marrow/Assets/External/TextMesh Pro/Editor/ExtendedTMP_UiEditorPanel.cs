using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace ExtendedTMPro.EditorUtilities
{
    [CustomEditor(typeof(ExtendedTextMeshProUGUI)), CanEditMultipleObjects]
	public class ExtendedTMP_UiEditorPanel : TMP_UiEditorPanel
    {
        private struct FoldoutSettingsHolder
        {
            public static bool textInput = true;
            public static bool fontSettings = true;
            public static bool extraSettings = false;
            public static bool shadowSetting = false;
            public static bool materialEditor = true;
        }

        private static readonly string[] UI_STATE_LABEL = new string[] { "\t- <i>Click to expand</i> -", "\t- <i>Click to collapse</i> -" };

        private SerializedProperty enableVertexWarping;
        private SerializedProperty vertexCurve;
        private SerializedProperty scaleMultiplierPerCharacter;
        private SerializedProperty havePropertiesChanged;

        private bool didPropertiesChange;

        #region UNITY METHODS
        /// <summary>
        /// Adds initializing of warping properties to <see cref="TMPro_uiEditorPanel.OnEnable"/>.
        /// </summary>
        new public void OnEnable()
        {
            base.OnEnable();

            enableVertexWarping = serializedObject.FindProperty("enableVertexWarping");
            vertexCurve = serializedObject.FindProperty("vertexCurve");
            scaleMultiplierPerCharacter = serializedObject.FindProperty("scaleMultiplierPerCharacter");

            havePropertiesChanged = serializedObject.FindProperty("m_havePropertiesChanged");
        }

        /// <summary>
        /// Adds warping settings to the custom inspector GUI of <see cref="TMPro_uiEditorPanel"/>.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("<b>WARPING SETTINGS</b>" + (FoldoutSettingsHolder.extraSettings ? UI_STATE_LABEL[1] : UI_STATE_LABEL[0]), TMP_UIStyleManager.Section_Label))
            {
                FoldoutSettingsHolder.extraSettings = !FoldoutSettingsHolder.extraSettings;
            }

            if (FoldoutSettingsHolder.extraSettings)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableVertexWarping, new GUIContent("Enable Warping?"));
                if (enableVertexWarping.boolValue)
                {
                    EditorGUILayout.PropertyField(vertexCurve, new GUIContent("Vertex curve"));
                    EditorGUILayout.PropertyField(scaleMultiplierPerCharacter, new GUIContent("Scale multiplier"));
                }
                if (EditorGUI.EndChangeCheck())
                {
                    didPropertiesChange = true;
                }
            }

            if (didPropertiesChange)
            {
                havePropertiesChanged.boolValue = true;
                didPropertiesChange = false;
                EditorUtility.SetDirty(target);
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}