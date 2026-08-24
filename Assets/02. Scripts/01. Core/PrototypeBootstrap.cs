using StatusWindow.Progression;
using StatusWindow.UI;
using UnityEngine;

namespace StatusWindow
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private PrototypeCatalog catalog;

        private void Awake()
        {
            if (catalog == null)
            {
                Debug.LogError("StatusWindowPrototypeCatalog reference is missing.", this);
                return;
            }

            var existingView = Object.FindFirstObjectByType<StatusWindowPrototype>();
            if (existingView != null)
            {
                existingView.Initialize(catalog);
                return;
            }

            var prototypeObject = new GameObject("StatusWindowPrototype");
            prototypeObject.AddComponent<StatusWindowPrototype>().Initialize(catalog);
        }
    }
}
