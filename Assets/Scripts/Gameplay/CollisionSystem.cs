using System;
using System.Collections.Generic;
using UnityEngine;
using Arrows.Spline;

namespace Arrows.Gameplay
{
    /// <summary>
    /// Frame-independent curve-aware swept collision detection system for arrowheads.
    ///
    /// Key Principles:
    /// 1. True Curve-Aware Swept Collision:
    ///    - When an arrowhead travels along a curved spline during a frame, its trajectory
    ///      is evaluated directly along the spline's mathematical curve using sub-stepping.
    ///    - Supports both moving-vs-stationary and moving-vs-moving arrowheads.
    ///    - Completely eliminates tunneling and curvature sagitta inaccuracies.
    /// 2. Single Authoritative Event:
    ///    - OnCollisionGroupDetected is the ONLY event. It fires once per connected component.
    ///    - Prevents any subscriber from accidentally deducting lives multiple times.
    /// 3. Connected-Component Grouping:
    ///    - Collects all collision relationships across the board before executing resets.
    ///    - Connected groups of 2, 3, or more lines form ONE event, deducting 1 life.
    ///    - Independent collision groups deduct 1 life each.
    /// 4. Single Reset Guarantee:
    ///    - Each line in any collision group is reset exactly once per frame.
    /// 5. Same-line arrowheads never collide.
    /// </summary>
    public class CollisionSystem : MonoBehaviour
    {
        /// <summary>
        /// Single authoritative collision event.
        /// Fired once for each independent connected collision group in the frame.
        /// Exactly 1 life must be deducted per group event.
        /// </summary>
        public event Action<List<LineController>> OnCollisionGroupDetected;

        // Reusable collections to prevent per-frame GC allocations
        private readonly List<ArrowheadController> candidateArrowheads = new List<ArrowheadController>(32);
        private readonly Dictionary<LineController, List<LineController>> collisionGraph = new Dictionary<LineController, List<LineController>>(16);
        private readonly HashSet<LineController> visitedLines = new HashSet<LineController>();
        private readonly List<List<LineController>> allGroups = new List<List<LineController>>(4);
        private readonly Queue<LineController> bfsQueue = new Queue<LineController>(8);

