using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.Reels
{
    using Underpin.SlotMachine.Core;

    /// <summary>
    /// Controller managing a single vertical reel strip.
    /// Implements smooth continuous vertical scrolling, symbol recycling/wrapping,
    /// seamless outcome injection, and harmonic elastic bounce-back deceleration.
    /// </summary>
    public class ReelStripController : MonoBehaviour
    {
        [Header("Reel Configuration")]
        [SerializeField] private int reelIndex = 0;
        [SerializeField] private float symbolHeight = 96f;
        [SerializeField] private float symbolSpacing = 16f;
        [SerializeField] private int visibleRowCount = 3;
        [SerializeField] private int totalSymbolViews = 5; // Extra buffer for continuous wrapping

        [Header("Spin Dynamics")]
        [SerializeField] private float maxSpinSpeed = 2400f; // Pixels per second
        [SerializeField] private float accelerationTime = 0.35f;
        [SerializeField] private float bounceOvershoot = 30f; // Elastic bounce distance in pixels
        [SerializeField] private float bounceDuration = 0.28f;

        [Header("Prefabs & Parents")]
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private ReelSymbolView symbolViewPrefab;

        public event Action<int> OnReelStopped;

        private List<ReelSymbolView> symbolViews = new List<ReelSymbolView>();
        private PaytableData paytable;
        private ISlotRNG rng;
        private Dictionary<SymbolId, Sprite> spriteMap = new Dictionary<SymbolId, Sprite>();

        private bool isSpinning = false;
        private bool isStopping = false;
        private float currentSpeed = 0f;
        private Coroutine spinCoroutine;
        private SymbolId[] targetSymbols; // [Top, Center, Bottom]

        public bool IsSpinning => isSpinning;
        public int ReelIndex => reelIndex;

        public float CellStep => symbolHeight + symbolSpacing;

        public void Initialize(int index, PaytableData paytableData, ISlotRNG rngService, Dictionary<SymbolId, Sprite> sprites)
        {
            this.reelIndex = index;
            this.paytable = paytableData;
            this.rng = rngService;
            this.spriteMap = sprites;

            // Fit all three complete symbols inside the cabinet's reel window.
            symbolHeight = 72f;
            symbolSpacing = 8f;

            if (containerRect == null)
            {
                containerRect = GetComponent<RectTransform>();
            }

            SetupSymbolViews();
        }

        private void SetupSymbolViews()
        {
            // Clear any preexisting child symbol views
            if (symbolViews.Count == 0)
            {
                var existing = containerRect.GetComponentsInChildren<ReelSymbolView>();
                foreach (var view in existing)
                {
                    symbolViews.Add(view);
                }
            }

            // If not enough views exist, spawn them
            while (symbolViews.Count < totalSymbolViews)
            {
                ReelSymbolView view;
                if (symbolViewPrefab != null)
                {
                    view = Instantiate(symbolViewPrefab, containerRect);
                }
                else
                {
                    GameObject go = new GameObject($"SymbolView_{symbolViews.Count}", typeof(RectTransform), typeof(Image), typeof(ReelSymbolView));
                    go.transform.SetParent(containerRect, false);
                    view = go.GetComponent<ReelSymbolView>();
                }
                symbolViews.Add(view);
            }

            // Position symbols vertically centered around the window:
            // Visible rows (0=Top, 1=Center, 2=Bottom) at Y = +CellStep, 0, -CellStep
            // Buffer symbols at Y = +2*CellStep and -2*CellStep
            for (int i = 0; i < symbolViews.Count; i++)
            {
                float targetY = (1 - i) * CellStep;
                symbolViews[i].RectTransform.anchoredPosition = new Vector2(0f, targetY);
                symbolViews[i].RectTransform.sizeDelta = new Vector2(symbolHeight, symbolHeight);

                SymbolId initialSym = (rng != null && paytable != null) ? rng.GetRandomSymbol(paytable) : (SymbolId)(i % 4);
                Sprite spr = spriteMap.ContainsKey(initialSym) ? spriteMap[initialSym] : null;
                symbolViews[i].SetSymbol(initialSym, spr);
            }
        }

        /// <summary>
        /// Begins spinning the reel downward with smooth acceleration.
        /// </summary>
        public void StartSpin()
        {
            if (isSpinning) return;

            isSpinning = true;
            isStopping = false;
            targetSymbols = null;

            if (spinCoroutine != null)
            {
                StopCoroutine(spinCoroutine);
            }

            spinCoroutine = StartCoroutine(SpinRoutine());
        }

        /// <summary>
        /// Commands the reel to stop and display the target outcome symbols.
        /// </summary>
        /// <param name="top">Row 0 symbol</param>
        /// <param name="center">Row 1 symbol (Main Payline)</param>
        /// <param name="bottom">Row 2 symbol</param>
        public void StopReel(SymbolId top, SymbolId center, SymbolId bottom)
        {
            targetSymbols = new SymbolId[] { top, center, bottom };
            isStopping = true;
        }

        private IEnumerator SpinRoutine()
        {
            float elapsedAcc = 0f;
            currentSpeed = 0f;

            // Phase 1: Acceleration
            while (elapsedAcc < accelerationTime && !isStopping)
            {
                elapsedAcc += Time.deltaTime;
                currentSpeed = Mathf.Lerp(0f, maxSpinSpeed, elapsedAcc / accelerationTime);
                ScrollSymbols(currentSpeed * Time.deltaTime);
                yield return null;
            }

            currentSpeed = maxSpinSpeed;

            // Phase 2: Steady Cruising Spin
            while (!isStopping)
            {
                ScrollSymbols(currentSpeed * Time.deltaTime);
                yield return null;
            }

            // Phase 3: Deceleration & Outcome Injection
            // Smoothly align the reel to place the 3 target symbols exactly into the visible window
            yield return StartCoroutine(SettleOutcomeRoutine());

            isSpinning = false;
            isStopping = false;
            spinCoroutine = null;

            OnReelStopped?.Invoke(reelIndex);
        }

        private void ScrollSymbols(float deltaY)
        {
            float minY = -2.5f * CellStep;
            float maxY = 2.5f * CellStep;

            for (int i = 0; i < symbolViews.Count; i++)
            {
                var rt = symbolViews[i].RectTransform;
                Vector2 pos = rt.anchoredPosition;
                pos.y -= deltaY;

                // When symbol passes the bottom threshold, wrap it back to the top
                if (pos.y < minY)
                {
                    pos.y += symbolViews.Count * CellStep;
                    // Assign a fresh randomized symbol as it recycles at the top
                    if (paytable != null && rng != null)
                    {
                        SymbolId nextSym = rng.GetRandomSymbol(paytable);
                        Sprite spr = spriteMap.ContainsKey(nextSym) ? spriteMap[nextSym] : null;
                        symbolViews[i].SetSymbol(nextSym, spr);
                    }
                }

                rt.anchoredPosition = pos;
            }
        }

        /// <summary>
        /// Smoothly lands the reel on the target outcome symbols with a realistic elastic bounce.
        /// </summary>
        private IEnumerator SettleOutcomeRoutine()
        {
            // Inject the 3 target symbols into the 3 visible views:
            // View 0 = Top (+CellStep)
            // View 1 = Center (0)
            // View 2 = Bottom (-CellStep)
            // View 3 = Top Buffer (+2*CellStep)
            // View 4 = Bottom Buffer (-2*CellStep)
            if (targetSymbols != null && targetSymbols.Length >= 3)
            {
                SetViewSymbol(0, targetSymbols[0]); // Top
                SetViewSymbol(1, targetSymbols[1]); // Center
                SetViewSymbol(2, targetSymbols[2]); // Bottom
                SetViewSymbol(3, (SymbolId)(((int)targetSymbols[0] + 1) % 4));
                SetViewSymbol(4, (SymbolId)(((int)targetSymbols[2] + 2) % 4));
            }

            // Step 1: Overshoot downwards past the center rest position
            float overshootDist = bounceOvershoot;
            float halfBounce = bounceDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfBounce)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfBounce;
                float currentOffset = Mathf.Lerp(0f, -overshootDist, EaseOutQuad(t));
                ApplyReelRestPositions(currentOffset);
                yield return null;
            }

            // Step 2: Harmonic Spring Rebound back to exact 0 rest position
            elapsed = 0f;
            while (elapsed < halfBounce)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfBounce;
                // Elastic rebound easing
                float currentOffset = Mathf.Lerp(-overshootDist, 0f, EaseOutElastic(t));
                ApplyReelRestPositions(currentOffset);
                yield return null;
            }

            // Final snap to exact resting coordinates
            ApplyReelRestPositions(0f);
        }

        private void SetViewSymbol(int viewIndex, SymbolId id)
        {
            if (viewIndex >= 0 && viewIndex < symbolViews.Count)
            {
                Sprite spr = spriteMap.ContainsKey(id) ? spriteMap[id] : null;
                symbolViews[viewIndex].SetSymbol(id, spr);
            }
        }

        private void ApplyReelRestPositions(float offset)
        {
            // View 0 = Top (+CellStep)
            // View 1 = Center (0)
            // View 2 = Bottom (-CellStep)
            // View 3 = Upper Buffer (+2*CellStep)
            // View 4 = Lower Buffer (-2*CellStep)
            float[] baseOffsets = new float[] { CellStep, 0f, -CellStep, 2f * CellStep, -2f * CellStep };
            for (int i = 0; i < symbolViews.Count && i < baseOffsets.Length; i++)
            {
                symbolViews[i].RectTransform.anchoredPosition = new Vector2(0f, baseOffsets[i] + offset);
            }
        }

        public ReelSymbolView GetSymbolViewAtRow(int rowIndex)
        {
            // Row 0 = Top, Row 1 = Center, Row 2 = Bottom
            if (rowIndex >= 0 && rowIndex < 3 && rowIndex < symbolViews.Count)
            {
                return symbolViews[rowIndex];
            }
            return null;
        }

        private float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        private float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            float p = 0.3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
        }
    }
}
