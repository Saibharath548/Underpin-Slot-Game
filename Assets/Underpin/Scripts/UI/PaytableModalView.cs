using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Underpin.SlotMachine.UI
{
    using Underpin.SlotMachine.Core;

    /// <summary>
    /// Paytable modal dialog allowing players to inspect all symbol payouts,
    /// winning combinations, and bonus mechanics at any time.
    /// </summary>
    public class PaytableModalView : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform entriesContainer;

        public event Action OnCloseClicked;

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (closeButton != null)
                closeButton.onClick.AddListener(() =>
                {
                    Hide();
                    OnCloseClicked?.Invoke();
                });

            HideImmediate();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            HideImmediate();
        }

        public void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }
    }
}
