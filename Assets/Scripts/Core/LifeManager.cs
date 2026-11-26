using System;
using UnityEngine;

namespace Arrows.Core
{
    /// <summary>
    /// Manages player lives for the level.
    /// Default starting lives: 3 (configured via LevelData).
    /// Fires events when lives change or when all lives are depleted.
    /// Pure gameplay logic — completely independent of UI rendering.
    /// </summary>
    public class LifeManager : MonoBehaviour
    {
        [SerializeField] private int startingLives = 3;

        private int currentLives;

        public int StartingLives => startingLives;
        public int CurrentLives => currentLives;
        public bool HasLivesRemaining => currentLives > 0;

        /// <summary>Fired when remaining lives change. Argument is current lives.</summary>
        public event Action<int> OnLifeChanged;

        /// <summary>Fired when current lives reaches zero.</summary>
        public event Action OnAllLivesLost;

        public void Initialize(int lives)
        {
            startingLives = Mathf.Max(1, lives);
            ResetLives();
        }

        public void ResetLives()
        {
            currentLives = startingLives;
            OnLifeChanged?.Invoke(currentLives);
        }

        /// <summary>
        /// Deducts exactly 1 life for a collision event.
        /// Returns true if a life was successfully deducted.
        /// </summary>
        public bool DeductLife()
        {
            if (currentLives <= 0) return false;

            currentLives--;
            Debug.Log($"<color=orange>[LifeManager] Life lost! Remaining: {currentLives}/{startingLives}</color>");

            OnLifeChanged?.Invoke(currentLives);

            if (currentLives <= 0)
            {
                Debug.Log("<color=red>[LifeManager] All lives lost! Level failed.</color>");
                OnAllLivesLost?.Invoke();
            }

            return true;
        }
    }
}

