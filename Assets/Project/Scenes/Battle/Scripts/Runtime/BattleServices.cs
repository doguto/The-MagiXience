using System;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scripts.Extensions;
using UniRx;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ノードから使うバトル内のサービス。BattleScenePresenter が設定する。
    public sealed class BattleServices
    {
        public BattleServices(BattleWorld world) => Bullets = new BulletSystem(world);

        public BulletSystem Bullets { get; }
        public EnemySpawner Spawner { get; set; }
        public BulletClearReceiver Clearer { get; set; }
        public EnemyAgent Boss { get; set; }
        public IBossHost BossHost { get; set; }
        public IScenarioService Scenario { get; set; }
        public StageProgress Progress { get; } = new();
        public Action<SeType> PlaySe { get; set; }
        public Action<BgmType> PlayBgm { get; set; }
        public UnityEngine.AudioSource BgmSource { get; set; }
        // ステージのサービスが自機や背景へ働きかけるときに使う
        public PlayerEntityPresenter Player { get; set; }
        public BackgroundPresenter Background { get; set; }

        // Phase ノードが通知するフェーズの区切りと進み具合 (HpSections や進行ゲージが購読する)
        readonly Subject<string> phaseStarted = new();
        public IObservable<string> OnPhaseStarted => phaseStarted;
        public void NotifyPhase(string phaseId) => phaseStarted.OnNext(phaseId);

        // デバッグ起動で指定したノードまでの経路。着いたら null に戻る
        public BehaviorTree.NodeJump Jump { get; set; }

        // 実行中のフェーズの進み具合 (0〜1)。終了条件から決まらなければ null
        readonly ReactiveProperty<float?> phaseProgress = new();
        public IReadOnlyReactiveProperty<float?> PhaseProgress => phaseProgress;
        public void SetPhaseProgress(float? progress) => phaseProgress.Value = progress;
    }
}
