#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Project.Scripts.Extensions.Message;
using Project.Scripts.Repository.ModelRepository;
using UniRx;
using UnityEngine;

namespace Project.Commons.Debugger.Scripts.Presenter
{
    public class DebugRuntimeModelInitializer : MonoBehaviour
    {
        [SerializeField] [Min(1)] int stageNumber = 1;
        [SerializeField, Min(0), Tooltip("開始する Section の番号 (ステージツリーの Stage 直下の並び順)")] int startSection;
        [SerializeField, Tooltip("ボス戦をこの Phase から始める。空なら最初から")] string bossStartPhase;
        [SerializeField, Range(1f, 100f), Tooltip("ボス戦開始時のボスの HP (%)")] float bossStartHpPercent = 100f;

        void Awake()
        {
            var runtimeModel = RuntimeModelRepository.Instance.Get();
            if (runtimeModel.IsInGame)
            {
                Debug.Log("[DebugRuntimeModelInitializer] RuntimeModel は既にゲーム中のためデバッグ初期化をスキップしました");
                return;
            }

            runtimeModel.SetForDebug(stageNumber, startSection, bossStartPhase, bossStartHpPercent);
            Debug.Log($"[DebugRuntimeModelInitializer] Stage={stageNumber}, StartSection={startSection}, BossPhase={bossStartPhase}, BossHp={bossStartHpPercent}% に設定しました");
        }

        void Start()
        {
            MessageBroker.Default.Publish(new SceneNavigationMessage(SceneNavigationState.Completed, "Battle"));
        }
    }
}
#endif