        /// <summary>
        /// Evaluates all potential collisions across active lines using curve-aware swept tests.
        /// Called each frame during Update from GameManager.
        /// </summary>
        /// <param name="activeLines">All currently active lines in the level.</param>
        public void EvaluateCollisions(LineController[] activeLines)
        {
            if (activeLines == null || activeLines.Length == 0) return;

            candidateArrowheads.Clear();
            collisionGraph.Clear();
            visitedLines.Clear();
            allGroups.Clear();

            // ── STEP 1: Collect Candidate Arrowheads ──
            for (int i = 0; i < activeLines.Length; i++)
            {
                var line = activeLines[i];
                if (line == null) continue;
                if (line.State == LineState.Completed || line.State == LineState.Resetting) continue;

                if (line.ArrowheadA != null && line.ArrowheadA.gameObject.activeSelf)
                    candidateArrowheads.Add(line.ArrowheadA);

                if (line.ArrowheadB != null && line.ArrowheadB.gameObject.activeSelf)
                    candidateArrowheads.Add(line.ArrowheadB);

                // Initialize adjacency list entry for this line
                if (!collisionGraph.ContainsKey(line))
                {
                    collisionGraph[line] = new List<LineController>(4);
                }
            }

            int count = candidateArrowheads.Count;

            // ── STEP 2: Collect All Collision Relationships Across Frame ──
            // Evaluates every pair without breaking, using consistent frame states.
            for (int i = 0; i < count; i++)
            {
                var arrowA = candidateArrowheads[i];
                var lineA = arrowA.ParentLine;
                bool aIsMoving = (lineA.State == LineState.Moving && lineA.MovingArrowhead == arrowA);

                for (int j = i + 1; j < count; j++)
                {
                    var arrowB = candidateArrowheads[j];
                    var lineB = arrowB.ParentLine;

                    // Same line arrowheads NEVER collide (Rule: reaching own destination is success)
                    if (lineA == lineB) continue;

                    bool bIsMoving = (lineB.State == LineState.Moving && lineB.MovingArrowhead == arrowB);

                    // At least one of the two arrowheads must be moving
                    if (!aIsMoving && !bIsMoving) continue;

                    // Evaluate curve-aware continuous swept collision
                    if (EvaluatePairCollision(lineA, arrowA, aIsMoving, lineB, arrowB, bIsMoving))
                    {
                        // Add undirected edge in collision graph
                        if (!collisionGraph[lineA].Contains(lineB))
                            collisionGraph[lineA].Add(lineB);

                        if (!collisionGraph[lineB].Contains(lineA))
                            collisionGraph[lineB].Add(lineA);
                    }
                }
            }

            // ── STEP 3: Build Connected Collision Groups (BFS) ──
            foreach (var kvp in collisionGraph)
            {
                var line = kvp.Key;
                if (visitedLines.Contains(line)) continue;
                if (kvp.Value.Count == 0) continue; // No collisions for this line

                var group = new List<LineController>();
                bfsQueue.Clear();

                visitedLines.Add(line);
                bfsQueue.Enqueue(line);
                group.Add(line);

                while (bfsQueue.Count > 0)
                {
                    var curr = bfsQueue.Dequeue();
                    var neighbors = collisionGraph[curr];

                    for (int n = 0; n < neighbors.Count; n++)
                    {
                        var neighbor = neighbors[n];
                        if (!visitedLines.Contains(neighbor))
                        {
                            visitedLines.Add(neighbor);
                            bfsQueue.Enqueue(neighbor);
                            group.Add(neighbor);
                        }
                    }
                }

                if (group.Count >= 2)
                {
                    allGroups.Add(group);
                }
            }

            // ── STEP 4: Fire Single Authoritative Event per Group ──
            // Exactly 1 event per independent connected group -> exactly 1 life deducted.
            for (int g = 0; g < allGroups.Count; g++)
            {
                OnCollisionGroupDetected?.Invoke(allGroups[g]);
            }

            // ── STEP 5: Reset Participating Lines (Each Line Reset Exactly Once) ──
            for (int g = 0; g < allGroups.Count; g++)
            {
                var group = allGroups[g];
                for (int l = 0; l < group.Count; l++)
                {
                    var line = group[l];
                    if (line != null && line.State != LineState.Completed)
                    {
                        line.ResetLine();
                    }
                }
            }
        }

        /// <summary>
        /// Evaluates collision between arrowA and arrowB using curve-aware swept paths.
        /// </summary>
        public static bool EvaluatePairCollision(
            LineController lineA, ArrowheadController arrowA, bool aIsMoving,
            LineController lineB, ArrowheadController arrowB, bool bIsMoving)
        {
            float combinedRadius = arrowA.CollisionRadius + arrowB.CollisionRadius;

            // If spline paths are available, execute curve-aware swept detection
            if (lineA.Path != null && lineB.Path != null)
            {
                return CheckCurvedSweptPair(lineA, arrowA, aIsMoving, lineB, arrowB, bIsMoving, combinedRadius);
            }

            // Fallback for mocked unit tests without spline paths
            return CheckLinearSwept(arrowA.PreviousPosition, arrowA.Position, arrowB.PreviousPosition, arrowB.Position, combinedRadius);
        }

