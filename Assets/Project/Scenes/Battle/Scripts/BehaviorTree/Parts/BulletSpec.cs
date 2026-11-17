using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- 弾の設定 ----------

    [Serializable]
    public sealed class BulletSpec
    {
        public BulletEntityPresenter prefab;
        [Tooltip("ツリーの Object パラメータ名。指定すると Prefab の代わりにその値を使う")] public string prefabParam;
        [Min(0)] public int damage = 10;
        [Tooltip("指定するとインラインの移動より優先")] public MotionPreset motionPreset;
        [SerializeReference, SubclassSelector] public List<MotionStep> motion = new();
        [Tooltip("飛距離の上限。0 で無制限")] public float range;
        public bool affectedByWind = true;

        public IReadOnlyList<MotionStep> Motion => motionPreset != null ? motionPreset.Steps : motion;
        public override string ToString() => !string.IsNullOrEmpty(prefabParam) ? "$" + prefabParam : prefab ? prefab.name : "(弾未設定)";

        // 定義は共有されるので、パラメータで差し替えるときは複製を返す
        public BulletSpec Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(prefabParam) || scope == null || !scope.TryGet(prefabParam, out var p)) return this;
            var resolved = p.ObjectValue as BulletEntityPresenter
                ?? (p.ObjectValue is GameObject go ? go.GetComponent<BulletEntityPresenter>() : null);
            if (resolved == null) return this;
            var copy = (BulletSpec)MemberwiseClone();
            copy.prefab = resolved;
            return copy;
        }
    }
}
