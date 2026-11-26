using UnityEngine;
using Arrows.Data;

namespace Arrows.Spline
{
    /// <summary>
    /// Attach this component to a GameObject in the scene to visually preview and edit
    /// LevelData splines directly inside the Unity Scene View using Gizmos and Editor Handles.
    /// </summary>
    [ExecuteAlways]
    public class LevelVisualizer : MonoBehaviour
    {
        [Tooltip("The LevelData asset to visualize in the Scene view.")]
        public LevelData levelData;

        [Tooltip("Number of preview segments per curve.")]
        [Range(10, 100)]
        public int previewResolution = 40;

        [Tooltip("Radius of control point gizmo spheres.")]
        public float pointGizmoRadius = 0.15f;

        [Tooltip("Show arrowhead tangent previews at endpoints.")]
        public bool showArrowheads = true;

        [Tooltip("Show collision radius discs around each arrowhead endpoint.")]
        public bool showCollisionRadius = true;

        private void OnDrawGizmos()
        {
            if (levelData == null || levelData.lines == null) return;

            foreach (var line in levelData.lines)
            {
                if (line == null || line.controlPoints == null || line.controlPoints.Length < 2)
                    continue;

                Gizmos.color = line.lineColor;

                // Draw control points
                for (int i = 0; i < line.controlPoints.Length; i++)
                {
                    Vector3 pt = line.controlPoints[i];
                    Gizmos.DrawWireSphere(pt, pointGizmoRadius);
                }

                // Sample and draw spline curve
                Vector3 prevPoint = CatmullRomSpline.EvaluatePath(line.controlPoints, 0f);
                for (int s = 1; s <= previewResolution; s++)
                {
                    float t = (float)s / previewResolution;
                    Vector3 currentPoint = CatmullRomSpline.EvaluatePath(line.controlPoints, t);
                    Gizmos.DrawLine(prevPoint, currentPoint);
                    prevPoint = currentPoint;
                }

                Vector2 startPos = CatmullRomSpline.EvaluatePath(line.controlPoints, 0f);
                Vector2 endPos = CatmullRomSpline.EvaluatePath(line.controlPoints, 1f);

                if (showArrowheads)
                {
                    // Draw start arrowhead tangent
                    Vector2 startTan = CatmullRomSpline.EvaluatePathTangent(line.controlPoints, 0f).normalized;
                    Gizmos.color = line.lineColor;
                    Gizmos.DrawRay(startPos, (Vector3)(startTan * 0.4f));

                    // Draw end arrowhead tangent (pointing backwards)
                    Vector2 endTan = CatmullRomSpline.EvaluatePathTangent(line.controlPoints, 1f).normalized;
                    Gizmos.DrawRay(endPos, (Vector3)(-endTan * 0.4f));
                }

                // Visualize the configured collision radius at both endpoints
                if (showCollisionRadius)
                {
                    Gizmos.color = new Color(line.lineColor.r, line.lineColor.g, line.lineColor.b, 0.4f);
                    Gizmos.DrawWireSphere(startPos, line.collisionRadius);
                    Gizmos.DrawWireSphere(endPos, line.collisionRadius);
                }
            }
        }
    }
}
