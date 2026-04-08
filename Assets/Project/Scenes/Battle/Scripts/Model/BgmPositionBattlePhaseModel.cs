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

            var previousSamples = audioSource.timeSamples;
            // OnEnter時点でしきい値以上なら、BGMループ後に改めて判定する
            // why: BGMループ前にフェーズのループが走った場合に、連鎖的にフェーズが終了してしまうのを防ぐ
            var waitingForLoop = previousSamples >= thresholdSamples;

            Observable.EveryUpdate()
                .Where(_ =>
                {
                    if (audioSource == null) return false;
                    var currentSamples = audioSource.timeSamples;
                    var looped = currentSamples < previousSamples;
                    previousSamples = currentSamples;

                    if (waitingForLoop)
                    {
                        // BGMループで巻き戻されたら通常判定に切り替え
                        if (looped)
                        {
                            waitingForLoop = false;
                            return currentSamples >= thresholdSamples;
                        }
                        return false;
                    }

                    // しきい値到達、またはBGMループ（しきい値を飛び越えた）で完了
                    return currentSamples >= thresholdSamples || looped;
                })
                .Take(1)
                .Subscribe(_ => CompletePhase())
                .AddTo(Disposables);
        }
    }
}
