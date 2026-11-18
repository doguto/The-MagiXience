using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// 起点と終点を指定して1本のビームを撃つシグナル(一枚絵式)。
    /// 「予告線を出す → 一定時間後にビーム本体を1体だけ生成する」までを1エントリで完結させる。
    ///
    /// ビーム本体は bodySourceIndex の enemySpawnPrefabs を線分の起点に生成し、
    /// その本体(SpriteBeamView/Presenter)が1枚のスプライトの表示/非表示でビームを表現する。
    /// 弾を連射して線を描く連射式は BeamAttackSignal を使うこと。両者はロジックが別なので分けている。
    ///
    /// PresetAttackSignal と同じく AttackTimeline 側で展開されるため、CreateEvent は直接呼ばれない。
    /// </summary>
    [Serializable]
    public class SpriteBeamAttackSignal : IAttackSignal
    {
        [SerializeField, Tooltip("ビームの起点と終点。isRelativeがtrueなら敵位置からの相対オフセット、falseならワールド座標")]
        BeamLine line = new();

        [SerializeField, Tooltip("trueなら line を敵位置からの相対オフセットとして扱う(展開時の敵位置基準・ワールド軸平行移動)")]
        bool isRelative;

        [SerializeField, Tooltip("ビーム本体に使う enemySpawnPrefabs のindex")]
        int bodySourceIndex;

        [SerializeField, Min(0f), Tooltip("ビーム本体の表示時間(秒)")]
        float bodyDuration = 1f;

        [SerializeField, Tooltip("発射前に予告線を出すか")]
        bool showWarning = true;

        [SerializeField, Min(0f), Tooltip("予告線を出してから本体を出すまでの秒数。予告線Viewの表示時間にもそのまま使われる")]
        float warningDuration = 1f;

        [SerializeField, Tooltip("予告線に使う enemySpawnPrefabs のindex")]
        int warningSourceIndex;

        public BeamLine Line => line;
        public bool IsRelative => isRelative;
        public int BodySourceIndex => bodySourceIndex;
        public float BodyDuration => bodyDuration;
        public bool ShowWarning => showWarning;
        public float WarningDuration => warningDuration;
        public int WarningSourceIndex => warningSourceIndex;

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            // SpriteBeamAttackSignalは直接CreateEventされない。AttackTimeline側で展開される。
            Debug.LogWarning("[SpriteBeamAttackSignal] CreateEvent should not be called directly. Use AttackTimeline expansion.");
            return default;
        }

        public IAttackSignal Clone() => new SpriteBeamAttackSignal
        {
            line = line.Clone(),
            isRelative = isRelative,
            bodySourceIndex = bodySourceIndex,
            bodyDuration = bodyDuration,
            showWarning = showWarning,
            warningDuration = warningDuration,
            warningSourceIndex = warningSourceIndex
        };
    }
}
