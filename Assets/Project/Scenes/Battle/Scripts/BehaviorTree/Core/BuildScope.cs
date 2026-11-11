using System;
using System.Collections.Generic;
using System.Linq;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // Build 時のパラメータ解決と SubTree の循環検出を担う。
    public sealed class BuildScope
    {
        readonly BuildScope parent;
        readonly BehaviorTreeAsset asset;
        readonly Dictionary<string, TreeParameter> values = new();

        BuildScope(BuildScope parent, BehaviorTreeAsset asset)
        {
            this.parent = parent;
            this.asset = asset;
        }

        public BehaviorTreeAsset Asset => asset;

        public static BuildScope Enter(BuildScope parent, BehaviorTreeAsset asset, IReadOnlyList<TreeParameter> overrides)
        {
            for (var s = parent; s != null; s = s.parent)
            {
                if (s.asset == asset)
                {
                    throw new InvalidOperationException($"[BehaviorTree] SubTree cycle detected: {DescribePath(parent)} -> {asset.name}");
                }
            }

            var scope = new BuildScope(parent, asset);
            foreach (var p in asset.Parameters)
            {
                if (!string.IsNullOrEmpty(p.Name)) scope.values[p.Name] = p;
            }

            if (overrides != null)
            {
                foreach (var o in overrides)
                {
                    if (string.IsNullOrEmpty(o.Name)) continue;
                    if (!scope.values.ContainsKey(o.Name))
                    {
                        UnityEngine.Debug.LogWarning($"[BehaviorTree] Unknown parameter override: {asset.name}.{o.Name}");
                        continue;
                    }
                    scope.values[o.Name] = o.ResolveAgainst(parent);
                }
            }

            return scope;
        }

        public bool TryGet(string name, out TreeParameter parameter) => values.TryGetValue(name, out parameter);

        static string DescribePath(BuildScope scope)
        {
            var names = new List<string>();
            for (var s = scope; s != null; s = s.parent) names.Add(s.asset ? s.asset.name : "null");
            names.Reverse();
            return string.Join(" -> ", names.Where(n => n != null));
        }
    }
}
