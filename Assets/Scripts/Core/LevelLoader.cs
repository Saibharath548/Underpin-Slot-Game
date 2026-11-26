using UnityEngine;
using Arrows.Data;
using Arrows.Gameplay;
using Arrows.Spline;

namespace Arrows.Core
{
    /// <summary>
    /// Factory that reads a LevelData ScriptableObject and instantiates all
    /// LineController GameObjects with their spline paths, renderers, and arrowheads.
    ///
    /// This class creates objects but holds no runtime state — it is a pure factory.
    /// All created objects are parented under a provided Transform for organization.
    /// </summary>
    public static class LevelLoader
    {
        /// <summary>
        /// Loads a level by instantiating all lines from the given LevelData.
        /// </summary>
        /// <param name="levelData">The level configuration to load.</param>
        /// <param name="parent">Parent transform for all spawned objects.</param>
        /// <returns>Array of created LineControllers, or empty array on failure.</returns>
        public static LineController[] LoadLevel(LevelData levelData, Transform parent)
        {
            if (levelData == null)
            {
                Debug.LogError("[LevelLoader] LevelData is null.");
                return System.Array.Empty<LineController>();
            }

            if (!levelData.Validate(out string error))
            {
                Debug.LogError($"[LevelLoader] Invalid level data: {error}");
                return System.Array.Empty<LineController>();
            }

            var controllers = new LineController[levelData.lines.Length];

            for (int i = 0; i < levelData.lines.Length; i++)
            {
                controllers[i] = CreateLine(levelData.lines[i], parent);
            }

            Debug.Log($"[LevelLoader] Loaded level '{levelData.levelName}' with {controllers.Length} lines.");
            return controllers;
        }

        /// <summary>
        /// Creates a single line's full object hierarchy:
        ///   Line_[id] (LineController, LineRenderer, SplineRenderer)
        ///     ├── ArrowA (ArrowheadController, MeshFilter, MeshRenderer, CircleCollider2D)
        ///     └── ArrowB (ArrowheadController, MeshFilter, MeshRenderer, CircleCollider2D)
        /// </summary>
        private static LineController CreateLine(LineDefinition def, Transform parent)
        {
            // ── Root object with LineRenderer and SplineRenderer ──
            var lineObj = new GameObject($"Line_{def.lineId}");
            lineObj.transform.SetParent(parent, worldPositionStays: false);

            lineObj.AddComponent<LineRenderer>();
            var splineRenderer = lineObj.AddComponent<SplineRenderer>();
            var controller = lineObj.AddComponent<LineController>();

            // ── Build spline path from control points ──
            var splinePath = new SplinePath(def.controlPoints);

            // ── Initialize renderer ──
            splineRenderer.Initialize(splinePath, def.lineColor, def.lineWidth);

            // ── Create arrowheads ──
            var arrowA = CreateArrowhead("ArrowA", splinePath, t: 0f, facingSign: 1f, def, lineObj.transform);
            var arrowB = CreateArrowhead("ArrowB", splinePath, t: 1f, facingSign: -1f, def, lineObj.transform);

            // ── Initialize controller (this also initializes the arrowheads) ──
            controller.Initialize(def, splinePath, splineRenderer, arrowA, arrowB);

            return controller;
        }

        /// <summary>
        /// Creates a single arrowhead GameObject with mesh, renderer, and collider.
        /// </summary>
        /// <param name="name">GameObject name.</param>
        /// <param name="path">Spline path for initial positioning.</param>
        /// <param name="t">Spline parameter for initial position (0 = start, 1 = end).</param>
        /// <param name="facingSign">+1 = face forward, -1 = face backward.</param>
        /// <param name="def">Line definition for color and scale.</param>
        /// <param name="parent">Parent transform.</param>
        private static ArrowheadController CreateArrowhead(
            string name,
            SplinePath path,
            float t,
            float facingSign,
            LineDefinition def,
            Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, worldPositionStays: false);

            // Scale
            obj.transform.localScale = Vector3.one * def.arrowheadScale;

            // Required components (ArrowheadController's RequireComponent will add these,
            // but we add them explicitly for clarity)
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.AddComponent<CircleCollider2D>();

            // ArrowheadController — Initialize is called later by LineController
            var controller = obj.AddComponent<ArrowheadController>();

            // Position at the correct endpoint
            Vector2 pos = path.EvaluatePosition(t);
            Vector2 tangent = path.EvaluateTangent(t);
            obj.transform.position = new Vector3(pos.x, pos.y, -0.1f);

            if (tangent.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(tangent.y * facingSign, tangent.x * facingSign) * Mathf.Rad2Deg;
                obj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            return controller;
        }

        /// <summary>
        /// Destroys all line objects that were previously loaded.
        /// </summary>
        public static void UnloadLevel(LineController[] lines)
        {
            if (lines == null) return;

            foreach (var line in lines)
            {
                if (line != null && line.gameObject != null)
                {
                    Object.Destroy(line.gameObject);
                }
            }
        }
    }
}

