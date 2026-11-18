using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- 弾の設定 ----------

    // 撃つ側が弾に渡す設定一式。弾の Prefab は見た目と当たり判定だけを持ち、動きと振る舞いはここから渡す
    [Serializable]
    public sealed class BulletConfig
    {
        public BulletEntityPresenter prefab;
        [Tooltip("ツリーの Object パラメータ名。指定すると Prefab の代わりにその値を使う")] public string prefabParam;
        [Min(0)] public int damage = 10;
        [Tooltip("指定するとインラインの移動より優先")] public MotionPreset motionPreset;
        [SerializeReference, SubclassSelector] public List<MotionStep> motion = new();
        [Tooltip("飛距離の上限。0 で無制限")] public float range;
        [Min(0), Tooltip("消えるまでのフレーム数。0 で無制限")] public int lifetime;
        [Tooltip("ステージの外力 (IBulletForce) を受ける")] public bool affectedByForce = true;
        [Tooltip("弾を動かすツリー。未指定なら移動を実行するだけ (移動はツリーの MoveSelf が実行する)")] public BulletTreeAsset behavior;
        public List<TreeParameter> overrides = new();
        [Tooltip("合図の宛先「グループ」で使う名前。空ならグループに入らない")] public string group;

        public IReadOnlyList<MotionStep> Motion => motionPreset != null ? motionPreset.Steps : motion;
        public override string ToString()
        {
            var name = !string.IsNullOrEmpty(prefabParam) ? "$" + prefabParam : prefab ? prefab.name : "(弾未設定)";
            var motion = MotionStep.Describe(Motion);
            var text = string.IsNullOrEmpty(motion) ? name : $"{name} ({motion})";
            return behavior != null ? $"{text} [{behavior.name}]" : text;
        }

        // 定義は共有されるので、パラメータで差し替えるときは複製を返す
        public BulletConfig Resolve(TreeScope scope)
        {
            if (string.IsNullOrEmpty(prefabParam) || scope == null || !scope.TryGet(prefabParam, out var p)) return this;
            var resolved = p.ObjectValue as BulletEntityPresenter
                ?? (p.ObjectValue is GameObject go ? go.GetComponent<BulletEntityPresenter>() : null);
            if (resolved == null) return this;
            var copy = (BulletConfig)MemberwiseClone();
            copy.prefab = resolved;
            return copy;
        }
    }
}
