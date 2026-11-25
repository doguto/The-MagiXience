using Project.Scripts.Extensions;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    static class TrailSummary
    {
        public static string Of(BtObject<TrailPath> path) =>
            !string.IsNullOrEmpty(path.param) ? "$" + path.param : path.value ? path.value.name : "(経路未設定)";
    }
}
