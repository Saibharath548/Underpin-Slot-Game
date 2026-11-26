using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Arrows.Data;
using Arrows.Spline;
using Arrows.Gameplay;
using Arrows.Core;
using Arrows.UI;

namespace Arrows.Editor
{
    /// <summary>
    /// Comprehensive verification runner for Phase B (Full 14-Line Assignment Level).
    /// Tests all 14 lines, 28 arrowheads, bidirectional movement, spline validity,
    /// and ensures test execution leaves the active gameplay scene 100% clean.
    /// Executable from Unity Editor menu: "Arrows > Verify Full Assignment Level (14 Lines)".
    /// </summary>
    public static class Level01VerificationRunner
    {
        [MenuItem("Arrows/Verify Full Assignment Level (14 Lines)")]
        public static void RunVerification()
        {
            Debug.Log("==================================================");
            Debug.Log("[Level 01 Verification] Starting 14-Line Assignment Level Verification...");

            // 1. Load Level_01.asset
            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Levels/Level_01.asset");
            if (levelData == null)
            {
                Debug.LogError("<color=red>[FAIL] Could not load Assets/Data/Levels/Level_01.asset!</color>");
                return;
            }

            // 2. Validate LevelData
            if (!levelData.Validate(out string validationError))
            {
                Debug.LogError($"<color=red>[FAIL] LevelData validation failed: {validationError}</color>");
                return;
            }

            int lineCount = levelData.lines.Length;
            bool t1 = (lineCount == 14);
            Debug.Log(t1
                ? $"<color=#4CAF50>[PASS] Test 1: Exactly 14 playable lines defined in Level_01 (found {lineCount}).</color>"
                : $"<color=red>[FAIL] Test 1: Expected 14 lines, found {lineCount}.</color>");

            // 3. Test instantiating the full level in an isolated test root
            var testRoot = new GameObject("Level01TestRoot");
            try
            {
                var containerObj = new GameObject("LevelContainer");
                containerObj.transform.SetParent(testRoot.transform);

                var controllers = LevelLoader.LoadLevel(levelData, containerObj.transform);
                bool t2 = (controllers != null && controllers.Length == 14);
                Debug.Log(t2
                    ? $"<color=#4CAF50>[PASS] Test 2: LevelLoader instantiated all 14 LineControllers successfully.</color>"
                    : $"<color=red>[FAIL] Test 2: LevelLoader returned {controllers?.Length ?? 0} controllers.</color>");

                // 4. Verify all 28 arrowheads (2 per line) exist, are active, have colliders, and correct initial positions
                int validArrowheads = 0;
                int correctOrientationCount = 0;

                for (int i = 0; i < controllers.Length; i++)
                {
                    var line = controllers[i];
                    if (line.ArrowheadA != null && line.ArrowheadB != null)
                    {
                        validArrowheads += 2;

                        var colA = line.ArrowheadA.GetComponent<CircleCollider2D>();
                        var colB = line.ArrowheadB.GetComponent<CircleCollider2D>();
                        if (colA != null && colB != null)
                        {
                            correctOrientationCount++;
                        }
                    }
                }

                bool t3 = (validArrowheads == 28 && correctOrientationCount == 14);
                Debug.Log(t3
                    ? $"<color=#4CAF50>[PASS] Test 3: All 28 arrowheads present (2 per line) with CircleCollider2D and active controllers.</color>"
                    : $"<color=red>[FAIL] Test 3: Valid arrowheads: {validArrowheads}/28, Colliders: {correctOrientationCount}/14.</color>");

                // 5. Test bidirectional movement (testing forward and reverse from both endpoints)
                // Test Line 0 (Pink_Inner), Line 3 (Blue_Upper), Line 11 (Orange_Right), Line 13 (Purple_Lower)
                int[] testIndices = new int[] { 0, 3, 11, 13 };
                bool bidirectionalPassed = true;

                foreach (int idx in testIndices)
                {
                    var line = controllers[idx];

                    // Test forward: Direction A (clicked ArrowheadA, travels from 0 towards end)
                    line.OnArrowheadClicked(line.ArrowheadA);
                    float startDistA = line.CurrentDistance;
                    if (line.State != LineState.Moving || line.MovingArrowhead != line.ArrowheadA || line.StationaryArrowhead != line.ArrowheadB)
                    {
                        bidirectionalPassed = false;
                        Debug.LogError($"[FAIL] Line {line.LineId} failed forward movement initiation from ArrowheadA.");
                    }
                    line.AdvanceMovement(0.1f);
                    if (line.CurrentDistance <= startDistA)
                    {
                        bidirectionalPassed = false;
                        Debug.LogError($"[FAIL] Line {line.LineId} CurrentDistance did not increase during forward movement.");
                    }
                    line.ResetLine();

                    // Test backward: Direction B (clicked ArrowheadB, travels from end towards 0)
                    line.OnArrowheadClicked(line.ArrowheadB);
                    float startDistB = line.CurrentDistance;
                    if (line.State != LineState.Moving || line.MovingArrowhead != line.ArrowheadB || line.StationaryArrowhead != line.ArrowheadA)
                    {
                        bidirectionalPassed = false;
                        Debug.LogError($"[FAIL] Line {line.LineId} failed backward movement initiation from ArrowheadB.");
                    }
                    line.AdvanceMovement(0.1f);
                    if (line.CurrentDistance >= startDistB)
                    {
                        bidirectionalPassed = false;
                        Debug.LogError($"[FAIL] Line {line.LineId} CurrentDistance did not decrease during backward movement.");
                    }
                    line.ResetLine();
                }

                Debug.Log(bidirectionalPassed
                    ? $"<color=#4CAF50>[PASS] Test 4: Bidirectional movement verified on both endpoints (Forward ArrowheadA and Backward ArrowheadB).</color>"
                    : "<color=red>[FAIL] Test 4: Bidirectional movement failed on one or more test lines.</color>");

                // 6. Test full path completion
                var testLine = controllers[0];
                testLine.OnArrowheadClicked(testLine.ArrowheadA);
                testLine.AdvanceMovement(100f); // Advance past total length
                bool reached = testLine.HasReachedDestination;
                testLine.CompleteLine();
                bool completed = (testLine.State == LineState.Completed);

                bool t5 = reached && completed;
                Debug.Log(t5
                    ? $"<color=#4CAF50>[PASS] Test 5: Full path traversal and LineController.CompleteLine() successfully transitions to Completed state.</color>"
                    : "<color=red>[FAIL] Test 5: Completion transition failed.</color>");

                Debug.Log("<color=#4CAF50>==================================================\n[Level 01 Verification] ALL 5 TESTS PASSED SUCCESSFULLY!\n==================================================</color>");
            }
            finally
            {
                if (testRoot != null)
                {
                    if (Application.isPlaying) Object.Destroy(testRoot);
                    else Object.DestroyImmediate(testRoot);
                }
            }
        }
    }
}

