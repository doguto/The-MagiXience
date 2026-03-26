namespace Project.Scripts.Extensions.Message
{
    public enum SceneNavigationState
    {
        InProgress = 0,
        Completed = 1
    }

    public class SceneNavigationMessage
    {
        public SceneNavigationState State { get; }
        public string SceneName { get; }

        public SceneNavigationMessage(SceneNavigationState state, string sceneName)
        {
            State = state;
            SceneName = sceneName;
        }
    }
}
