using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    public class PlayerEntityView : EntityViewBase
    {
        [Header("Damage Flash")]
        [SerializeField] SpriteRenderer bodySpriteRenderer;
        [SerializeField] Color flashColor = Color.black;

        // memo: HpBarを大量に場に出すことになったら、Canvasを使わない設計にしたい
        // Claudeの提案；SpriteRenderer + Material Property （カスタムシェーダー）、9-Sliced Sprite + Size 直接操作、Quad Mesh自作 (最軽量だがやや手間)
        [Header("Hp Bar")]
        [SerializeField] Image hpBarFillImage;

        [Header("Animation - Run")]
        [SerializeField] Sprite[] runSprites;
        [SerializeField] float runFrameDuration = 0.1f;

        [Header("Animation - Stay")]
        [Tooltip("Stay.png")]
        [SerializeField] Sprite staySprite;
        [Tooltip("Run減速→Stay切替えにかける合計時間")]
        [SerializeField] float slowToStayDuration = 1.5f;
        [Tooltip("減速時のフレーム間隔の最大倍率")]
        [SerializeField] float slowMaxMultiplier = 6f;

        [Header("Animation - Charge")]
        [SerializeField] Sprite chargeSprite;

        [Header("Animation - Attack Sprites")]
        [Tooltip("通常攻撃前")]
        [SerializeField] Sprite beforeAttackSprite;
        [Tooltip("攻撃中、右足前")]
        [SerializeField] Sprite attackFireRightSprite;
        [Tooltip("攻撃中、左足前")]
        [SerializeField] Sprite attackMotionLeftSprite;

        [Header("Animation - Attack Timing")]
        [SerializeField] float attackFrameDuration = 0.1f;

        // memo: State関連の処理はPresenterにあった方がいいのかも
        public enum AnimationState
        {
            Run,
            Charge,
            Attack,
            Stay,
        }

        Color originalBodyColor;
        AnimationState currentState = AnimationState.Run;
        bool runFrozen;
        float frameTimer;
        int runFrameIndex;

        // Attack シーケンス再生用
        Sprite[] attackSequence;
        int attackSeqIndex;

        // Run減速→Stay切替え用
        bool slowingToStay;
        float slowElapsed;

        void Reset()
        {
            bodySpriteRenderer = GetComponent<SpriteRenderer>();
        }

        protected override void OnAwakeView()
        {
            if (bodySpriteRenderer == null)
            {
                bodySpriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (bodySpriteRenderer != null)
            {
                originalBodyColor = bodySpriteRenderer.color;
            }
            ApplyRunFrame(0);
        }

        public void UpdateAnimation()
        {
            switch (currentState)
            {
                case AnimationState.Run:
                    UpdateRun();
                    break;
                case AnimationState.Charge:
                    // 静止1枚、更新不要
                    break;
                case AnimationState.Attack:
                    UpdateAttack();
                    break;
                case AnimationState.Stay:
                    // 静止1枚、更新不要
                    break;
            }
        }

        void UpdateRun()
        {
            if (runFrozen) return;
            if (runSprites == null || runSprites.Length == 0) return;

            float frameDuration = runFrameDuration;
            if (slowingToStay)
            {
                slowElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(slowElapsed / slowToStayDuration);
                // フレーム間隔を 1倍 → slowMaxMultiplier倍 に伸ばす
                frameDuration = runFrameDuration * Mathf.Lerp(1f, slowMaxMultiplier, t);

                if (slowElapsed >= slowToStayDuration)
                {
                    EnterStay();
                    return;
                }
            }

            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                runFrameIndex = (runFrameIndex + 1) % runSprites.Length;
                ApplyRunFrame(runFrameIndex);
            }
        }

        void UpdateAttack()
        {
            if (attackSequence == null || attackSequence.Length == 0)
            {
                EnterRun();
                return;
            }

            frameTimer += Time.deltaTime;
            if (frameTimer < attackFrameDuration) return;

            frameTimer -= attackFrameDuration;
            attackSeqIndex++;

            if (attackSeqIndex >= attackSequence.Length)
            {
                EnterRun();
                return;
            }

            if (bodySpriteRenderer != null)
            {
                bodySpriteRenderer.sprite = attackSequence[attackSeqIndex];
            }
        }

        void ApplyRunFrame(int index)
        {
            if (runSprites == null || runSprites.Length == 0) return;
            if (bodySpriteRenderer == null) return;
            bodySpriteRenderer.sprite = runSprites[Mathf.Clamp(index, 0, runSprites.Length - 1)];
        }

        public void EnterRun()
        {
            currentState = AnimationState.Run;
            frameTimer = 0f;
            runFrameIndex = 0;
            attackSequence = null;
            attackSeqIndex = 0;
            slowingToStay = false;
            slowElapsed = 0f;
            ApplyRunFrame(0);
        }

        /// <summary>
        /// Run状態のまま、フレーム間隔を徐々に伸ばしてStayに移行する。
        /// </summary>
        public void BeginSlowToStay()
        {
            // すでにStay or 減速中なら何もしない
            if (currentState == AnimationState.Stay) return;
            if (slowingToStay) return;

            currentState = AnimationState.Run;
            slowingToStay = true;
            slowElapsed = 0f;
            // frameTimer/runFrameIndex は現在の見た目を維持するためにリセットしない
        }

        public void EnterStay()
        {
            currentState = AnimationState.Stay;
            slowingToStay = false;
            slowElapsed = 0f;
            frameTimer = 0f;
            if (staySprite != null && bodySpriteRenderer != null)
            {
                bodySpriteRenderer.sprite = staySprite;
            }
        }

        public void EnterCharge()
        {
            if (currentState == AnimationState.Attack) return; // 攻撃モーション中は割り込まない
            currentState = AnimationState.Charge;
            frameTimer = 0f;
            if (chargeSprite != null && bodySpriteRenderer != null)
            {
                bodySpriteRenderer.sprite = chargeSprite;
            }
        }

        public void EnterAttack()
        {
            // 攻撃中の連打はシーケンス継続（割り込まない）
            if (currentState == AnimationState.Attack) return;

            // 起点となる現在の状態によってシーケンスを変える
            if (currentState == AnimationState.Charge)
            {
                // Charge→Attack: 計4フレーム
                // [0]=AttackMotion2(現在表示) [1]=右 [2]=AttackMotion3 [3]=右
                attackSequence = new[]
                {
                    attackFireRightSprite,
                    attackMotionLeftSprite,
                    attackFireRightSprite,
                    attackMotionLeftSprite,
                };
            }
            else
            {
                // Run→Attack: 計4フレーム
                // [0]=Run_n(現在表示を維持) [1]=左 [2]=右 [3]=AttackMotion3
                Sprite currentRun = (runSprites != null && runSprites.Length > 0)
                    ? runSprites[Mathf.Clamp(runFrameIndex, 0, runSprites.Length - 1)]
                    : null;
                attackSequence = new[]
                {
                    beforeAttackSprite,
                    attackFireRightSprite,
                    attackMotionLeftSprite,
                };
            }

            currentState = AnimationState.Attack;
            frameTimer = 0f;
            attackSeqIndex = 0;
            if (attackSequence[0] != null && bodySpriteRenderer != null)
            {
                bodySpriteRenderer.sprite = attackSequence[0];
            }
        }

        public void SetRunFrozen(bool frozen)
        {
            runFrozen = frozen;
            if (frozen)
            {
                ApplyRunFrame(0);
                frameTimer = 0f;
                runFrameIndex = 0;
            }
        }

        public void SetDamageFlashActive(bool active)
        {
            if (bodySpriteRenderer == null) return;
            bodySpriteRenderer.color = active ? flashColor : originalBodyColor;
        }

        public void ResetDamageFlash()
        {
            if (bodySpriteRenderer == null) return;
            bodySpriteRenderer.color = originalBodyColor;
        }

        public void SetHpRatio(float ratio)
        {
            if (hpBarFillImage == null) return;
            hpBarFillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}
