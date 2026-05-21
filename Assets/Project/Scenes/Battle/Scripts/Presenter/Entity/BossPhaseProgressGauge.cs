using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scripts.Model;
using Project.Scripts.Presenter;

namespace Project.Scenes.Battle.Scripts.Presenter.Entity
{
    // 1面ボス専用の予告ゲージ。BGM位置を直接見て、現在フェーズの終了までの進捗を Image.fillAmount に反映する。
    //
    // 拡張メモ：将来 BGM以外の終了条件（時間制限・HPしきい値など）も扱いたい場合は、
    //   1) BattlePhaseModelBase に virtual float? RemainingProgress を生やして各派生で実装し、
    //   2) ここでは activePhase.RemainingProgress を見るだけにする
    // のように一般化できる。今は1面ボス専用のため、フェーズごとの thresholdSamples を Inspector で直書きする。
    public class BossPhaseProgressGauge : MonoPresenter
    {
        [SerializeField] Image fillImage;

        [Tooltip("各フェーズの終了予定 BGM サンプル位置（絶対値）を、フェーズが開始される順に並べる。"
                 + "区間 [前のthreshold, このthreshold) でゲージが1→0に進む。最初のフェーズは [0, threshold[0])。")]
        [SerializeField] int[] phaseThresholdSamples;

        [Tooltip("配列の末尾まで使い切った後、ここで指定したインデックスに戻ってループする。"
                 + "例: [intro, a, b, c] で loopBackIndex=1 → intro→a→b→c→a→b→c→… となる。"
                 + "ループしない場合は配列長以上の値を設定。")]
        [SerializeField] int loopBackIndex = 1;

        BattlePhaseStateMachine phaseStateMachine;

        int phaseIndex = -1;
        int phaseStartThreshold;
        int phaseEndThreshold;
        bool isFirstPhase = true;
        bool isActive;
        float lastLogTime;

        readonly CompositeDisposable disposables = new();

        protected override void Start()
        {
            base.Start();

            phaseStateMachine = FindFirstObjectByType<BattlePhaseStateMachine>();
            if (phaseStateMachine == null)
            {
                Debug.LogWarning("[BossPhaseProgressGauge] BattlePhaseStateMachine not found in scene.", this);
                SetVisible(false);
                return;
            }

            phaseStateMachine.OnPhaseStarted
                .Subscribe(_ => OnPhaseStarted())
                .AddTo(disposables);
            
            SetVisible(false); 
            // DebugRuntimeModelInitializerを使ってBossから開始した場合は初回phaseStateMachineのフラグを取り逃がすのでここで回収
            if (RuntimeModelRepository.Get().CurrentSituation == BattleSituation.Boss)
            {
                OnPhaseStarted();
            }
        }

        void OnPhaseStarted()
        {
            if (phaseThresholdSamples == null || phaseThresholdSamples.Length == 0)
            {
                Debug.LogWarning("[BossPhaseProgressGauge] OnPhaseStarted: thresholds is empty, hiding gauge.", this);
                SetVisible(false);
                return;
            }

            var audioSource = soundManager?.BgmAudioSource;
            if (isFirstPhase)
            {
                phaseIndex = InferInitialPhaseIndex(audioSource != null ? audioSource.timeSamples : 0);
                isFirstPhase = false;
            }
            else
            {
                phaseIndex++;
            }
            
            int currentIndex = ResolvePhaseIndex(phaseIndex);
            phaseEndThreshold = phaseThresholdSamples[currentIndex];

            int prevRawIndex = phaseIndex - 1;
            phaseStartThreshold = prevRawIndex < 0 ? 0 : phaseThresholdSamples[ResolvePhaseIndex(prevRawIndex)];
            
            isActive = true;
            SetVisible(true);
        }

        // 初回購読時、現在のBGMサンプル位置から「いま何番目のフェーズか」を推定する。
        // 配列の thresholds を昇順とみなし、currentSamples 以上の最初のthresholdが今のフェーズ。
        int InferInitialPhaseIndex(int currentSamples)
        {
            for (int i = 0; i < phaseThresholdSamples.Length; i++)
            {
                if (currentSamples < phaseThresholdSamples[i])
                {
                    return i;
                }
            }
            return phaseThresholdSamples.Length - 1;
        }

        void Update()
        {
            if (!isActive || !fillImage) return;

            var audioSource = soundManager?.BgmAudioSource;
            if (audioSource == null || audioSource.clip == null) return;

            int currentSamples = audioSource.timeSamples;

            int sectionLength = phaseEndThreshold - phaseStartThreshold;
            int elapsed = currentSamples - phaseStartThreshold;

            if (sectionLength <= 0) return;

            float ratio = Mathf.Clamp01(1-(float)elapsed / sectionLength);
            fillImage.fillAmount = ratio;
        }

        int ResolvePhaseIndex(int rawIndex)
        {
            int length = phaseThresholdSamples.Length;
            if (rawIndex < length) return rawIndex;

            // ループ
            phaseIndex = loopBackIndex;
            return loopBackIndex;
        }

        void SetVisible(bool visible)
        {
            if (fillImage == null) return;
            fillImage.gameObject.SetActive(visible);
        }

        void OnDestroy()
        {
            disposables.Dispose();
        }
    }
}
