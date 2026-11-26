using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arrows.UI
{
    /// <summary>
    /// Reusable modal popup for Win ("Level Cleared") and Lose ("Out of Lives") outcomes.
    /// Provides full-screen input blocking, card display, and a Retry/Play Again button.
    /// </summary>
    public class GameOverPopup : MonoBehaviour
    {
        [Header("Components (Optional - Auto-assembled if null)")]
        [SerializeField] private Image backdropImage;
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private Image cardImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text buttonText;

        public Button ActionButton { get { EnsureHierarchy(); return actionButton; } }
        public Text TitleText { get { EnsureHierarchy(); return titleText; } }
        public Text SubtitleText { get { EnsureHierarchy(); return subtitleText; } }
        public Image BackdropImage { get { EnsureHierarchy(); return backdropImage; } }
        public Image CardImage { get { EnsureHierarchy(); return cardImage; } }
        public bool IsShowing => gameObject.activeSelf;

        private Action onActionCallback;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            EnsureHierarchy();
        }

        public void EnsureHierarchy()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // If components not wired in inspector, assemble procedurally
            if (backdropImage == null || cardRect == null)
            {
                BuildProceduralHierarchy();
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(HandleButtonClicked);
                actionButton.onClick.AddListener(HandleButtonClicked);
            }
        }

        /// <summary>
        /// Displays the popup with customizable title, subtitle, button label, and theme color.
        /// </summary>
        public void Show(string title, string subtitle, string buttonLabel, Color titleColor, Action onButtonClick)
        {
            EnsureHierarchy();
            onActionCallback = onButtonClick;

            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = titleColor;
            }

            if (subtitleText != null)
            {
                subtitleText.text = subtitle;
            }

            if (buttonText != null)
            {
                buttonText.text = buttonLabel;
            }

            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AnimateShow());
        }

        /// <summary>
        /// Hides the popup immediately.
        /// </summary>
        public void Hide()
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
        }

        private void HandleButtonClicked()
        {
            Hide();
            onActionCallback?.Invoke();
        }

        private System.Collections.IEnumerator AnimateShow()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            if (cardRect != null) cardRect.localScale = Vector3.one * 0.85f;

            float elapsed = 0f;
            float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth ease-out quad
                float ease = 1f - (1f - t) * (1f - t);

                canvasGroup.alpha = ease;
                if (cardRect != null)
                {
                    cardRect.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, ease);
                }

                yield return null;
            }

            canvasGroup.alpha = 1f;
            if (cardRect != null) cardRect.localScale = Vector3.one;
        }

        private void BuildProceduralHierarchy()
        {
            var rootRect = GetComponent<RectTransform>();
            if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // Fullscreen backdrop (blocks raycasts to gameplay)
            backdropImage = GetComponent<Image>();
            if (backdropImage == null) backdropImage = gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0.08f, 0.09f, 0.12f, 0.72f);
            backdropImage.raycastTarget = true;

            // Center dialog card
            var cardObj = new GameObject("Card");
            cardObj.transform.SetParent(transform, false);
            cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(440f, 320f);

            cardImage = cardObj.AddComponent<Image>();
            cardImage.sprite = UISpriteFactory.GetCardSprite();
            cardImage.type = Image.Type.Sliced;
            cardImage.color = Color.white;

            // Vertical Layout for Card content
            var layout = cardObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 40, 36);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Default font (Unity 6.6 uses LegacyRuntime.ttf)
            Font defaultFont = GetDefaultUIFont();

            // Title
            var titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(360f, 48f);
            titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.text = "LEVEL CLEARED!";
            titleText.color = new Color(0.18f, 0.8f, 0.44f);

            // Subtitle
            var subObj = new GameObject("SubtitleText");
            subObj.transform.SetParent(cardObj.transform, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.sizeDelta = new Vector2(360f, 56f);
            subtitleText = subObj.AddComponent<Text>();
            subtitleText.font = defaultFont;
            subtitleText.fontSize = 17;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.text = "All lines cleared successfully.";
            subtitleText.color = new Color(0.4f, 0.45f, 0.5f);

            // Spacer
            var spacerObj = new GameObject("Spacer");
            spacerObj.transform.SetParent(cardObj.transform, false);
            var spacerRect = spacerObj.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(360f, 10f);

            // Action Button
            var btnObj = new GameObject("ActionButton");
            btnObj.transform.SetParent(cardObj.transform, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(240f, 54f);

            var btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = UISpriteFactory.GetButtonSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.24f, 0.51f, 0.96f); // Clean Royal Blue

            actionButton = btnObj.AddComponent<Button>();
            actionButton.targetGraphic = btnImg;
            var colors = actionButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            actionButton.colors = colors;

            // Button label
            var btnLblObj = new GameObject("Text");
            btnLblObj.transform.SetParent(btnObj.transform, false);
            var btnLblRect = btnLblObj.AddComponent<RectTransform>();
            btnLblRect.anchorMin = Vector2.zero;
            btnLblRect.anchorMax = Vector2.one;
            btnLblRect.offsetMin = Vector2.zero;
            btnLblRect.offsetMax = Vector2.zero;

            buttonText = btnLblObj.AddComponent<Text>();
            buttonText.font = defaultFont;
            buttonText.fontSize = 18;
            buttonText.fontStyle = FontStyle.Bold;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.text = "Play Again";
            buttonText.color = Color.white;
        }

        /// <summary>
        /// Retrieves a valid built-in UI font safely across Unity versions.
        /// In Unity 6+, LegacyRuntime.ttf is the supported built-in runtime font.
        /// </summary>
        public static Font GetDefaultUIFont()
        {
            Font font = null;
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch {}

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch {}
            }

            return font;
        }

        private void OnDestroy()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(HandleButtonClicked);
            }
        }
    }
}

