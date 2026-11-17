namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // WarningLine の子に置けるビーム。狙いを受け取ったら、自分で狙い直さずにそれで撃つ
    public interface IBeam
    {
        BeamAim CreateAim(BattleContext ctx);
        void UseAim(BeamAim aim);
    }
}
