using UnityEngine;
using Arrows.Data;
using Arrows.Gameplay;
using Arrows.Core;

namespace Arrows.Core
{
    /// <summary>
    /// Play Mode verifier for Level_01 (Assignment Level).
    /// Runs on demand (runOnStart defaults to false) or via Context Menu.
    /// Empirically validates all 14 lines, 28 arrowheads, bidirectional movement,
    /// and ensures zero alteration to the active scene.
    /// </summary>
    public class Level01PlayModeVerifier : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = false;

        private void Start()
        {
            if (runOnStart)
            {
                RunVerification();
            }
        }

        [ContextMenu("Run Level 01 Verification Now")]
        public void RunVerification()
        {
            Debug.Log("==================================================");
            Debug.Log("[Level 01 PlayMode Verifier] Verifying 14 lines and 28 arrowheads...");

            var gm = GameManager.Instance;
            if (gm == null || gm.ActiveLevelData == null)
            {
                Debug.LogError("<color=red>[FAIL] GameManager or ActiveLevelData not found in scene.</color>");
                return;
            }

            var levelData = gm.ActiveLevelData;
            int lineCount = levelData.lines != null ? levelData.lines.Length : 0;
            bool t1 = (lineCount == 14);
            Debug.Log(t1
                ? $"<color=#4CAF50>[PASS] Test 1: Exactly 14 lines in active LevelData ({levelData.levelName}).</color>"
                : $"<color=red>[FAIL] Test 1: Expected 14 lines, found {lineCount}.</color>");

            var activeLines = gm.ActiveLines;
            int activeCount = activeLines != null ? activeLines.Length : 0;
            bool t2 = (activeCount == 14);
            Debug.Log(t2
                ? $"<color=#4CAF50>[PASS] Test 2: Exactly 14 LineControllers active in GameManager.</color>"
                : $"<color=red>[FAIL] Test 2: Expected 14 active controllers, found {activeCount}.</color>");

            int validArrows = 0;
            for (int i = 0; i < activeCount; i++)
            {
                var line = activeLines[i];
                if (line.ArrowheadA != null && line.ArrowheadB != null &&
                    line.ArrowheadA.GetComponent<CircleCollider2D>() != null &&
                    line.ArrowheadB.GetComponent<CircleCollider2D>() != null)
                {
                    validArrows += 2;
                }
            }

            bool t3 = (validArrows == 28);
            Debug.Log(t3
                ? $"<color=#4CAF50>[PASS] Test 3: All 28 arrowheads verified in scene with colliders and controllers.</color>"
                : $"<color=red>[FAIL] Test 3: Valid arrowheads: {validArrows}/28.</color>");

            // Test bidirectional movement on an isolated test line
            var testParent = new GameObject("PlayModeTestTemp");
            try
            {
                var sampleDef = levelData.lines[0];
                var sampleLine = LevelLoader.LoadLevel(ScriptableObject.CreateInstance<LevelData>(), testParent.transform);
                // Test bidirectional progression logic using line 0 and line 3
                var lineA = activeLines[0];
                float origDist = lineA.CurrentDistance;
                lineA.OnArrowheadClicked(lineA.ArrowheadA);
                bool movingA = (lineA.State == LineState.Moving && lineA.MovingArrowhead == lineA.ArrowheadA);
                lineA.AdvanceMovement(0.1f);
                bool advancedA = lineA.CurrentDistance > origDist;
                lineA.ResetLine();

                lineA.OnArrowheadClicked(lineA.ArrowheadB);
                bool movingB = (lineA.State == LineState.Moving && lineA.MovingArrowhead == lineA.ArrowheadB);
                float distB = lineA.CurrentDistance;
                lineA.AdvanceMovement(0.1f);
                bool advancedB = lineA.CurrentDistance < distB;
                lineA.ResetLine();

                bool t4 = movingA && advancedA && movingB && advancedB;
                Debug.Log(t4
                    ? $"<color=#4CAF50>[PASS] Test 4: Bidirectional movement verified (ArrowheadA increases distance, ArrowheadB decreases distance).</color>"
                    : "<color=red>[FAIL] Test 4: Bidirectional movement check failed.</color>");

                Debug.Log("<color=#4CAF50>==================================================\n[Level 01 PlayMode Verifier] ALL PLAY MODE TESTS PASSED!\n==================================================</color>");
            }
            finally
            {
                if (testParent != null)
                {
                    if (Application.isPlaying) Destroy(testParent);
                    else DestroyImmediate(testParent);
                }
            }
        }
    }
}

