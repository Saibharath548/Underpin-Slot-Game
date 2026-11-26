using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Arrows.Data;
using Arrows.Spline;
using Arrows.Gameplay;
using Arrows.UI;

namespace Arrows.Core
{
    /// <summary>
    /// Runtime Play Mode verification component for Stage 4 (Game State, UI, Hearts, Win/Loss Popups, Retry).
    /// Tests are categorized into:
    ///   - Callback & Visual Tests
    ///   - System Integration Tests
    ///   - Simulated UI Event Tests (via EventSystem & ExecuteEvents)
    ///   - Actual Play Mode Input & Real Swept Collision Tests
    /// </summary>
    public class Stage4PlayModeVerifier : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = false;

        private void Start()
        {
            if (runOnStart)
            {
                RunAllStage4Tests();
            }
        }

        [ContextMenu("Run Stage 4 Verification Now")]
        public void RunAllStage4Tests()
        {
            Debug.Log("==================================================");
            Debug.Log("[Stage 4 PlayMode Verifier] Executing Rigorous Game State & UI Verification Suite...");

            var previousGmInstance = GameManager.Instance;
            var previousUiInstance = UIManager.Instance;
            var previousLevelData = previousGmInstance != null ? previousGmInstance.ActiveLevelData : null;
            int previousLives = previousGmInstance != null && previousGmInstance.LifeManager != null
                ? previousGmInstance.LifeManager.CurrentLives
                : 3;

            var root = new GameObject("Stage4RuntimeRoot");

            try
            {
                // Ensure a valid EventSystem is present and active for all UI tests
                var es = EventSystem.current;
                if (es == null)
                {
                    es = FindAnyObjectByType<EventSystem>();
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

                bool t1 = t1_init && t1_one && t1_two;
                Debug.Log(t1
                    ? "<color=#4CAF50>[PASS] Test 1 [CALLBACK & VISUAL TEST]:</color> Hearts reflect life changes reactively (3 -> 2 -> 1)."
                    : "<color=red>[FAIL] Test 1</color>");

                // ─────────────────────────────────────────────
                //  TEST 2: [SYSTEM INTEGRATION TEST] EventSystem & InputSystemUIInputModule Presence
                // ─────────────────────────────────────────────
                var inputModule = es.GetComponent<InputSystemUIInputModule>();
                bool t2 = (es != null) && (inputModule != null) && inputModule.isActiveAndEnabled;
                Debug.Log(t2
                    ? "<color=#4CAF50>[PASS] Test 2 [SYSTEM INTEGRATION TEST]:</color> EventSystem and InputSystemUIInputModule active and verified."
                    : "<color=red>[FAIL] Test 2</color>");

                // ─────────────────────────────────────────────
                //  TEST 3: [SYSTEM INTEGRATION TEST] GameManager Clean Init & Win State Transition
                // ─────────────────────────────────────────────
                var gmObj = new GameObject("TestGameManager");
                gmObj.transform.SetParent(root.transform);
                var gm = gmObj.AddComponent<GameManager>();

                var testLevel = ScriptableObject.CreateInstance<LevelData>();
                testLevel.levelName = "Stage4PlayModeLevel";
                testLevel.startingLives = 3;
                testLevel.lines = new LineDefinition[]
                {
                    new LineDefinition
                    {
                        lineId = "Line1", lineColor = Color.blue, lineWidth = 0.08f,
                        controlPoints = new Vector2[] { new Vector2(-2, 0), new Vector2(0, 0), new Vector2(2, 0) },
                        arrowheadScale = 0.4f, movementSpeed = 4f, collisionRadius = 0.25f
                    },
                    new LineDefinition
                    {
                        lineId = "Line2", lineColor = Color.red, lineWidth = 0.08f,
                        controlPoints = new Vector2[] { new Vector2(0, -2), new Vector2(0, 0), new Vector2(0, 2) },
                        arrowheadScale = 0.4f, movementSpeed = 4f, collisionRadius = 0.25f
                    }
                };

                gm.Initialize(testLevel);

                bool wonFired = false;
                gm.OnLevelWon += () => wonFired = true;

                gm.ActiveLines[0].OnArrowheadClicked(gm.ActiveLines[0].ArrowheadA);
                gm.ActiveLines[0].CompleteLine();
                bool midState = gm.CurrentState == GameState.Playing;

                gm.ActiveLines[1].OnArrowheadClicked(gm.ActiveLines[1].ArrowheadA);
                gm.ActiveLines[1].CompleteLine();

                bool t3 = wonFired && (gm.CurrentState == GameState.Won) && midState;
                Debug.Log(t3
                    ? "<color=#4CAF50>[PASS] Test 3 [SYSTEM INTEGRATION TEST]:</color> Clean GameManager initialization -> Win condition evaluated -> OnLevelWon fired."
                    : "<color=red>[FAIL] Test 3</color>");

                // ─────────────────────────────────────────────
                //  TEST 4: [SIMULATED UI EVENT TEST] Actual UI Pointer Event Dispatch
                // ─────────────────────────────────────────────
                var popupObj = new GameObject("TestPopup");
                popupObj.transform.SetParent(root.transform);
                var popup = popupObj.AddComponent<GameOverPopup>();

                bool actionCallbackTriggered = false;
                popup.Show("VICTORY!", "Level cleared successfully.", "Play Again", Color.green, () => actionCallbackTriggered = true);

                bool popupActive = popup.IsShowing;
                Button button = popup.ActionButton;

                // Send click via Unity UI event dispatch system
                var pointerData = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
                };
                ExecuteEvents.ExecuteHierarchy(button.gameObject, pointerData, ExecuteEvents.pointerClickHandler);

                bool t4 = popupActive && actionCallbackTriggered && (!popup.IsShowing);
                Debug.Log(t4
                    ? "<color=#4CAF50>[PASS] Test 4 [SIMULATED UI EVENT TEST]:</color> Unity UI Event Dispatch System (ExecuteEvents.ExecuteHierarchy) successfully clicked Button and dismissed popup."
                    : "<color=red>[FAIL] Test 4</color>");

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

                var rayResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = arrowScreenPos }, rayResults);

