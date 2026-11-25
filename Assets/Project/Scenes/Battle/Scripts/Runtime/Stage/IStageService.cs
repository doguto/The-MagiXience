using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ステージ (道中/ボス戦) 単位で有効になる機能。宣言したステージでだけ生成され、終了時に破棄される
    public interface IStageService
    {
        void Begin(StageContext stage);
        void Tick(float dt);
        void End();
    }
}
