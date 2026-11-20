namespace Underpin.SlotMachine.Core
{
    /// <summary>
    /// Identifiers for the slot machine symbols.
    /// Matched to the provided asset sprites:
    /// Symbol 1: Lucky Seven (Red 7)
    /// Symbol 2: Cherries
    /// Symbol 3: Golden Bell
    /// Symbol 4: BAR
    /// </summary>
    public enum SymbolId
    {
        LuckySeven = 0,
        Cherry = 1,
        Bell = 2,
        Bar = 3
    }

    /// <summary>
    /// State machine states for the slot machine lifecycle.
    /// </summary>
    public enum SlotState
    {
        Idle,
        LeverPulling,
        Spinning,
        Stopping,
        EvaluatingWin,
        WinCelebration,
        FreeSpins,
        PopupOpen
    }

    /// <summary>
    /// Categorization of win magnitude for visual & audio escalation.
    /// </summary>
    public enum WinGrade
    {
        None = 0,
        Small = 1,
        Medium = 2,
        BigWin = 3,
        Jackpot = 4
    }

    /// <summary>
    /// Payline definitions across the 3x3 reel matrix.
    /// </summary>
    public enum PaylinePattern
    {
        CenterRow = 0,   // Middle horizontal line: (0,1), (1,1), (2,1)
        TopRow = 1,      // Top horizontal line:    (0,0), (1,0), (2,0)
        BottomRow = 2,   // Bottom horizontal line: (0,2), (1,2), (2,2)
        DiagonalDown = 3,// Top-left to bottom-right: (0,0), (1,1), (2,2)
        DiagonalUp = 4   // Bottom-left to top-right: (0,2), (1,1), (2,0)
    }
}
