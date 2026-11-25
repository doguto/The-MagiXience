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
            ExitRush();
        }

        // ===== 通しプレイ =====
        // 1面から最終面までを連続でプレイするモード。ステージ間で体力とコンティニュー回数を引き継ぐ。
        public bool IsRushMode { get; private set; }
        // 次のステージ開始時に適用する現在HP。0以下なら引き継ぎなし（満タンで開始）
        public int RushCarryHp { get; private set; }
        // 通しプレイ全体でのコンティニュー回数（最大HPテーブルの参照に使う）
        public int RushContinueCount { get; private set; }

        public void StartRush(BattleStageType firstStage)
        {
            IsRushMode = true;
            RushCarryHp = 0;
            RushContinueCount = 0;
            CurrentStageType = firstStage;
            CheckpointSection = 0;
        }

        // ステージクリア時に、次のステージへ引き継ぐ状態を保存する
        public void SaveRushCarryOver(int currentHp, int continueCount)
        {
            RushCarryHp = currentHp;
            RushContinueCount = continueCount;
        }

        public void ExitRush()
        {
            IsRushMode = false;
            RushCarryHp = 0;
            RushContinueCount = 0;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ボス戦をこのフェーズ・HP (%) から始める。空 / 100 なら指定なし
        public string DebugBossStartPhase { get; private set; }
        public float DebugBossStartHpPercent { get; private set; } = 100f;

        public void SetForDebug(int stageNumber, int startSection, string bossStartPhase, float bossStartHpPercent)
        {
            CurrentStageType = BattleStageTypeExtensions.FromInt(stageNumber);
            CheckpointSection = startSection;
            DebugBossStartPhase = bossStartPhase;
            DebugBossStartHpPercent = bossStartHpPercent;
        }
#endif
    }
}
