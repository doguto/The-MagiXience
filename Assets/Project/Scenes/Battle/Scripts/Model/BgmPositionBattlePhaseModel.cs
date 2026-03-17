using System;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BgmPositionBattlePhaseModel : BattlePhaseModelBase
    {
        readonly int thresholdSamples;
        readonly Func<AudioSource> getBgmAudioSource;

        public BgmPositionBattlePhaseModel(
            BattlePhaseDefinition definition,
            int thresholdSamples,
            Func<AudioSource> getBgmAudioSource)
            : base(definition)
        {
            this.thresholdSamples = thresholdSamples;
            this.getBgmAudioSource = getBgmAudioSource;
        }

        protected override void OnEnter()
        {
            var audioSource = getBgmAudioSource?.Invoke();
            if (audioSource == null || audioSource.clip == null)
            {
                Debug.LogWarning("[BgmPositionBattlePhaseModel] AudioSource or clip not found, completing phase immediately.");
                CompletePhase();
                return;
            }

            // 既にしきい値以上なら即完了
            if (audioSource.timeSamples >= thresholdSamples)
            {
                CompletePhase();
                return;
            }

            Observable.EveryUpdate()
                .Where(_ => audioSource != null && audioSource.timeSamples >= thresholdSamples)
                .Take(1)
                .Subscribe(_ => CompletePhase())
                .AddTo(Disposables);
        }
    }
}
