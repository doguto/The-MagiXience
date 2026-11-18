using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 移動の対象。弾は風補正前の「本来の軌道」、敵/ボスは Transform そのもの。
    public interface IMotionBody
    {
        Vector3 Position { get; set; }
        Quaternion Rotation { get; set; }
        Transform Transform { get; }
    }
}
