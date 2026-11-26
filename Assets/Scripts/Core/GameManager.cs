using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Arrows.Data;
using Arrows.Gameplay;
using Arrows.UI;

namespace Arrows.Core
{
    /// <summary>
    /// Core game manager.
    /// Coordinates level loading, user input, collision evaluation, life management,
    /// and level completion/failure states.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Active LevelData asset to load on start.")]
        [SerializeField] private LevelData activeLevelData;

        [Tooltip("Camera used for screen-to-world raycasting.")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("Transform that holds all instantiated line objects.")]
        [SerializeField] private Transform levelContainer;

        [Header("Subsystems")]
        [SerializeField] private LifeManager lifeManager;
        [SerializeField] private CollisionSystem collisionSystem;

        private LineController[] activeLines;
        private bool isInputEnabled = true;

        public GameState CurrentState { get; private set; } = GameState.Playing;
        public LevelData ActiveLevelData => activeLevelData;
        public LineController[] ActiveLines => activeLines;
        public LifeManager LifeManager => lifeManager;
        public CollisionSystem CollisionSystem => collisionSystem;

        /// <summary>Fired when all lines in the level are successfully completed.</summary>
        public event Action OnLevelWon;

        /// <summary>Fired when player loses all lives.</summary>
        public event Action OnLevelLost;

        /// <summary>Fired when level is restarted.</summary>
        public event Action OnLevelRestarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (levelContainer == null)
            {
                var containerObj = new GameObject("LevelContainer");
                containerObj.transform.SetParent(transform);
                levelContainer = containerObj.transform;
            }

            // Ensure required subsystems exist
            if (lifeManager == null)
            {
                lifeManager = GetComponent<LifeManager>();
                if (lifeManager == null) lifeManager = gameObject.AddComponent<LifeManager>();
            }

            if (collisionSystem == null)
            {
                collisionSystem = GetComponent<CollisionSystem>();
                if (collisionSystem == null) collisionSystem = gameObject.AddComponent<CollisionSystem>();
            }

