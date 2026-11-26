using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Arrows.Core;

namespace Arrows.UI
{
    /// <summary>
    /// Central manager for gameplay user interface.
    /// Manages the Canvas, LivesDisplay, Win/Lose popups, and EventSystem.
    /// Can assemble its entire visual hierarchy procedurally if not assigned in Inspector.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Scene References (Auto-created if null)")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private LivesDisplay livesDisplay;
        [SerializeField] private GameOverPopup gameOverPopup;

        public Canvas UICanvas => uiCanvas;
        public LivesDisplay LivesDisplay => livesDisplay;
        public GameOverPopup GameOverPopup => gameOverPopup;

        private bool isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUIHierarchy();
        }

        /// <summary>
        /// Explicit initialization method for UIManager.
        /// Can be called deterministically from tests or during scene startup.
        /// </summary>
        public void Initialize(GameManager gm = null)
        {
            if (isInitialized) return;
            isInitialized = true;

            EnsureUIHierarchy();

            if (gm == null) gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnLevelWon -= HandleLevelWon;
                gm.OnLevelWon += HandleLevelWon;
                gm.OnLevelLost -= HandleLevelLost;
                gm.OnLevelLost += HandleLevelLost;
                gm.OnLevelRestarted -= HandleLevelRestarted;
                gm.OnLevelRestarted += HandleLevelRestarted;

                if (gm.LifeManager != null && livesDisplay != null)
                {
                    gm.LifeManager.OnLifeChanged -= HandleLifeChanged;
                    gm.LifeManager.OnLifeChanged += HandleLifeChanged;
                    livesDisplay.Initialize(gm.LifeManager.StartingLives);
                    livesDisplay.SetLives(gm.LifeManager.CurrentLives, gm.LifeManager.StartingLives);
                }
            }

            if (gameOverPopup != null)
            {
                gameOverPopup.Hide();
            }
        }

        private void Start()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }

        private void HandleLifeChanged(int current)
        {
            if (livesDisplay != null)
            {
                var gm = GameManager.Instance;
                int max = gm != null && gm.LifeManager != null ? gm.LifeManager.StartingLives : 3;
                livesDisplay.SetLives(current, max);
            }
        }

        private void HandleLevelWon()
        {
            if (gameOverPopup != null)
            {
                gameOverPopup.Show(
                    title: "LEVEL CLEARED!",
                    subtitle: "All colored lines successfully removed without losing all lives.",
                    buttonLabel: "Play Again",
                    titleColor: new Color(0.18f, 0.8f, 0.44f), // Emerald Green
                    onButtonClick: () => GameManager.Instance.RestartLevel()
                );
            }
        }

        private void HandleLevelLost()
        {
            if (gameOverPopup != null)
            {
                gameOverPopup.Show(
                    title: "OUT OF LIVES!",
                    subtitle: "Too many collisions occurred. Try a different line order!",
                    buttonLabel: "Try Again",
                    titleColor: new Color(0.92f, 0.26f, 0.21f), // Crimson
                    onButtonClick: () => GameManager.Instance.RestartLevel()
                );
            }
        }

        private void HandleLevelRestarted()
        {
            if (gameOverPopup != null)
            {
                gameOverPopup.Hide();
            }

            var gm = GameManager.Instance;
            if (gm != null && gm.LifeManager != null && livesDisplay != null)
            {
                livesDisplay.SetLives(gm.LifeManager.CurrentLives, gm.LifeManager.StartingLives);
            }
        }

        /// <summary>
        /// Validates that Canvas, EventSystem, LivesDisplay, and GameOverPopup exist,
        /// creating them procedurally if missing.
        /// </summary>
        public void EnsureUIHierarchy()
        {
            // 1. Ensure EventSystem exists with modern InputSystemUIInputModule
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }

            // 2. Ensure Canvas exists
            if (uiCanvas == null)
            {
                uiCanvas = GetComponentInChildren<Canvas>();
            }

            if (uiCanvas == null)
            {
                var canvasObj = new GameObject("UICanvas");
                canvasObj.transform.SetParent(transform, false);

                uiCanvas = canvasObj.AddComponent<Canvas>();
                uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                uiCanvas.sortingOrder = 100;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 3. Ensure Top Bar & LivesDisplay exist
            if (livesDisplay == null)
            {
                livesDisplay = uiCanvas.GetComponentInChildren<LivesDisplay>();
            }

            if (livesDisplay == null)
            {
                var topBarObj = new GameObject("TopBar");
                topBarObj.transform.SetParent(uiCanvas.transform, false);

                var topRect = topBarObj.AddComponent<RectTransform>();
                topRect.anchorMin = new Vector2(0f, 1f);
                topRect.anchorMax = new Vector2(1f, 1f);
                topRect.pivot = new Vector2(0.5f, 1f);
                topRect.anchoredPosition = new Vector2(0f, -24f);
                topRect.sizeDelta = new Vector2(0f, 60f);

                var livesObj = new GameObject("LivesDisplay");
                livesObj.transform.SetParent(topBarObj.transform, false);

                var livesRect = livesObj.AddComponent<RectTransform>();
                livesRect.anchorMin = new Vector2(0.5f, 0.5f);
                livesRect.anchorMax = new Vector2(0.5f, 0.5f);
                livesRect.pivot = new Vector2(0.5f, 0.5f);
                livesRect.anchoredPosition = Vector2.zero;

                livesDisplay = livesObj.AddComponent<LivesDisplay>();
            }

            // 4. Ensure GameOverPopup exists
            if (gameOverPopup == null)
            {
                gameOverPopup = uiCanvas.GetComponentInChildren<GameOverPopup>(includeInactive: true);
            }

            if (gameOverPopup == null)
            {
                var popupObj = new GameObject("GameOverPopup");
                popupObj.transform.SetParent(uiCanvas.transform, false);

                var popupRect = popupObj.AddComponent<RectTransform>();
                popupRect.anchorMin = Vector2.zero;
                popupRect.anchorMax = Vector2.one;
                popupRect.offsetMin = Vector2.zero;
                popupRect.offsetMax = Vector2.zero;

                gameOverPopup = popupObj.AddComponent<GameOverPopup>();
                popupObj.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnLevelWon -= HandleLevelWon;
                gm.OnLevelLost -= HandleLevelLost;
                gm.OnLevelRestarted -= HandleLevelRestarted;

                if (gm.LifeManager != null)
                {
                    gm.LifeManager.OnLifeChanged -= HandleLifeChanged;
                }
            }
        }
    }
}
