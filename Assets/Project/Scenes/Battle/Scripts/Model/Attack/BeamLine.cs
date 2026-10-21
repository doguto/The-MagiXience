using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// ビーム1本分の線分を、ワールド座標の起点と終点で表したもの。
    ///
    /// 方向・長さ・角度は全てここから導出する。アセット側で方向を別途持たせると
    /// 起点終点とズレても気づけないため、ビーム系では必ずこの型を単一の真実にすること。
    /// </summary>
    [Serializable]
    public class BeamLine
    {
        [SerializeField, Tooltip("ビームの起点(ワールド座標)")]
        Vector2 start;

        [SerializeField, Tooltip("ビームの終点(ワールド座標)")]
        Vector2 end;

        public BeamLine() { }

        public BeamLine(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }

        public Vector2 Start => start;
        public Vector2 End => end;

        /// <summary>起点と終点が完全に一致している場合のみ Vector2.left にフォールバックする</summary>
        public Vector2 Direction
        {
            get
            {
                var delta = end - start;
                return delta == Vector2.zero ? Vector2.left : delta.normalized;
            }
        }

        public float Length => (end - start).magnitude;

        /// <summary>+X 方向を正面としたときの、線分に沿った回転</summary>
        public Quaternion Rotation
        {
            get
            {
                var dir = Direction;
                return Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
        }

        public BeamLine Clone() => new(start, end);
    }
}
