using Project.Scripts.Extensions;

namespace Project.Scripts.Model
{
    public enum BattleSituation
    {
        Way,
        Boss
    }

    public class RuntimeModel : ModelBase
    {
        public BattleStageType CurrentStageType { get; set; } = BattleStageType.Null;
        public bool IsInGame => CurrentStageType != BattleStageType.Null;
        public BattleSituation CurrentSituation { get; set; } = BattleSituation.Way;

        public int GetScenarioNumber()
        {
            return (CurrentStageType.AsInt() - 1) * 2 + (CurrentSituation == BattleSituation.Boss ? 2 : 1);
        }

        public void SetSituation(BattleSituation situation)
        {
            CurrentSituation = situation;
        }

        public void AdvanceToNextSequence()
        {
            if (CurrentSituation == BattleSituation.Way)
            {
                CurrentSituation = BattleSituation.Boss;
                UnityEngine.Debug.Log($"[RuntimeModel] Stage {CurrentStageType.AsInt()}: Way → Boss");
            }
            else
            {
                CurrentStageType = CurrentStageType.Next();
                CurrentSituation = BattleSituation.Way;
                UnityEngine.Debug.Log($"[RuntimeModel] Stage advanced: {CurrentStageType.AsInt() - 1} → {CurrentStageType.AsInt()}");
            }
        }

        public void ExitStage()
        {
            CurrentStageType = BattleStageType.Null;
            CurrentSituation = BattleSituation.Way;
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
            CurrentSituation = BattleSituation.Way;
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
        /// <summary>
        /// デバッグ起動時の開始フェーズ。シーケンス内の全グループを通した連番で、0 が先頭。
        /// </summary>
        public int DebugStartPhaseIndex { get; private set; }

        public void SetForDebug(int stageNumber, BattleSituation situation, int startPhaseIndex = 0)
        {
            CurrentStageType = BattleStageTypeExtensions.FromInt(stageNumber);
            CurrentSituation = situation;
            DebugStartPhaseIndex = startPhaseIndex;
        }
#endif
    }
}
