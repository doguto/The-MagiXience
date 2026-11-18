using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    public struct MotionLaunch
    {
        // 発射方向。移動によって使う/使わないが異なる (旧 IMovementStep の direction 引数と同じ)
        public Vector2 Direction;
        // 攻撃の発射時刻 (秒)。頂点の揺らぎなど発射時刻で変わる移動が使う
        public float AttackTime;
    }
}
