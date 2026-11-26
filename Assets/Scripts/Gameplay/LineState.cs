namespace Arrows.Gameplay
{
    /// <summary>
    /// Possible states for a line during gameplay.
    /// </summary>
    public enum LineState
    {
        /// <summary>Line is idle. Both arrowheads are at their endpoints and interactable.</summary>
        Idle,

        /// <summary>One arrowhead is moving along the path. Both arrowheads are non-interactable.</summary>
        Moving,

        /// <summary>Line has been successfully completed. Both arrowheads and the line are removed.</summary>
        Completed,

        /// <summary>Line is resetting after a collision. Non-interactable during this state.</summary>
        Resetting
    }
}

