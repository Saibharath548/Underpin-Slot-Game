using UnityEngine;
using UnityEditor;
using Arrows.Data;
using Arrows.Spline;

namespace Arrows.Editor
{
    /// <summary>
    /// Custom Inspector for LevelData ScriptableObject.
    /// Provides helper buttons to validate lines and preview curve details.
    /// </summary>
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var levelData = (LevelData)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Level Authoring Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate Level Configuration", GUILayout.Height(30)))
            {
                if (levelData.Validate(out string error))
                {
                    EditorUtility.DisplayDialog("Validation Success", $"Level '{levelData.levelName}' is valid with {levelData.lines.Length} lines.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Validation Error", error, "OK");
                }
            }

            if (levelData.lines != null)
            {
                EditorGUILayout.HelpBox($"Total Lines Defined: {levelData.lines.Length}", MessageType.Info);
            }
        }
    }
}

