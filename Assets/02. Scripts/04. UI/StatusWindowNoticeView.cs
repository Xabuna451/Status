using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatusWindow.UI
{
    public sealed class StatusWindowNoticeView : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField] private Button confirmButton;

        internal void Show(string message, Action onConfirm)
        {
            Show(message, "확인", onConfirm);
        }

        internal void Show(string message, string confirmLabel, Action onConfirm)
        {
            messageText.text = message;
            var label = confirmButton.GetComponentInChildren<Text>();
            if (label != null) label.text = confirmLabel;
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => onConfirm?.Invoke());
            gameObject.SetActive(true);
        }

        internal void Hide() => gameObject.SetActive(false);
    }
}
