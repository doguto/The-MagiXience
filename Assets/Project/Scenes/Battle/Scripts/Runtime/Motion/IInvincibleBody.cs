namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 移動のステップが無敵を与えられる対象 (弾)。無敵を与えるステップの実行中だけ ON になる
    public interface IInvincibleBody
    {
        bool Invincible { set; }
    }
}
