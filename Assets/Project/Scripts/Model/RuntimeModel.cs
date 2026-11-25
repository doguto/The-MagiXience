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
        // DebugStartSection を始めるときは、ツリーの DebugStartPath のノードから・ボスの HP (%) を DebugBossStartHpPercent にして始める
        public int DebugStartSection { get; private set; }
        public string DebugStartPath { get; private set; }
        public float DebugBossStartHpPercent { get; private set; } = 100f;

        public void SetForDebug(int stageNumber, int startSection, string startPath, float bossStartHpPercent)
        {
            CurrentStageType = BattleStageTypeExtensions.FromInt(stageNumber);
            CheckpointSection = startSection;
            DebugStartSection = startSection;
            DebugStartPath = startPath;
            DebugBossStartHpPercent = bossStartHpPercent;
        }
#endif
    }
}
