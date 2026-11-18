using System;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 敵・弾が場から消えた理由。消えるときのツリーは、指定した理由で消えたときだけ実行する
    [Flags]
    public enum DespawnReason
    {
        None = 0,
        // 敵: HP が 0 になった / 弾: 何かに当たって消えた
        Defeated = 1 << 0,
        // 寿命・飛距離が尽きた
        Expired = 1 << 1,
        OutOfScreen = 1 << 2,
        // ツリーの Despawn ノード
        Despawned = 1 << 3,
        // 一括消去・リトライ・バトル終了
        Cleared = 1 << 4,
    }
}
