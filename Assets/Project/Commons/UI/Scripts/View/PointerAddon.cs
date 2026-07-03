using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Commons.UI.Scripts.View
{
    public class PointerAddon : MonoBehaviour
    {
        // 後方互換用
        [SerializeField] ButtonBase button;
        [SerializeField] Selectable selectable;
        [SerializeField] GameObject pointer;
        [SerializeField] Vector2 pointerOffset = new(-1f, 0f);

        Transform myTransform;

        void Awake()
        {
            myTransform = transform;
        }

        void Start()
        {
            if (button != null)
            {
                // ButtonBaseは独自のフォーカスイベントを発火するためそれを購読する。
                button.OnFocusedEvent
                    .Subscribe(_ => OnButtonFocused())
                    .AddTo(this);
            }
            else if (selectable != null)
            {
                // Slider等の標準SelectableはEventSystemのOnSelectでフォーカスを検知する。
                selectable.OnSelectAsObservable()
                    .Subscribe(_ => OnButtonFocused())
                    .AddTo(this);
            }
            else
            {
                Debug.LogWarning("[PointerAddon] button も selectable も設定されていません", this);
            }
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
