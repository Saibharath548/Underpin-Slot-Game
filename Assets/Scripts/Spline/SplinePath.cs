using UnityEngine;
using System;

namespace Arrows.Spline
{
    /// <summary>
    /// Manages a Catmull-Rom spline path with arc-length parameterization.
    ///
    /// Raw spline parameter t does NOT produce constant speed (movement is faster
    /// on straight sections, slower on curves). This class pre-computes an arc-length
    /// lookup table to convert between distance-along-path and raw parameter t,
    /// enabling constant-speed movement.
    ///
    /// This is a pure data/computation class — no MonoBehaviour dependency.
    /// </summary>
    public class SplinePath
    {
        private readonly Vector2[] controlPoints;
        private readonly int segmentCount;
        private readonly int totalSamples;

        // Arc-length lookup tables (pre-computed)
        private readonly float[] sampleParameters;     // raw t at each sample index
        private readonly float[] cumulativeArcLength;   // cumulative distance at each sample
        private readonly Vector2[] samplePositions;     // position at each sample

        /// <summary>Total arc length of the entire path in world units.</summary>
        public float TotalLength { get; private set; }

        /// <summary>Number of user-provided control points.</summary>
        public int ControlPointCount => controlPoints.Length;

        /// <summary>Position at the start of the path (t=0).</summary>
        public Vector2 StartPosition => samplePositions[0];

        /// <summary>Position at the end of the path (t=1).</summary>
        public Vector2 EndPosition => samplePositions[totalSamples - 1];

        /// <summary>Tangent direction at the start of the path.</summary>
        public Vector2 StartTangent => EvaluateTangent(0f);

        /// <summary>Tangent direction at the end of the path.</summary>
        public Vector2 EndTangent => EvaluateTangent(1f);

        /// <summary>
        /// Creates a SplinePath from control points and pre-computes arc-length tables.
        /// </summary>
        /// <param name="points">At least 2 control points defining the path.</param>
        /// <param name="samplesPerSegment">
        /// Number of samples per spline segment for arc-length approximation.
        /// Higher values give more accurate constant-speed movement.
        /// </param>
        public SplinePath(Vector2[] points, int samplesPerSegment = 64)
        {
            if (points == null || points.Length < 2)
                throw new ArgumentException("SplinePath requires at least 2 control points.");

            controlPoints = points;
            segmentCount = points.Length - 1;
            totalSamples = segmentCount * samplesPerSegment + 1;

            sampleParameters = new float[totalSamples];
            cumulativeArcLength = new float[totalSamples];
            samplePositions = new Vector2[totalSamples];

            BuildArcLengthTable();
        }

        /// <summary>
        /// Pre-computes cumulative arc length at evenly-spaced parameter samples.
        /// </summary>
        private void BuildArcLengthTable()
        {
            Vector2 prevPos = CatmullRomSpline.EvaluatePath(controlPoints, 0f);
            sampleParameters[0] = 0f;
            cumulativeArcLength[0] = 0f;
            samplePositions[0] = prevPos;

            float cumulative = 0f;

            for (int i = 1; i < totalSamples; i++)
            {
                float t = (float)i / (totalSamples - 1);
                Vector2 pos = CatmullRomSpline.EvaluatePath(controlPoints, t);

                cumulative += Vector2.Distance(prevPos, pos);

                sampleParameters[i] = t;
                cumulativeArcLength[i] = cumulative;
                samplePositions[i] = pos;

                prevPos = pos;
            }

            TotalLength = cumulative;
        }

        // ─────────────────────────────────────────────
        //  Position and tangent evaluation
        // ─────────────────────────────────────────────

        /// <summary>
        /// Evaluates position at raw parameter t ∈ [0, 1] (NOT arc-length parameterized).
        /// </summary>
        public Vector2 EvaluatePosition(float t)
        {
            return CatmullRomSpline.EvaluatePath(controlPoints, t);
        }

