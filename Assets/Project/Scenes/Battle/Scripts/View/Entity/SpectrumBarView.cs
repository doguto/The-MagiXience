using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    /// <summary>
    /// スペクトラムバー1本分のビジュアルとコライダーを管理する。
    /// </summary>
    public class SpectrumBarView : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        BoxCollider2D triggerCollider;
        BoxCollider2D solidCollider;

        float barWidth;

        public void Initialize(float width, Color color)
        {
            barWidth = width;

            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateWhitePixelSprite();
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = 10;

            // Rigidbody2D（Static）: 物理ボールとの衝突応答に必要
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            // Trigger（プレイヤーダメージ検出用）
            triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;

            // Solid（物理ボール反射用）
            solidCollider = gameObject.AddComponent<BoxCollider2D>();
            solidCollider.isTrigger = false;

            transform.localScale = new Vector3(barWidth, 0f, 1f);
        }

        public void UpdateBar(float height, float screenBottomY)
        {
            // スケール更新（幅は固定、高さのみ変動）
            var scale = transform.localScale;
            scale.y = height;
            transform.localScale = scale;

            // 画面下端からバーを積み上げる位置に配置
            var pos = transform.position;
            pos.y = screenBottomY + height * 0.5f;
            transform.position = pos;

            // コライダーサイズはスケールに合わせて1x1（SpriteRendererのスケールで拡大される）
            var colliderSize = new Vector2(1f, 1f);
            triggerCollider.size = colliderSize;
            solidCollider.size = colliderSize;
        }

        static Sprite CreateWhitePixelSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
