using System;
using UnityEngine;

namespace Underpin.SlotMachine.Core
{
    /// <summary>
    /// Configuration data for an individual slot machine symbol.
    /// Contains identifiers, display names, sprite reference, payout multipliers,
    /// and weighted probabilities for RNG distribution.
    /// </summary>
    [Serializable]
    public class SymbolConfig
    {
        [SerializeField] private SymbolId id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;
        [SerializeField] private int payout3x;
        [SerializeField] private int payout2x;
        [SerializeField] private int weight;
        [SerializeField] private Color glowColor = Color.yellow;

        public SymbolId Id => id;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public int Payout3x => payout3x;
        public int Payout2x => payout2x;
        public int Weight => weight;
        public Color GlowColor => glowColor;

        public SymbolConfig(SymbolId id, string displayName, int payout3x, int payout2x, int weight, Color glowColor, Sprite sprite = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.payout3x = payout3x;
            this.payout2x = payout2x;
            this.weight = weight;
            this.glowColor = glowColor;
            this.sprite = sprite;
        }

        public void SetSprite(Sprite newSprite)
        {
            this.sprite = newSprite;
        }
    }
}
