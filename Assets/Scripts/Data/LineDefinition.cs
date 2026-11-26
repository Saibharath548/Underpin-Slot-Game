using UnityEngine;

namespace Arrows.Data
{
    /// <summary>
    /// Serializable definition for a single colored line in a level.
    /// Each line has a set of spline control points, a color, and movement parameters.
    /// </summary>
    [System.Serializable]
    public class LineDefinition
    {
        [Tooltip("Unique identifier for this line.")]
        public string lineId;

        [Tooltip("Color of the line and its arrowheads.")]
        public Color lineColor = Color.white;

        [Tooltip("Width of the rendered line.")]
        [Range(0.02f, 0.5f)]
        public float lineWidth = 0.08f;

        [Tooltip("Catmull-Rom spline control points defining the path. The path passes through all points.")]
        public Vector2[] controlPoints;

        [Tooltip("Scale multiplier for the arrowhead triangles.")]
        [Range(0.1f, 3f)]
        public float arrowheadScale = 0.4f;

        [Tooltip("Movement speed of the arrowhead in world units per second.")]
        [Range(0.5f, 15f)]
        public float movementSpeed = 3f;

        [Tooltip("Radius used for arrowhead collision detection.")]
        [Range(0.05f, 1f)]
        public float collisionRadius = 0.25f;

        /// <summary>
        /// Validates that the line definition has enough data to construct a path.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(lineId)
                && controlPoints != null
                && controlPoints.Length >= 2;
        }
    }
}

