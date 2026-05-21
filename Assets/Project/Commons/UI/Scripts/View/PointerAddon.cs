using UniRx;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    public class PointerAddon : MonoBehaviour
    {
        [SerializeField] ButtonBase button;
        [SerializeField] GameObject pointer;
        [SerializeField] Vector2 pointerOffset = new(-1f, 0f);

        Transform myTransform;

        void Awake()
        {
            myTransform = transform;
        }

        void Start()
        {
            button.OnFocusedEvent
                .Subscribe(_ => OnButtonFocused())
                .AddTo(this);
        }

        void OnButtonFocused()
        {
            var pos = myTransform.position;
            pointer.transform.position = new Vector3(
                pos.x + pointerOffset.x,
                pos.y + pointerOffset.y,
                pos.z
            );
            pointer.SetActive(true);
        }
    }
}
