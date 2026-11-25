using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 走りながら弾を置く攻撃の経路。MovePath と PlaceAlongPath が同じアセットを読むので、移動と弾の位置がずれない
    [CreateAssetMenu(fileName = "TrailPath", menuName = "Battle/Trail Path")]
    public class TrailPath : ScriptableObject
    {
        [Serializable]
        public struct Segment
        {
            [Tooltip("Relative なら直前の点からの移動量、そうでなければワールド座標")] public Vector2 position;
            [Tooltip("この区間に置いた弾を撃つ向き")] public Vector2 direction;
            [Tooltip("区間内で隣り合う弾の角度 (度)")] public float spread;
            [Tooltip("全体の発射時刻からさらに遅らせるフレーム数")] public int extraFireDelayFrames;
        }

        [SerializeField, Tooltip("ON: 各点を直前の点からの移動量として扱う")] bool relative = true;
        [SerializeField, Min(1), Tooltip("経路全体を等速で移動するフレーム数")] int frames = 30;
        [SerializeField] List<Segment> segments = new();

        public bool Relative => relative;
        public int Frames => Mathf.Max(1, frames);
        public IReadOnlyList<Segment> Segments => segments;
    }
}
