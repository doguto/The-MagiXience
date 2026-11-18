using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    public class BossEntityModel : EnemyEntityModel
    {
        readonly ReactiveProperty<IReadOnlyList<float>> hpBreaks = new(Array.Empty<float>());

        public BossEntityModel(int maxHp, int contactDamage) : base(maxHp, contactDamage) { }

        // HP ゲージの区切り (0〜1、大きい順)。ツリーの HpSections が設定する
        public IReadOnlyReactiveProperty<IReadOnlyList<float>> HpBreaks => hpBreaks;
        public void SetHpBreaks(IReadOnlyList<float> breaks) => hpBreaks.Value = breaks;

        public float DamageMultiplier { get; set; } = 1f;

        // デバッグ起動で途中の HP から始めるときに使う
        public void SetHpPercent(float percent) => SetCurrentHp(Mathf.Max(1, Mathf.CeilToInt(MaxHp * Mathf.Clamp(percent, 0f, 100f) / 100f)));

        public override void TakeDamage(int damage)
        {
            if (damage < 0)
            {
                Debug.LogWarning($"Negative damage value: {damage}");
                return;
            }
            base.TakeDamage(Mathf.FloorToInt(damage * DamageMultiplier));
        }

        public override void Dispose()
        {
            hpBreaks.Dispose();
            base.Dispose();
        }
    }
}
