using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Arrows.Data;
using Arrows.Spline;
using Arrows.Gameplay;
using Arrows.Core;

namespace Arrows.Editor
{
    public static class Stage3TestRunner
    {
        [MenuItem("Arrows/Run Stage 3 Verification")]
        public static void RunVerification()
        {
            Debug.Log("==================================================");
            Debug.Log("[Stage 3 Verification] Starting 7-Scenario Regression Suite (Curve-Aware Swept & Deterministic Pipeline)...");

            var root = new GameObject("Stage3TestRoot");

            try
            {
                // Setup LifeManager
                var lifeManagerObj = new GameObject("LifeManager");
                lifeManagerObj.transform.SetParent(root.transform);
                var lifeManager = lifeManagerObj.AddComponent<LifeManager>();
                lifeManager.Initialize(20);

                var collisionObj = new GameObject("CollisionSystem");
                collisionObj.transform.SetParent(root.transform);
                var collisionSystem = collisionObj.AddComponent<CollisionSystem>();

                int groupEventsCount = 0;
                List<List<LineController>> capturedGroups = new List<List<LineController>>();

                // Subscribe ONLY to the single authoritative event
                collisionSystem.OnCollisionGroupDetected += (group) =>
                {
                    groupEventsCount++;
                    capturedGroups.Add(new List<LineController>(group));
                    lifeManager.DeductLife();
                };

                // Setup test lines intersecting at (0, 0)
                // Length = 4.0m, speed = 4.0 m/s, midpoint (0, 0) at distance 2.0m.
                var lineA = CreateTestLine("LineA", Color.red, new Vector2(-2, 0), new Vector2(2, 0), root.transform);
                var lineB = CreateTestLine("LineB", Color.blue, new Vector2(0, -2), new Vector2(0, 2), root.transform);
                var lineC = CreateTestLine("LineC", Color.yellow, new Vector2(-1.4142f, -1.4142f), new Vector2(1.4142f, 1.4142f), root.transform);
                var lineD = CreateTestLine("LineD", Color.magenta, new Vector2(1.4142f, -1.4142f), new Vector2(-1.4142f, 1.4142f), root.transform);

                // Independent pair intersecting at (10, 10), length 4.0m, midpoint at 2.0m
                var lineE = CreateTestLine("LineE", Color.cyan, new Vector2(8, 10), new Vector2(12, 10), root.transform);
                var lineF = CreateTestLine("LineF", Color.gray, new Vector2(10, 8), new Vector2(10, 12), root.transform);

                // ─────────────────────────────────────────────
                //  TEST 1: Three-Way Connected Collision
                // ─────────────────────────────────────────────
                lineA.OnArrowheadClicked(lineA.ArrowheadA);
                lineB.OnArrowheadClicked(lineB.ArrowheadA);
                lineC.OnArrowheadClicked(lineC.ArrowheadA);

                // Step 1: Advance to approach junction (1.8m: dt = 0.45s)
                lineA.AdvanceMovement(0.45f);
                lineB.AdvanceMovement(0.45f);
                lineC.AdvanceMovement(0.45f);

                // Step 2: Swept collision frame across (0, 0) (0.4m: dt = 0.10s)
                lineA.AdvanceMovement(0.10f);
                lineB.AdvanceMovement(0.10f);
                lineC.AdvanceMovement(0.10f);

                int livesBefore = lifeManager.CurrentLives;
                groupEventsCount = 0;
                capturedGroups.Clear();

                collisionSystem.EvaluateCollisions(new LineController[] { lineA, lineB, lineC });

                bool test1Pass = (groupEventsCount == 1) &&
                                 (capturedGroups.Count > 0 && capturedGroups[0].Count == 3) &&
                                 (lifeManager.CurrentLives == livesBefore - 1) &&
                                 (lineA.State == LineState.Idle && lineB.State == LineState.Idle && lineC.State == LineState.Idle);

                if (test1Pass)
                {
                    Debug.Log($"<color=green>[PASS] Test 1 (Three-Way Connected Collision):</color> Detected 1 connected group of 3 lines. Exactly 1 life deducted ({livesBefore} -> {lifeManager.CurrentLives}). All 3 lines reset to Idle.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 1: groups={groupEventsCount}, groupCount={(capturedGroups.Count > 0 ? capturedGroups[0].Count : 0)}, lives={lifeManager.CurrentLives}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 2: Four-Way Connected Collision
                // ─────────────────────────────────────────────
                lineA.ResetLine(); lineB.ResetLine(); lineC.ResetLine(); lineD.ResetLine();
                lineA.OnArrowheadClicked(lineA.ArrowheadA);
                lineB.OnArrowheadClicked(lineB.ArrowheadA);
                lineC.OnArrowheadClicked(lineC.ArrowheadA);
                lineD.OnArrowheadClicked(lineD.ArrowheadA);

                lineA.AdvanceMovement(0.45f);
                lineB.AdvanceMovement(0.45f);
                lineC.AdvanceMovement(0.45f);
                lineD.AdvanceMovement(0.45f);

                lineA.AdvanceMovement(0.10f);
                lineB.AdvanceMovement(0.10f);
                lineC.AdvanceMovement(0.10f);
                lineD.AdvanceMovement(0.10f);

                livesBefore = lifeManager.CurrentLives;
                groupEventsCount = 0;
                capturedGroups.Clear();

                collisionSystem.EvaluateCollisions(new LineController[] { lineA, lineB, lineC, lineD });

                bool test2Pass = (groupEventsCount == 1) &&
                                 (capturedGroups.Count > 0 && capturedGroups[0].Count == 4) &&
                                 (lifeManager.CurrentLives == livesBefore - 1) &&
                                 (lineA.State == LineState.Idle && lineB.State == LineState.Idle && lineC.State == LineState.Idle && lineD.State == LineState.Idle);

                if (test2Pass)
                {
                    Debug.Log($"<color=green>[PASS] Test 2 (Four-Way Connected Collision):</color> Detected 1 connected group of 4 lines. Exactly 1 life deducted ({livesBefore} -> {lifeManager.CurrentLives}). All 4 lines reset to Idle.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 2: groups={groupEventsCount}, groupCount={(capturedGroups.Count > 0 ? capturedGroups[0].Count : 0)}, lives={lifeManager.CurrentLives}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 3: Two Independent Collision Pairs in the Same Frame
                // ─────────────────────────────────────────────
                lineA.ResetLine(); lineB.ResetLine(); lineE.ResetLine(); lineF.ResetLine();
                lineA.OnArrowheadClicked(lineA.ArrowheadA);
                lineB.OnArrowheadClicked(lineB.ArrowheadA);
                lineE.OnArrowheadClicked(lineE.ArrowheadA);
                lineF.OnArrowheadClicked(lineF.ArrowheadA);

                lineA.AdvanceMovement(0.45f);
                lineB.AdvanceMovement(0.45f);
                lineE.AdvanceMovement(0.45f);
                lineF.AdvanceMovement(0.45f);

                lineA.AdvanceMovement(0.10f);
                lineB.AdvanceMovement(0.10f);
                lineE.AdvanceMovement(0.10f);
                lineF.AdvanceMovement(0.10f);

                livesBefore = lifeManager.CurrentLives;
                groupEventsCount = 0;
                capturedGroups.Clear();

                collisionSystem.EvaluateCollisions(new LineController[] { lineA, lineB, lineE, lineF });

                bool test3Pass = (groupEventsCount == 2) &&
                                 (capturedGroups.Count == 2) &&
                                 (capturedGroups[0].Count == 2 && capturedGroups[1].Count == 2) &&
                                 (lifeManager.CurrentLives == livesBefore - 2) &&
                                 (lineA.State == LineState.Idle && lineB.State == LineState.Idle && lineE.State == LineState.Idle && lineF.State == LineState.Idle);

                if (test3Pass)
                {
                    Debug.Log($"<color=green>[PASS] Test 3 (Two Independent Collision Pairs):</color> Detected 2 separate groups. Exactly 2 lives deducted ({livesBefore} -> {lifeManager.CurrentLives}). All 4 lines reset once.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 3: groups={groupEventsCount}, lives={lifeManager.CurrentLives}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 4: One Connected Group and One Independent Group in Same Frame
                // ─────────────────────────────────────────────
                lineA.ResetLine(); lineB.ResetLine(); lineC.ResetLine(); lineE.ResetLine(); lineF.ResetLine();
                lineA.OnArrowheadClicked(lineA.ArrowheadA);
                lineB.OnArrowheadClicked(lineB.ArrowheadA);
                lineC.OnArrowheadClicked(lineC.ArrowheadA);
                lineE.OnArrowheadClicked(lineE.ArrowheadA);
                lineF.OnArrowheadClicked(lineF.ArrowheadA);

                lineA.AdvanceMovement(0.45f);
                lineB.AdvanceMovement(0.45f);
                lineC.AdvanceMovement(0.45f);
                lineE.AdvanceMovement(0.45f);
                lineF.AdvanceMovement(0.45f);

                lineA.AdvanceMovement(0.10f);
                lineB.AdvanceMovement(0.10f);
                lineC.AdvanceMovement(0.10f);
                lineE.AdvanceMovement(0.10f);
                lineF.AdvanceMovement(0.10f);

                livesBefore = lifeManager.CurrentLives;
                groupEventsCount = 0;
                capturedGroups.Clear();

                collisionSystem.EvaluateCollisions(new LineController[] { lineA, lineB, lineC, lineE, lineF });

                bool test4Pass = (groupEventsCount == 2) &&
                                 (capturedGroups.Count == 2) &&
                                 (lifeManager.CurrentLives == livesBefore - 2) &&
                                 (lineA.State == LineState.Idle && lineB.State == LineState.Idle && lineC.State == LineState.Idle &&
                                  lineE.State == LineState.Idle && lineF.State == LineState.Idle);

                if (test4Pass)
                {
                    Debug.Log($"<color=green>[PASS] Test 4 (Connected Group + Independent Group):</color> Detected 2 groups (sizes {capturedGroups[0].Count} and {capturedGroups[1].Count}). Exactly 2 lives deducted ({livesBefore} -> {lifeManager.CurrentLives}). All 5 lines reset.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 4: groups={groupEventsCount}, lives={lifeManager.CurrentLives}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 5: Moving-versus-Stationary on Curved Spline (Full EvaluateCollisions Pipeline)
                // ─────────────────────────────────────────────
                var curveDef1 = new LineDefinition
                {
                    lineId = "LineCurved1",
                    lineColor = Color.green,
                    lineWidth = 0.08f,
                    controlPoints = new Vector2[]
                    {
                        new Vector2(-1.5f, -0.5f),
                        new Vector2(-1.0f, 0.0f),
                        new Vector2(0.0f, 1.0f),
                        new Vector2(1.0f, 0.0f),
                        new Vector2(1.5f, -0.5f)
                    },
                    arrowheadScale = 0.4f,
                    movementSpeed = 4f,
                    collisionRadius = 0.25f
                };
                var lineCurved1 = CreateTestLineCustomDef(curveDef1, root.transform);

                var obstacleDef = new LineDefinition
                {
                    lineId = "ObstacleAtApex",
                    lineColor = Color.white,
                    lineWidth = 0.08f,
                    controlPoints = new Vector2[] { new Vector2(0f, 0.95f), new Vector2(0f, 2.0f) },
                    arrowheadScale = 0.4f,
                    movementSpeed = 4f,
                    collisionRadius = 0.25f
                };
                var obstacleLine = CreateTestLineCustomDef(obstacleDef, root.transform);

                lineCurved1.OnArrowheadClicked(lineCurved1.ArrowheadA);
                float apexDist = lineCurved1.Path.TotalLength * 0.5f;

                // Advance with AdvanceMovement to approach apex
                float dtApproach = (apexDist - 0.2f) / curveDef1.movementSpeed;
                lineCurved1.AdvanceMovement(dtApproach);

                // Advance swept frame across apex
                float dtSweep = 0.4f / curveDef1.movementSpeed;
                lineCurved1.AdvanceMovement(dtSweep);

                livesBefore = lifeManager.CurrentLives;
                groupEventsCount = 0;
                capturedGroups.Clear();

                collisionSystem.EvaluateCollisions(new LineController[] { lineCurved1, obstacleLine });

                bool test5Pass = (groupEventsCount == 1) &&
                                 (lifeManager.CurrentLives == livesBefore - 1) &&
                                 (lineCurved1.State == LineState.Idle && obstacleLine.State == LineState.Idle);

                if (test5Pass)
                {
                    Debug.Log($"<color=green>[PASS] Test 5 (Moving-vs-Stationary Curved Spline - Full Pipeline):</color> Complete EvaluateCollisions() pipeline detected apex collision. Fired group event, deducted 1 life ({livesBefore} -> {lifeManager.CurrentLives}), and reset both lines.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 5: groups={groupEventsCount}, lives={lifeManager.CurrentLives}, lineCurved1State={lineCurved1.State}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 6: Moving-versus-Moving on Curved Splines (Full EvaluateCollisions Pipeline)
                // ─────────────────────────────────────────────
                var curveDef2 = new LineDefinition
                {
                    lineId = "LineCurved2",
                    lineColor = Color.magenta,
                    lineWidth = 0.08f,
                    controlPoints = new Vector2[]
                    {
                        new Vector2(-0.5f, -1.5f),
                        new Vector2(0.0f, -1.0f),
                        new Vector2(0.0f, 1.0f),
                        new Vector2(0.0f, 1.5f)
                    },
                    arrowheadScale = 0.4f,
                    movementSpeed = 4f,
                    collisionRadius = 0.25f
                };
                var lineCurved2 = CreateTestLineCustomDef(curveDef2, root.transform);

                lineCurved1.ResetLine();
                lineCurved2.ResetLine();

                lineCurved1.OnArrowheadClicked(lineCurved1.ArrowheadA);
                lineCurved2.OnArrowheadClicked(lineCurved2.ArrowheadA);

                float apexDist2 = lineCurved2.Path.TotalLength * 0.75f;

                lineCurved1.AdvanceMovement((apexDist - 0.2f) / curveDef1.movementSpeed);
                lineCurved2.AdvanceMovement((apexDist2 - 0.2f) / curveDef2.movementSpeed);

                lineCurved1.AdvanceMovement(0.4f / curveDef1.movementSpeed);
                lineCurved2.AdvanceMovement(0.4f / curveDef2.movementSpeed);

                livesBefore = lifeManager.CurrentLives;
                groupEventsCount = 0;
                capturedGroups.Clear();

                collisionSystem.EvaluateCollisions(new LineController[] { lineCurved1, lineCurved2 });

                bool test6Pass = (groupEventsCount == 1) &&
                                 (lifeManager.CurrentLives == livesBefore - 1) &&
                                 (lineCurved1.State == LineState.Idle && lineCurved2.State == LineState.Idle);

                if (test6Pass)
                {
                    Debug.Log($"<color=green>[PASS] Test 6 (Moving-vs-Moving Curved Splines - Full Pipeline):</color> Complete EvaluateCollisions() detected intersection of two simultaneously curved swept paths. Deducted 1 life ({livesBefore} -> {lifeManager.CurrentLives}) and reset both lines.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 6: groups={groupEventsCount}, lives={lifeManager.CurrentLives}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 7: Destination-Frame Collision Resolution
                // ─────────────────────────────────────────────
                lineA.ResetLine();
                lineB.ResetLine();

                lineB.ArrowheadA.SnapToPosition(new Vector2(2f, 0f), Vector2.up);
                lineA.OnArrowheadClicked(lineA.ArrowheadA);

                // Phase 1: Advance lineA to destination in one step
                lineA.AdvanceMovement(10f);
                bool reachedFlag = lineA.HasReachedDestination;

                livesBefore = lifeManager.CurrentLives;
                groupEventsCount = 0;

                // Phase 2: Evaluate collisions
                collisionSystem.EvaluateCollisions(new LineController[] { lineA, lineB });

                // Phase 3: Finalize completions
                if (lineA.State == LineState.Moving && lineA.HasReachedDestination)
                {
                    lineA.CompleteLine();
                }

                bool test7Pass = reachedFlag &&
                                 (groupEventsCount == 1) &&
                                 (lifeManager.CurrentLives == livesBefore - 1) &&
                                 (lineA.State == LineState.Idle) &&
                                 (!lineA.HasReachedDestination);

                if (test7Pass)
                {
                    Debug.Log($"<color=green>[PASS] Test 7 (Destination-Frame Collision Resolution):</color> Final movement step evaluated before completion. Arrival collision detected -> Line reset to Idle instead of false completion.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 7: reachedFlag={reachedFlag}, lineAState={lineA.State}, groups={groupEventsCount}");
                    return;
                }

                Debug.Log("<color=green>==================================================\n[Stage 3 Verification] ALL 7 REGRESSION TESTS PASSED CLEANLY WITH ZERO ERRORS!\n==================================================</color>");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static LineController CreateTestLine(string id, Color color, Vector2 start, Vector2 end, Transform parent)
        {
            var def = new LineDefinition
            {
                lineId = id,
                lineColor = color,
                lineWidth = 0.08f,
                controlPoints = new Vector2[] { start, (start + end) * 0.5f, end },
                arrowheadScale = 0.4f,
                movementSpeed = 4f,
                collisionRadius = 0.25f
            };
            return CreateTestLineCustomDef(def, parent);
        }

        private static LineController CreateTestLineCustomDef(LineDefinition def, Transform parent)
        {
            var lineObj = new GameObject($"Line_{def.lineId}");
            lineObj.transform.SetParent(parent);
            var lr = lineObj.AddComponent<LineRenderer>();
            var sRenderer = lineObj.AddComponent<SplineRenderer>();
            var controller = lineObj.AddComponent<LineController>();

            var arrowAObj = new GameObject("ArrowA");
            arrowAObj.transform.SetParent(lineObj.transform);
            arrowAObj.AddComponent<MeshFilter>();
            arrowAObj.AddComponent<MeshRenderer>();
            arrowAObj.AddComponent<CircleCollider2D>();
            var arrowA = arrowAObj.AddComponent<ArrowheadController>();

            var arrowBObj = new GameObject("ArrowB");
            arrowBObj.transform.SetParent(lineObj.transform);
            arrowBObj.AddComponent<MeshFilter>();
            arrowBObj.AddComponent<MeshRenderer>();
            arrowBObj.AddComponent<CircleCollider2D>();
            var arrowB = arrowBObj.AddComponent<ArrowheadController>();

            var spline = new SplinePath(def.controlPoints);
            sRenderer.Initialize(spline, def.lineColor, def.lineWidth);
            controller.Initialize(def, spline, sRenderer, arrowA, arrowB);

            return controller;
        }
    }
}
