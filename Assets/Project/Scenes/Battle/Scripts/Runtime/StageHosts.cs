using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ツリーから会話を流す窓口。会話シーンの読み込みや操作の無効化は実装側 (Presenter) が持つ
    public interface IScenarioService
    {
        // skip なら会話を見せず、コマンドだけを順に積んで即完了にする
        IScenarioHandle Play(ScenarioData data, bool skip, bool restorePlayerControl);
    }

    public interface IScenarioHandle
    {
        bool IsCompleted { get; }
        bool TryDequeueCommand(out ScenarioStep command);
        void Cancel();
    }

    // 会話シーンから届いたコマンドを、BT のフレームで取り出すまで溜めておく
    public sealed class ScenarioCommandQueue : IScenarioHandle
    {
        readonly Queue<ScenarioStep> commands = new();
        Action onCancel;

        public ScenarioCommandQueue(Action onCancel = null) => this.onCancel = onCancel;

        public bool IsCompleted { get; private set; }

        public static ScenarioCommandQueue Skipped(ScenarioData data)
        {
            var queue = new ScenarioCommandQueue();
            if (data != null)
            {
                foreach (var step in data.Commands) queue.Enqueue(step);
            }
            queue.Complete();
            return queue;
        }

        public void Enqueue(ScenarioStep command) => commands.Enqueue(command);
        public void Complete() => IsCompleted = true;

        public bool TryDequeueCommand(out ScenarioStep command)
        {
            if (commands.Count > 0)
            {
                command = commands.Dequeue();
                return true;
            }
            command = null;
            return false;
        }

        public void Cancel()
        {
            commands.Clear();
            IsCompleted = true;
            var cancel = onCancel;
            onCancel = null;
            cancel?.Invoke();
        }
    }

    // ボスの生成とボス戦の開始。生成は会話の途中、戦闘開始は会話の後になるので分けている
    public interface IBossHost
    {
        BossDefinition Current { get; }
        bool Spawn(BossDefinition definition);
        BattleActor StartBattle(BehaviorTreeAsset tree);
        // 撃破演出まで終わったら true
        bool IsDefeated { get; }
    }

    // ステージツリーの進み具合。Section の開始をシーン側 (BGM・背景・リトライ地点) へ知らせる
    public sealed class StageProgress
    {
        public int StartSection { get; set; }
        // StartSection をリトライなどで途中から始めたか。会話を飛ばすかの判断に使う
        public bool ResumeStart { get; set; }

        public int CurrentSection { get; private set; } = -1;
        public BattleSituation CurrentSituation { get; private set; }
        public bool CurrentSectionResumed { get; private set; }

        public event Action<int, BattleSituation, bool> SectionStarted;

        public void Enter(int index, BattleSituation situation, bool resumed)
        {
            CurrentSection = index;
            CurrentSituation = situation;
            CurrentSectionResumed = resumed;
            SectionStarted?.Invoke(index, situation, resumed);
        }
    }
}
