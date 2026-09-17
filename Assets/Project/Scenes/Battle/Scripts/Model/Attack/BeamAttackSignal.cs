using System;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    /// <summary>
    /// 起点と終点を指定して1本のビームを撃つシグナル。
    /// 「予告線を出す → 一定時間後に bulletPreset を shotCount 回だけ連射する」までを1エントリで完結させる。
    ///
    /// PresetAttackSignal と同じく AttackTimeline 側で展開されるため、CreateEvent は直接呼ばれない。
    /// 展開時に内側エントリの directionProvider を BeamLineDirectionConfig で上書きするので、
    /// bulletPreset には方向を持たない汎用プリセット(CrossBeamPair 等)をそのまま流用できる。
    /// 逆に、エントリごとに別方向へ撃ち分けるプリセットをここに指定してはいけない(方向が潰れる)。
    /// </summary>
    [Serializable]
    public class BeamAttackSignal : IAttackSignal
    {
        [SerializeField, Tooltip("ビームの起点と終点(ワールド座標)")]
        BeamLine line = new();

        [SerializeField, Tooltip("ビーム1周期分の弾を定義したプリセット")]
        AttackPreset bulletPreset;

        [SerializeField, Min(1), Tooltip("bulletPreset を何回繰り返すか。長いほどビームの持続が伸びる")]
        int shotCount = 36;

        [SerializeField, Min(0.01f), Tooltip("連射の間隔(秒)。短いほど弾が密になりビームらしくなる")]
        float shotInterval = 0.0833f;

        [SerializeField, Tooltip("発射前に予告線を出すか")]
        bool showWarning = true;

        [SerializeField, Min(0f), Tooltip("予告線を出してから発射を始めるまでの秒数。予告線Viewの表示時間にもそのまま使われる")]
        float warningDuration = 1f;

        [SerializeField, Min(0f), Tooltip("予告線の太さ(ワールド単位)。0以下なら予告線Prefab側の設定を使う")]
        float warningWidth;

        [SerializeField, Tooltip("予告線に使う enemySpawnPrefabs のindex")]
        int warningSourceIndex;

        public BeamLine Line => line;
        public AttackPreset BulletPreset => bulletPreset;
        public int ShotCount => shotCount;
        public float ShotInterval => shotInterval;
        public bool ShowWarning => showWarning;
        public float WarningDuration => warningDuration;
        public float WarningWidth => warningWidth;
        public int WarningSourceIndex => warningSourceIndex;

        public AttackEvent CreateEvent(IDirectionProvider directionProvider, IRotationProvider rotationProvider, int sourceIndex = 0, SeType seType = SeType.None)
        {
            // BeamAttackSignalは直接CreateEventされない。AttackTimeline側で展開される。
            Debug.LogWarning("[BeamAttackSignal] CreateEvent should not be called directly. Use AttackTimeline expansion.");
            return default;
        }

        public IAttackSignal Clone() => new BeamAttackSignal
        {
            line = line.Clone(),
            bulletPreset = bulletPreset,
            shotCount = shotCount,
            shotInterval = shotInterval,
            showWarning = showWarning,
            warningDuration = warningDuration,
            warningWidth = warningWidth,
            warningSourceIndex = warningSourceIndex
        };
    }
}
