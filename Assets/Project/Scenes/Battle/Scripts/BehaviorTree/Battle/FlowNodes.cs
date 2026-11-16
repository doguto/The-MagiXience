using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- ステージの進行 ----------

    [Serializable, NodeInfo("ステージツリーの一番上に置く。子の Section を順に実行し、リトライ時は記録した Section から始める", "Flow")]
    public sealed class Stage : CompositeNode
    {
        [NonSerialized] int index;

        public override string Summary => $"{Children.Count} Section";

        protected override void OnSetup(TreeScope scope)
        {
            foreach (var child in Children)
            {
                if (child is not Section) Debug.LogWarning($"[BehaviorTree] Stage の子は Section にしてください: {child.GetType().Name}");
            }
        }

        static BattleSituation SituationOf(Node child) => child is Section section ? section.Situation : BattleSituation.Way;

        protected override void OnEnter(BattleContext ctx)
        {
            var progress = ctx.Services.Progress;
            index = Mathf.Clamp(progress.StartSection, 0, Mathf.Max(0, Children.Count - 1));
            if (index < Children.Count) progress.Enter(index, SituationOf(Children[index]), progress.ResumeStart);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (index < Children.Count)
            {
                var status = Children[index].Tick(ctx);
                if (status != NodeStatus.Success) return status;
                index++;
                if (index < Children.Count) ctx.Services.Progress.Enter(index, SituationOf(Children[index]), false);
            }
            return NodeStatus.Success;
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (index < Children.Count) Children[index].Abort(ctx);
        }
    }

    [Serializable, NodeInfo("ステージの区切り。リトライするとこの Section の頭からやり直す。Situation は BGM・背景などシーン側の扱いに使う", "Flow")]
    public sealed class Section : DecoratorNode
    {
        [SerializeField] string label;
        [SerializeField] BattleSituation situation;

        public string Label => label;
        public BattleSituation Situation => situation;
        public override string Summary => $"{(string.IsNullOrEmpty(label) ? "(名前なし)" : label)} [{situation}]";

        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx);
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }

    // ---------- 道中 ----------

    [Serializable]
    public sealed class WaveEntry
    {
        [Min(0), Tooltip("Wave の開始から数えたフレーム")] public int frame;
        [SerializeReference] public Spawn spawn = new();
    }

    [Serializable, NodeInfo("出現一覧を時刻どおりに出し、全部出したあと終了条件が成立したら完了する", "Flow")]
    public sealed class Wave : ActionNode
    {
        [SerializeField] List<WaveEntry> entries = new();
        [SerializeReference, SubclassSelector, Tooltip("全部出したあとに待つ条件。未設定なら最後の出現で完了")] Condition until;

        [NonSerialized] WaveEntry[] ordered;
        [NonSerialized] int elapsed;
        [NonSerialized] int next;

        public List<WaveEntry> Entries => entries;

        public Condition Until
        {
            get => until;
            set => until = value;
        }

        public override string Summary
        {
            get
            {
                var last = entries.Count == 0 ? 0 : entries.Max(e => e.frame);
                return $"{entries.Count} 体 / {last}f まで" + (until != null ? " → " + until.Summary : "");
            }
        }

        protected override void OnSetup(TreeScope scope)
        {
            ordered = entries.Where(e => e?.spawn != null).OrderBy(e => e.frame).ToArray();
            foreach (var entry in ordered) entry.spawn.Setup(scope);
            until?.Setup(scope);
        }

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            next = 0;
            until?.Reset(ctx);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (next < ordered.Length && ordered[next].frame <= elapsed)
            {
                ordered[next].spawn.Tick(ctx);
                next++;
            }
            elapsed++;
            if (next < ordered.Length) return NodeStatus.Running;
            return until == null || until.Evaluate(ctx) ? NodeStatus.Success : NodeStatus.Running;
        }
    }

    // ---------- BGM ----------

    [Serializable, NodeInfo("このステージの BGM を流す (曲名の表示はシーン側)", "Action/Stage")]
    public sealed class PlayBgm : ActionNode
    {
        [SerializeField] BgmType bgm = BgmType.BattleWay;

        public override string Summary => bgm.ToString();

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            ctx.Services.PlayBgm?.Invoke(bgm);
            return NodeStatus.Success;
        }
    }

    // ---------- ボス ----------

    [Serializable, NodeInfo("ボスを出して登場の移動を流す。ツリーはまだ付けない (BossBattle で付ける)", "Action/Boss")]
    public sealed class SpawnBoss : ActionNode
    {
        [SerializeField] BossDefinition boss;

        public override string Summary => boss ? boss.name : "(ボス未設定)";

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var host = ctx.Services.BossHost;
            if (host == null || boss == null)
            {
                Debug.LogWarning("[BehaviorTree] SpawnBoss: BossHost または BossDefinition がありません");
                return NodeStatus.Failure;
            }
            return host.Spawn(boss) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    [Serializable, NodeInfo("出ているボスにツリーを付けて戦闘を始め、撃破演出が終わるまで待つ", "Action/Boss")]
    public sealed class BossBattle : ActionNode
    {
        [SerializeField, Tooltip("未指定なら BossDefinition のツリー")] BehaviorTreeAsset tree;

        [NonSerialized] bool started;

        public override string Summary => tree ? tree.name : "(BossDefinition のツリー)";

        protected override void OnEnter(BattleContext ctx)
        {
            var host = ctx.Services.BossHost;
            var resolved = tree != null ? tree : host?.Current != null ? host.Current.Tree : null;
            started = host != null && host.Current != null && host.StartBattle(resolved) != null;
            if (!started) Debug.LogWarning("[BehaviorTree] BossBattle: ボスが出ていないか、ツリーがありません");
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (!started) return NodeStatus.Failure;
            return ctx.Services.BossHost.IsDefeated ? NodeStatus.Success : NodeStatus.Running;
        }
    }

    // ---------- 会話 ----------

    [Serializable, NodeInfo("会話を流し、終わるまで待つ。会話中のコマンドは同じ名前の OnCommand の子を実行する", "Flow")]
    public sealed class PlayScenario : CompositeNode
    {
        [SerializeField] ScenarioData scenario;
        [SerializeField, Tooltip("リトライでこの Section を途中から始めたときは会話を見せず、コマンドだけ実行する")] bool skipOnResume = true;
        [SerializeField, Tooltip("会話の後に自機の攻撃を戻す。ステージ最後の会話では OFF")] bool restorePlayerControl = true;

        [NonSerialized] Dictionary<string, OnCommand> handlers;
        [NonSerialized] List<OnCommand> running;
        [NonSerialized] IScenarioHandle handle;

        public ScenarioData Data => scenario;

        public override string Summary
        {
            get
            {
                var text = (scenario ? scenario.name : "(会話未設定)") + (skipOnResume ? "" : " / リトライでも流す");
                var missing = MissingCommands().ToArray();
                var unused = UnusedCommands().ToArray();
                if (missing.Length > 0) text += "\n⚠ OnCommand が無い: " + string.Join(", ", missing);
                if (unused.Length > 0) text += "\n⚠ 会話に無いコマンド: " + string.Join(", ", unused);
                return text;
            }
        }

        IEnumerable<string> HandledCommands() =>
            Children.OfType<OnCommand>().Select(c => c.Command).Where(c => !string.IsNullOrEmpty(c));

        // 会話に出てくるのに、受け取る OnCommand が無いコマンド
        public IEnumerable<string> MissingCommands()
        {
            if (scenario == null) return Enumerable.Empty<string>();
            var handled = new HashSet<string>(HandledCommands());
            return scenario.Commands.Select(c => c.function).Distinct().Where(f => !handled.Contains(f));
        }

        IEnumerable<string> UnusedCommands()
        {
            if (scenario == null) return Enumerable.Empty<string>();
            var used = new HashSet<string>(scenario.Commands.Select(c => c.function));
            return HandledCommands().Where(c => !used.Contains(c));
        }

        protected override void OnSetup(TreeScope scope)
        {
            handlers = new Dictionary<string, OnCommand>();
            foreach (var onCommand in Children.OfType<OnCommand>())
            {
                if (!string.IsNullOrEmpty(onCommand.Command)) handlers[onCommand.Command] = onCommand;
            }
            running = new List<OnCommand>();
        }

        protected override void OnEnter(BattleContext ctx)
        {
            running.Clear();
            var skip = skipOnResume && ctx.Services.Progress.CurrentSectionResumed;
            var service = ctx.Services.Scenario;
            if (service == null && !skip) Debug.LogWarning("[BehaviorTree] PlayScenario: 会話サービスが無いため、コマンドだけ実行します");
            handle = service != null ? service.Play(scenario, skip, restorePlayerControl) : ScenarioCommandQueue.Skipped(scenario);
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            while (handle.TryDequeueCommand(out var command))
            {
                if (!handlers.TryGetValue(command.function, out var handler))
                {
                    Debug.LogWarning($"[BehaviorTree] PlayScenario: コマンド '{command.function}' に対応する OnCommand がありません ({scenario?.name})");
                    continue;
                }
                if (running.Contains(handler))
                {
                    Debug.LogWarning($"[BehaviorTree] PlayScenario: '{command.function}' の前の実行が終わっていないため無視しました");
                    continue;
                }
                running.Add(handler);
            }

            for (var i = 0; i < running.Count; i++)
            {
                var status = running[i].Tick(ctx);
                if (status == NodeStatus.Running) continue;
                running.RemoveAt(i--);
            }

            return handle.IsCompleted && running.Count == 0 ? NodeStatus.Success : NodeStatus.Running;
        }

        protected override void OnAbort(BattleContext ctx)
        {
            foreach (var node in running) node.Abort(ctx);
            running.Clear();
            handle?.Cancel();
        }
    }

    [Serializable, NodeInfo("PlayScenario の子に置く。会話がこのコマンドの行に来たら子を実行する", "Flow")]
    public sealed class OnCommand : DecoratorNode
    {
        [SerializeField, Tooltip("シナリオの function 名 (例: SpawnBoss)")] string command;

        public string Command
        {
            get => command;
            set => command = value;
        }

        public override string Summary => string.IsNullOrEmpty(command) ? "(コマンド未設定)" : "On " + command;

        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx);
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }
}
