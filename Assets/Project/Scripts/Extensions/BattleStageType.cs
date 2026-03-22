namespace Project.Scripts.Extensions
{
    public enum BattleStageType
    {
        _1 = 1,
        _2 = 2,
        _3 = 3,
        _4 = 4,
        _5 = 5,
        _6 = 6,
        Ex = 7,
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
    }
}
