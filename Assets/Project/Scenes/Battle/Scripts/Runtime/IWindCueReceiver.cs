using Project.Scenes.Battle.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 風の合図の演出を受け持つ敵が実装する (風向きの切り替え自体は BossWindCue ノードが行う)
    public interface IWindCueReceiver
    {
        void PlayCue(BossWindCueSettings cue);
    }
}
