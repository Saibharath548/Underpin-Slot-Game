namespace Underpin.SlotMachine.Core
{
    /// <summary>
    /// Interface for random number generation in the slot machine.
    /// Provides uniform integer generation, weighted symbol picking, and matrix outcomes.
    /// Enables easy mocking, deterministic unit testing, and cryptographic audits.
    /// </summary>
    public interface ISlotRNG
    {
        int NextInt(int minInclusive, int maxExclusive);
        double NextDouble();
        SymbolId GetRandomSymbol(PaytableData paytable);
        SymbolId[,] GenerateSpinOutcome(int reelCount, int rowCount, PaytableData paytable);
        void SetSeed(int seed);
    }
}
