using System;
using System.Collections.Generic;
using UnityEngine;

namespace Underpin.SlotMachine.Core
{
    /// <summary>
    /// ScriptableObject defining the paytable, symbol configurations,
    /// return-to-player (RTP) weights, and bonus rules.
    /// Also supports dynamic runtime fallback generation if asset is not pre-assigned.
    /// </summary>
    [CreateAssetMenu(fileName = "PaytableData", menuName = "Underpin/Slot Machine/Paytable Data")]
    public class PaytableData : ScriptableObject
    {
        [Header("Symbol Definitions")]
        [SerializeField] private List<SymbolConfig> symbols = new List<SymbolConfig>();

        [Header("Payout & Game Balance Settings")]
        [SerializeField] private int freeSpinsAwardCount = 5;
        [SerializeField] private int freeSpinsMultiplier = 2;
        [SerializeField] private SymbolId freeSpinsTriggerSymbol = SymbolId.Bar;

        public IReadOnlyList<SymbolConfig> Symbols => symbols;
        public int FreeSpinsAwardCount => freeSpinsAwardCount;
        public int FreeSpinsMultiplier => freeSpinsMultiplier;
        public SymbolId FreeSpinsTriggerSymbol => freeSpinsTriggerSymbol;

        /// <summary>
        /// Retrieves the SymbolConfig corresponding to a given SymbolId.
        /// </summary>
        public SymbolConfig GetConfig(SymbolId id)
        {
            for (int i = 0; i < symbols.Count; i++)
            {
                if (symbols[i].Id == id)
                    return symbols[i];
            }
            return null;
        }

        /// <summary>
        /// Computes the total cumulative RNG weight of all symbols in the paytable.
        /// </summary>
        public int GetTotalWeight()
        {
            int total = 0;
            for (int i = 0; i < symbols.Count; i++)
            {
                total += symbols[i].Weight;
            }
            return total;
        }

        /// <summary>
        /// Populates default balanced paytable data if needed.
        /// Payout multipliers:
        /// - 3x Lucky Seven (7): 100x (Jackpot tier, weight 12)
        /// - 3x Golden Bell: 40x (High tier, weight 20)
        /// - 3x BAR: 20x (Medium tier + Free Spins trigger, weight 28)
        /// - 3x Cherries: 10x (Frequent win tier, weight 40)
        /// - 2x Cherries: 2x consolation payout
        /// </summary>
        public void InitializeDefaults(Dictionary<SymbolId, Sprite> spriteMap = null)
        {
            symbols.Clear();

            Sprite s7 = spriteMap != null && spriteMap.ContainsKey(SymbolId.LuckySeven) ? spriteMap[SymbolId.LuckySeven] : null;
            Sprite sCherry = spriteMap != null && spriteMap.ContainsKey(SymbolId.Cherry) ? spriteMap[SymbolId.Cherry] : null;
            Sprite sBell = spriteMap != null && spriteMap.ContainsKey(SymbolId.Bell) ? spriteMap[SymbolId.Bell] : null;
            Sprite sBar = spriteMap != null && spriteMap.ContainsKey(SymbolId.Bar) ? spriteMap[SymbolId.Bar] : null;

            symbols.Add(new SymbolConfig(SymbolId.LuckySeven, "Lucky 7", 100, 0, 12, new Color(1f, 0.2f, 0.2f), s7));
            symbols.Add(new SymbolConfig(SymbolId.Bell, "Golden Bell", 40, 0, 20, new Color(1f, 0.85f, 0.1f), sBell));
            symbols.Add(new SymbolConfig(SymbolId.Bar, "BAR", 20, 0, 28, new Color(0.2f, 0.8f, 1f), sBar));
            symbols.Add(new SymbolConfig(SymbolId.Cherry, "Cherries", 10, 2, 40, new Color(1f, 0.3f, 0.6f), sCherry));
        }
    }
}
