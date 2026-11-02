using Project.Scenes.Battle.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>フェーズ設定に従って腕上げと風を同期する。攻撃・移動は通常のBossプリセットで実行する。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossEntityPresenter))]
    public class Stage3WindBossController : MonoBehaviour
    {
        [SerializeField] Sprite raisedArmsSprite;

        BossEntityPresenter boss;
        SpriteRenderer spriteRenderer;
        Sprite idleSprite;
        Vector3 idleScale;
        BossWindCueSettings cue;
        float elapsed;

        void Awake()
        {
            boss = GetComponent<BossEntityPresenter>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            idleSprite = spriteRenderer.sprite;
            idleScale = transform.localScale;
            boss.PhaseStarted += BeginSection;
        }

        void BeginSection(BattlePhaseModelBase phase, BattleTimelineBuilderAsset builder)
        {
            RestoreSprite();
            cue = builder != null && builder.BossWindCue?.enabled == true ? builder.BossWindCue : null;
            elapsed = 0f;
            if (cue == null) return;
            BattleWind.Begin(cue.CreateWind(BattleWind.Direction));
            if (cue.cueSeconds > 0f && raisedArmsSprite != null) spriteRenderer.sprite = raisedArmsSprite;
        }

        void Update()
        {
            if (cue == null || boss.Model == null || !boss.Model.IsAlive) return;
            elapsed += Time.deltaTime;
            bool raisingArms = elapsed < cue.cueSeconds;
            if (raisedArmsSprite != null) spriteRenderer.sprite = raisingArms ? raisedArmsSprite : idleSprite;
            float lift = raisingArms ? Mathf.Sin(Mathf.PI * elapsed / cue.cueSeconds) : 0f;
            transform.localScale = idleScale + Vector3.Scale(idleScale,
                new Vector3(cue.cueScale.x, cue.cueScale.y, 0f)) * lift;
        }

        void RestoreSprite()
        {
            if (spriteRenderer != null) spriteRenderer.sprite = idleSprite;
            transform.localScale = idleScale;
        }

        void OnDestroy()
        {
            if (boss != null) boss.PhaseStarted -= BeginSection;
        }
    }
}
