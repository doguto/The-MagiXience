using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    // memo: このenumは外に出して1ファイルにする方がいいのか？
    public enum AttackEventType
    {
        Bullet,
        EnemySpawn,
    }

    /// <summary>SpawnOffsets の解釈方法。</summary>
    public enum AttackSpawnSpace
    {
        /// <summary>発射元(BulletPoolや敵本体)からの相対オフセット</summary>
        Source,

        /// <summary>ワールド座標そのもの。ビームのように発射元と無関係な位置から出す場合に使う</summary>
        World,
    }

    public readonly struct AttackEvent
    {
        public readonly AttackEventType Type;
        public readonly int SourceIndex;
        public readonly SeType SeType;
        public readonly IReadOnlyList<Vector2> Directions;
        public readonly IReadOnlyList<Vector2> SpawnOffsets;
        public readonly IReadOnlyList<Quaternion> Rotations;
        public readonly MovementPreset MovementOverride;
        public readonly AttackSpawnSpace SpawnSpace;

        /// <summary>ビームの射程(ワールド単位)。0で無制限。弾の消滅距離と予告線の長さの両方に使う</summary>
        public readonly float Range;

        /// <summary>ビームの予告時間(秒)。0で未指定。予告線Viewの表示時間として使う</summary>
        public readonly float Duration;

        /// <summary>予告線の太さ(ワールド単位)。0以下で未指定、予告線Prefab側の設定を使う</summary>
        public readonly float Width;

        /// <summary>弾を生成してから実際に移動を開始するまでの待機秒数。0で即座に移動開始(従来動作)。全弾共通の値。</summary>
        public readonly float StartDelay;

        /// <summary>弾ごとに異なる移動開始待機秒数。指定があれば StartDelay より優先される</summary>
        public readonly IReadOnlyList<float> StartDelays;

        /// <summary>弾ごとの生成(出現)自体の遅延秒数。null/未指定で全弾同時に出現(従来動作)</summary>
        public readonly IReadOnlyList<float> SpawnDelays;

        /// <summary>発射元自身(Enemy/Boss本体)に対して、発射(弾の配置)と並行して再生する移動ステップ。nullなら移動なし(従来動作)</summary>
        public readonly IMovementStep MovementStep;

        /// <summary>Bulletタイプの弾自身に適用する移動ステップ(全弾共通)。nullならBullet Prefab側のmovementStepsを使う(従来動作)</summary>
        public readonly IMovementStep BulletMovement;

        public AttackEvent(AttackEventType type, IReadOnlyList<Vector2> directions = null, int sourceIndex = 0, IReadOnlyList<Vector2> spawnOffsets = null, SeType seType = SeType.None, IReadOnlyList<Quaternion> rotations = null, MovementPreset movementOverride = null, AttackSpawnSpace spawnSpace = AttackSpawnSpace.Source, float range = 0f, float duration = 0f, float width = 0f, float startDelay = 0f, IReadOnlyList<float> startDelays = null, IReadOnlyList<float> spawnDelays = null, IMovementStep movementStep = null, IMovementStep bulletMovement = null)
        {
            Type = type;
            SourceIndex = sourceIndex;
            SeType = seType;
            Directions = directions;
            SpawnOffsets = spawnOffsets;
            Rotations = rotations;
            MovementOverride = movementOverride;
            SpawnSpace = spawnSpace;
            Range = range;
            Duration = duration;
            Width = width;
            StartDelay = startDelay;
            StartDelays = startDelays;
            SpawnDelays = spawnDelays;
            MovementStep = movementStep;
            BulletMovement = bulletMovement;
        }

        /// <summary>index番目の弾に適用する移動開始待機秒数を解決する。StartDelays優先、無ければ全弾共通のStartDelay。</summary>
        public float GetStartDelayAt(int index)
        {
            if (StartDelays != null && index < StartDelays.Count) return StartDelays[index];
            return StartDelay;
        }

        /// <summary>index番目の弾の生成自体の遅延秒数を解決する。未指定なら0(即時出現)。</summary>
        public float GetSpawnDelayAt(int index)
        {
            if (SpawnDelays != null && index < SpawnDelays.Count) return SpawnDelays[index];
            return 0f;
        }

        public static AttackEvent Single(Vector2 direction, Quaternion rotation, int sourceIndex = 0, SeType seType = SeType.None) => new(AttackEventType.Bullet, new[] { direction }, sourceIndex, seType: seType, rotations: new[] { Normalize(rotation) });

        public static AttackEvent Spawn(Vector2 direction, Quaternion rotation, int sourceIndex, Vector2 spawnOffset, SeType seType = SeType.None, MovementPreset movementOverride = null, float width = 0f) => new(AttackEventType.EnemySpawn, new[] { direction }, sourceIndex, new[] { spawnOffset }, seType, new[] { Normalize(rotation) }, movementOverride, width: width);

        public static AttackEvent SpawnMulti(IReadOnlyList<Vector2> directions, IReadOnlyList<Quaternion> rotations, int sourceIndex, IReadOnlyList<Vector2> spawnOffsets, SeType seType = SeType.None, MovementPreset movementOverride = null) => new(AttackEventType.EnemySpawn, directions, sourceIndex, spawnOffsets, seType, rotations, movementOverride);

        /// <summary>ワールド座標の一点に生成する。ビームの予告線のように発射元から切り離したい生成物で使う。</summary>
        public static AttackEvent SpawnAtWorld(Vector2 worldPosition, Vector2 direction, Quaternion rotation, int sourceIndex, float range = 0f, float duration = 0f, SeType seType = SeType.None, float width = 0f) =>
            new(AttackEventType.EnemySpawn, new[] { direction }, sourceIndex, new[] { worldPosition }, seType, new[] { Normalize(rotation) }, null, AttackSpawnSpace.World, range, duration, width);

        /// <summary>
        /// 発射位置をワールド座標の origin 基準に差し替えたコピーを返す。
        /// 元の SpawnOffsets は origin からの相対として温存するので、NWayの散らしオフセットも保たれる。
        /// </summary>
        public AttackEvent WithWorldOrigin(Vector2 origin, float range)
        {
            var count = Directions?.Count ?? 1;
            var origins = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var offset = SpawnOffsets != null && i < SpawnOffsets.Count ? SpawnOffsets[i] : Vector2.zero;
                origins[i] = origin + offset;
            }

            return new AttackEvent(Type, Directions, SourceIndex, origins, SeType, Rotations, MovementOverride, AttackSpawnSpace.World, range, Duration, Width, StartDelay, StartDelays, SpawnDelays, MovementStep, BulletMovement);
        }

        // default(Quaternion) は (0,0,0,0) で不正なので identity に補正
        static Quaternion Normalize(Quaternion q) => q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f ? Quaternion.identity : q;
    }
}
