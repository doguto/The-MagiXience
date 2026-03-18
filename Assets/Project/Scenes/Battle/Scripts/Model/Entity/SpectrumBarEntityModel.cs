namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    /// <summary>
    /// 破壊不可能なスペクトラムバー用エンティティモデル。
    /// EnemyEntityModelを継承するため、PlayerEntityModel.OnCollisionの
    /// `other is EnemyEntityModel` で自動的にダメージ処理される。
    /// </summary>
    public class SpectrumBarEntityModel : EnemyEntityModel
    {
        public SpectrumBarEntityModel(int contactDamage)
            : base(int.MaxValue, contactDamage)
        {
        }

        public override void OnCollision(EntityBase other)
        {
            // ダメージを受けない（破壊不可）
        }
    }
}
