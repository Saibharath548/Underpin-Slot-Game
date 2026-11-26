using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    using Underpin.SlotMachine.Core;

    /// <summary>
    /// Primary HUD controller managing all game interface elements:
    /// balance, current bet, last win, animated coin tickers, action buttons,
    /// status banner, and bonus mode indicators.
    /// </summary>
    public class SlotUIManager : MonoBehaviour
    {
        [Header("Readout Texts")]
        [SerializeField] private Text balanceText;
        [SerializeField] private Text betText;
        [SerializeField] private Text winText;
        [SerializeField] private Text messageText;
        [SerializeField] private Text freeSpinsBadgeText;

        [Header("Action Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betMinusButton;
        [SerializeField] private Button betPlusButton;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private Button paytableButton;
        [SerializeField] private Button resetBalanceButton;

        [Header("Button Labels / Badges")]
        [SerializeField] private Text autoSpinBadgeText;
        [SerializeField] private GameObject freeSpinsBanner;

        [Header("Modals")]
        [SerializeField] private WinPopupView winPopupView;
        [SerializeField] private PaytableModalView paytableModalView;

        public event Action OnSpinClicked;
        public event Action OnBetMinusClicked;
        public event Action OnBetPlusClicked;
        public event Action OnAutoSpinClicked;
        public event Action OnPaytableClicked;
        public event Action OnResetBalanceClicked;

        public WinPopupView WinPopup => winPopupView;
        public PaytableModalView PaytableModal => paytableModalView;

        private Coroutine winCounterCoroutine;
        private int displayedWinCredits = 0;

        private void Awake()
        {
            EnsureSingleAudioListener();

            ConfigureButtonFeedback(spinButton, new Color(1f, 0.78f, 0.24f));
            ConfigureButtonFeedback(betMinusButton, new Color(0.34f, 0.78f, 0.95f));
            ConfigureButtonFeedback(betPlusButton, new Color(0.34f, 0.78f, 0.95f));
            ConfigureButtonFeedback(autoSpinButton, new Color(0.30f, 0.82f, 0.60f));
            ConfigureButtonFeedback(paytableButton, new Color(0.34f, 0.78f, 0.95f));
            ConfigureButtonFeedback(resetBalanceButton, new Color(0.95f, 0.55f, 0.35f));

            if (spinButton != null) spinButton.onClick.AddListener(() => OnSpinClicked?.Invoke());
            if (betMinusButton != null) betMinusButton.onClick.AddListener(() => OnBetMinusClicked?.Invoke());
            if (betPlusButton != null) betPlusButton.onClick.AddListener(() => OnBetPlusClicked?.Invoke());
            if (autoSpinButton != null) autoSpinButton.onClick.AddListener(() => OnAutoSpinClicked?.Invoke());
            if (paytableButton != null) paytableButton.onClick.AddListener(() => OnPaytableClicked?.Invoke());
            if (resetBalanceButton != null) resetBalanceButton.onClick.AddListener(() => OnResetBalanceClicked?.Invoke());

        }

        private static void EnsureSingleAudioListener()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>();
            AudioListener keeper = null;
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
                keeper = mainCamera.GetComponent<AudioListener>();

            if (keeper == null && listeners.Length > 0)
                keeper = listeners[0];

            if (keeper == null && mainCamera != null)
                keeper = mainCamera.gameObject.AddComponent<AudioListener>();

            if (keeper != null)
                keeper.enabled = true;

            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i] != keeper)
                    listeners[i].enabled = false;
            }
        }

        private static void ConfigureButtonFeedback(Button button, Color accent)
        {
            if (button == null) return;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, accent, 0.22f);
            colors.pressedColor = Color.Lerp(accent, Color.black, 0.12f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.55f, 0.58f, 0.64f, 0.55f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }

        public void UpdateBalance(int balance)
        {
            if (balanceText != null)
                balanceText.text = balance.ToString("N0");
        }

        public void UpdateBet(int bet)
        {
            if (betText != null)
                betText.text = bet.ToString("N0");
        }

        public void UpdateWin(int win, bool animate = true)
        {
            if (winCounterCoroutine != null)
            {
                StopCoroutine(winCounterCoroutine);
                winCounterCoroutine = null;
            }

            if (!animate || win == 0)
            {
                displayedWinCredits = win;
                if (winText != null)
                    winText.text = win > 0 ? win.ToString("N0") : "0";
            }
            else
            {
                winCounterCoroutine = StartCoroutine(AnimateWinCounter(displayedWinCredits, win, 1.0f));
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
                messageText.text = message;
        }

        public void SetAutoSpinState(bool isAutoSpinning, int remainingSpins = 0)
        {
            if (autoSpinBadgeText != null)
            {
                autoSpinBadgeText.text = isAutoSpinning ? $"AUTO ({remainingSpins})" : "AUTO";
                autoSpinBadgeText.color = isAutoSpinning ? Color.green : Color.white;
            }
        }

        public void SetFreeSpinsState(bool isFreeSpinsActive, int remainingSpins = 0, int multiplier = 1)
        {
            if (freeSpinsBanner != null)
                freeSpinsBanner.SetActive(isFreeSpinsActive);

            if (freeSpinsBadgeText != null)
            {
                freeSpinsBadgeText.text = isFreeSpinsActive
                    ? $"FREE SPINS: {remainingSpins} (x{multiplier})"
                    : "";
            }
        }

        public void SetSpinInteractable(bool interactable)
        {
            if (spinButton != null)
                spinButton.interactable = interactable;

            if (betMinusButton != null)
                betMinusButton.interactable = interactable;

            if (betPlusButton != null)
                betPlusButton.interactable = interactable;
        }

        private IEnumerator AnimateWinCounter(int startValue, int endValue, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int current = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
                displayedWinCredits = current;
                if (winText != null)
                    winText.text = current.ToString("N0");
                yield return null;
            }

            displayedWinCredits = endValue;
            if (winText != null)
                winText.text = endValue.ToString("N0");
            winCounterCoroutine = null;
        }
    }
}
