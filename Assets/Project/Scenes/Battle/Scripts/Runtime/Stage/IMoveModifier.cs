using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 自機の移動速度を外から変える受け口。ステージのサービスが登録する
    public interface IMoveModifier
    {
        Vector2 Modify(Vector2 velocity, Vector2 input, float speed);
    }
}
