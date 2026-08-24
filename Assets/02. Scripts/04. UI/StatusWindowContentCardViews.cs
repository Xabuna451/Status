using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatusWindow.UI
{
    /// <summary>Editable prefab view for the repeated status-window choice card.</summary>
    public sealed class StatusWindowActionCardView : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionText;
        [SerializeField] private Image accent;

        internal void Bind(string title, string description, string action, Action callback, Color accentColor)
        {
            titleText.text = title;
            descriptionText.text = description;
            actionText.text = action;
            accent.color = accentColor;
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() => callback?.Invoke());
        }
    }

}
