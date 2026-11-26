using UnityEngine;

namespace Arrows.Spline
{
    /// <summary>
    /// Renders a spline path using Unity's LineRenderer component.
    /// Supports partial rendering (erasing behind a moving arrowhead)
    /// and visual highlighting for the hint system.
    ///
    /// This component manages ONLY rendering — it has no knowledge of
    /// gameplay state, movement, or collision.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class SplineRenderer : MonoBehaviour
    {
        private LineRenderer lineRenderer;

        // Full set of sampled points (never modified after initialization)
        private Vector3[] fullPoints;
        private float[] pointParameters;
        private int fullPointCount;

        // Pre-allocated active points buffer to eliminate per-frame GC allocations
        private Vector3[] activePointsBuffer;

        // Cached original style
        private Color baseColor;
        private float baseWidth;

        // Highlight state
        private bool isHighlighted;

        /// <summary>
        /// Initializes the renderer by sampling the spline and configuring the LineRenderer.
        /// </summary>
        /// <param name="path">The spline path to render.</param>
        /// <param name="color">Line color.</param>
        /// <param name="width">Line width in world units.</param>
        /// <param name="sampleCount">Number of sample points (more = smoother curves).</param>
        public void Initialize(SplinePath path, Color color, float width, int sampleCount = 100)
        {
            lineRenderer = GetComponent<LineRenderer>();
            baseColor = color;
            baseWidth = width;
            fullPointCount = sampleCount;

            // Sample the spline
            fullPoints = new Vector3[sampleCount];
            pointParameters = new float[sampleCount];
            path.SamplePath(fullPoints, pointParameters, sampleCount);

            // Buffer sized to hold start point + all samples + end point + safety margin
            activePointsBuffer = new Vector3[sampleCount + 4];

            // Configure LineRenderer
            ConfigureLineRenderer(color, width);
            SetFullyVisible();
        }

        /// <summary>
        /// Sets up LineRenderer properties.
        /// </summary>
        private void ConfigureLineRenderer(Color color, float width)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.sortingOrder = 0;

            // Use Sprites/Default shader — compatible with URP 2D
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            lineRenderer.material = mat;
        }

        /// <summary>
        /// Sets the visible portion of the line to [startT, endT], pinning the endpoints
        /// exactly to startPos and endPos.
        ///
        /// This eliminates any visual stepping, gaps, or premature disappearance at the boundaries
        /// when an arrowhead is moving.
        /// </summary>
        /// <param name="startT">Normalized parameter representing the start of the visible range.</param>
        /// <param name="endT">Normalized parameter representing the end of the visible range.</param>
        /// <param name="startPos">Exact world-space position for the start of the visible line.</param>
        /// <param name="endPos">Exact world-space position for the end of the visible line.</param>
        public void SetVisibleRange(float startT, float endT, Vector3 startPos, Vector3 endPos)
        {
            if (fullPoints == null || lineRenderer == null) return;

            // If the moving arrowhead has reached or passed the destination
            if (startT >= endT)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            int count = 0;
            activePointsBuffer[count++] = startPos;

            // Include all pre-sampled points that strictly lie between startT and endT
            for (int i = 0; i < fullPointCount; i++)
            {
                float t = pointParameters[i];
                if (t > startT + 0.0001f && t < endT - 0.0001f)
                {
                    activePointsBuffer[count++] = fullPoints[i];
                }
            }

            activePointsBuffer[count++] = endPos;

            lineRenderer.positionCount = count;
            lineRenderer.SetPositions(activePointsBuffer);
        }

        /// <summary>
        /// Overload for setting visible range using only parameters.
        /// Uses the sampled boundary positions.
        /// </summary>
        public void SetVisibleRange(float startT, float endT)
        {
            if (fullPoints == null || fullPointCount < 2) return;
            SetVisibleRange(startT, endT, fullPoints[0], fullPoints[fullPointCount - 1]);
        }

        /// <summary>
        /// Makes the entire line visible (restores all sampled points).
        /// </summary>
        public void SetFullyVisible()
        {
            if (fullPoints == null || lineRenderer == null) return;

            lineRenderer.positionCount = fullPointCount;
            lineRenderer.SetPositions(fullPoints);
        }

        /// <summary>
        /// Hides the entire line (sets position count to 0).
        /// </summary>
        public void Hide()
        {
            if (lineRenderer != null)
                lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// Toggles highlight mode for the hint system.
        /// When highlighted, the line appears brighter and slightly wider.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (lineRenderer == null) return;
            if (isHighlighted == highlighted) return;

            isHighlighted = highlighted;

            if (highlighted)
            {
                // Brighten color and widen
                Color bright = Color.Lerp(baseColor, Color.white, 0.5f);
                lineRenderer.startColor = bright;
                lineRenderer.endColor = bright;
                lineRenderer.startWidth = baseWidth * 2f;
                lineRenderer.endWidth = baseWidth * 2f;
                lineRenderer.material.color = bright;
            }
            else
            {
                // Restore original appearance
                lineRenderer.startColor = baseColor;
                lineRenderer.endColor = baseColor;
                lineRenderer.startWidth = baseWidth;
                lineRenderer.endWidth = baseWidth;
                lineRenderer.material.color = baseColor;
            }
        }
    }
}
