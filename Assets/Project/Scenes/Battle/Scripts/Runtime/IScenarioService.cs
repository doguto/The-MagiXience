using Project.Scenes.Scenario.Scripts.Model;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ツリーから会話を流す窓口。会話シーンの読み込みや操作の無効化は実装側 (Presenter) が持つ
    public interface IScenarioService
    {
        // skip なら会話を見せず、コマンドだけを順に積んで即完了にする
        IScenarioHandle Play(ScenarioData data, bool skip, bool restorePlayerControl);
    }
}
