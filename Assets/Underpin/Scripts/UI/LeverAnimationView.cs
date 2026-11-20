using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    /// <summary>
    /// Handles the animated mechanical lever pull using the provided slot-machine1..5 sprites.
    /// Cycles smoothly from resting position (frame 1) to fully pulled down (frame 3)
    /// and springing back (frame 5), creating tactile physical feedback on spin.
    /// </summary>
    public class LeverAnimationView : MonoBehaviour
    {
        [Header("Cabinet Display Image")]
        [SerializeField] private Image cabinetImage;

        [Header("Animation Frames (slot-machine1 to 5)")]
        [SerializeField] private List<Sprite> leverFrames = new List<Sprite>();

        [Header("Timing")]
        [SerializeField] private float frameDuration = 0.06f;

        private Coroutine pullCoroutine;
        public bool IsAnimating => pullCoroutine != null;

        private void Awake()
        {
            if (cabinetImage == null)
            {
                cabinetImage = GetComponent<Image>();
            }
        }

        public void SetFrames(List<Sprite> frames)
        {
            leverFrames = frames;
            if (cabinetImage != null && leverFrames != null && leverFrames.Count > 0 && leverFrames[0] != null)
            {
                cabinetImage.sprite = leverFrames[0];
            }
        }

        /// <summary>
        /// Plays the physical lever pull animation sequence.
        /// </summary>
        public void PlayLeverPull(Action onPullDownComplete = null, Action onAnimationFinished = null)
        {
            if (pullCoroutine != null)
            {
                StopCoroutine(pullCoroutine);
            }
            pullCoroutine = StartCoroutine(LeverPullRoutine(onPullDownComplete, onAnimationFinished));
        }

        private IEnumerator LeverPullRoutine(Action onPullDownComplete, Action onAnimationFinished)
        {
            if (leverFrames == null || leverFrames.Count == 0 || cabinetImage == null)
            {
                onPullDownComplete?.Invoke();
                onAnimationFinished?.Invoke();
                pullCoroutine = null;
                yield break;
            }

            // Sequence: 0 -> 1 -> 2 (fully pulled) -> 3 -> 4 (back to rest)
            for (int i = 0; i < leverFrames.Count; i++)
            {
                if (leverFrames[i] != null)
                {
                    cabinetImage.sprite = leverFrames[i];
                }

                // When reaching the bottom of the stroke (frame index 2), trigger reel launch
                if (i == 2)
                {
                    onPullDownComplete?.Invoke();
                }

                yield return new WaitForSeconds(frameDuration);
            }

            // Ensure resting on frame 0
            if (leverFrames.Count > 0 && leverFrames[0] != null)
            {
                cabinetImage.sprite = leverFrames[0];
            }

            onAnimationFinished?.Invoke();
            pullCoroutine = null;
        }
    }
}