        /// <summary>
        /// Evaluates the tangent (unnormalized) at raw parameter t.
        /// </summary>
        public Vector2 EvaluateTangent(float t)
        {
            return CatmullRomSpline.EvaluatePathTangent(controlPoints, t);
        }

        /// <summary>
        /// Evaluates position at a given distance along the path (arc-length parameterized).
        /// This produces constant-speed movement.
        /// </summary>
        public Vector2 EvaluatePositionAtDistance(float distance)
        {
            float t = DistanceToParameter(distance);
            return CatmullRomSpline.EvaluatePath(controlPoints, t);
        }

        /// <summary>
        /// Evaluates tangent at a given distance along the path.
        /// </summary>
        public Vector2 EvaluateTangentAtDistance(float distance)
        {
            float t = DistanceToParameter(distance);
            return CatmullRomSpline.EvaluatePathTangent(controlPoints, t);
        }

        // ─────────────────────────────────────────────
        //  Arc-length ↔ parameter conversion
        // ─────────────────────────────────────────────

        /// <summary>
        /// Converts distance along the path to the corresponding raw spline parameter t.
        /// Uses binary search on the pre-computed arc-length table with linear interpolation.
        /// </summary>
        public float DistanceToParameter(float distance)
        {
            if (TotalLength <= 0f) return 0f;

            distance = Mathf.Clamp(distance, 0f, TotalLength);

            // Handle exact boundaries
            if (distance <= 0f) return 0f;
            if (distance >= TotalLength) return 1f;

            // Binary search for the two samples bracketing this distance
            int lo = 0;
            int hi = totalSamples - 1;

            while (lo < hi - 1)
            {
                int mid = (lo + hi) >> 1;
                if (cumulativeArcLength[mid] <= distance)
                    lo = mid;
                else
                    hi = mid;
            }

            // Linear interpolation between the two bracketing samples
            float segLength = cumulativeArcLength[hi] - cumulativeArcLength[lo];
            if (segLength < 0.0001f)
                return sampleParameters[lo];

            float fraction = (distance - cumulativeArcLength[lo]) / segLength;
            return Mathf.Lerp(sampleParameters[lo], sampleParameters[hi], fraction);
        }

        /// <summary>
        /// Converts raw parameter t to distance along the path.
        /// </summary>
        public float ParameterToDistance(float t)
        {
            t = Mathf.Clamp01(t);

            if (t <= 0f) return 0f;
            if (t >= 1f) return TotalLength;

            // Binary search for the two samples bracketing this parameter
            int lo = 0;
            int hi = totalSamples - 1;

            while (lo < hi - 1)
            {
                int mid = (lo + hi) >> 1;
                if (sampleParameters[mid] <= t)
                    lo = mid;
                else
                    hi = mid;
            }

            float paramRange = sampleParameters[hi] - sampleParameters[lo];
            if (paramRange < 0.0001f)
                return cumulativeArcLength[lo];

            float fraction = (t - sampleParameters[lo]) / paramRange;
            return Mathf.Lerp(cumulativeArcLength[lo], cumulativeArcLength[hi], fraction);
        }

        // ─────────────────────────────────────────────
        //  Sampled positions (for rendering)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Fills the provided arrays with evenly-sampled positions and their corresponding
        /// parameter t values. Used by SplineRenderer for efficient line rendering.
        /// </summary>
        /// <param name="positions">Output array (must be pre-allocated to sampleCount).</param>
        /// <param name="parameters">Output array (must be pre-allocated to sampleCount).</param>
        /// <param name="sampleCount">Number of samples to generate.</param>
        public void SamplePath(Vector3[] positions, float[] parameters, int sampleCount)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                Vector2 pos = CatmullRomSpline.EvaluatePath(controlPoints, t);
                positions[i] = new Vector3(pos.x, pos.y, 0f);
                parameters[i] = t;
            }
        }
    }
}

