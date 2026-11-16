using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pulsar.UI
{
    [RequireComponent(typeof(Selectable))]
    public class SelectOnHover : MonoBehaviour, IPointerEnterHandler
    {
        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable.IsActive() && _selectable.IsInteractable() &&
                EventSystem.current != null && EventSystem.current.sendNavigationEvents)
            {
                _selectable.Select();
            }
        }
    }
}
