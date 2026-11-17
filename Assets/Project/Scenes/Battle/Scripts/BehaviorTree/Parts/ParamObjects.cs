using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ノードの「〜Param」欄 (パラメータ名) から Object パラメータの値を引く。名前が空なら固定値を使う
    public static class ParamObjects
    {
        public static T Resolve<T>(TreeScope scope, string param, T fallback) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(param)) return fallback;
            if (scope != null && scope.TryGet(param, out var p)) return p.ObjectValue as T;
            Debug.LogWarning($"[BehaviorTree] パラメータ '{param}' が見つかりません");
            return fallback;
        }
    }
}
