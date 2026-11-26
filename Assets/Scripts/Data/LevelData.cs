using UnityEngine;

namespace Arrows.Data
{
    /// <summary>
    /// ScriptableObject holding all configuration data for a single level.
    /// Create new levels via Assets > Create > Arrows > Level Data.
    /// Gameplay logic reads this data but never modifies it.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_New", menuName = "Arrows/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("Display name for this level.")]
        public string levelName = "Unnamed Level";

        [Tooltip("Number of lives the player starts with.")]
        [Range(1, 10)]
        public int startingLives = 3;

        [Tooltip("All colored lines in this level.")]
        public LineDefinition[] lines;

        /// <summary>
        /// Validates all line definitions in the level.
        /// </summary>
        public bool Validate(out string error)
        {
            if (lines == null || lines.Length == 0)
            {
                error = "Level has no lines defined.";
                return false;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == null)
                {
                    error = $"Line at index {i} is null.";
                    return false;
                }

                if (!lines[i].IsValid())
                {
                    error = $"Line '{lines[i].lineId}' at index {i} is invalid (needs an ID and at least 2 control points).";
                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}

