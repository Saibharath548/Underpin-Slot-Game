using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underpin.SlotMachine.Core
{
    using Underpin.SlotMachine.Audio;
    using Underpin.SlotMachine.Reels;
    using Underpin.SlotMachine.UI;

    /// <summary>
    /// Master game manager coordinating all slot machine systems:
    /// RNG outcome generation, balance/credits management, sequential reel spin
    /// and bounce landing, payline evaluation, bonus features, and UI presentation.
    /// </summary>
    public class SlotGameManager : MonoBehaviour
    {
        [Header("Core Services")]
        [SerializeField] private PaytableData paytableData;
        private SlotRNGService rngService;

        [Header("Reels & Display")]
        [SerializeField] private List<ReelStripController> reels = new List<ReelStripController>();
        [SerializeField] private LeverAnimationView leverView;

        [Header("UI & Audio")]
        [SerializeField] private SlotUIManager uiManager;
        [SerializeField] private SlotAudioManager audioManager;

        [Header("Wallet & Bet Settings")]
        [SerializeField] private int initialBalance = 1000;
        [SerializeField] private int[] betLevels = new int[] { 10, 20, 50, 100, 200, 500 };
        [SerializeField] private int activePaylines = 1; // Classic slot: centre horizontal payline only

        [Header("Timing Configuration")]
        [SerializeField] private float baseSpinDuration = 0.8f;
        [SerializeField] private float reelStopDelay = 0.35f;
        [SerializeField] private float anticipationDelay = 0.8f;

        private int currentBalance;
        private int betIndex = 0;
        private SlotState currentState = SlotState.Idle;
        private bool isAutoSpinActive = false;
        private int remainingAutoSpins = 0;

        // Bonus / Free Spins state
        private bool isFreeSpinsActive = false;
        private int remainingFreeSpins = 0;
        private int pendingGambleWin = 0;

        private SymbolId[,] currentOutcomeGrid;
        private SpinResult lastSpinResult;
        private int reelsStoppedCount = 0;
        private bool isInitialized = false;

        public int CurrentBalance => currentBalance;
        public int CurrentBet => betLevels[betIndex];
        public SlotState CurrentState => currentState;

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            // Wallet persistence
            currentBalance = PlayerPrefs.GetInt("Underpin_Slot_Balance", initialBalance);
            if (currentBalance <= 0) currentBalance = initialBalance;

            if (rngService == null)
            {
                rngService = new SlotRNGService();
            }

            if (paytableData == null)
            {
                paytableData = ScriptableObject.CreateInstance<PaytableData>();
                paytableData.InitializeDefaults();
            }

            // Runtime-only reel data must be rebuilt when the scene loads.
            // The previous scene builder initialized these objects in the Editor, but
            // private runtime collections and sprite maps are not serialized by Unity.
            var runtimeSpriteMap = new Dictionary<SymbolId, Sprite>();
            foreach (SymbolConfig symbol in paytableData.Symbols)
            {
                if (symbol != null && symbol.Sprite != null)
                    runtimeSpriteMap[symbol.Id] = symbol.Sprite;
            }

            if (reels == null || reels.Count != 3)
            {
                Debug.LogError("[SlotGameManager] Expected exactly 3 reel controllers. Check the SlotGameManager Inspector references.", this);
            }
            else
            {
                for (int i = 0; i < reels.Count; i++)
                {
                    if (reels[i] == null)
                    {
                        Debug.LogError($"[SlotGameManager] Reel {i} is not assigned.", this);
                        continue;
                    }

                    reels[i].Initialize(i, paytableData, rngService, runtimeSpriteMap);
                }
            }

            // Wire up UI events
            if (uiManager != null)
            {
                uiManager.OnSpinClicked += HandleSpinRequest;
                uiManager.OnBetMinusClicked += HandleBetMinus;
                uiManager.OnBetPlusClicked += HandleBetPlus;
                uiManager.OnAutoSpinClicked += HandleAutoSpinToggle;
                uiManager.OnPaytableClicked += HandlePaytableToggle;
                uiManager.OnResetBalanceClicked += HandleResetBalance;

                if (uiManager.WinPopup != null)
                {
                    uiManager.WinPopup.OnCollectClicked += HandlePopupCollect;
                    uiManager.WinPopup.OnGambleClicked += HandlePopupGamble;
                }

                uiManager.UpdateBalance(currentBalance);
                uiManager.UpdateBet(CurrentBet);
                uiManager.UpdateWin(0, false);
                uiManager.SetMessage("READY TO PLAY - PRESS SPIN!");
                uiManager.SetAutoSpinState(false);
                uiManager.SetFreeSpinsState(false);
            }

            // Wire up Reel events
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] == null) continue;
                reels[i].OnReelStopped -= HandleReelStopped;
                reels[i].OnReelStopped += HandleReelStopped;
            }

            currentState = SlotState.Idle;
        }

        private void HandleSpinRequest()
        {
            if (currentState != SlotState.Idle && currentState != SlotState.FreeSpins)
                return;

            // Insufficient funds check (free spins are wager-free)
            if (!isFreeSpinsActive && currentBalance < CurrentBet)
            {
                if (uiManager != null)
                    uiManager.SetMessage("INSUFFICIENT CREDITS! RESET OR LOWER BET.");
                audioManager?.PlayButtonClick();
                StopAutoSpin();
                return;
            }

            StartCoroutine(SpinFlowRoutine());
        }

        private IEnumerator SpinFlowRoutine()
        {
            currentState = SlotState.Spinning;
            uiManager?.SetSpinInteractable(false);
            uiManager?.UpdateWin(0, false);

            // 1. Deduct Wager (unless during Free Spins)
            if (!isFreeSpinsActive)
            {
                currentBalance -= CurrentBet;
                PlayerPrefs.SetInt("Underpin_Slot_Balance", currentBalance);
                uiManager?.UpdateBalance(currentBalance);
                uiManager?.SetMessage("GOOD LUCK!");
            }
            else
            {
                remainingFreeSpins--;
                uiManager?.SetFreeSpinsState(true, remainingFreeSpins, paytableData.FreeSpinsMultiplier);
                uiManager?.SetMessage($"FREE SPIN ({remainingFreeSpins} REMAINING) - 2X MULTIPLIER!");
            }

            audioManager?.PlaySpinStart();

            // 2. Animate Lever Pull
            bool leverPullDown = false;
            if (leverView != null)
            {
                leverView.PlayLeverPull(onPullDownComplete: () => leverPullDown = true);
                while (!leverPullDown)
                {
                    yield return null;
                }
            }

            // 3. RNG Outcome Generation
            currentOutcomeGrid = rngService.GenerateSpinOutcome(reels.Count, 3, paytableData);

            // Evaluate Win Outcome
            int mult = isFreeSpinsActive ? paytableData.FreeSpinsMultiplier : 1;
            int betPerLine = Mathf.Max(1, CurrentBet / activePaylines);
            lastSpinResult = WinEvaluator.Evaluate(currentOutcomeGrid, betPerLine, activePaylines, paytableData, mult);

            // 4. Launch Reel Spins
            audioManager?.StartReelLoop();
            reelsStoppedCount = 0;

            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].StartSpin();
            }

            // 5. Spin duration
            yield return new WaitForSeconds(baseSpinDuration);

            // 6. Sequential Reel Stopping
            // Check for Near Miss / Anticipation (Reels 0 and 1 match high symbol)
            bool isAnticipation = (reels.Count >= 3 &&
                                   currentOutcomeGrid[0, 1] == currentOutcomeGrid[1, 1] &&
                                   currentOutcomeGrid[0, 1] == SymbolId.LuckySeven);

            for (int i = 0; i < reels.Count; i++)
            {
                // Anticipate on the final reel if 2 matching Lucky Sevens landed!
                if (i == reels.Count - 1 && isAnticipation)
                {
                    uiManager?.SetMessage("JACKPOT CHANCE! HOLD YOUR BREATH...");
                    yield return new WaitForSeconds(anticipationDelay);
                }
                else if (i > 0)
                {
                    yield return new WaitForSeconds(reelStopDelay);
                }

                // Command reel to land on outcome: [Top, Center, Bottom]
                reels[i].StopReel(currentOutcomeGrid[i, 0], currentOutcomeGrid[i, 1], currentOutcomeGrid[i, 2]);
            }

            // Wait for all reels to finish their elastic bounce
            while (reelsStoppedCount < reels.Count)
            {
                yield return null;
            }

            audioManager?.StopReelLoop();

            // 7. Evaluate and Celebrate Outcomes
            yield return StartCoroutine(ResolveSpinOutcomeRoutine());
        }

        private void HandleReelStopped(int reelIndex)
        {
            reelsStoppedCount++;
            audioManager?.PlayReelStop(reelIndex);
        }

        private IEnumerator ResolveSpinOutcomeRoutine()
        {
            currentState = SlotState.EvaluatingWin;

            if (lastSpinResult != null && lastSpinResult.IsWin)
            {
                currentState = SlotState.WinCelebration;

                // Credit winnings
                currentBalance += lastSpinResult.TotalPayoutCredits;
                PlayerPrefs.SetInt("Underpin_Slot_Balance", currentBalance);

                uiManager?.UpdateWin(lastSpinResult.TotalPayoutCredits, true);
                uiManager?.UpdateBalance(currentBalance);
                audioManager?.PlayWin(lastSpinResult.Grade);

                // Highlight winning symbols
                HighlightWinningSymbols(lastSpinResult);

                // Update HUD message
                string winMsg = lastSpinResult.IsJackpot
                    ? $"JACKPOT!! WON {lastSpinResult.TotalPayoutCredits} CREDITS!"
                    : $"WINNER! WON {lastSpinResult.TotalPayoutCredits} CREDITS ({lastSpinResult.TotalMultiplier}x)!";
                uiManager?.SetMessage(winMsg);

                // Big Win or Jackpot Modal Popup
                if (lastSpinResult.Grade == WinGrade.Jackpot || lastSpinResult.Grade == WinGrade.BigWin)
                {
                    pendingGambleWin = lastSpinResult.TotalPayoutCredits;
                    string popupTitle = lastSpinResult.IsJackpot ? "JACKPOT!" : "BIG WIN!";
                    uiManager?.WinPopup?.Show(popupTitle, lastSpinResult.TotalPayoutCredits.ToString("N0") + " CREDITS", "CONGRATULATIONS!", allowGamble: true);
                    currentState = SlotState.PopupOpen;
                    yield break;
                }

                // Free Spins trigger check
                if (lastSpinResult.TriggersFreeSpins && !isFreeSpinsActive)
                {
                    yield return new WaitForSeconds(1.0f);
                    ActivateFreeSpins(lastSpinResult.FreeSpinsAwarded);
                }

                yield return new WaitForSeconds(1.4f);
            }
            else
            {
                uiManager?.SetMessage("NO WIN - SPIN AGAIN!");
                yield return new WaitForSeconds(0.4f);
            }

            // Free Spins progression check
            if (isFreeSpinsActive && remainingFreeSpins <= 0)
            {
                isFreeSpinsActive = false;
                uiManager?.SetFreeSpinsState(false);
                uiManager?.SetMessage("FREE SPINS COMPLETED! RETURNING TO REGULAR PLAY.");
                yield return new WaitForSeconds(1.0f);
            }

            // Post-spin completion
            currentState = isFreeSpinsActive ? SlotState.FreeSpins : SlotState.Idle;
            uiManager?.SetSpinInteractable(true);

            // Auto-spin continuation
            if (isAutoSpinActive)
            {
                if (remainingAutoSpins > 0) remainingAutoSpins--;
                uiManager?.SetAutoSpinState(true, remainingAutoSpins);

                if (remainingAutoSpins > 0 || remainingAutoSpins == -1) // -1 for infinite
                {
                    yield return new WaitForSeconds(0.8f);
                    HandleSpinRequest();
                }
                else
                {
                    StopAutoSpin();
                }
            }
            else if (isFreeSpinsActive)
            {
                // Free spins automatically roll
                yield return new WaitForSeconds(0.8f);
                HandleSpinRequest();
            }
        }

        private void HighlightWinningSymbols(SpinResult result)
        {
            if (result == null || result.WinningLines == null) return;

            foreach (var line in result.WinningLines)
            {
                SymbolConfig cfg = paytableData.GetConfig(line.Symbol);
                Color glowColor = cfg != null ? cfg.GlowColor : Color.yellow;

                foreach (var pos in line.Positions)
                {
                    if (pos.x >= 0 && pos.x < reels.Count)
                    {
                        var symbolView = reels[pos.x].GetSymbolViewAtRow(pos.y);
                        symbolView?.StartWinHighlight(glowColor, 2.5f);
                    }
                }
            }
        }

        private void ActivateFreeSpins(int spins)
        {
            isFreeSpinsActive = true;
            remainingFreeSpins = spins;
            uiManager?.SetFreeSpinsState(true, remainingFreeSpins, paytableData.FreeSpinsMultiplier);
            uiManager?.WinPopup?.Show("FREE SPINS!", $"{spins} FREE SPINS AWARDED", "ALL WINS DOUBLED (2X)!", false);
            currentState = SlotState.PopupOpen;
        }

        private void HandlePopupCollect()
        {
            uiManager?.WinPopup?.Hide();
            currentState = isFreeSpinsActive ? SlotState.FreeSpins : SlotState.Idle;
            uiManager?.SetSpinInteractable(true);

            if (isFreeSpinsActive)
            {
                StartCoroutine(DelayedSpin(0.8f));
            }
            else if (isAutoSpinActive)
            {
                StartCoroutine(DelayedSpin(0.8f));
            }
        }

        private void HandlePopupGamble()
        {
            // 50/50 Gamble mini-game (Double or Nothing!)
            bool won = (rngService.NextDouble() >= 0.5);
            if (won)
            {
                int doubled = pendingGambleWin;
                currentBalance += doubled;
                PlayerPrefs.SetInt("Underpin_Slot_Balance", currentBalance);
                uiManager?.UpdateBalance(currentBalance);
                uiManager?.UpdateWin(pendingGambleWin * 2, true);
                audioManager?.PlayWin(WinGrade.BigWin);
                uiManager?.WinPopup?.Show("DOUBLE UP!", $"{pendingGambleWin * 2} CREDITS!", "YOU DOUBLED YOUR WIN!", false);
            }
            else
            {
                currentBalance -= pendingGambleWin;
                PlayerPrefs.SetInt("Underpin_Slot_Balance", currentBalance);
                uiManager?.UpdateBalance(currentBalance);
                uiManager?.UpdateWin(0, false);
                audioManager?.PlayButtonClick();
                uiManager?.WinPopup?.Show("BAD LUCK!", "0 CREDITS", "BETTER LUCK NEXT TIME!", false);
            }
            pendingGambleWin = 0;
        }

        private IEnumerator DelayedSpin(float delay)
        {
            yield return new WaitForSeconds(delay);
            HandleSpinRequest();
        }

        private void HandleBetMinus()
        {
            if (currentState != SlotState.Idle) return;
            audioManager?.PlayButtonClick();
            if (betIndex > 0)
            {
                betIndex--;
                uiManager?.UpdateBet(CurrentBet);
            }
        }

        private void HandleBetPlus()
        {
            if (currentState != SlotState.Idle) return;
            audioManager?.PlayButtonClick();
            if (betIndex < betLevels.Length - 1)
            {
                betIndex++;
                uiManager?.UpdateBet(CurrentBet);
            }
        }

        private void HandleAutoSpinToggle()
        {
            audioManager?.PlayButtonClick();
            if (isAutoSpinActive)
            {
                StopAutoSpin();
            }
            else
            {
                isAutoSpinActive = true;
                remainingAutoSpins = 20; // 20 auto-spins by default
                uiManager?.SetAutoSpinState(true, remainingAutoSpins);
                if (currentState == SlotState.Idle)
                {
                    HandleSpinRequest();
                }
            }
        }

        private void StopAutoSpin()
        {
            isAutoSpinActive = false;
            remainingAutoSpins = 0;
            uiManager?.SetAutoSpinState(false);
        }

        private void HandlePaytableToggle()
        {
            audioManager?.PlayButtonClick();
            if (uiManager?.PaytableModal != null)
            {
                if (uiManager.PaytableModal.gameObject.activeSelf)
                    uiManager.PaytableModal.Hide();
                else
                    uiManager.PaytableModal.Show();
            }
        }

        private void HandleResetBalance()
        {
            audioManager?.PlayButtonClick();
            currentBalance = initialBalance;
            PlayerPrefs.SetInt("Underpin_Slot_Balance", currentBalance);
            uiManager?.UpdateBalance(currentBalance);
            uiManager?.SetMessage("BALANCE RESET TO 1,000 CREDITS!");
        }
    }
}
