using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.Reels
{
    using Underpin.SlotMachine.Core;

    /// <summary>
    /// View component representing a single symbol inside a reel strip.
    /// Handles sprite assignment, dynamic scaling, and celebratory pulsing win glow.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ReelSymbolView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image symbolImage;
        [SerializeField] private Image glowImage;

        private RectTransform rectTransform;
        private SymbolId currentSymbolId;
        private Coroutine pulseCoroutine;
        private Vector3 initialScale = Vector3.one;

        public SymbolId CurrentSymbolId => currentSymbolId;
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = GetComponent<RectTransform>();
                return rectTransform;
            }
        }

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (symbolImage == null)
                symbolImage = GetComponent<Image>();
            initialScale = transform.localScale;
        }

        /// <summary>
        /// Updates the view with the given symbol ID and sprite asset.
        /// </summary>
        public void SetSymbol(SymbolId id, Sprite sprite)
        {
            currentSymbolId = id;
            if (symbolImage == null)
                symbolImage = GetComponent<Image>();
            if (symbolImage != null)
            {
                symbolImage.sprite = sprite;
                symbolImage.enabled = (sprite != null);
            }
            ResetHighlight();
        }

        /// <summary>
        /// Initiates an animated celebratory pulsing scale effect when part of a win.
        /// </summary>
        public void StartWinHighlight(Color highlightColor, float duration = 2.0f)
        {
            StopWinHighlight();
            pulseCoroutine = StartCoroutine(PulseRoutine(highlightColor, duration));
        }

        /// <summary>
        /// Resets the highlight state back to normal resting visuals.
        /// </summary>
        public void ResetHighlight()
        {
            StopWinHighlight();
            transform.localScale = initialScale;
            if (glowImage != null)
            {
                glowImage.enabled = false;
            }
            if (symbolImage != null)
            {
                symbolImage.color = Color.white;
            }
        }

        private void StopWinHighlight()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }

        private IEnumerator PulseRoutine(Color highlightColor, float duration)
        {
            float elapsed = 0f;
            if (glowImage != null)
            {
                glowImage.enabled = true;
                glowImage.color = highlightColor;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Pleasant sinusoidal bounce between 1.0x and 1.25x scale
                float sinFactor = Mathf.Sin(elapsed * 10f) * 0.5f + 0.5f;
                float scale = Mathf.Lerp(1.0f, 1.25f, sinFactor);
                transform.localScale = initialScale * scale;

                if (symbolImage != null)
                {
                    // Subtle brightness oscillation
                    symbolImage.color = Color.Lerp(Color.white, highlightColor, sinFactor * 0.4f);
                }

                yield return null;
            }

            ResetHighlight();
        }
    }
}
