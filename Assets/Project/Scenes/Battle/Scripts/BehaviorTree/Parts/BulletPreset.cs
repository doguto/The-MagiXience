using Project.Scenes.Battle.Scripts.Presenter.Entity;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [CreateAssetMenu(fileName = "Bullet", menuName = "Battle/Bullet Preset")]
    public sealed class BulletPreset : ScriptableObject
    {
        [SerializeField] BulletEntityPresenter prefab;
        [SerializeField, Min(0)] int damage = 10;
        [SerializeField, Tooltip("飛距離の上限。0 で無制限")] float range;
        [SerializeField, Min(0), Tooltip("消えるまでのフレーム数。0 で無制限")] int lifetime;
        [SerializeField, Tooltip("ステージの外力 (IBulletForce) を受ける")] bool affectedByForce = true;

        public BulletEntityPresenter Prefab => prefab;
        public int Damage => damage;
        public float Range => range;
        public int Lifetime => lifetime;
        public bool AffectedByForce => affectedByForce;
    }
}
