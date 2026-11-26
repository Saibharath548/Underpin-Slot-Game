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
            // A zero-scale root makes every control invisible and prevents raycasts.
            // Keep the HUD usable even if a scene is regenerated with bad transform data.
            transform.localScale = Vector3.one;
            EnsureSingleAudioListener();
            ApplyVisualLayout();

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            ConfigureButtonFeedback(spinButton, new Color(1f, 0.78f, 0.24f));
            ConfigureButtonFeedback(betMinusButton, new Color(0.34f, 0.78f, 0.95f));
            ConfigureButtonFeedback(betPlusButton, new Color(0.34f, 0.78f, 0.95f));
            ConfigureButtonFeedback(autoSpinButton, new Color(0.30f, 0.82f, 0.60f));
            ConfigureButtonFeedback(paytableButton, new Color(0.34f, 0.78f, 0.95f));
            ConfigureButtonFeedback(resetBalanceButton, new Color(0.95f, 0.55f, 0.35f));

            // Keep the top utility controls readable at the reference resolution.
            ResizeTopButton(paytableButton, new Vector2(160f, 54f));
            ResizeTopButton(resetBalanceButton, new Vector2(130f, 54f));

            if (spinButton != null) spinButton.onClick.AddListener(() => OnSpinClicked?.Invoke());
            if (betMinusButton != null) betMinusButton.onClick.AddListener(() => OnBetMinusClicked?.Invoke());
            if (betPlusButton != null) betPlusButton.onClick.AddListener(() => OnBetPlusClicked?.Invoke());
            if (autoSpinButton != null) autoSpinButton.onClick.AddListener(() => OnAutoSpinClicked?.Invoke());
            if (paytableButton != null) paytableButton.onClick.AddListener(() => OnPaytableClicked?.Invoke());
            if (resetBalanceButton != null) resetBalanceButton.onClick.AddListener(() => OnResetBalanceClicked?.Invoke());

            CreateHowToPlayOverlay();
        }

        private void ApplyVisualLayout()
        {
            Transform background = transform.Find("Background");
            Image backgroundImage = background != null ? background.GetComponent<Image>() : null;
            if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
                backgroundImage.color = new Color(0.035f, 0.045f, 0.09f, 1f);
            }

            Transform machine = transform.Find("SlotMachineRoot");
            if (machine != null)
            {
                RectTransform machineRect = machine.GetComponent<RectTransform>();
                if (machineRect != null)
                {
                    machineRect.anchorMin = new Vector2(0.5f, 0.5f);
                    machineRect.anchorMax = new Vector2(0.5f, 0.5f);
                    machineRect.pivot = new Vector2(0.5f, 0.5f);
                    machineRect.sizeDelta = new Vector2(900f, 700f);
                    machineRect.anchoredPosition = new Vector2(0f, 38f);
                    machineRect.localScale = Vector3.one;
                }

                Transform middleBox = machine.Find("MiddleBox");
                if (middleBox != null)
                {
                    RectTransform middleRect = middleBox.GetComponent<RectTransform>();
                    if (middleRect != null)
                    {
                        middleRect.anchorMin = new Vector2(0.5f, 0.5f);
                        middleRect.anchorMax = new Vector2(0.5f, 0.5f);
                        middleRect.pivot = new Vector2(0.5f, 0.5f);
                        middleRect.sizeDelta = new Vector2(658f, 277f);
                        middleRect.anchoredPosition = new Vector2(-42f, 12f);
                    }

                    Transform reelsWindow = middleBox.Find("ReelsMaskWindow");
                    if (reelsWindow != null)
                    {
                        RectTransform windowRect = reelsWindow.GetComponent<RectTransform>();
                        if (windowRect != null)
                        {
                            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
                            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
                            windowRect.pivot = new Vector2(0.5f, 0.5f);
                            windowRect.sizeDelta = new Vector2(490f, 190f);
                            windowRect.anchoredPosition = Vector2.zero;
                        }

                        // A transparent Graphic makes the clipping region explicit and reliable.
                        Image maskGraphic = reelsWindow.GetComponent<Image>();
                        if (maskGraphic == null) maskGraphic = reelsWindow.gameObject.AddComponent<Image>();
                        maskGraphic.color = new Color(1f, 1f, 1f, 0f);
                        maskGraphic.raycastTarget = false;
                        if (reelsWindow.GetComponent<RectMask2D>() == null)
                            reelsWindow.gameObject.AddComponent<RectMask2D>();

                        float[] reelX = { -142f, 0f, 142f };
                        for (int i = 0; i < 3; i++)
                        {
                            Transform column = reelsWindow.Find("ReelColumn_" + i);
                            if (column == null) continue;
                            RectTransform columnRect = column.GetComponent<RectTransform>();
                            if (columnRect == null) continue;
                            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
                            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
                            columnRect.pivot = new Vector2(0.5f, 0.5f);
                            columnRect.sizeDelta = new Vector2(110f, 190f);
                            columnRect.anchoredPosition = new Vector2(reelX[i], 0f);
                            columnRect.localScale = Vector3.one;
                        }
                    }
                }
            }

            Transform hud = transform.Find("HUD_Panel");
            if (hud != null)
            {
                RectTransform hudRect = hud.GetComponent<RectTransform>();
                if (hudRect != null)
                {
                    hudRect.anchorMin = new Vector2(0.5f, 0f);
                    hudRect.anchorMax = new Vector2(0.5f, 0f);
                    hudRect.pivot = new Vector2(0.5f, 0f);
                    hudRect.sizeDelta = new Vector2(1200f, 230f);
                    hudRect.anchoredPosition = Vector2.zero;
                }

                if (hud.Find("HUD_Backplate") == null)
                {
                    GameObject dock = new GameObject("HUD_Backplate", typeof(RectTransform), typeof(Image));
                    dock.transform.SetParent(hud, false);
                    dock.transform.SetAsFirstSibling();
                    RectTransform dockRect = dock.GetComponent<RectTransform>();
                    dockRect.anchorMin = new Vector2(0.5f, 0f);
                    dockRect.anchorMax = new Vector2(0.5f, 0f);
                    dockRect.pivot = new Vector2(0.5f, 0f);
                    dockRect.sizeDelta = new Vector2(1160f, 185f);
                    dockRect.anchoredPosition = new Vector2(0f, 20f);
                    Image dockImage = dock.GetComponent<Image>();
                    dockImage.color = new Color(0.035f, 0.05f, 0.105f, 0.96f);
                    dockImage.raycastTarget = false;
                }

                SetChildRect(hud, "BalanceBox", new Vector2(-420f, 110f), new Vector2(190f, 92f));
                SetChildRect(hud, "Btn_BetMinus", new Vector2(-245f, 110f), new Vector2(64f, 64f));
                SetChildRect(hud, "BetBox", new Vector2(-130f, 110f), new Vector2(130f, 92f));
                SetChildRect(hud, "Btn_BetPlus", new Vector2(-20f, 110f), new Vector2(64f, 64f));
                SetChildRect(hud, "WinBox", new Vector2(145f, 110f), new Vector2(180f, 92f));
                SetChildRect(hud, "Btn_Spin", new Vector2(365f, 110f), new Vector2(126f, 126f));
                SetChildRect(hud, "Btn_AutoSpin", new Vector2(505f, 110f), new Vector2(100f, 64f));
            }

            PlaceTopButton(paytableButton, new Vector2(220f, -24f), new Vector2(160f, 54f), false);
            PlaceTopButton(resetBalanceButton, new Vector2(-30f, -24f), new Vector2(130f, 54f), true);

            Transform topBanner = transform.Find("TopBanner");
            if (topBanner != null)
            {
                RectTransform bannerRect = topBanner.GetComponent<RectTransform>();
                if (bannerRect != null)
                {
                    bannerRect.sizeDelta = new Vector2(760f, 70f);
                    bannerRect.anchoredPosition = new Vector2(0f, -18f);
                }
            }

            if (spinButton != null)
            {
                Image spinImage = spinButton.GetComponent<Image>();
                if (spinImage != null)
                {
                    spinImage.sprite = CreateSpinButtonSprite();
                    spinImage.color = Color.white;
                    spinImage.preserveAspect = true;
                    spinImage.type = Image.Type.Simple;
                }

                spinButton.transition = Selectable.Transition.ColorTint;
                ColorBlock colors = spinButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.92f, 0.62f, 1f);
                colors.pressedColor = new Color(0.78f, 0.62f, 0.3f, 1f);
                colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.75f);
                spinButton.colors = colors;

                Text spinLabel = CreateOverlayText(spinButton.transform, "SPIN", 24, FontStyle.Bold,
                    Color.white, Vector2.zero, new Vector2(112f, 112f));
                spinLabel.name = "SpinActionLabel";
                spinLabel.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static void SetChildRect(Transform parent, string childName, Vector2 position, Vector2 size)
        {
            Transform child = parent.Find(childName);
            if (child == null) return;
            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PlaceTopButton(Button button, Vector2 position, Vector2 size, bool rightAligned)
        {
            if (button == null) return;
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = new Vector2(rightAligned ? 1f : 0f, 1f);
            rect.anchorMax = new Vector2(rightAligned ? 1f : 0f, 1f);
            rect.pivot = new Vector2(rightAligned ? 1f : 0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            ResizeTopButton(button, size);
        }

        private static Sprite CreateSpinButtonSprite()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Runtime_SpinButton";
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color gold = new Color(1f, 0.68f, 0.12f, 1f);
            Color darkGold = new Color(0.48f, 0.25f, 0.04f, 1f);
            Color navy = new Color(0.12f, 0.16f, 0.34f, 1f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - 63.5f;
                float dy = y - 63.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                Color pixel = radius <= 62f && radius >= 54f ? darkGold :
                    radius < 54f && radius >= 48f ? gold :
                    radius < 48f ? navy : clear;
                texture.SetPixel(x, y, pixel);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 128f);
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

        private static void ResizeTopButton(Button button, Vector2 size)
        {
            if (button == null) return;
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = size;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Truncate;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
            }
        }

        private void CreateHowToPlayOverlay()
        {
            // Runtime-created so existing scenes receive the help screen without manual Inspector wiring.
            Transform canvasRoot = transform;
            GameObject helpButtonObject = new GameObject("Btn_HowToPlay", typeof(RectTransform), typeof(Image), typeof(Button));
            helpButtonObject.transform.SetParent(canvasRoot, false);
            RectTransform helpRect = helpButtonObject.GetComponent<RectTransform>();
            helpRect.anchorMin = new Vector2(0f, 1f);
            helpRect.anchorMax = new Vector2(0f, 1f);
            helpRect.pivot = new Vector2(0f, 1f);
            helpRect.anchoredPosition = new Vector2(28f, -24f);
            helpRect.sizeDelta = new Vector2(190f, 52f);
            Image helpImage = helpButtonObject.GetComponent<Image>();
            helpImage.color = new Color(0.12f, 0.20f, 0.38f, 1f);
            Button helpButton = helpButtonObject.GetComponent<Button>();
            ConfigureButtonFeedback(helpButton, new Color(0.34f, 0.78f, 0.95f));
            CreateOverlayText(helpButtonObject.transform, "HOW TO PLAY", 18, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(180f, 48f));

            GameObject overlay = new GameObject("HowToPlayOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(canvasRoot, false);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            Image dim = overlay.GetComponent<Image>();
            dim.color = new Color(0.015f, 0.02f, 0.06f, 0.82f);
            CanvasGroup group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject panel = new GameObject("HowToPlayPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(790f, 650f);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.10f, 0.20f, 1f);

            CreateOverlayText(panel.transform, "HOW TO PLAY", 38, FontStyle.Bold, new Color(1f, 0.82f, 0.22f), new Vector2(0f, 265f), new Vector2(700f, 58f));
            string instructions =
                "1. SET YOUR BET  -  Use the  -  and  +  buttons to choose your wager.\\n\\n" +
                "2. SPIN  -  Press the large SPIN button. Your bet is deducted before the reels roll.\\n\\n" +
                "3. MATCH SYMBOLS  -  Matching symbols on an active payline award credits. Open PAYTABLE to see every payout.\\n\\n" +
                "4. TRACK YOUR CREDITS  -  BALANCE is your available credits; WIN shows the latest payout.\\n\\n" +
                "5. AUTO SPIN  -  Press AUTO to start repeated spins. Press it again to stop.\\n\\n" +
                "6. BIG WIN  -  COLLECT keeps the prize. DOUBLE UP risks that prize in a 50/50 double-or-nothing gamble.";
            CreateOverlayText(panel.transform, instructions, 22, FontStyle.Normal, Color.white, new Vector2(0f, -5f), new Vector2(680f, 440f));

            GameObject closeObject = new GameObject("Btn_CloseHowToPlay", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(panel.transform, false);
            RectTransform closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 24f);
            closeRect.sizeDelta = new Vector2(220f, 58f);
            closeObject.GetComponent<Image>().color = new Color(0.55f, 0.32f, 0.08f, 1f);
            Button closeButton = closeObject.GetComponent<Button>();
            ConfigureButtonFeedback(closeButton, new Color(1f, 0.78f, 0.24f));
            CreateOverlayText(closeObject.transform, "GOT IT", 20, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(210f, 54f));

            helpButton.onClick.AddListener(() =>
            {
                overlay.transform.SetAsLastSibling();
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            });
            closeButton.onClick.AddListener(() =>
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                PlayerPrefs.SetInt("Underpin_Slot_HelpSeen", 1);
            });
            overlay.SetActive(false);
            helpButton.onClick.AddListener(() =>
            {
                overlay.SetActive(true);
                overlay.transform.SetAsLastSibling();
            });

            // Show the guide automatically on the first visit; the help button remains available later.
            if (PlayerPrefs.GetInt("Underpin_Slot_HelpSeen", 0) == 0)
            {
                overlay.SetActive(true);
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
        }

        private static Text CreateOverlayText(Transform parent, string value, int fontSize, FontStyle style, Color color, Vector2 position, Vector2 size)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = fontSize >= 30 || parent.name.StartsWith("Btn_") ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.15f;
            text.raycastTarget = false;
            return text;
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
