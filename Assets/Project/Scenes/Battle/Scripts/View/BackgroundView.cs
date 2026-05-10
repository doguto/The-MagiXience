using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Battle.Scripts.View
{
    /// <summary>
    /// 背景画像のテクスチャUVオフセットをマテリアルに反映するだけのView。
    /// スクロール速度や減速の計算はPresenter側が持つ。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class BackgroundView : MonoBehaviour
    {
        const string TexturePropertyName = "_MainTex";

        Material material;

        void Awake()
        {
            var image = GetComponent<Image>();
            material = image.material;
        }

        public void ApplyOffset(Vector2 offset)
        {
            if (material == null) return;
            material.SetTextureOffset(TexturePropertyName, offset);
        }

        void OnDestroy()
        {
            if (material != null)
            {
                material.SetTextureOffset(TexturePropertyName, Vector2.zero);
            }
        }
    }
}
