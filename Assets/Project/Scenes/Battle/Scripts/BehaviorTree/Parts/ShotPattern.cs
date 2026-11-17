using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 基準方向から弾ごとの方向・ずれ・遅延を作る。弾を撃つノードと BulletBeam が持つ (読むだけのデータなので複製しない。パラメータで差し替えるときは Resolve が複製を返す)
    [Serializable]
    public abstract class ShotPattern
    {
        public abstract void Build(Vector2 baseDirection, List<Shot> shots);
        public virtual string Summary => GetType().Name.Replace("Pattern", "");
        public virtual ShotPattern Resolve(TreeScope scope) => this;

        public static Vector2 Rotate(float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        // 奇数は中央に 1 発、偶数は中央を空ける
        public static float FanAngle(float baseAngle, int count, float spread, int i) =>
            count % 2 == 1
                ? baseAngle - spread * (count - 1) / 2f + spread * i
                : baseAngle - spread / 2f - spread * (count / 2 - 1) + spread * i;
    }
}
