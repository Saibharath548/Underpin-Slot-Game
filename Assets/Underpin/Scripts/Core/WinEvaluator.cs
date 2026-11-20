using System;
using System.Collections.Generic;
using UnityEngine;

namespace Underpin.SlotMachine.Core
{
    /// <summary>
    /// Information about an individual winning payline.
    /// </summary>
    [Serializable]
    public class WinningLine
    {
        public PaylinePattern Pattern;
        public SymbolId Symbol;
        public int MatchCount;
        public int Multiplier;
        public int PayoutCredits;
        public Vector2Int[] Positions; // (reelIndex, rowIndex)

        public WinningLine(PaylinePattern pattern, SymbolId symbol, int matchCount, int multiplier, int payoutCredits, Vector2Int[] positions)
        {
            Pattern = pattern;
            Symbol = symbol;
            MatchCount = matchCount;
            Multiplier = multiplier;
            PayoutCredits = payoutCredits;
            Positions = positions;
        }
    }

    /// <summary>
    /// Aggregated result of evaluating a spin's outcome grid.
    /// </summary>
    [Serializable]
    public class SpinResult
    {
        public int TotalPayoutCredits;
        public int TotalMultiplier;
        public WinGrade Grade;
        public List<WinningLine> WinningLines = new List<WinningLine>();
        public bool IsJackpot;
        public bool TriggersFreeSpins;
        public int FreeSpinsAwarded;

        public bool IsWin => TotalPayoutCredits > 0;
    }

    /// <summary>
    /// Evaluates win conditions across the slot machine's outcome grid.
    /// Supports multi-line checks (Center, Top, Bottom, Diagonals),
    /// 3-of-a-kind jackpot matching, 2-of-a-kind cherry payouts, and bonus triggers.
    /// </summary>
    public static class WinEvaluator
    {
        // 5 standard paylines defined across 3 reels and 3 rows:
        // [reel, row] where row 0 = Top, row 1 = Center, row 2 = Bottom
        private static readonly Vector2Int[][] PaylineDefinitions = new Vector2Int[][]
        {
            // Center Row (Classic Main Payline)
            new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) },

            // Top Row
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },

            // Bottom Row
            new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2) },

            // Diagonal Down (Top-Left to Bottom-Right)
            new Vector2Int[] { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2) },

            // Diagonal Up (Bottom-Left to Top-Right)
            new Vector2Int[] { new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(2, 0) }
        };

        /// <summary>
        /// Evaluates the 3x3 outcome grid against active paylines and paytable rules.
        /// </summary>
        /// <param name="grid">2D outcome array [reelIndex, rowIndex]</param>
        /// <param name="betPerLine">Bet placed per active payline</param>
        /// <param name="activeLinesCount">Number of active paylines (1 to 5)</param>
        /// <param name="paytable">Paytable data containing payouts and symbol configs</param>
        /// <param name="freeSpinsMultiplier">Multiplier applied during free spin rounds</param>
        public static SpinResult Evaluate(SymbolId[,] grid, int betPerLine, int activeLinesCount, PaytableData paytable, int freeSpinsMultiplier = 1)
        {
            var result = new SpinResult();
            if (grid == null || paytable == null) return result;

            activeLinesCount = Mathf.Clamp(activeLinesCount, 1, PaylineDefinitions.Length);

            int totalMultiplier = 0;

            for (int lineIdx = 0; lineIdx < activeLinesCount; lineIdx++)
            {
                Vector2Int[] coords = PaylineDefinitions[lineIdx];
                PaylinePattern pattern = (PaylinePattern)lineIdx;

                SymbolId s0 = grid[coords[0].x, coords[0].y];
                SymbolId s1 = grid[coords[1].x, coords[1].y];
                SymbolId s2 = grid[coords[2].x, coords[2].y];

                // Check 1: 3-of-a-kind (All 3 symbols match across the payline)
                if (s0 == s1 && s1 == s2)
                {
                    SymbolConfig config = paytable.GetConfig(s0);
                    if (config != null && config.Payout3x > 0)
                    {
                        int mult = config.Payout3x * freeSpinsMultiplier;
                        int payout = mult * betPerLine;
                        totalMultiplier += mult;

                        result.WinningLines.Add(new WinningLine(pattern, s0, 3, mult, payout, coords));

                        // Check for Jackpot (3 Lucky 7s)
                        if (s0 == SymbolId.LuckySeven)
                        {
                            result.IsJackpot = true;
                        }

                        // Check for Free Spins trigger (3 BARs)
                        if (s0 == paytable.FreeSpinsTriggerSymbol)
                        {
                            result.TriggersFreeSpins = true;
                            result.FreeSpinsAwarded = paytable.FreeSpinsAwardCount;
                        }
                    }
                }
                // Check 2: 2-of-a-kind Cherries (Left-to-Right consolation payout: Reel 0 & Reel 1)
                else if (s0 == SymbolId.Cherry && s1 == SymbolId.Cherry)
                {
                    SymbolConfig cherryConfig = paytable.GetConfig(SymbolId.Cherry);
                    if (cherryConfig != null && cherryConfig.Payout2x > 0)
                    {
                        int mult = cherryConfig.Payout2x * freeSpinsMultiplier;
                        int payout = mult * betPerLine;
                        totalMultiplier += mult;

                        result.WinningLines.Add(new WinningLine(pattern, SymbolId.Cherry, 2, mult, payout, new Vector2Int[] { coords[0], coords[1] }));
                    }
                }
            }

            result.TotalMultiplier = totalMultiplier;
            result.TotalPayoutCredits = totalMultiplier * betPerLine;

            // Determine Win Grade for visual/audio pacing
            if (result.IsJackpot || totalMultiplier >= 100)
            {
                result.Grade = WinGrade.Jackpot;
            }
            else if (totalMultiplier >= 25)
            {
                result.Grade = WinGrade.BigWin;
            }
            else if (totalMultiplier >= 10)
            {
                result.Grade = WinGrade.Medium;
            }
            else if (totalMultiplier > 0)
            {
                result.Grade = WinGrade.Small;
            }
            else
            {
                result.Grade = WinGrade.None;
            }

            return result;
        }
    }
}
