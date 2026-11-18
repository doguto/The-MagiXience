using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 敵弾の本来の軌道に外力によるずれを足す受け口。ステージのサービスが登録する
    public interface IBulletForce
    {
        // 発射時点で外力を受け始めるか
        bool Active { get; }
        void Integrate(Vector2 authoredVelocity, float dt, ref Vector2 velocity, ref Vector2 offset);
    }
}
