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
    public sealed class StageNodeDefinition : CompositeNodeDefinition
    {
        public override string Summary => $"{Children.Count} Section";

        public override INode Build(BuildScope scope)
        {
            var sections = new List<(INode node, BattleSituation situation)>();
            foreach (var child in Children)
            {
                if (child == null) continue;
                var situation = child is SectionNodeDefinition section ? section.Situation : BattleSituation.Way;
                if (child is not SectionNodeDefinition) Debug.LogWarning($"[BehaviorTree] Stage の子は Section にしてください: {child.GetType().Name}");
                sections.Add((child.Create(scope), situation));
            }
            return new StageNode(sections.ToArray());
        }

        sealed class StageNode : Node
        {
            readonly (INode node, BattleSituation situation)[] sections;
            int index;

            public StageNode((INode, BattleSituation)[] sections) => this.sections = sections;

            protected override void OnEnter(BattleContext ctx)
            {
                var progress = ctx.Services.Progress;
                index = Mathf.Clamp(progress.StartSection, 0, Mathf.Max(0, sections.Length - 1));
                if (index < sections.Length) progress.Enter(index, sections[index].situation, progress.ResumeStart);
            }

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                while (index < sections.Length)
                {
                    var status = sections[index].node.Tick(ctx);
                    if (status != NodeStatus.Success) return status;
                    index++;
                    if (index < sections.Length) ctx.Services.Progress.Enter(index, sections[index].situation, false);
                }
                return NodeStatus.Success;
            }

            protected override void OnAbort(BattleContext ctx)
            {
                if (index < sections.Length) sections[index].node.Abort(ctx);
            }
        }
    }

    [Serializable, NodeInfo("ステージの区切り。リトライするとこの Section の頭からやり直す。Situation は BGM・背景などシーン側の扱いに使う", "Flow")]
    public sealed class SectionNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField] string label;
        [SerializeField] BattleSituation situation;

        public string Label => label;
        public BattleSituation Situation => situation;
        public override string Summary => $"{(string.IsNullOrEmpty(label) ? "(名前なし)" : label)} [{situation}]";

        public override INode Build(BuildScope scope) => BuildChild(scope);
    }

    // ---------- 道中 ----------

    [Serializable]
    public sealed class WaveEntry
    {
        [Min(0), Tooltip("Wave の開始から数えたフレーム")] public int frame;
        [SerializeReference] public SpawnNodeDefinition spawn = new();
    }

    [Serializable, NodeInfo("出現一覧を時刻どおりに出し、全部出したあと終了条件が成立したら完了する", "Flow")]
    public sealed class WaveNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] List<WaveEntry> entries = new();
        [SerializeReference, SubclassSelector, Tooltip("全部出したあとに待つ条件。未設定なら最後の出現で完了")] ConditionDefinition until;

        public List<WaveEntry> Entries => entries;

        public ConditionDefinition Until
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

        public override INode Build(BuildScope scope)
        {
            var ordered = entries.Where(e => e?.spawn != null).OrderBy(e => e.frame).ToArray();
            var frames = ordered.Select(e => e.frame).ToArray();
            var spawns = ordered.Select(e => e.spawn.Create(scope)).ToArray();
            return new WaveNode(frames, spawns, until?.Build(scope));
        }

        sealed class WaveNode : Node
        {
            readonly int[] frames;
            readonly INode[] spawns;
            readonly ICondition until;
            int elapsed;
            int next;

            public WaveNode(int[] frames, INode[] spawns, ICondition until)
            {
                this.frames = frames;
                this.spawns = spawns;
                this.until = until;
            }

            protected override void OnEnter(BattleContext ctx)
            {
                elapsed = 0;
                next = 0;
                until?.Reset(ctx);
            }

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                while (next < spawns.Length && frames[next] <= elapsed)
                {
                    spawns[next].Tick(ctx);
                    next++;
                }
                elapsed++;
                if (next < spawns.Length) return NodeStatus.Running;
                return until == null || until.Evaluate(ctx) ? NodeStatus.Success : NodeStatus.Running;
            }
        }
    }

    // ---------- BGM ----------

    [Serializable, NodeInfo("このステージの BGM を流す (曲名の表示はシーン側)", "Action/Stage")]
    public sealed class PlayBgmNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BgmType bgm = BgmType.BattleWay;

        public override string Summary => bgm.ToString();

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            ctx.Services.PlayBgm?.Invoke(bgm);
            return NodeStatus.Success;
        });
    }

    // ---------- ボス ----------

    [Serializable, NodeInfo("ボスを出して登場の移動を流す。ツリーはまだ付けない (BossBattle で付ける)", "Action/Boss")]
    public sealed class SpawnBossNodeDefinition : ActionNodeDefinition
    {
        [SerializeField] BossDefinition boss;

        public override string Summary => boss ? boss.name : "(ボス未設定)";

        public override INode Build(BuildScope scope) => new ActionNode(ctx =>
        {
            var host = ctx.Services.BossHost;
            if (host == null || boss == null)
            {
                Debug.LogWarning("[BehaviorTree] SpawnBoss: BossHost または BossDefinition がありません");
                return NodeStatus.Failure;
            }
            return host.Spawn(boss) ? NodeStatus.Success : NodeStatus.Failure;
        });
    }

    [Serializable, NodeInfo("出ているボスにツリーを付けて戦闘を始め、撃破演出が終わるまで待つ", "Action/Boss")]
    public sealed class BossBattleNodeDefinition : ActionNodeDefinition
    {
        [SerializeField, Tooltip("未指定なら BossDefinition のツリー")] BehaviorTreeAsset tree;

        public override string Summary => tree ? tree.name : "(BossDefinition のツリー)";

        public override INode Build(BuildScope scope) => new BossBattleNode(tree);

        sealed class BossBattleNode : Node
        {
            readonly BehaviorTreeAsset tree;
            bool started;

            public BossBattleNode(BehaviorTreeAsset tree) => this.tree = tree;

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
    }

    // ---------- 会話 ----------

    [Serializable, NodeInfo("会話を流し、終わるまで待つ。会話中のコマンドは同じ名前の OnCommand の子を実行する", "Flow")]
    public sealed class ScenarioNodeDefinition : CompositeNodeDefinition
    {
        [SerializeField] ScenarioData scenario;
        [SerializeField, Tooltip("リトライでこの Section を途中から始めたときは会話を見せず、コマンドだけ実行する")] bool skipOnResume = true;
        [SerializeField, Tooltip("会話の後に自機の攻撃を戻す。ステージ最後の会話では OFF")] bool restorePlayerControl = true;

        public ScenarioData Scenario => scenario;

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
            Children.OfType<OnCommandNodeDefinition>().Select(c => c.Command).Where(c => !string.IsNullOrEmpty(c));

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

        public override INode Build(BuildScope scope)
        {
            var handlers = new Dictionary<string, INode>();
            foreach (var child in Children)
            {
                if (child is not OnCommandNodeDefinition onCommand || string.IsNullOrEmpty(onCommand.Command)) continue;
                handlers[onCommand.Command] = onCommand.Create(scope);
            }
            return new ScenarioNode(scenario, skipOnResume, restorePlayerControl, handlers);
        }

        sealed class ScenarioNode : Node
        {
            readonly ScenarioData scenario;
            readonly bool skipOnResume;
            readonly bool restorePlayerControl;
            readonly Dictionary<string, INode> handlers;
            readonly List<INode> running = new();
            IScenarioHandle handle;

            public ScenarioNode(ScenarioData scenario, bool skipOnResume, bool restorePlayerControl, Dictionary<string, INode> handlers)
            {
                this.scenario = scenario;
                this.skipOnResume = skipOnResume;
                this.restorePlayerControl = restorePlayerControl;
                this.handlers = handlers;
            }

            protected override void OnEnter(BattleContext ctx)
            {
                running.Clear();
                var skip = skipOnResume && ctx.Services.Progress.CurrentSectionResumed;
                var service = ctx.Services.Scenario;
                if (service == null && !skip) Debug.LogWarning("[BehaviorTree] Scenario: 会話サービスが無いため、コマンドだけ実行します");
                handle = service != null ? service.Play(scenario, skip, restorePlayerControl) : ScenarioCommandQueue.Skipped(scenario);
            }

            protected override NodeStatus OnTick(BattleContext ctx)
            {
                while (handle.TryDequeueCommand(out var command))
                {
                    if (!handlers.TryGetValue(command.function, out var handler))
                    {
                        Debug.LogWarning($"[BehaviorTree] Scenario: コマンド '{command.function}' に対応する OnCommand がありません ({scenario?.name})");
                        continue;
                    }
                    if (running.Contains(handler))
                    {
                        Debug.LogWarning($"[BehaviorTree] Scenario: '{command.function}' の前の実行が終わっていないため無視しました");
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
    }

    [Serializable, NodeInfo("Scenario の子に置く。会話がこのコマンドの行に来たら子を実行する", "Flow")]
    public sealed class OnCommandNodeDefinition : DecoratorNodeDefinition
    {
        [SerializeField, Tooltip("シナリオの function 名 (例: SpawnBoss)")] string command;

        public string Command
        {
            get => command;
            set => command = value;
        }

        public override string Summary => string.IsNullOrEmpty(command) ? "(コマンド未設定)" : "On " + command;

        public override INode Build(BuildScope scope) => BuildChild(scope);
    }
}
