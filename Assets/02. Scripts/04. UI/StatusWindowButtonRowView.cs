using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatusWindow.UI
{
    /// <summary>Editable prefab view for a full-width content action.</summary>
    public sealed class StatusWindowButtonRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Text labelText;

        internal void Bind(string label, Action callback, Color color)
        {
            labelText.text = label;
            buttonImage.color = color;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }
    }
}
