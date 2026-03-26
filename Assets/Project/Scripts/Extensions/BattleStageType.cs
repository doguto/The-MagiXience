namespace Project.Scripts.Extensions
{
    public enum BattleStageType
    {
        Stage1 = 1,
        Stage2 = 2,
        Stage3 = 3,
        Stage4 = 4,
        Stage5 = 5,
        Stage6 = 6,
        StageEx = 7,
        Null = -1
    }

    public static class BattleStageTypeExtensions
    {
        public static int AsInt(this BattleStageType stageType)
        {
            return (int)stageType;
        }

        public static BattleStageType FromInt(int stageNumber)
        {
            if (1 <= stageNumber && stageNumber <= 7)
            {
                return (BattleStageType)stageNumber;
            }

            return BattleStageType.Null;
        }

        public static BattleStageType Next(this BattleStageType stageType)
        {
            var currentNumber = stageType.AsInt();
            currentNumber++;
            return FromInt(currentNumber);
        }
    }
}
