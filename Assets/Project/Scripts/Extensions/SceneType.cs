namespace Project.Scripts.Extensions
{
    public enum SceneType
    {
        Global,
        Title,
        StageList,
        Stage1,
        Stage2,
        Stage3,
        Stage4,
        Stage5,
        Stage6,
        StageEx,
        Scenario,
        Entry,
    }

    public static class SceneTypeExtension
    {
        public static string ToSceneName(this SceneType sceneType)
        {
            // StageN系はSceneの名前的には全てBattle
            return sceneType switch
            {
                SceneType.Global => "Global",
                SceneType.Title => "Title",
                SceneType.StageList => "StageList",
                SceneType.Scenario => "Scenario",
                SceneType.Entry => "Entry",
                _ => "Battle"
            };
        }
    }
}
