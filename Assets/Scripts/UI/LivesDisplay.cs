using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arrows.Core;

namespace Arrows.UI
{
    /// <summary>
    /// Displays player lives as a row of heart icons.
    /// Responds reactively to LifeManager.OnLifeChanged events.
    /// </summary>
    public class LivesDisplay : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Horizontal spacing between heart icons.")]
        [SerializeField] private float heartSpacing = 16f;

        [Tooltip("Size (width & height) of each heart icon in UI units.")]
        [SerializeField] private float heartSize = 42f;

        private readonly List<Image> heartImages = new List<Image>();
        private int currentLives = 3;
        private int maxLives = 3;

        public int ActiveHeartCount => currentLives;
        public int TotalHeartCount => heartImages.Count;
        public Image GetHeartImage(int index) => (index >= 0 && index < heartImages.Count) ? heartImages[index] : null;

        /// <summary>
        /// Initializes the display for a given maximum number of lives.
        /// Builds the heart icon GameObjects dynamically if needed.
        /// </summary>
        public void Initialize(int startingLives)
        {
            maxLives = Mathf.Max(1, startingLives);
            currentLives = maxLives;

            // Clear any existing hearts
            for (int i = 0; i < heartImages.Count; i++)
            {
                if (heartImages[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(heartImages[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(heartImages[i].gameObject);
                    }
                }
            }
            heartImages.Clear();

            // Ensure layout component exists
            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = heartSpacing;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            var csf = GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = gameObject.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Create heart icon GameObjects
            for (int i = 0; i < maxLives; i++)
            {
                var heartObj = new GameObject($"Heart_{i}");
                heartObj.transform.SetParent(transform, false);

                var rect = heartObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(heartSize, heartSize);

                var img = heartObj.AddComponent<Image>();
                img.sprite = UISpriteFactory.GetFilledHeartSprite();
                img.color = Color.white;
                img.raycastTarget = false;

                heartImages.Add(img);
            }

            UpdateVisuals();
        }

        /// <summary>
        /// Updates heart states based on current life count.
        /// </summary>
        public void SetLives(int current)
        {
            SetLives(current, maxLives);
        }

        /// <summary>
        /// Updates heart states based on current life count and max lives.
        /// </summary>
        public void SetLives(int current, int max)
        {
            if (max != maxLives)
            {
                Initialize(max);
            }

            int previous = currentLives;
            currentLives = Mathf.Clamp(current, 0, maxLives);

            UpdateVisuals();

            // Animate heart loss if life decreased
            if (currentLives < previous && currentLives < heartImages.Count)
            {
                for (int i = currentLives; i < previous && i < heartImages.Count; i++)
                {
                    StartCoroutine(AnimateHeartLost(heartImages[i]));
                }
            }
        }

        private void UpdateVisuals()
        {
            for (int i = 0; i < heartImages.Count; i++)
            {
                if (heartImages[i] == null) continue;

                if (i < currentLives)
                {
                    heartImages[i].sprite = UISpriteFactory.GetFilledHeartSprite();
                    heartImages[i].color = Color.white;
                }
                else
                {
                    heartImages[i].sprite = UISpriteFactory.GetEmptyHeartSprite();
                    heartImages[i].color = new Color(1f, 1f, 1f, 0.45f);
                }
            }
        }

        private System.Collections.IEnumerator AnimateHeartLost(Image heartImg)
        {
            if (heartImg == null) yield break;

            Transform t = heartImg.transform;
            Vector3 originalScale = Vector3.one;

            // Quick pop out
            float elapsed = 0f;
            float duration = 0.12f;
            while (elapsed < duration)
            {
                if (heartImg == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                t.localScale = Vector3.Lerp(originalScale, originalScale * 1.35f, progress);
                yield return null;
            }

            // Settle back to normal size
            elapsed = 0f;
            duration = 0.18f;
            while (elapsed < duration)
            {
                if (heartImg == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                t.localScale = Vector3.Lerp(originalScale * 1.35f, originalScale, progress);
                yield return null;
            }

            if (heartImg != null)
            {
                t.localScale = originalScale;
            }
        }
    }
}
