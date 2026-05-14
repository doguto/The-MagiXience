using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    /// <summary>
    /// シーン上の全Bulletをプールに返却するためのカスタムSignalAsset
    /// </summary>
    [CreateAssetMenu(fileName = "BulletClearSignal", menuName = "Battle/Signals/Bullet Clear Signal")]
    public class BulletClearSignal : SignalAsset
    {
    }
}
