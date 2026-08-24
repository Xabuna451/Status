using UnityEngine;
using UnityEngine.UI;

namespace StatusWindow.UI
{
    /// <summary>Editable prefab view for a content section heading.</summary>
    public sealed class StatusWindowHeadingCardView : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;

        internal void Bind(string title, string description)
        {
            titleText.text = title;
            descriptionText.text = description;
        }
    }
}
