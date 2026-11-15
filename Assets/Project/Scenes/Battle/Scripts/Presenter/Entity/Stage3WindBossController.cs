using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    /// <summary>BossWindCue ノードの合図に合わせて腕を上げる演出。風向きの切り替えはノード側が行う。</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BossEntityPresenter))]
    public class Stage3WindBossController : MonoBehaviour, IWindCueReceiver
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
        }

        // 腕を上げる演出。風向きの切り替えは呼び出し側 (BossWindCue ノード) が行う
        public void PlayCue(BossWindCueSettings settings)
        {
            RestoreSprite();
            cue = settings;
            elapsed = 0f;
            if (cue == null) return;
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
    }
}
