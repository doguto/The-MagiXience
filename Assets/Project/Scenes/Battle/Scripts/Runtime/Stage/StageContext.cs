using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ステージ (道中/ボス戦) 単位で有効になる機能。宣言したステージでだけ生成され、終了時に破棄される
    public interface IStageService
    {
        void Begin(StageContext stage);
        void Tick(float dt);
        void End();
    }

    // BattleStageAsset に並べるサービスの定義。実行時の状態は Create で作る別インスタンスに持たせる
    [Serializable]
    public abstract class StageServiceDefinition
    {
        public abstract IStageService Create();
    }

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
