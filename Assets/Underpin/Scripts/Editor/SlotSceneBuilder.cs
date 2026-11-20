using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Underpin.SlotMachine.Editor
{
    using Underpin.SlotMachine.Audio;
    using Underpin.SlotMachine.Core;
    using Underpin.SlotMachine.Reels;
    using Underpin.SlotMachine.UI;

    /// <summary>
    /// Editor automation tool that constructs the entire playable SlotMachineScene
    /// with pixel-perfect UI, responsive Canvas hierarchy, masked reels,
    /// audio triggers, modals, and complete component wiring.
    /// </summary>
    public static class SlotSceneBuilder
    {
        [MenuItem("Underpin/Setup/2. Build Complete Slot Machine Scene")]
        public static void BuildScene()
        {
            Debug.Log("[SlotSceneBuilder] Starting complete Slot Machine Scene assembly...");

            // Step 1: Ensure all textures are imported as Sprites
            SlotTextureImporterHelper.ConfigureAllTextures();

            // Step 2: Load Sprites
            Sprite bgSprite = LoadSprite("bg_gradient.png");
            Sprite boxSprite = LoadSprite("slot_machine_Middle_box.png");
            Sprite popupSprite = LoadSprite("popup.png");

            List<Sprite> cabinetFrames = new List<Sprite>
            {
                LoadSprite("slot-machine1.png"),
                LoadSprite("slot-machine2.png"),
                LoadSprite("slot-machine3.png"),
                LoadSprite("slot-machine4.png"),
                LoadSprite("slot-machine5.png")
            };

            Dictionary<SymbolId, Sprite> symbolSprites = new Dictionary<SymbolId, Sprite>
            {
                { SymbolId.LuckySeven, LoadSprite("slot-symbol1.png") },
                { SymbolId.Cherry, LoadSprite("slot-symbol2.png") },
                { SymbolId.Bell, LoadSprite("slot-symbol3.png") },
                { SymbolId.Bar, LoadSprite("slot-symbol4.png") }
            };

            Sprite spinNormal = LoadSprite("btn_spin_normal.png");
            Sprite spinHover = LoadSprite("btn_spin_hover.png");
            Sprite spinPressed = LoadSprite("btn_spin_pressed.png");

            Sprite minusNormal = LoadSprite("btn_bet_minus_normal.png");
            Sprite minusPressed = LoadSprite("btn_bet_minus_pressed.png");

            Sprite plusNormal = LoadSprite("btn_bet_plus_normal.png");
            Sprite plusPressed = LoadSprite("btn_bet_plus_pressed.png");

            Sprite yesNormal = LoadSprite("btn_yes_normal.png");
            Sprite yesPressed = LoadSprite("btn_yes_pressed.png");

            Sprite noNormal = LoadSprite("btn_no_normal.png");
            Sprite noPressed = LoadSprite("btn_no_pressed.png");

            // Step 3: Load Audio Clips
            AudioClip clickClip = LoadAudio("button_click.wav");
            AudioClip startClip = LoadAudio("spin_start.wav");
            AudioClip loopClip = LoadAudio("reel_loop.wav");
            AudioClip stopClip = LoadAudio("reel_stop.wav");
            AudioClip smallWinClip = LoadAudio("win_small.wav");
            AudioClip bigWinClip = LoadAudio("win_big.wav");
            AudioClip jackpotClip = LoadAudio("jackpot.wav");

            // Step 4: Create new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            GameObject camGo = new GameObject("Main Camera");
            Camera cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.14f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;

            // 2. EventSystem
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // 3. Canvas
            GameObject canvasGo = new GameObject("SlotCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 4. Background
            GameObject bgGo = CreateUIObject("Background", canvasGo.transform);
            SetStretch(bgGo.GetComponent<RectTransform>());
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.color = Color.white;

            // 5. Machine Root
            GameObject machineRoot = CreateUIObject("SlotMachineRoot", canvasGo.transform);
            RectTransform machineRootRect = machineRoot.GetComponent<RectTransform>();
            machineRootRect.sizeDelta = new Vector2(900, 700);
            machineRootRect.anchoredPosition = new Vector2(0, 40);

            // Cabinet Graphic (with lever animation)
            GameObject cabinetGo = CreateUIObject("CabinetGraphic", machineRoot.transform);
            RectTransform cabinetRect = cabinetGo.GetComponent<RectTransform>();
            cabinetRect.sizeDelta = new Vector2(816, 624);
            cabinetRect.anchoredPosition = Vector2.zero;
            Image cabinetImg = cabinetGo.AddComponent<Image>();
            cabinetImg.sprite = cabinetFrames[0];
            LeverAnimationView leverView = cabinetGo.AddComponent<LeverAnimationView>();
            leverView.SetFrames(cabinetFrames);

            // Middle Box (Reel Frame Window)
            GameObject middleBoxGo = CreateUIObject("MiddleBox", machineRoot.transform);
            RectTransform middleBoxRect = middleBoxGo.GetComponent<RectTransform>();
            middleBoxRect.sizeDelta = new Vector2(658, 277);
            // Center aligns inside cabinet window
            middleBoxRect.anchoredPosition = new Vector2(-42, 12);
            Image middleBoxImg = middleBoxGo.AddComponent<Image>();
            middleBoxImg.sprite = boxSprite;

            // Reels Mask Window
            GameObject reelsWindowGo = CreateUIObject("ReelsMaskWindow", middleBoxGo.transform);
            RectTransform reelsWindowRect = reelsWindowGo.GetComponent<RectTransform>();
            reelsWindowRect.sizeDelta = new Vector2(490, 240);
            reelsWindowRect.anchoredPosition = Vector2.zero;
            reelsWindowGo.AddComponent<RectMask2D>();

            // Create Paytable Data Asset
            string paytablePath = "Assets/Underpin/PaytableData.asset";
            PaytableData paytable = AssetDatabase.LoadAssetAtPath<PaytableData>(paytablePath);
            if (paytable == null)
            {
                paytable = ScriptableObject.CreateInstance<PaytableData>();
                paytable.InitializeDefaults(symbolSprites);
                AssetDatabase.CreateAsset(paytable, paytablePath);
            }
            else
            {
                paytable.InitializeDefaults(symbolSprites);
                EditorUtility.SetDirty(paytable);
            }

            ISlotRNG initRng = new SlotRNGService();

            // 3 Reel Columns
            List<ReelStripController> reelControllers = new List<ReelStripController>();
            float[] reelXPositions = new float[] { -142f, 0f, 142f };

            for (int r = 0; r < 3; r++)
            {
                GameObject reelColGo = CreateUIObject($"ReelColumn_{r}", reelsWindowGo.transform);
                RectTransform reelColRect = reelColGo.GetComponent<RectTransform>();
                reelColRect.sizeDelta = new Vector2(110, 240);
                reelColRect.anchoredPosition = new Vector2(reelXPositions[r], 0);

                ReelStripController reelCtrl = reelColGo.AddComponent<ReelStripController>();
                reelCtrl.Initialize(r, paytable, initRng, symbolSprites);
                reelControllers.Add(reelCtrl);
            }

            // 6. HUD / Bottom Control Panel
            GameObject hudGo = CreateUIObject("HUD_Panel", canvasGo.transform);
            RectTransform hudRect = hudGo.GetComponent<RectTransform>();
            hudRect.sizeDelta = new Vector2(1920, 320);
            hudRect.anchorMin = new Vector2(0.5f, 0f);
            hudRect.anchorMax = new Vector2(0.5f, 0f);
            hudRect.pivot = new Vector2(0.5f, 0f);
            hudRect.anchoredPosition = Vector2.zero;

            // Balance Display Box
            GameObject balBox = CreateDisplayCard("BalanceBox", hudGo.transform, new Vector2(-420, 100), new Vector2(210, 90), "BALANCE", "1,000", out Text balValText);

            // Bet Minus Button
            GameObject minusBtnGo = CreateButton("Btn_BetMinus", hudGo.transform, new Vector2(-220, 100), new Vector2(75, 75), minusNormal, minusPressed, "-");
            Button minusBtn = minusBtnGo.GetComponent<Button>();

            // Bet Display Box
            GameObject betBox = CreateDisplayCard("BetBox", hudGo.transform, new Vector2(-110, 100), new Vector2(120, 90), "BET", "10", out Text betValText);

            // Bet Plus Button
            GameObject plusBtnGo = CreateButton("Btn_BetPlus", hudGo.transform, new Vector2(0, 100), new Vector2(75, 75), plusNormal, plusPressed, "+");
            Button plusBtn = plusBtnGo.GetComponent<Button>();

            // Win Display Box
            GameObject winBox = CreateDisplayCard("WinBox", hudGo.transform, new Vector2(170, 100), new Vector2(200, 90), "WIN", "0", out Text winValText);
            winValText.color = new Color(1f, 0.9f, 0.2f);

            // Spin Button
            GameObject spinBtnGo = CreateButton("Btn_Spin", hudGo.transform, new Vector2(380, 100), new Vector2(150, 150), spinNormal, spinPressed, "");
            Button spinBtn = spinBtnGo.GetComponent<Button>();
            // Add hover state
            var ss = spinBtn.spriteState;
            ss.highlightedSprite = spinHover;
            ss.pressedSprite = spinPressed;
            spinBtn.spriteState = ss;

            // Auto Spin Button
            GameObject autoBtnGo = CreateButton("Btn_AutoSpin", hudGo.transform, new Vector2(510, 100), new Vector2(85, 65), null, null, "AUTO");
            Button autoBtn = autoBtnGo.GetComponent<Button>();
            Text autoBadgeText = autoBtnGo.GetComponentInChildren<Text>();

            // Top Status Banner
            GameObject topBannerGo = CreateUIObject("TopBanner", canvasGo.transform);
            RectTransform topBannerRect = topBannerGo.GetComponent<RectTransform>();
            topBannerRect.anchorMin = new Vector2(0.5f, 1f);
            topBannerRect.anchorMax = new Vector2(0.5f, 1f);
            topBannerRect.pivot = new Vector2(0.5f, 1f);
            topBannerRect.sizeDelta = new Vector2(900, 80);
            topBannerRect.anchoredPosition = new Vector2(0, -20);
            Image topBannerBg = topBannerGo.AddComponent<Image>();
            topBannerBg.color = new Color(0f, 0f, 0f, 0.5f);

            Text messageText = CreateText("MessageText", topBannerGo.transform, new Vector2(0, 10), new Vector2(850, 40), "READY TO PLAY - PRESS SPIN!", 26, FontStyle.Bold, Color.white);
            messageText.alignment = TextAnchor.MiddleCenter;

            Text freeSpinsBadge = CreateText("FreeSpinsBadge", topBannerGo.transform, new Vector2(0, -20), new Vector2(850, 30), "", 20, FontStyle.Bold, Color.yellow);
            freeSpinsBadge.alignment = TextAnchor.MiddleCenter;

            // Info / Paytable Button
            GameObject infoBtnGo = CreateButton("Btn_Info", canvasGo.transform, new Vector2(-540, 460), new Vector2(100, 50), null, null, "PAYTABLE");
            Button infoBtn = infoBtnGo.GetComponent<Button>();

            // Reset Balance Button
            GameObject resetBtnGo = CreateButton("Btn_Reset", canvasGo.transform, new Vector2(540, 460), new Vector2(100, 50), null, null, "RESET");
            Button resetBtn = resetBtnGo.GetComponent<Button>();

            // 7. Modals
            // Win Popup Modal
            GameObject popupRoot = CreateUIObject("WinPopupModal", canvasGo.transform);
            SetStretch(popupRoot.GetComponent<RectTransform>());
            Image popupDim = popupRoot.AddComponent<Image>();
            popupDim.color = new Color(0f, 0f, 0f, 0.75f);
            CanvasGroup popupCg = popupRoot.AddComponent<CanvasGroup>();

            GameObject popupFrame = CreateUIObject("PopupFrame", popupRoot.transform);
            RectTransform popupFrameRect = popupFrame.GetComponent<RectTransform>();
            popupFrameRect.sizeDelta = new Vector2(700, 370);
            popupFrameRect.anchoredPosition = Vector2.zero;
            Image popupImg = popupFrame.AddComponent<Image>();
            popupImg.sprite = popupSprite;

            Text popTitle = CreateText("PopTitle", popupFrame.transform, new Vector2(0, 100), new Vector2(600, 50), "BIG WIN!", 42, FontStyle.Bold, new Color(1f, 0.85f, 0.1f));
            popTitle.alignment = TextAnchor.MiddleCenter;

            Text popAmount = CreateText("PopAmount", popupFrame.transform, new Vector2(0, 20), new Vector2(600, 60), "500 CREDITS", 48, FontStyle.Bold, Color.white);
            popAmount.alignment = TextAnchor.MiddleCenter;

            Text popSubtitle = CreateText("PopSubtitle", popupFrame.transform, new Vector2(0, -45), new Vector2(600, 35), "CONGRATULATIONS!", 22, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f));
            popSubtitle.alignment = TextAnchor.MiddleCenter;

            GameObject popCollectBtnGo = CreateButton("Btn_Collect", popupFrame.transform, new Vector2(-110, -115), new Vector2(190, 75), yesNormal, yesPressed, "COLLECT");
            Button popCollectBtn = popCollectBtnGo.GetComponent<Button>();

            GameObject popGambleBtnGo = CreateButton("Btn_Gamble", popupFrame.transform, new Vector2(110, -115), new Vector2(190, 75), noNormal, noPressed, "DOUBLE UP");
            Button popGambleBtn = popGambleBtnGo.GetComponent<Button>();

            WinPopupView winPopupView = popupRoot.AddComponent<WinPopupView>();
            SetPrivateField(winPopupView, "canvasGroup", popupCg);
            SetPrivateField(winPopupView, "panelRect", popupFrameRect);
            SetPrivateField(winPopupView, "titleText", popTitle);
            SetPrivateField(winPopupView, "amountText", popAmount);
            SetPrivateField(winPopupView, "subtitleText", popSubtitle);
            SetPrivateField(winPopupView, "collectButton", popCollectBtn);
            SetPrivateField(winPopupView, "gambleButton", popGambleBtn);

            // Paytable Modal
            GameObject paytableModalGo = CreateUIObject("PaytableModal", canvasGo.transform);
            SetStretch(paytableModalGo.GetComponent<RectTransform>());
            Image ptDim = paytableModalGo.AddComponent<Image>();
            ptDim.color = new Color(0f, 0f, 0f, 0.85f);
            CanvasGroup ptCg = paytableModalGo.AddComponent<CanvasGroup>();

            GameObject ptFrame = CreateUIObject("PaytableFrame", paytableModalGo.transform);
            RectTransform ptFrameRect = ptFrame.GetComponent<RectTransform>();
            ptFrameRect.sizeDelta = new Vector2(800, 520);
            ptFrameRect.anchoredPosition = Vector2.zero;
            Image ptImg = ptFrame.AddComponent<Image>();
            ptImg.color = new Color(0.1f, 0.12f, 0.2f, 0.95f);

            Text ptTitle = CreateText("PtTitle", ptFrame.transform, new Vector2(0, 220), new Vector2(700, 45), "PAYTABLE & RULES", 32, FontStyle.Bold, Color.yellow);
            ptTitle.alignment = TextAnchor.MiddleCenter;

            // Paytable Rows
            string[] symNames = new string[] { "Lucky 7 (Jackpot)", "Golden Bell", "BAR (Free Spins Trigger)", "Cherries" };
            string[] symPays = new string[] { "3x = 100x Bet", "3x = 40x Bet", "3x = 20x Bet + 5 FREE SPINS", "3x = 10x Bet | 2x = 2x Bet" };
            SymbolId[] sIds = new SymbolId[] { SymbolId.LuckySeven, SymbolId.Bell, SymbolId.Bar, SymbolId.Cherry };

            for (int i = 0; i < 4; i++)
            {
                float rowY = 140 - (i * 75);
                GameObject symRow = CreateUIObject($"Row_{sIds[i]}", ptFrame.transform);
                RectTransform symRowRect = symRow.GetComponent<RectTransform>();
                symRowRect.sizeDelta = new Vector2(700, 65);
                symRowRect.anchoredPosition = new Vector2(0, rowY);

                // Icon
                GameObject iconGo = CreateUIObject("Icon", symRow.transform);
                RectTransform iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.sizeDelta = new Vector2(55, 55);
                iconRect.anchoredPosition = new Vector2(-280, 0);
                Image iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = symbolSprites[sIds[i]];

                // Name & Payout
                Text rText = CreateText("RowText", symRow.transform, new Vector2(60, 0), new Vector2(550, 50), $"{symNames[i]}\n{symPays[i]}", 20, FontStyle.Bold, Color.white);
                rText.alignment = TextAnchor.MiddleLeft;
            }

            GameObject ptCloseBtnGo = CreateButton("Btn_ClosePt", ptFrame.transform, new Vector2(0, -210), new Vector2(160, 55), null, null, "CLOSE");
            Button ptCloseBtn = ptCloseBtnGo.GetComponent<Button>();

            PaytableModalView paytableModalView = paytableModalGo.AddComponent<PaytableModalView>();
            SetPrivateField(paytableModalView, "canvasGroup", ptCg);
            SetPrivateField(paytableModalView, "closeButton", ptCloseBtn);

            // 8. SlotUIManager
            SlotUIManager uiManager = canvasGo.AddComponent<SlotUIManager>();
            SetPrivateField(uiManager, "balanceText", balValText);
            SetPrivateField(uiManager, "betText", betValText);
            SetPrivateField(uiManager, "winText", winValText);
            SetPrivateField(uiManager, "messageText", messageText);
            SetPrivateField(uiManager, "freeSpinsBadgeText", freeSpinsBadge);
            SetPrivateField(uiManager, "spinButton", spinBtn);
            SetPrivateField(uiManager, "betMinusButton", minusBtn);
            SetPrivateField(uiManager, "betPlusButton", plusBtn);
            SetPrivateField(uiManager, "autoSpinButton", autoBtn);
            SetPrivateField(uiManager, "paytableButton", infoBtn);
            SetPrivateField(uiManager, "resetBalanceButton", resetBtn);
            SetPrivateField(uiManager, "autoSpinBadgeText", autoBadgeText);
            SetPrivateField(uiManager, "winPopupView", winPopupView);
            SetPrivateField(uiManager, "paytableModalView", paytableModalView);

            // 9. SlotAudioManager
            GameObject audioGo = new GameObject("AudioManager");
            SlotAudioManager audioManager = audioGo.AddComponent<SlotAudioManager>();
            audioManager.SetClips(clickClip, startClip, loopClip, stopClip, smallWinClip, bigWinClip, jackpotClip);

            // 10. SlotGameManager
            GameObject gmGo = new GameObject("SlotGameManager");
            SlotGameManager gameManager = gmGo.AddComponent<SlotGameManager>();
            SetPrivateField(gameManager, "paytableData", paytable);
            SetPrivateField(gameManager, "reels", reelControllers);
            SetPrivateField(gameManager, "leverView", leverView);
            SetPrivateField(gameManager, "uiManager", uiManager);
            SetPrivateField(gameManager, "audioManager", audioManager);

            // Save Prefabs for modularity & evaluation requirements
            string prefabsDir = "Assets/Underpin/Prefabs";
            if (!Directory.Exists(prefabsDir)) Directory.CreateDirectory(prefabsDir);

            if (reelControllers.Count > 0 && reelControllers[0] != null)
            {
                PrefabUtility.SaveAsPrefabAsset(reelControllers[0].gameObject, $"{prefabsDir}/ReelColumn.prefab");
            }
            PrefabUtility.SaveAsPrefabAsset(popupRoot, $"{prefabsDir}/WinPopup.prefab");
            PrefabUtility.SaveAsPrefabAsset(paytableModalGo, $"{prefabsDir}/PaytableModal.prefab");

            // Step 5: Save Scene
            string scenePath = "Assets/Underpin/Scenes/SlotMachineScene.unity";
            string sceneDir = Path.GetDirectoryName(scenePath);
            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SlotSceneBuilder] Scene saved successfully to: {scenePath}");

            // Step 6: Set as default build scene
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            Debug.Log("[SlotSceneBuilder] Configured EditorBuildSettings scenes.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("================ SLOT MACHINE SETUP COMPLETE ================");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static GameObject CreateDisplayCard(string name, Transform parent, Vector2 pos, Vector2 size, string title, string initialValue, out Text valueText)
        {
            GameObject card = CreateUIObject(name, parent);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchoredPosition = pos;
            cardRt.sizeDelta = size;

            Image img = card.AddComponent<Image>();
            img.color = new Color(0.08f, 0.10f, 0.18f, 0.9f);

            Text tTitle = CreateText("Title", card.transform, new Vector2(0, 18), new Vector2(size.x - 10, 24), title, 16, FontStyle.Bold, new Color(0.7f, 0.75f, 0.85f));
            tTitle.alignment = TextAnchor.MiddleCenter;

            valueText = CreateText("Value", card.transform, new Vector2(0, -12), new Vector2(size.x - 10, 36), initialValue, 26, FontStyle.Bold, Color.white);
            valueText.alignment = TextAnchor.MiddleCenter;

            return card;
        }

        private static GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, Sprite normal, Sprite pressed, string label)
        {
            GameObject btnGo = CreateUIObject(name, parent);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchoredPosition = pos;
            btnRt.sizeDelta = size;

            Image img = btnGo.AddComponent<Image>();
            img.sprite = normal;
            if (normal == null)
            {
                img.color = new Color(0.2f, 0.25f, 0.4f);
            }
            else
            {
                img.color = Color.white;
            }

            Button btn = btnGo.AddComponent<Button>();
            if (pressed != null)
            {
                btn.transition = Selectable.Transition.SpriteSwap;
                var ss = btn.spriteState;
                ss.pressedSprite = pressed;
                btn.spriteState = ss;
            }
            else
            {
                btn.transition = Selectable.Transition.ColorTint;
            }

            if (!string.IsNullOrEmpty(label))
            {
                Text txt = CreateText("Label", btnGo.transform, Vector2.zero, size, label, 20, FontStyle.Bold, Color.white);
                txt.alignment = TextAnchor.MiddleCenter;
            }

            return btnGo;
        }

        private static Text CreateText(string name, Transform parent, Vector2 pos, Vector2 size, string text, int fontSize, FontStyle fontStyle, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text t = go.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = fontSize;
            t.fontStyle = fontStyle;
            t.color = color;
            return t;
        }

        private static Sprite LoadSprite(string fileName)
        {
            string path = $"Assets/Underpin/Textures/{fileName}";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static AudioClip LoadAudio(string fileName)
        {
            string path = $"Assets/Underpin/Audio/{fileName}";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
