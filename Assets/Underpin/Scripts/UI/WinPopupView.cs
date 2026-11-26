using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    using Underpin.SlotMachine.Core;

    /// <summary>
    /// Modal popup dialog using popup.png.
    /// Celebrates Big Wins, Jackpots, and Free Spin triggers with animated entrance,
    /// dynamic celebration text, payout readout, and Collect / Gamble options.
    /// </summary>
    public class WinPopupView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Text titleText;
        [SerializeField] private Text amountText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button gambleButton;

        public event Action OnCollectClicked;
        public event Action OnGambleClicked;

        private Coroutine animCoroutine;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (panelRect == null) panelRect = GetComponent<RectTransform>();

            if (collectButton != null)
                collectButton.onClick.AddListener(() => OnCollectClicked?.Invoke());

            if (gambleButton != null)
                gambleButton.onClick.AddListener(() => OnGambleClicked?.Invoke());

            // The original button artwork already contains YES / NO lettering.
            // The generated UI added COLLECT / DOUBLE UP labels on top of it.
            // Remove that baked lettering by using clean, solid button faces.
            ConfigureActionButton(collectButton, "COLLECT", new Color(0.62f, 0.39f, 0.08f, 1f));
            ConfigureActionButton(gambleButton, "DOUBLE UP", new Color(0.62f, 0.39f, 0.08f, 1f));

            Image dimImage = GetComponent<Image>();
            if (dimImage != null)
                dimImage.color = new Color(0.01f, 0.01f, 0.04f, 0.38f);

            HideImmediate();
        }

        private static void ConfigureActionButton(Button button, string label, Color faceColor)
        {
            if (button == null) return;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = faceColor;
            }

            Text labelText = button.GetComponentInChildren<Text>(true);
            if (labelText != null)
            {
                labelText.text = label;
                labelText.fontSize = 20;
                labelText.fontStyle = FontStyle.Bold;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
                labelText.verticalOverflow = VerticalWrapMode.Truncate;
                labelText.raycastTarget = false;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.62f, 1f);
            colors.pressedColor = new Color(0.82f, 0.62f, 0.25f, 1f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }

        public void Show(string title, string amount, string subtitle, bool allowGamble = false)
        {
            if (titleText != null) titleText.text = title;
            if (amountText != null) amountText.text = amount;
            if (subtitleText != null) subtitleText.text = subtitle;

            if (gambleButton != null)
                gambleButton.gameObject.SetActive(allowGamble);

            gameObject.SetActive(true);

            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(ShowAnimRoutine());
        }

        public void Hide()
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(HideAnimRoutine());
        }

        public void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private IEnumerator ShowAnimRoutine()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t * 2f);

                // Elastic scale-pop: 0.3 -> 1.08 -> 1.0
                float scale = EaseOutBack(t);
                if (panelRect != null) panelRect.localScale = Vector3.one * scale;

                yield return null;
            }

            if (panelRect != null) panelRect.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            animCoroutine = null;
        }

        private IEnumerator HideAnimRoutine()
        {
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (canvasGroup != null) canvasGroup.alpha = 1f - t;
                if (panelRect != null) panelRect.localScale = Vector3.one * (1f - t * 0.3f);
                yield return null;
            }

            HideImmediate();
            animCoroutine = null;
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
        }
    }
}
