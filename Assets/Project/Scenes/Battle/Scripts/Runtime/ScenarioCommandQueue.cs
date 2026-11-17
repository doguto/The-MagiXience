using System;
using System.Collections.Generic;
using Project.Scenes.Scenario.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Runtime
{
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
}
