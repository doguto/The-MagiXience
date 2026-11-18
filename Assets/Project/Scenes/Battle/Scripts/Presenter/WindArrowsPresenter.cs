using UnityEngine;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.View;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    /// <summary>
    /// BattleWindの向き・強さに合わせて矢印グリッドを流す。背景のスクロール分も足して海の上を流れて見せる。
    /// 風が無効、または無風(None)の間は非表示。
    /// </summary>
    public class WindArrowsPresenter : MonoPresenter
    {
        [SerializeField] WindArrowsView[] views;
        [SerializeField] BackgroundPresenter backgroundPresenter;
        [SerializeField, Min(0f), Tooltip("Player Speed Ratio = 1 のときの流れる速さ(背景ドット/秒)。")]
        float speedPerRatio = 150f;

        Vector2 offset;

        void Update()
        {
            Vector2 direction = BattleWind.Vector;
            bool visible = direction != Vector2.zero;
            foreach (var view in views) view.SetVisible(visible);
            if (!visible) return;

            Vector2 velocity = direction * (speedPerRatio * BattleWind.PlayerSpeedRatio);
            if (backgroundPresenter != null) velocity += backgroundPresenter.ScrollVelocityPixels;
            offset += velocity * Time.deltaTime;

            foreach (var view in views)
            {
                view.SetDirection(direction);
                view.ApplyOffset(offset);
            }
        }
    }
}
