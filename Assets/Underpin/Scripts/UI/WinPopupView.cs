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

            HideImmediate();
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
