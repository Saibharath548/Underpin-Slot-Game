using System;
using System.Security.Cryptography;

namespace Underpin.SlotMachine.Core
{
    /// <summary>
    /// Production-grade implementation of ISlotRNG.
    /// Utilizes System.Random with an optional seed for deterministic testing,
    /// or high-entropy crypto-seeded generation for authentic casino unpredictability.
    /// Implements weighted distribution based on PaytableData symbol weights.
    /// </summary>
    public class SlotRNGService : ISlotRNG
    {
        private Random random;
        private bool isDeterministic;

        public bool IsDeterministic => isDeterministic;

        public SlotRNGService()
        {
            // Seed using high-entropy crypto bytes for maximum unpredictability
            byte[] seedBytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(seedBytes);
            }
            int seed = BitConverter.ToInt32(seedBytes, 0);
            random = new Random(seed);
            isDeterministic = false;
        }

        public SlotRNGService(int deterministicSeed)
        {
            random = new Random(deterministicSeed);
            isDeterministic = true;
        }

        public void SetSeed(int seed)
        {
            random = new Random(seed);
            isDeterministic = true;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
                return minInclusive;
            return random.Next(minInclusive, maxExclusive);
        }

        public double NextDouble()
        {
            return random.NextDouble();
        }

        /// <summary>
        /// Selects a SymbolId using a weighted random algorithm based on paytable weights.
        /// </summary>
        public SymbolId GetRandomSymbol(PaytableData paytable)
        {
            if (paytable == null || paytable.Symbols == null || paytable.Symbols.Count == 0)
            {
                // Fallback to uniform distribution if no paytable is supplied
                return (SymbolId)NextInt(0, 4);
            }

            int totalWeight = paytable.GetTotalWeight();
            if (totalWeight <= 0)
                return (SymbolId)NextInt(0, paytable.Symbols.Count);

            int roll = NextInt(0, totalWeight);
            int runningSum = 0;

            for (int i = 0; i < paytable.Symbols.Count; i++)
            {
                runningSum += paytable.Symbols[i].Weight;
                if (roll < runningSum)
                {
                    return paytable.Symbols[i].Id;
                }
            }

            return paytable.Symbols[paytable.Symbols.Count - 1].Id;
        }

        /// <summary>
        /// Generates the complete 2D outcome matrix [reel, row] for a spin.
        /// </summary>
        public SymbolId[,] GenerateSpinOutcome(int reelCount, int rowCount, PaytableData paytable)
        {
            SymbolId[,] grid = new SymbolId[reelCount, rowCount];
            for (int r = 0; r < reelCount; r++)
            {
                for (int row = 0; row < rowCount; row++)
                {
                    grid[r, row] = GetRandomSymbol(paytable);
                }
            }
            return grid;
        }
    }
}
