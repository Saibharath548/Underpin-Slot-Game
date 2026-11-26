using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using System.Collections.Generic;
using Arrows.Data;
using Arrows.Spline;
using Arrows.Gameplay;
using Arrows.Core;
using Arrows.UI;

namespace Arrows.Editor
{
    /// <summary>
    /// Comprehensive verification suite for Stage 4 (Game State, UI, Hearts, Win/Loss Popups, Retry).
    /// Tests are categorized into Callback tests, Simulated UI Event tests, and System Integration tests.
    /// Executable from Unity Editor menu: "Arrows > Run Stage 4 Verification".
    /// </summary>
    public static class Stage4TestRunner
    {
        [MenuItem("Arrows/Run Stage 4 Verification")]
        public static void RunVerification()
        {
            Debug.Log("==================================================");
            Debug.Log("[Stage 4 Verification] Starting Rigorous Game State & UI Verification Suite...");

            // Preserve singleton references to restore on cleanup
            var previousGmInstance = GameManager.Instance;
            var previousUiInstance = UIManager.Instance;

            var root = new GameObject("Stage4EditorTestRoot");

            try
            {
                // Ensure a valid EventSystem is present and active for all UI tests
                var es = EventSystem.current;
                if (es == null)
                {
                    es = Object.FindAnyObjectByType<EventSystem>();
                }
                if (es == null)
                {
                    var esObj = new GameObject("TestEventSystem");
                    esObj.transform.SetParent(root.transform);
                    es = esObj.AddComponent<EventSystem>();
                    esObj.AddComponent<InputSystemUIInputModule>();
                }
                EventSystem.current = es;

                // ─────────────────────────────────────────────
                //  TEST 1: [CALLBACK & VISUAL TEST] LivesDisplay Reactivity & Heart States
                // ─────────────────────────────────────────────
                var livesObj = new GameObject("TestLivesDisplay");
                livesObj.transform.SetParent(root.transform);
                var livesDisplay = livesObj.AddComponent<LivesDisplay>();
                livesDisplay.Initialize(3);

                var lifeManagerObj = new GameObject("TestLifeManager");
                lifeManagerObj.transform.SetParent(root.transform);
                var lifeManager = lifeManagerObj.AddComponent<LifeManager>();
                lifeManager.Initialize(3);

                lifeManager.OnLifeChanged += (c) => livesDisplay.SetLives(c, lifeManager.StartingLives);

                bool t1_init = (lifeManager.CurrentLives == 3) && (livesDisplay.ActiveHeartCount == 3) && (livesDisplay.TotalHeartCount == 3);
                lifeManager.DeductLife();
                bool t1_one = (lifeManager.CurrentLives == 2) && (livesDisplay.ActiveHeartCount == 2);
                lifeManager.DeductLife();
                bool t1_two = (lifeManager.CurrentLives == 1) && (livesDisplay.ActiveHeartCount == 1);

                if (t1_init && t1_one && t1_two)
                {
                    Debug.Log("<color=green>[PASS] Test 1 [CALLBACK & VISUAL TEST]:</color> Hearts reflect life changes reactively (3 -> 2 -> 1). Heart count synchronized.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 1: init={t1_init}, afterOne={t1_one}, afterTwo={t1_two}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 2: [SYSTEM INTEGRATION TEST] EventSystem & InputSystemUIInputModule Verification
                // ─────────────────────────────────────────────
                var inputModule = es.GetComponent<InputSystemUIInputModule>();
                bool t2Pass = (es != null) && (inputModule != null) && inputModule.isActiveAndEnabled;
                if (t2Pass)
                {
                    Debug.Log("<color=green>[PASS] Test 2 [SYSTEM INTEGRATION TEST]:</color> EventSystem and InputSystemUIInputModule verified present and active.");
                }
                else
                {
                    Debug.LogError("[FAIL] Test 2: EventSystem or InputSystemUIInputModule missing or disabled.");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 3: [SYSTEM INTEGRATION TEST] GameManager Clean Init & Win Transition
                // ─────────────────────────────────────────────
                var gmObj = new GameObject("TestGameManager");
                gmObj.transform.SetParent(root.transform);
                var gm = gmObj.AddComponent<GameManager>();

                var testLevel = ScriptableObject.CreateInstance<LevelData>();
                testLevel.levelName = "Stage4TestLevel";
                testLevel.startingLives = 3;
                testLevel.lines = new LineDefinition[]
                {
                    new LineDefinition
                    {
                        lineId = "Line1",
                        lineColor = Color.blue,
                        lineWidth = 0.08f,
                        controlPoints = new Vector2[] { new Vector2(-2, 0), new Vector2(0, 0), new Vector2(2, 0) },
                        arrowheadScale = 0.4f,
                        movementSpeed = 4f,
                        collisionRadius = 0.25f
                    },
                    new LineDefinition
                    {
                        lineId = "Line2",
                        lineColor = Color.red,
                        lineWidth = 0.08f,
                        controlPoints = new Vector2[] { new Vector2(0, -2), new Vector2(0, 0), new Vector2(0, 2) },
                        arrowheadScale = 0.4f,
                        movementSpeed = 4f,
                        collisionRadius = 0.25f
                    }
                };

                gm.Initialize(testLevel);

                bool wonFired = false;
                gm.OnLevelWon += () => wonFired = true;

                // Complete Line 1
                gm.ActiveLines[0].OnArrowheadClicked(gm.ActiveLines[0].ArrowheadA);
                gm.ActiveLines[0].CompleteLine();
                bool midPlayingState = gm.CurrentState == GameState.Playing;

                // Complete Line 2
                gm.ActiveLines[1].OnArrowheadClicked(gm.ActiveLines[1].ArrowheadA);
                gm.ActiveLines[1].CompleteLine();

                bool t3Pass = wonFired && (gm.CurrentState == GameState.Won) && midPlayingState;
                if (t3Pass)
                {
                    Debug.Log("<color=green>[PASS] Test 3 [SYSTEM INTEGRATION TEST]:</color> Clean GameManager initialization -> Win condition evaluated -> OnLevelWon fired.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 3: wonFired={wonFired}, state={gm.CurrentState}, midPlayingState={midPlayingState}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 4: [SIMULATED UI EVENT TEST] Actual UI Pointer Event Dispatch
                // ─────────────────────────────────────────────
                var popupObj = new GameObject("TestPopup");
                popupObj.transform.SetParent(root.transform);
                var popup = popupObj.AddComponent<GameOverPopup>();

                bool uiEventDispatched = false;
                popup.Show(
                    title: "VICTORY!",
                    subtitle: "All lines cleared successfully.",
                    buttonLabel: "Play Again",
                    titleColor: Color.green,
                    onButtonClick: () => uiEventDispatched = true
                );

                bool popupShowingBefore = popup.IsShowing;
                Button realButton = popup.ActionButton;

                var pointerData = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, realButton.transform.position)
                };

                ExecuteEvents.ExecuteHierarchy(realButton.gameObject, pointerData, ExecuteEvents.pointerClickHandler);

                bool popupHiddenAfter = !popup.IsShowing;
                bool t4Pass = popupShowingBefore && uiEventDispatched && popupHiddenAfter;

                if (t4Pass)
                {
                    Debug.Log("<color=green>[PASS] Test 4 [SIMULATED UI EVENT TEST]:</color> Unity UI Event Dispatch System (ExecuteEvents.ExecuteHierarchy) successfully clicked Button and dismissed popup.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 4: showingBefore={popupShowingBefore}, dispatched={uiEventDispatched}, hiddenAfter={popupHiddenAfter}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 5: [SIMULATED UI EVENT TEST] UI Input Blocking Over Arrowheads
                // ─────────────────────────────────────────────
                gm.RestartLevel();
                var underlyingLine = gm.ActiveLines[0];
                var underlyingArrow = underlyingLine.ArrowheadA;

                popup.Show("OUT OF LIVES!", "Try again.", "Try Again", Color.red, () => gm.RestartLevel());

                Vector2 arrowScreenPos = Camera.main != null
                    ? (Vector2)Camera.main.WorldToScreenPoint(underlyingArrow.transform.position)
                    : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                var raycastResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = arrowScreenPos }, raycastResults);

                bool uiInterceptedRaycast = raycastResults.Count > 0 && raycastResults[0].gameObject.transform.IsChildOf(popup.transform);
                bool clickAccepted = gm.ProcessPointerClick(arrowScreenPos);
                bool arrowIgnored = (underlyingLine.State == LineState.Idle);

                bool t5Pass = uiInterceptedRaycast && (!clickAccepted) && arrowIgnored;
                if (t5Pass)
                {
                    Debug.Log("<color=green>[PASS] Test 5 [SIMULATED UI EVENT TEST]:</color> UI Backdrop blocked pointer raycast. GameManager rejected click; underlying arrowhead remained Idle.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 5: uiIntercepted={uiInterceptedRaycast}, clickAccepted={clickAccepted}, arrowState={underlyingLine.State}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 6: [SIMULATED UI EVENT TEST] Complete Retry Flow via UI Event Dispatch
                // ─────────────────────────────────────────────
                var retryPointerData = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, popup.ActionButton.transform.position)
                };
                ExecuteEvents.ExecuteHierarchy(popup.ActionButton.gameObject, retryPointerData, ExecuteEvents.pointerClickHandler);

                bool t6Pass = (gm.CurrentState == GameState.Playing) &&
                              (gm.LifeManager.CurrentLives == 3) &&
                              (!popup.IsShowing) &&
                              (gm.ActiveLines[0].State == LineState.Idle) &&
                              (gm.ActiveLines[1].State == LineState.Idle);

                if (t6Pass)
                {
                    Debug.Log("<color=green>[PASS] Test 6 [SIMULATED UI EVENT TEST]:</color> Clicking Retry button via UI Event System restored Playing state, full lives (3/3), and Idle lines.");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 6: state={gm.CurrentState}, lives={gm.LifeManager.CurrentLives}, popupShowing={popup.IsShowing}");
                    return;
                }

                // ─────────────────────────────────────────────
                //  TEST 7: [ACTUAL SYSTEM INTEGRATION TEST] Full End-to-End Gameplay:
                //  Real Swept Collisions via AdvanceMovement -> CollisionSystem.EvaluateCollisions -> GameManager -> LifeManager ->
                //  LivesDisplay -> GameOverPopup -> UI Event Dispatch Retry -> Restored Gameplay!
                // ─────────────────────────────────────────────
                var uiObj = new GameObject("TestUIManager");
                uiObj.transform.SetParent(root.transform);
                var uiManager = uiObj.AddComponent<UIManager>();
                uiManager.Initialize(gm);

                var multiLevel = ScriptableObject.CreateInstance<LevelData>();
                multiLevel.levelName = "CollisionTestLevel";
                multiLevel.startingLives = 3;
                multiLevel.lines = new LineDefinition[]
                {
                    new LineDefinition
                    {
                        lineId = "LineA1", lineColor = Color.blue, lineWidth = 0.08f,
                        controlPoints = new Vector2[] { new Vector2(-2, 0), new Vector2(0, 0), new Vector2(2, 0) },
                        arrowheadScale = 0.4f, movementSpeed = 4f, collisionRadius = 0.25f
                    },
                    new LineDefinition
                    {
                        lineId = "LineA2", lineColor = Color.red, lineWidth = 0.08f,
                        controlPoints = new Vector2[] { new Vector2(0, -2), new Vector2(0, 0), new Vector2(0, 2) },
                        arrowheadScale = 0.4f, movementSpeed = 4f, collisionRadius = 0.25f
                    },
                    new LineDefinition
                    {
                        lineId = "LineB1", lineColor = Color.green, lineWidth = 0.08f,
                        controlPoints = new Vector2[] { new Vector2(-2, 4), new Vector2(0, 4), new Vector2(2, 4) },
                        arrowheadScale = 0.4f, movementSpeed = 4f, collisionRadius = 0.25f
                    },
                    new LineDefinition
                    {
                        lineId = "LineB2", lineColor = Color.yellow, lineWidth = 0.08f,
                        controlPoints = new Vector2[] { new Vector2(0, 2), new Vector2(0, 4), new Vector2(0, 6) },
                        arrowheadScale = 0.4f, movementSpeed = 4f, collisionRadius = 0.25f
                    }
                };

                // Safely load the 4-line collision level using GameManager.LoadLevel
                gm.LoadLevel(multiLevel);

                bool initCheck = (gm.LifeManager.CurrentLives == 3) && (uiManager.LivesDisplay.ActiveHeartCount == 3);

                // ── Real Collision 1: LineA1 and LineA2 sweep across (0, 0) via AdvanceMovement ──
                gm.ActiveLines[0].OnArrowheadClicked(gm.ActiveLines[0].ArrowheadA);
                gm.ActiveLines[1].OnArrowheadClicked(gm.ActiveLines[1].ArrowheadA);
                // Step 1: approach junction (1.8m: dt = 0.45s)
                gm.ActiveLines[0].AdvanceMovement(0.45f);
                gm.ActiveLines[1].AdvanceMovement(0.45f);
                // Step 2: swept collision frame across (0, 0) (0.4m: dt = 0.10s)
                gm.ActiveLines[0].AdvanceMovement(0.10f);
                gm.ActiveLines[1].AdvanceMovement(0.10f);

                // Run real CollisionSystem evaluation
                gm.CollisionSystem.EvaluateCollisions(new LineController[] { gm.ActiveLines[0], gm.ActiveLines[1] });

                bool col1Pass = (gm.LifeManager.CurrentLives == 2) &&
                                (uiManager.LivesDisplay.ActiveHeartCount == 2) &&
                                (gm.ActiveLines[0].State == LineState.Idle) &&
                                (gm.ActiveLines[1].State == LineState.Idle);

                // ── Real Collision 2: LineB1 and LineB2 sweep across (0, 4) via AdvanceMovement ──
                gm.ActiveLines[2].OnArrowheadClicked(gm.ActiveLines[2].ArrowheadA);
                gm.ActiveLines[3].OnArrowheadClicked(gm.ActiveLines[3].ArrowheadA);
                gm.ActiveLines[2].AdvanceMovement(0.45f);
                gm.ActiveLines[3].AdvanceMovement(0.45f);
                gm.ActiveLines[2].AdvanceMovement(0.10f);
                gm.ActiveLines[3].AdvanceMovement(0.10f);

                gm.CollisionSystem.EvaluateCollisions(new LineController[] { gm.ActiveLines[2], gm.ActiveLines[3] });

                bool col2Pass = (gm.LifeManager.CurrentLives == 1) &&
                                (uiManager.LivesDisplay.ActiveHeartCount == 1) &&
                                (gm.ActiveLines[2].State == LineState.Idle) &&
                                (gm.ActiveLines[3].State == LineState.Idle);

                // ── Real Collision 3: LineA1 and LineA2 sweep across (0, 0) again -> Depletes final life ──
                gm.ActiveLines[0].OnArrowheadClicked(gm.ActiveLines[0].ArrowheadA);
                gm.ActiveLines[1].OnArrowheadClicked(gm.ActiveLines[1].ArrowheadA);
                gm.ActiveLines[0].AdvanceMovement(0.45f);
                gm.ActiveLines[1].AdvanceMovement(0.45f);
                gm.ActiveLines[0].AdvanceMovement(0.10f);
                gm.ActiveLines[1].AdvanceMovement(0.10f);

                gm.CollisionSystem.EvaluateCollisions(new LineController[] { gm.ActiveLines[0], gm.ActiveLines[1] });

                bool col3Pass = (gm.LifeManager.CurrentLives == 0) &&
                                (uiManager.LivesDisplay.ActiveHeartCount == 0) &&
                                (gm.CurrentState == GameState.Lost) &&
                                (uiManager.GameOverPopup.IsShowing);

                // ── Click Retry button on UIManager's popup via UI Event System ──
                var finalRetryPointer = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, uiManager.GameOverPopup.ActionButton.transform.position)
                };
                ExecuteEvents.ExecuteHierarchy(uiManager.GameOverPopup.ActionButton.gameObject, finalRetryPointer, ExecuteEvents.pointerClickHandler);

                bool restoredPass = (gm.CurrentState == GameState.Playing) &&
                                    (gm.LifeManager.CurrentLives == 3) &&
                                    (uiManager.LivesDisplay.ActiveHeartCount == 3) &&
                                    (!uiManager.GameOverPopup.IsShowing) &&
                                    (gm.ActiveLines[0].State == LineState.Idle) &&
                                    (gm.ActiveLines[1].State == LineState.Idle);

                bool t7Pass = initCheck && col1Pass && col2Pass && col3Pass && restoredPass;
                if (t7Pass)
                {
                    Debug.Log("<color=green>[PASS] Test 7 [ACTUAL SYSTEM INTEGRATION TEST]:</color> Real Swept Collisions -> CollisionSystem.EvaluateCollisions -> GameManager -> LifeManager -> LivesDisplay -> GameOverPopup -> UI Event Dispatch Retry -> Full Gameplay Restored!");
                }
                else
                {
                    Debug.LogError($"[FAIL] Test 7: init={initCheck}, col1={col1Pass}, col2={col2Pass}, col3={col3Pass}, restored={restoredPass}");
                    return;
                }

                Debug.Log("<color=green>==================================================\n[Stage 4 Verification] ALL 7 TESTS PASSED CLEANLY WITH ZERO ERRORS!\n==================================================</color>");
            }
            finally
            {
                // Guaranteed cleanup
                Object.DestroyImmediate(root);

                var strayCanvas = GameObject.Find("UICanvas");
                if (strayCanvas != null) Object.DestroyImmediate(strayCanvas);

                var strayEs = GameObject.Find("EventSystem");
                if (strayEs != null && strayEs != EventSystem.current?.gameObject) Object.DestroyImmediate(strayEs);

                typeof(GameManager).GetProperty("Instance").SetValue(null, previousGmInstance);
                typeof(UIManager).GetProperty("Instance").SetValue(null, previousUiInstance);
            }
        }
    }
}
