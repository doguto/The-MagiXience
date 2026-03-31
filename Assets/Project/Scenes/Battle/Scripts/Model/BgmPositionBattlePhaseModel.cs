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

            var previousSamples = audioSource.timeSamples;

            Observable.EveryUpdate()
                .Where(_ =>
                {
                    if (audioSource == null) return false;
                    var currentSamples = audioSource.timeSamples;
                    // ループ検出（サンプル数が減少した）または閾値到達で完了
                    var reached = currentSamples >= thresholdSamples || currentSamples < previousSamples;
                    previousSamples = currentSamples;
                    return reached;
                })
                .Take(1)
                .Subscribe(_ => CompletePhase())
                .AddTo(Disposables);
        }
    }
}
