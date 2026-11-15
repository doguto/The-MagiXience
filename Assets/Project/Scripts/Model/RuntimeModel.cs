using Project.Scripts.Extensions;

namespace Project.Scripts.Model
{
    // ステージツリーの Section に付ける種類。BGM や背景など、シーン側の扱いを分けるのに使う
    public enum BattleSituation
    {
        Way,
        Boss
    }

    public class RuntimeModel : ModelBase
    {
        public BattleStageType CurrentStageType { get; set; } = BattleStageType.Null;
        public bool IsInGame => CurrentStageType != BattleStageType.Null;

        // リトライで戻る Section の番号 (ステージツリーの Stage ノード直下の並び順)
        public int CheckpointSection { get; set; }

        public void ExitStage()
        {
            CurrentStageType = BattleStageType.Null;
            CheckpointSection = 0;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SetForDebug(int stageNumber, int startSection)
        {
            CurrentStageType = BattleStageTypeExtensions.FromInt(stageNumber);
            CheckpointSection = startSection;
        }
#endif
    }
}