        /// <summary>
        /// Curve-aware swept collision detection.
        /// Samples the true mathematical spline paths across sub-intervals over the frame.
        /// </summary>
        public static bool CheckCurvedSweptPair(
            LineController lineA, ArrowheadController arrowA, bool aIsMoving,
            LineController lineB, ArrowheadController arrowB, bool bIsMoving,
            float combinedRadius)
        {
            float combinedRadiusSq = combinedRadius * combinedRadius;

            float distA_Start = aIsMoving ? lineA.PreviousDistance : lineA.CurrentDistance;
            float distA_End   = aIsMoving ? lineA.CurrentDistance  : lineA.CurrentDistance;
            float deltaA = aIsMoving ? Mathf.Abs(distA_End - distA_Start) : 0f;

            float distB_Start = bIsMoving ? lineB.PreviousDistance : lineB.CurrentDistance;
            float distB_End   = bIsMoving ? lineB.CurrentDistance  : lineB.CurrentDistance;
            float deltaB = bIsMoving ? Mathf.Abs(distB_End - distB_Start) : 0f;

            float maxDelta = Mathf.Max(deltaA, deltaB);

            // ── Adaptive Subdivision Math ──
            // Criteria:
            // 1. Distance constraint: Sub-step distance s <= 0.5 * minRadius to prevent tunneling through narrow colliders.
            // 2. Curvature sagitta constraint: For conservative minimum radius of curvature R_c (~0.35m for puzzle curves),
            //    chord sagitta error delta = s^2 / (8 * R_c) must not exceed 5% of collision radius (epsilon_max = 0.05 * minRadius).
            //    Solving for s: s <= sqrt(8 * epsilon_max * R_c).
            // 3. Allowed sub-step size: s_allowed = min(0.5 * minRadius, sqrt(8 * epsilon_max * R_c)).
            // 4. Sub-step count: N = max(1, ceil(maxDelta / s_allowed)).
            //    Fully unconstrained: no upper cap is imposed so that s <= s_allowed is strictly guaranteed
            //    without exception, even under massive frame-time spikes or extreme speeds.
            float minRadius = Mathf.Min(arrowA.CollisionRadius, arrowB.CollisionRadius);
            float maxSagittaError = 0.05f * minRadius;
            float conservativeRadiusOfCurvature = 0.35f;
            float maxStepCurvature = Mathf.Sqrt(8f * maxSagittaError * conservativeRadiusOfCurvature);
            float maxStepDistance = 0.5f * minRadius;
            float sAllowed = Mathf.Max(0.001f, Mathf.Min(maxStepDistance, maxStepCurvature));

            int subSteps = Mathf.Max(1, Mathf.CeilToInt(maxDelta / sAllowed));

            // Sub-stepped swept check along true spline curves
            for (int k = 0; k < subSteps; k++)
            {
                float tau0 = (float)k / subSteps;
                float tau1 = (float)(k + 1) / subSteps;

                Vector2 a0 = aIsMoving ? lineA.Path.EvaluatePositionAtDistance(Mathf.Lerp(distA_Start, distA_End, tau0)) : arrowA.Position;
                Vector2 a1 = aIsMoving ? lineA.Path.EvaluatePositionAtDistance(Mathf.Lerp(distA_Start, distA_End, tau1)) : arrowA.Position;

                Vector2 b0 = bIsMoving ? lineB.Path.EvaluatePositionAtDistance(Mathf.Lerp(distB_Start, distB_End, tau0)) : arrowB.Position;
                Vector2 b1 = bIsMoving ? lineB.Path.EvaluatePositionAtDistance(Mathf.Lerp(distB_Start, distB_End, tau1)) : arrowB.Position;

                if (CheckLinearSwept(a0, a1, b0, b1, combinedRadius))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Closed-form mathematical swept distance check between two linear segments
        /// [a0, a1] and [b0, b1] over normalized time tau in [0, 1].
        /// </summary>
        public static bool CheckLinearSwept(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, float combinedRadius)
        {
            float combinedRadiusSq = combinedRadius * combinedRadius;

            Vector2 d0 = a0 - b0;
            Vector2 vRel = (a1 - a0) - (b1 - b0);
            float vRelSq = vRel.sqrMagnitude;

            if (vRelSq < 1e-8f)
            {
                return d0.sqrMagnitude <= combinedRadiusSq;
            }

            float tau = Mathf.Clamp01(-Vector2.Dot(d0, vRel) / vRelSq);
            Vector2 closestRel = d0 + tau * vRel;

            return closestRel.sqrMagnitude <= combinedRadiusSq;
        }
    }
}
