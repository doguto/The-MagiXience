using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 自機の移動速度を外から変える受け口。ステージのサービスが登録する
    public interface IMoveModifier
    {
        Vector2 Modify(Vector2 velocity, Vector2 input, float speed);
    }

    // 敵弾の本来の軌道に外力によるずれを足す受け口。ステージのサービスが登録する
    public interface IBulletForce
    {
        // 発射時点で外力を受け始めるか
        bool Active { get; }
        // 発射時点の外力の予定上の向き (MotionLaunch.WindAxis に渡す)
        Vector2 LaunchAxis { get; }
        void Integrate(Vector2 authoredVelocity, float dt, ref Vector2 velocity, ref Vector2 offset);
    }
}
