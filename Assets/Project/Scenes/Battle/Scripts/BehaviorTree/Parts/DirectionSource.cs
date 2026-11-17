using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ---------- 方向 ----------

    [Serializable]
    public abstract class DirectionSource
    {
        public abstract Vector2 GetDirection(in EmitContext e);
        // 発射位置を差し替える場合のみ値を返す (画面端からの発射など)
        public virtual Vector2? GetOrigin(in EmitContext e) => null;
        public virtual string Summary => GetType().Name.Replace("Direction", "");
    }
}
