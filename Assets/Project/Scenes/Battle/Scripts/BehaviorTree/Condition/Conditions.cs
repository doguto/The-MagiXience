using System.Collections.Generic;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    static class Conditions
    {
        public static string Join(List<Condition> conditions, string separator)
        {
            var parts = new List<string>();
            foreach (var c in conditions) parts.Add(c?.Summary ?? "?");
            return parts.Count == 0 ? "(空)" : "(" + string.Join(separator, parts) + ")";
        }

        public static void Setup(List<Condition> conditions, TreeScope scope)
        {
            foreach (var c in conditions) c?.Setup(scope);
        }

        public static void Reset(List<Condition> conditions, BattleContext ctx)
        {
            foreach (var c in conditions) c?.Reset(ctx);
        }
    }
}
