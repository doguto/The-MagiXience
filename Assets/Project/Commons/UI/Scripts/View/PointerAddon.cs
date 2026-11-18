using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
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
        Canvas rootCanvas;

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

            // 購読前(モーダル表示直後のInit)にフォーカスされていた場合も位置を合わせる
            if (IsCurrentlyFocused()) OnButtonFocused();
        }

        bool IsCurrentlyFocused()
        {
            if (button != null) return button.IsFocused;
            if (selectable == null || EventSystem.current == null) return false;
            return EventSystem.current.currentSelectedGameObject == selectable.gameObject;
        }

        void OnButtonFocused()
        {
            // pointerOffsetはCanvas座標系での値
            var offset = GetRootCanvasTransform().TransformVector(pointerOffset);
            var pos = myTransform.position;
            pointer.transform.position = new Vector3(
                pos.x + offset.x,
                pos.y + offset.y,
                pos.z
            );
            pointer.SetActive(true);
        }

        Transform GetRootCanvasTransform()
        {
            if (rootCanvas == null)
            {
                var canvas = GetComponentInParent<Canvas>(true);
                rootCanvas = canvas != null ? canvas.rootCanvas : null;
            }

            return rootCanvas != null ? rootCanvas.transform : myTransform.root;
        }
    }
}
