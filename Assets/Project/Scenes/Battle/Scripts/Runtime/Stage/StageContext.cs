using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    public sealed class StageContext
    {
        readonly List<IStageService> services = new();

        public StageContext(BattleWorld world, IEnumerable<StageServiceDefinition> definitions)
        {
            World = world;
            if (definitions == null) return;
            foreach (var definition in definitions)
            {
                var service = definition?.Create();
                if (service != null) services.Add(service);
            }
        }

        public BattleWorld World { get; }

        public T Get<T>() where T : class, IStageService
        {
            foreach (var service in services)
            {
                if (service is T typed) return typed;
            }
            return null;
        }

        public bool TryGet<T>(out T service) where T : class, IStageService
        {
            service = Get<T>();
            return service != null;
        }

        // このステージで宣言されていないサービスを使おうとしたときの警告 (同じ内容は 1 回だけ)
        public void WarnMissing<T>(string user) where T : class, IStageService
        {
            var key = $"{user}/{typeof(T).Name}";
            if (!warned.Add(key)) return;
            Debug.LogWarning($"[StageContext] {user} requires {typeof(T).Name}, but the stage does not declare it.");
        }

        readonly HashSet<string> warned = new();

        internal void Begin()
        {
            foreach (var service in services) service.Begin(this);
        }

        internal void Tick(float dt)
        {
            foreach (var service in services) service.Tick(dt);
        }

        internal void End()
        {
            foreach (var service in services) service.End();
        }
    }
}
