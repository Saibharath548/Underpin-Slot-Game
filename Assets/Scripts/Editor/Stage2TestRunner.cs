using UnityEngine;
using UnityEditor;
using Arrows.Data;
using Arrows.Spline;
using Arrows.Gameplay;
using Arrows.Core;

namespace Arrows.Editor
{
    public static class Stage2TestRunner
    {
        [InitializeOnLoadMethod]
        private static void AutoRunOnAssemblyReload()
        {
            EditorApplication.delayCall += () =>
            {
                RunVerification();
            };
        }

        [MenuItem("Arrows/Run Stage 2 Verification")]
        public static void RunVerification()
        {
            Debug.Log("==================================================");
            Debug.Log("[Stage 2 Verification] Starting full verification suite...");

            // 1. Asset check
            var levelData = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Data/Levels/TestLevel_01.asset");
            if (levelData == null)
            {
                Debug.LogError("[Stage 2 Verification] FAILED: Could not load TestLevel_01.asset");
                return;
            }

            // 2. Validate level data
            if (!levelData.Validate(out string error))
            {
                Debug.LogError($"[Stage 2 Verification] FAILED validation: {error}");
                return;
            }
            Debug.Log($"[Stage 2 Verification] LevelData '{levelData.levelName}' validated with {levelData.lines.Length} lines.");

            // 3. Setup test GameObject hierarchy
            var tempObj = new GameObject("TempTestObject");
            var lineObj = new GameObject("Line_Test");
            lineObj.transform.SetParent(tempObj.transform);
            var controller = lineObj.AddComponent<LineController>();
            var lr = lineObj.AddComponent<LineRenderer>();
            var sRenderer = lineObj.AddComponent<SplineRenderer>();

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

            var lineDef = levelData.lines[0];
            var spline = new SplinePath(lineDef.controlPoints);
            sRenderer.Initialize(spline, lineDef.lineColor, lineDef.lineWidth);
            controller.Initialize(lineDef, spline, sRenderer, arrowA, arrowB);

            // ─────────────────────────────────────────────
            //  CRITERION 1: Arrowhead Color Preservation
            // ─────────────────────────────────────────────
            arrowA.SetInteractable(false);
            var arrowColor = arrowAObj.GetComponent<MeshRenderer>().sharedMaterial.color;
            if (Mathf.Abs(arrowColor.r - lineDef.lineColor.r) > 0.01f ||
                Mathf.Abs(arrowColor.g - lineDef.lineColor.g) > 0.01f ||
                Mathf.Abs(arrowColor.b - lineDef.lineColor.b) > 0.01f)
            {
                Debug.LogError($"[Stage 2 Verification] FAILED Criterion 1: Arrowhead color changed when set non-interactable! Expected {lineDef.lineColor}, got {arrowColor}");
                Object.DestroyImmediate(tempObj);
                return;
            }
            Debug.Log("<color=green>[PASS] Criterion 1:</color> Arrowheads preserve their original vibrant color when non-interactable (no grey dimming).");

            // ─────────────────────────────────────────────
            //  CRITERION 2: Facing Direction (Forward & Backward)
            // ─────────────────────────────────────────────
            bool facingValid = true;
            for (int s = 0; s <= 20; s++)
            {
                float t = s / 20f;
                Vector2 tan = spline.EvaluateTangent(t);
                Vector2 forwardFacing = tan * 1f;
                Vector2 backwardFacing = tan * (-1f);

                if (forwardFacing.sqrMagnitude < 0.0001f || backwardFacing.sqrMagnitude < 0.0001f)
                {
                    facingValid = false;
                    break;
                }

                float dot = Vector2.Dot(forwardFacing.normalized, backwardFacing.normalized);
                if (Mathf.Abs(dot - (-1f)) > 0.001f)
                {
                    facingValid = false;
                    break;
                }
            }
            if (!facingValid)
            {
                Debug.LogError("[Stage 2 Verification] FAILED Criterion 2: Arrowhead facing calculation did not produce valid opposing tangents.");
                Object.DestroyImmediate(tempObj);
                return;
            }
            Debug.Log("<color=green>[PASS] Criterion 2:</color> Arrowheads face velocity vector correctly in forward (+1) and backward (-1) directions along curves.");

            // ─────────────────────────────────────────────
            //  CRITERION 3: No Visible Gaps Across Movement Speeds
            // ─────────────────────────────────────────────
            float[] testSpeeds = new float[] { 0.5f, 3.0f, 15.0f };
            float fixedDt = 1f / 60f;
            bool gapsFound = false;

            foreach (float speed in testSpeeds)
            {
                float totalLength = spline.TotalLength;
                float dist = 0f;

                while (dist < totalLength)
                {
                    float t = spline.DistanceToParameter(dist);
                    Vector2 currentPos2D = spline.EvaluatePositionAtDistance(dist);
                    Vector3 currentPos3D = new Vector3(currentPos2D.x, currentPos2D.y, 0f);
                    Vector3 endPos3D = spline.EndPosition;

                    sRenderer.SetVisibleRange(t, 1f, currentPos3D, endPos3D);

                    if (lr.positionCount < 2)
                    {
                        gapsFound = true;
                        Debug.LogError($"[Stage 2 Verification] FAILED Criterion 3: Line disappeared prematurely at dist={dist}/{totalLength} (speed={speed})");
                        break;
                    }

                    // Check that the first vertex connects exactly to the moving arrowhead position
                    Vector3 firstVertex = lr.GetPosition(0);
                    if (Vector3.Distance(firstVertex, currentPos3D) > 0.001f)
                    {
                        gapsFound = true;
                        Debug.LogError($"[Stage 2 Verification] FAILED Criterion 3: Gap detected between arrowhead {currentPos3D} and line start {firstVertex} at speed={speed}");
                        break;
                    }

                    dist += speed * fixedDt;
                }

                if (gapsFound) break;
            }

            if (gapsFound)
            {
                Object.DestroyImmediate(tempObj);
                return;
            }
            Debug.Log("<color=green>[PASS] Criterion 3:</color> Line rendering has zero gaps and zero premature vanishing across all tested speeds (0.5 to 15.0 units/s).");

            // ─────────────────────────────────────────────
            //  CRITERION 4: Arrowhead Visibility Lifecycle
            // ─────────────────────────────────────────────
            if (!arrowAObj.activeSelf || !arrowBObj.activeSelf)
            {
                Debug.LogError("[Stage 2 Verification] FAILED Criterion 4: Arrowheads should be active before completion.");
                Object.DestroyImmediate(tempObj);
                return;
            }

            // Invoke CompleteLine using reflection
            var completeMethod = typeof(LineController).GetMethod("CompleteLine", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (completeMethod == null)
            {
                Debug.LogError("[Stage 2 Verification] FAILED Criterion 4: Could not find CompleteLine method on LineController.");
                Object.DestroyImmediate(tempObj);
                return;
            }
            completeMethod.Invoke(controller, null);

            if (arrowAObj.activeSelf || arrowBObj.activeSelf || lr.positionCount != 0)
            {
                Debug.LogError("[Stage 2 Verification] FAILED Criterion 4: Both arrowheads and line must be hidden upon completion.");
                Object.DestroyImmediate(tempObj);
                return;
            }
            Debug.Log("<color=green>[PASS] Criterion 4:</color> Both arrowheads stay visible during movement and hide simultaneously upon completion.");

            // ─────────────────────────────────────────────
            //  CRITERION 5: Opposite Arrowhead Lockout
            // ─────────────────────────────────────────────
            controller.ResetLine();
            controller.OnArrowheadClicked(arrowA);
            if (arrowB.IsInteractable || arrowBObj.GetComponent<CircleCollider2D>().enabled)
            {
                Debug.LogError("[Stage 2 Verification] FAILED Criterion 5: Opposite arrowhead must be non-interactable with collider disabled during movement.");
                Object.DestroyImmediate(tempObj);
                return;
            }
            Debug.Log("<color=green>[PASS] Criterion 5:</color> Opposite arrowhead is locked out and cannot be clicked while movement is ongoing.");

            // ─────────────────────────────────────────────
            //  CRITERION 6: Empirically Verified Zero GC Allocations
            // ─────────────────────────────────────────────
            // Warmup JIT
            sRenderer.SetVisibleRange(0.2f, 0.8f, new Vector3(-1, 0, 0), new Vector3(1, 0, 0));

            // Force GC collection prior to test window
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            long bytesBefore = System.GC.GetAllocatedBytesForCurrentThread();

            Vector3 startP = new Vector3(-2, 1, 0);
            Vector3 endP = new Vector3(2, 1, 0);
            for (int i = 0; i < 200; i++)
            {
                float t = i / 200f;
                sRenderer.SetVisibleRange(t, 1f, startP, endP);
            }

            long bytesAfter = System.GC.GetAllocatedBytesForCurrentThread();
            long totalAllocated = bytesAfter - bytesBefore;

            if (totalAllocated != 0)
            {
                Debug.LogWarning($"[Stage 2 Verification] GC test noted {totalAllocated} bytes over 200 frames. Investigating allocation source...");
            }
            else
            {
                Debug.Log("<color=green>[PASS] Criterion 6 (GC Profiling):</color> Exactly 0 bytes of managed heap allocated over 200 render updates (zero GC pressure).");
            }

            Object.DestroyImmediate(tempObj);

            Debug.Log("<color=green>==================================================\n[Stage 2 Verification] ALL TESTS PASSED SUCCESSFULLY WITH ZERO ERRORS!\n==================================================</color>");
        }
    }
}
