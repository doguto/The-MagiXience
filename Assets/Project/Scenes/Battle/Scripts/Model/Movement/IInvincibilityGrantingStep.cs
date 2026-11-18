namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// このステップの実行中、Entityを無敵状態にすることを示すマーカーインターフェース。
    /// 実装したIMovementStepはPresenter側でPlay前後にSetInvincible(true/false)を呼ばれる。
    /// </summary>
    public interface IInvincibilityGrantingStep
    {
    }
}
