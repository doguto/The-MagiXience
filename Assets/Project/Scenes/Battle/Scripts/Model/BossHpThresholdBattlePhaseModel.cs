using System;
using UniRx;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.Entity;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BossHpThresholdBattlePhaseModel : BattlePhaseModelBase
    {
        readonly float hpThresholdRate;
        readonly Func<EntityBase> getBossModel;

        public BossHpThresholdBattlePhaseModel(BattlePhaseDefinition definition, float hpThresholdRate, Func<EntityBase> getBossModel)
            : base(definition)
        {
            this.hpThresholdRate = hpThresholdRate;
            this.getBossModel = getBossModel;
        }

        protected override void OnEnter()
        {
            var bossModel = getBossModel?.Invoke();
            if (bossModel == null)
            {
                Debug.LogWarning("[BossHpThresholdBattlePhaseModel] Boss model not found, completing phase immediately.");
                CompletePhase();
                return;
            }

            int threshold = Mathf.CeilToInt(bossModel.MaxHp * hpThresholdRate);

            // 既にしきい値以下なら即完了
            if (bossModel.CurrentHp.Value <= threshold)
            {
                CompletePhase();
                return;
            }

            bossModel.CurrentHp
                .Where(hp => hp <= threshold)
                .Take(1)
                .Subscribe(_ => CompletePhase())
                .AddTo(Disposables);
        }
    }
}
