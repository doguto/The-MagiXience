using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    /// <summary>
    /// HPバーの色帯1つ分の定義。upperRatio はこの帯の上端（残量割合）を表す。
    /// 例: [(1.0, 赤), (0.7, 橙), (0.25, 黄)] は
    ///     残量100%〜70%=赤、70%〜25%=橙、25%〜0%=黄 を意味する。
    /// upperRatio は降順（大きい順）で並べる想定。
    /// </summary>
    [Serializable]
    public struct HpBarColorStop
    {
        [Range(0f, 1f)] public float upperRatio;
        public Color color;
    }
}
