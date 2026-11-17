using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // BattleStageAsset に並べるサービスの定義。実行時の状態は Create で作る別インスタンスに持たせる
    [Serializable]
    public abstract class StageServiceDefinition
    {
        public abstract IStageService Create();
    }
}