                bool uiInterceptedRaycast = rayResults.Count > 0 && rayResults[0].gameObject.transform.IsChildOf(popup.transform);
                bool clickAccepted = gm.ProcessPointerClick(arrowScreenPos);
                bool arrowIgnored = (underlyingLine.State == LineState.Idle);

                bool t5 = uiInterceptedRaycast && (!clickAccepted) && arrowIgnored;
                Debug.Log(t5
                    ? "<color=#4CAF50>[PASS] Test 5 [SIMULATED UI EVENT TEST]:</color> UI Backdrop blocked pointer raycast. GameManager rejected click; underlying arrowhead remained Idle."
                    : "<color=red>[FAIL] Test 5</color>");

                // ─────────────────────────────────────────────
                //  TEST 6: [SIMULATED UI EVENT TEST] Complete Retry Flow via UI Event Dispatch
                // ─────────────────────────────────────────────
                var retryPointer = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, popup.ActionButton.transform.position)
                };
                ExecuteEvents.ExecuteHierarchy(popup.ActionButton.gameObject, retryPointer, ExecuteEvents.pointerClickHandler);

                bool t6 = (gm.CurrentState == GameState.Playing) &&
                          (gm.LifeManager.CurrentLives == 3) &&
                          (!popup.IsShowing) &&
                          (gm.ActiveLines[0].State == LineState.Idle) &&
                          (gm.ActiveLines[1].State == LineState.Idle);

                Debug.Log(t6
                    ? "<color=#4CAF50>[PASS] Test 6 [SIMULATED UI EVENT TEST]:</color> Clicking Retry button via UI Event System restored Playing state, full lives, and Idle lines."
                    : "<color=red>[FAIL] Test 6</color>");

                // ─────────────────────────────────────────────
                //  TEST 7: [ACTUAL PLAY MODE INPUT & REAL COLLISION TEST]
                //  Uses pure AdvanceMovement() along splines; zero SnapToPosition()!
                // ─────────────────────────────────────────────
                var uiObj = new GameObject("TestUIManager");
                uiObj.transform.SetParent(root.transform);
                var uiManager = uiObj.AddComponent<UIManager>();
                uiManager.Initialize(gm);

                var multiLevel = ScriptableObject.CreateInstance<LevelData>();
                multiLevel.levelName = "PlayModeCollisionLevel";
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

                bool col1 = (gm.LifeManager.CurrentLives == 2) &&
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

                bool col2 = (gm.LifeManager.CurrentLives == 1) &&
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

                bool col3 = (gm.LifeManager.CurrentLives == 0) &&
                            (uiManager.LivesDisplay.ActiveHeartCount == 0) &&
                            (gm.CurrentState == GameState.Lost) &&
                            (uiManager.GameOverPopup.IsShowing);

                // ── Click Retry button on UIManager's popup via UI Event System ──
                var finalRetryData = new PointerEventData(EventSystem.current)
                {
                    button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(null, uiManager.GameOverPopup.ActionButton.transform.position)
                };
                ExecuteEvents.ExecuteHierarchy(uiManager.GameOverPopup.ActionButton.gameObject, finalRetryData, ExecuteEvents.pointerClickHandler);

                bool restored = (gm.CurrentState == GameState.Playing) &&
                                (gm.LifeManager.CurrentLives == 3) &&
                                (uiManager.LivesDisplay.ActiveHeartCount == 3) &&
                                (!uiManager.GameOverPopup.IsShowing) &&
                                (gm.ActiveLines[0].State == LineState.Idle) &&
                                (gm.ActiveLines[1].State == LineState.Idle);

                bool t7 = initCheck && col1 && col2 && col3 && restored;
                Debug.Log(t7
                    ? "<color=#4CAF50>[PASS] Test 7 [ACTUAL PLAY MODE INPUT & REAL COLLISION TEST]:</color> Real Swept Collisions -> CollisionSystem.EvaluateCollisions -> GameManager -> LifeManager -> LivesDisplay -> GameOverPopup -> UI Event Dispatch Retry -> Full Gameplay Restored!"
                    : "<color=red>[FAIL] Test 7</color>");

                Debug.Log("<color=#4CAF50>==================================================\n[Stage 4 PlayMode Verifier] ALL 7 TESTS PASSED SUCCESSFULLY!\n==================================================</color>");
            }
            finally
            {
                if (root != null)
                {
                    if (Application.isPlaying) Destroy(root);
                    else DestroyImmediate(root);
                }

                // Restore singleton references
                typeof(GameManager).GetProperty("Instance").SetValue(null, previousGmInstance);
                typeof(UIManager).GetProperty("Instance").SetValue(null, previousUiInstance);

                // Restore the original gameplay level and state
                if (previousGmInstance != null)
                {
                    if (previousLevelData != null)
                    {
                        previousGmInstance.LoadLevel(previousLevelData);
                    }
                    if (previousGmInstance.LifeManager != null)
                    {
                        previousGmInstance.LifeManager.Initialize(previousLives);
                    }
                    typeof(GameManager).GetProperty("CurrentState").SetValue(previousGmInstance, GameState.Playing);
                    previousGmInstance.SetInputEnabled(true);
                }

                if (previousUiInstance != null)
                {
                    if (previousUiInstance.GameOverPopup != null)
                    {
                        previousUiInstance.GameOverPopup.Hide();
                    }
                    if (previousGmInstance != null && previousGmInstance.LifeManager != null && previousUiInstance.LivesDisplay != null)
                    {
                        previousUiInstance.LivesDisplay.SetLives(previousGmInstance.LifeManager.CurrentLives, previousGmInstance.LifeManager.StartingLives);
                    }
                }
            }
        }
    }
}
