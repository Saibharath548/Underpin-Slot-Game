using System;
using UnityEngine;

namespace Underpin.SlotMachine.Audio
{
    using Underpin.SlotMachine.Core;

    /// <summary>
    /// Audio manager for the slot machine game.
    /// Coordinates mechanical SFX, reel tick loops, sequential stop thuds,
    /// celebratory win fanfares, and coin sounds.
    /// </summary>
    public class SlotAudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource loopSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip spinStartClip;
        [SerializeField] private AudioClip reelLoopClip;
        [SerializeField] private AudioClip reelStopClip;
        [SerializeField] private AudioClip winSmallClip;
        [SerializeField] private AudioClip winBigClip;
        [SerializeField] private AudioClip jackpotClip;

        private void Awake()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (loopSource == null)
            {
                loopSource = gameObject.AddComponent<AudioSource>();
                loopSource.loop = true;
                loopSource.playOnAwake = false;
            }
        }

        public void SetClips(
            AudioClip click, AudioClip start, AudioClip loop, AudioClip stop,
            AudioClip smallWin, AudioClip bigWin, AudioClip jackpot)
        {
            buttonClickClip = click;
            spinStartClip = start;
            reelLoopClip = loop;
            reelStopClip = stop;
            winSmallClip = smallWin;
            winBigClip = bigWin;
            jackpotClip = jackpot;
        }

        public void PlayButtonClick()
        {
            PlaySFX(buttonClickClip, 1.0f, UnityEngine.Random.Range(0.95f, 1.05f));
        }

        public void PlaySpinStart()
        {
            PlaySFX(spinStartClip, 1.0f);
        }

        public void StartReelLoop()
        {
            if (reelLoopClip != null && loopSource != null)
            {
                loopSource.clip = reelLoopClip;
                loopSource.volume = 0.5f;
                loopSource.pitch = 1.0f;
                loopSource.Play();
            }
        }

        public void StopReelLoop()
        {
            if (loopSource != null && loopSource.isPlaying)
            {
                loopSource.Stop();
            }
        }

        public void PlayReelStop(int reelIndex)
        {
            // Ascending pitch for sequential reels gives satisfying progressive landing feedback!
            float pitch = 1.0f + (reelIndex * 0.08f);
            PlaySFX(reelStopClip, 0.9f, pitch);
        }

        public void PlayWin(WinGrade grade)
        {
            switch (grade)
            {
                case WinGrade.Jackpot:
                    PlaySFX(jackpotClip, 1.0f);
                    break;
                case WinGrade.BigWin:
                    PlaySFX(winBigClip, 1.0f);
                    break;
                case WinGrade.Medium:
                case WinGrade.Small:
                    PlaySFX(winSmallClip, 0.9f);
                    break;
            }
        }

        private void PlaySFX(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
