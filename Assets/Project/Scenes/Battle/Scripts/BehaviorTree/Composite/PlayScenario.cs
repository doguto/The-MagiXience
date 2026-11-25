using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Scenario.Scripts.Model;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
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
            // 経路が中の OnCommand へ続くときは、会話を見せずにコマンドだけ流してそこまで進める
            var skip = skipOnResume && ctx.Services.Progress.CurrentSectionResumed || JumpTarget(ctx) >= 0;
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
}
