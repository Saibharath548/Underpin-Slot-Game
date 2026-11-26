using UnityEngine;
using UnityEditor;
using Arrows.Spline;

namespace Arrows.Editor
{
    /// <summary>
    /// Custom editor for LevelVisualizer that draws interactive position handles in Scene View
    /// for every control point in the LevelData asset.
    /// </summary>
    [CustomEditor(typeof(LevelVisualizer))]
    public class LevelVisualizerEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var visualizer = (LevelVisualizer)target;
            if (visualizer.levelData == null || visualizer.levelData.lines == null) return;

            var serializedObj = new SerializedObject(visualizer.levelData);
            var linesProp = serializedObj.FindProperty("lines");

            for (int l = 0; l < visualizer.levelData.lines.Length; l++)
            {
                var line = visualizer.levelData.lines[l];
                if (line == null || line.controlPoints == null) continue;

                var lineProp = linesProp.GetArrayElementAtIndex(l);
                var controlPointsProp = lineProp.FindPropertyRelative("controlPoints");

                Handles.color = line.lineColor;

                for (int p = 0; p < line.controlPoints.Length; p++)
                {
                    Vector3 currentPos = line.controlPoints[p];

                    // Label for the control point
                    Handles.Label(currentPos + Vector3.up * 0.25f, $"{line.lineId}[{p}]");

                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Handles.PositionHandle(currentPos, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(visualizer.levelData, $"Move Control Point {line.lineId}[{p}]");
                        controlPointsProp.GetArrayElementAtIndex(p).vector2Value = new Vector2(newPos.x, newPos.y);
                        serializedObj.ApplyModifiedProperties();
                        EditorUtility.SetDirty(visualizer.levelData);
                    }
                }
            }
        }
    }
}