            if (FindAnyObjectByType<UIManager>() == null)
            {
                var uiObj = new GameObject("UIManager");
                uiObj.AddComponent<UIManager>();
            }
        }

        private bool isInitialized = false;

        /// <summary>
        /// Explicit initialization method for GameManager and its subsystems.
        /// Can be called deterministically from tests or during scene startup.
        /// Supports cleanly re-initializing or loading a new LevelData asset.
        /// </summary>
        public void Initialize(LevelData levelData = null)
        {
            if (levelData != null)
            {
                activeLevelData = levelData;
            }
            else if (isInitialized)
            {
                return;
            }

            isInitialized = true;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (levelContainer == null)
            {
                var containerObj = new GameObject("LevelContainer");
                containerObj.transform.SetParent(transform);
                levelContainer = containerObj.transform;
            }

            // Ensure required subsystems exist
            if (lifeManager == null)
            {
                lifeManager = GetComponent<LifeManager>();
                if (lifeManager == null) lifeManager = gameObject.AddComponent<LifeManager>();
            }

            if (collisionSystem == null)
            {
                collisionSystem = GetComponent<CollisionSystem>();
                if (collisionSystem == null) collisionSystem = gameObject.AddComponent<CollisionSystem>();
            }

            // Hook subsystem events (always remove first to avoid duplicate subscriptions)
            if (collisionSystem != null)
            {
                collisionSystem.OnCollisionGroupDetected -= HandleCollisionGroup;
                collisionSystem.OnCollisionGroupDetected += HandleCollisionGroup;
            }

            if (lifeManager != null)
            {
                int startingLives = activeLevelData != null ? activeLevelData.startingLives : 3;
                lifeManager.Initialize(startingLives);
                lifeManager.OnAllLivesLost -= HandleAllLivesLost;
                lifeManager.OnAllLivesLost += HandleAllLivesLost;
            }

            CurrentState = GameState.Playing;
            SetInputEnabled(true);

            if (activeLevelData != null)
            {
                LoadCurrentLevel();
            }
            else
            {
                Debug.LogWarning("[GameManager] No active LevelData assigned.");
            }
        }

        /// <summary>
        /// Safely loads a new LevelData asset at runtime, unhooking old lines and resetting lives.
        /// </summary>
        public void LoadLevel(LevelData newLevel)
        {
            if (newLevel == null) return;
            activeLevelData = newLevel;
            CurrentState = GameState.Playing;
            SetInputEnabled(true);
            if (lifeManager != null)
            {
                lifeManager.Initialize(activeLevelData.startingLives);
            }
            LoadCurrentLevel();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (isInputEnabled)
            {
                HandleInput();
            }

            if (activeLines == null || activeLines.Length == 0) return;

            // ── PHASE 1: Advance Movement ──
            // Moves arrowheads along their paths. Lines that reach their destination set
            // HasReachedDestination = true, but remain in Moving state with arrowheads active.
            float dt = Time.deltaTime;
            for (int i = 0; i < activeLines.Length; i++)
            {
                var line = activeLines[i];
                if (line != null && line.State == LineState.Moving)
                {
                    line.AdvanceMovement(dt);
                }
            }

            // ── PHASE 2: Evaluate Swept Collisions ──
            // Evaluates all moving arrowheads across their swept path [previousDistance, currentDistance],
            // including the arrival segment. If an arrow collided on its arrival step, it resets!
            if (collisionSystem != null)
            {
                collisionSystem.EvaluateCollisions(activeLines);
            }

            // ── PHASE 3: Finalize Completions ──
            // Any line that reached its destination and survived collision evaluation is now completed safely.
            for (int i = 0; i < activeLines.Length; i++)
            {
                var line = activeLines[i];
                if (line != null && line.State == LineState.Moving && line.HasReachedDestination)
                {
                    line.CompleteLine();
                }
            }
        }

        /// <summary>
        /// Reads input from Mouse or Touchscreen using Unity's New Input System.
        /// </summary>
        private void HandleInput()
        {
            if (CurrentState != GameState.Playing) return;

            Vector2 screenPos = Vector2.zero;
            bool pressed = false;

            // Mouse click
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPos = Mouse.current.position.ReadValue();
                pressed = true;
            }
            // Touch tap
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = true;
            }

            if (pressed)
            {
                ProcessPointerClick(screenPos);
            }
        }

        /// <summary>
        /// Processes a pointer click at screen coordinates.
        /// Checks EventSystem UI raycasting to block gameplay clicks when pointer is over UI.
        /// Returns true if an arrowhead was clicked and interaction started.
        /// </summary>
        public bool ProcessPointerClick(Vector2 screenPos)
        {
            if (CurrentState != GameState.Playing || !isInputEnabled) return false;

            // Prevent raycasting into gameplay when clicking UI elements
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return false;
                }

                var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
                var uiHits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, uiHits);
                if (uiHits.Count > 0)
                {
                    return false; // Pointer hit a UI element (e.g. popup card, backdrop, or button)
                }
            }

            if (mainCamera != null)
            {
                Vector3 worldPos3D = mainCamera.ScreenToWorldPoint(screenPos);
                Vector2 worldPos = new Vector2(worldPos3D.x, worldPos3D.y);

                // Raycast against CircleCollider2D on Arrowheads
                RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
                if (hit.collider != null)
                {
                    var arrowhead = hit.collider.GetComponent<ArrowheadController>();
                    if (arrowhead != null && arrowhead.IsInteractable)
                    {
                        arrowhead.HandleClick();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Instantiates lines from activeLevelData using LevelLoader.
        /// </summary>
        public void LoadCurrentLevel()
        {
            if (activeLines != null && activeLines.Length > 0)
            {
                foreach (var line in activeLines)
                {
                    if (line != null)
                    {
                        line.OnLineCompleted -= HandleLineCompleted;
                    }
                }
                LevelLoader.UnloadLevel(activeLines);
                activeLines = null;
            }

            activeLines = LevelLoader.LoadLevel(activeLevelData, levelContainer);

            foreach (var line in activeLines)
            {
                line.OnLineCompleted += HandleLineCompleted;
            }

            if (lifeManager != null)
            {
                lifeManager.ResetLives();
            }
        }

        /// <summary>
        /// Collision handler: called once per connected collision group detected in the frame.
        /// Deducts exactly 1 life for the entire connected group.
        /// </summary>
        private void HandleCollisionGroup(System.Collections.Generic.List<LineController> group)
        {
            if (group == null || group.Count < 2) return;

            string ids = string.Join(", ", group.ConvertAll(l => l.LineId));
            Debug.Log($"<color=red>[GameManager] Collision Event: Connected Group of {group.Count} lines ({ids})! Deducting 1 life.</color>");

            if (lifeManager != null)
            {
                lifeManager.DeductLife();
            }
        }

        private void HandleAllLivesLost()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Lost;
                SetInputEnabled(false);
                Debug.Log("<color=red>[GameManager] GAME OVER: All lives lost. Disabling input.</color>");
                OnLevelLost?.Invoke();
            }
        }

        private void HandleLineCompleted(LineController line)
        {
            Debug.Log($"[GameManager] Line completed: {line.LineId}");

            // Check if all lines completed
            bool allComplete = true;
            foreach (var l in activeLines)
            {
                if (l != null && l.State != LineState.Completed)
                {
                    allComplete = false;
                    break;
                }
            }

            if (allComplete && CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Won;
                SetInputEnabled(false);
                Debug.Log("<color=green>[GameManager] Level Complete! All lines cleared!</color>");
                OnLevelWon?.Invoke();
            }
        }

        /// <summary>
        /// Restarts the active level: resets lives, reinstantiates lines, restores Playing state,
        /// and enables player interaction.
        /// </summary>
        public void RestartLevel()
        {
            CurrentState = GameState.Playing;
            SetInputEnabled(true);
            LoadCurrentLevel();
            OnLevelRestarted?.Invoke();
            Debug.Log("<color=cyan>[GameManager] Level restarted cleanly.</color>");
        }

        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (collisionSystem != null)
            {
                collisionSystem.OnCollisionGroupDetected -= HandleCollisionGroup;
            }

            if (lifeManager != null)
            {
                lifeManager.OnAllLivesLost -= HandleAllLivesLost;
            }

            if (activeLines != null)
            {
                foreach (var line in activeLines)
                {
                    if (line != null)
                    {
                        line.OnLineCompleted -= HandleLineCompleted;
                    }
                }
            }
        }
    }
}
