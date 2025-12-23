namespace Project.Scripts.Model
{
    /// <summary>
    /// ゲームの進行状況（道中 or ボス戦）
    /// </summary>
    public enum GameSituation
    {
        Way,  // 道中
        Boss  // ボス戦
    }

    public class RuntimeModel : ModelBase
    {
        public int CurrentStageNumber { get; internal set; } = 1;
        public GameSituation CurrentSituation { get; internal set; } = GameSituation.Way;

        /// <summary>
        /// 現在の進行状況からシナリオ番号を計算（1〜14）
        /// ステージ1道中=1, ステージ1ボス=2, ステージ2道中=3, ...
        /// </summary>
        public int GetScenarioNumber()
        {
            return (CurrentStageNumber - 1) * 2 + (CurrentSituation == GameSituation.Boss ? 2 : 1);
        }

        /// <summary>
        /// GameSituationを直接設定する
        /// </summary>
        public void SetSituation(GameSituation situation)
        {
            CurrentSituation = situation;
        }

        /// <summary>
        /// ゲームを次のフェーズに進める
        /// Way → Boss → 次ステージのWay
        /// </summary>
        public void AdvanceToNextPhase()
        {
            if (CurrentSituation == GameSituation.Way)
            {
                CurrentSituation = GameSituation.Boss;
            }
            else
            {
                CurrentStageNumber++;
                CurrentSituation = GameSituation.Way;
            }
        }

        /// <summary>
        /// ステージプレイを終了し、非プレイ状態にする
        /// ステージ選択画面に戻るときなどに使用
        /// </summary>
        public void ExitStage()
        {
            CurrentStageNumber = -1;
            CurrentSituation = GameSituation.Way;
        }
    }
}
