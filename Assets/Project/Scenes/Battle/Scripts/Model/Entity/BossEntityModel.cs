using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class BossEntityModel : EnemyEntityModel
    {
        readonly ReactiveProperty<int> normalHp;
        readonly ReactiveProperty<int> strongHp;
        readonly float overflowDamageMultiplier;

        public BossEntityModel(int maxHp, int contactDamage, float strongHpRatio, float overflowDamageMultiplier)
            : base(maxHp, contactDamage)
        {
            float clampedRatio = Mathf.Clamp01(strongHpRatio);
            StrongMaxHp = Mathf.Clamp(Mathf.RoundToInt(maxHp * clampedRatio), 0, maxHp);
            NormalMaxHp = maxHp - StrongMaxHp;
            this.overflowDamageMultiplier = Mathf.Max(0f, overflowDamageMultiplier);

            normalHp = new ReactiveProperty<int>(NormalMaxHp);
            strongHp = new ReactiveProperty<int>(StrongMaxHp);
        }

        public int NormalMaxHp { get; }
        public int StrongMaxHp { get; }
        public IReadOnlyReactiveProperty<int> NormalHp => normalHp;
        public IReadOnlyReactiveProperty<int> StrongHp => strongHp;
        public bool ShouldUseStrongAttack => normalHp.Value <= 0;
        public bool IsInStrongMode { get; private set; }

        // memo: NormalHpが0になった後でも、次の発狂フェーズに遷移するまでは
        // ダメージ全体に overflowDamageMultiplier をかけ続けるためのフラグ。
        // BattleScenePresenterがBuilderStrongに切り替えるタイミングで呼ぶ。
        public void EnterStrongMode()
        {
            IsInStrongMode = true;
        }

        public override void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            if (damage < 0)
            {
                Debug.LogWarning($"Negative damage value: {damage}");
                return;
            }

            if (!IsInStrongMode)
            {
                int absorbed = Mathf.Min(damage, normalHp.Value);
                int overflow = damage - absorbed;
                if (absorbed > 0)
                {
                    normalHp.Value -= absorbed;
                }

                int overflowDamage = Mathf.FloorToInt(overflow * overflowDamageMultiplier);
                if (overflowDamage > 0)
                {
                    strongHp.Value = Mathf.Max(0, strongHp.Value - overflowDamage);
                }
            }
            else
            {
                strongHp.Value = Mathf.Max(0, strongHp.Value - damage);
            }

            SetCurrentHp(normalHp.Value + strongHp.Value);
        }

        public override void Dispose()
        {
            normalHp.Dispose();
            strongHp.Dispose();
            base.Dispose();
        }
    }
}
