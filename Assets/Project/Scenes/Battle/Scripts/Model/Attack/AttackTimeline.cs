using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger;
using Project.Scripts.Extensions;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AttackTimeline : IAttackStrategy
    {
        [SerializeField] List<AttackPhase> phases = new() { new AttackPhase() };

        Subject<AttackEvent> onAttackTiming;
        CompositeDisposable disposables;
        CompositeDisposable phaseDisposables;
        Func<Vector3> getPlayerPosition;
        Func<Vector3> getEnemyPosition;
        Func<Quaternion> getEnemyRotation;
        IReadOnlyReactiveProperty<int> currentHp;
        int maxHp;

        public IObservable<AttackEvent> OnAttackTiming => onAttackTiming;
        public bool IsCompleted { get; private set; }

        public void InitializeProviders(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation, IReadOnlyReactiveProperty<int> currentHp = null, int maxHp = 0)
        {
            this.getPlayerPosition = getPlayerPosition;
            this.getEnemyPosition = getEnemyPosition;
            this.getEnemyRotation = getEnemyRotation;
            this.currentHp = currentHp;
            this.maxHp = maxHp;

            foreach (var phase in phases)
            {
                foreach (var entry in phase.entries)
                {
                    InitializeEntryProviders(entry);
                }
            }
        }

        void InitializeEntryProviders(AttackTimelineEntry entry)
        {
            entry.directionProvider?.Initialize(getPlayerPosition, getEnemyPosition, getEnemyRotation);
            entry.rotationProvider.Initialize(getPlayerPosition, getEnemyPosition, getEnemyRotation);
        }

        public void Initialize()
        {
            onAttackTiming = new Subject<AttackEvent>();
            disposables = new CompositeDisposable();
            IsCompleted = false;

            if (phases.Count == 0) return;

            StartPhase(0);
        }

        // Phaseを配列の先頭から順番に進める。nextPhaseTriggerがSubscribe直後に条件を満たしていれば
        // (例: 開始済みの敵にいきなり低いHP閾値のPresetを差し替えた場合)そのまま連鎖して先のPhaseまで進む。
        void StartPhase(int index)
        {
            var phase = phases[index];

            if (phaseDisposables != null)
            {
                disposables.Remove(phaseDisposables);
                phaseDisposables.Dispose();
            }
            phaseDisposables = new CompositeDisposable();
            disposables.Add(phaseDisposables);

            bool isLastPhase = index >= phases.Count - 1;
            ScheduleEntries(phase, isLastPhase);

            if (!isLastPhase && phase.nextPhaseTrigger != null)
            {
                var context = new AttackPhaseTriggerContext(currentHp, maxHp);
                phase.nextPhaseTrigger.CreateTrigger(context)
                    .Subscribe(_ => StartPhase(index + 1))
                    .AddTo(phaseDisposables);
            }
        }

        void ScheduleEntries(AttackPhase phase, bool isLastPhase)
        {
            if (phase.entries.Count == 0) return;

            if (phase.loop)
            {
                int totalCycles = Mathf.FloorToInt((phase.loopEnd - phase.loopStart) / phase.cycleDuration);

                for (int cycle = 0; cycle <= totalCycles; cycle++)
                {
                    foreach (var entry in phase.entries)
                    {
                        float fireTime = phase.loopStart + cycle * phase.cycleDuration + entry.time;
                        if (fireTime > phase.loopEnd) continue;

                        ScheduleEntry(entry, fireTime);
                    }
                }

                if (isLastPhase)
                {
                    Observable.Timer(TimeSpan.FromSeconds(phase.loopEnd))
                        .Subscribe(_ => IsCompleted = true)
                        .AddTo(phaseDisposables);
                }
            }
            else
            {
                float maxTime = 0f;
                foreach (var entry in phase.entries)
                {
                    ScheduleEntry(entry, entry.time);
                    if (entry.time > maxTime) maxTime = entry.time;
                }

                if (isLastPhase)
                {
                    Observable.Timer(TimeSpan.FromSeconds(maxTime))
                        .Subscribe(_ => IsCompleted = true)
                        .AddTo(phaseDisposables);
                }
            }
        }

        const int MaxPresetDepth = 8;

        void ScheduleEntry(AttackTimelineEntry entry, float fireTime, int depth = 0)
        {
            if (entry.signal is PresetAttackSignal presetSignal)
            {
                if (presetSignal.Preset == null) return;
                if (depth >= MaxPresetDepth)
                {
                    Debug.LogError("[AttackTimeline] Preset nesting depth limit reached. Circular reference?");
                    return;
                }
                ExpandPreset(presetSignal, entry.seType, fireTime, depth + 1);
                return;
            }

            if (entry.signal is BeamAttackSignal beamSignal)
            {
                if (depth >= MaxPresetDepth)
                {
                    Debug.LogError("[AttackTimeline] Preset nesting depth limit reached. Circular reference?");
                    return;
                }
                ExpandBeam(beamSignal, entry.seType, fireTime, depth + 1);
                return;
            }

            if (entry.signal is SpriteBeamAttackSignal spriteBeamSignal)
            {
                ExpandSpriteBeam(spriteBeamSignal, fireTime);
                return;
            }

            Observable.Timer(TimeSpan.FromSeconds(fireTime))
                .Subscribe(_ =>
                {
                    if (entry.signal != null)
                    {
                        var sourceIndex = entry.sourceIndexProvider?.Get() ?? 0;
                        var attackEvent = entry.signal.CreateEvent(entry.directionProvider, entry.rotationProvider, sourceIndex, entry.seType);
                        onAttackTiming.OnNext(ApplyBeamOrigin(attackEvent, entry.directionProvider));
                    }
                })
                .AddTo(phaseDisposables);
        }

        /// <summary>
        /// directionProvider がビーム線を持っている弾イベントは、発射口ではなく線分の起点から出し、
        /// 終点で消えるよう射程を持たせる。シグナル側にビームを意識させないための後処理。
        /// </summary>
        static AttackEvent ApplyBeamOrigin(AttackEvent attackEvent, IDirectionProvider directionProvider)
        {
            if (attackEvent.Type != AttackEventType.Bullet) return attackEvent;
            if (directionProvider is not IBeamLineProvider beamProvider) return attackEvent;

            var line = beamProvider.Line;
            return attackEvent.WithWorldOrigin(line.Start, line.Length);
        }

        /// <summary>
        /// ビーム1本を「予告線 → 連射」に展開する。
        /// 起点・終点は展開時に確定して全弾で共有するため、予告線と弾の軌道がズレることはない。
        /// </summary>
        void ExpandBeam(BeamAttackSignal signal, SeType parentSeType, float baseTime, int depth)
        {
            var line = signal.Line;

            if (signal.ShowWarning)
            {
                Observable.Timer(TimeSpan.FromSeconds(baseTime))
                    .Subscribe(_ => onAttackTiming.OnNext(AttackEvent.SpawnAtWorld(
                        line.Start, line.Direction, line.Rotation, signal.WarningSourceIndex, line.Length, signal.WarningDuration)))
                    .AddTo(phaseDisposables);
            }

            if (signal.BulletPreset == null) return;

            var timeline = signal.BulletPreset.CreateTimeline();
            if (timeline == null) return;

            // ネストしたプリセットと同じく、先頭Phaseのみを1周期分として展開する
            var entries = timeline.phases.Count > 0 ? timeline.phases[0].entries : null;
            if (entries == null || entries.Count == 0) return;

            foreach (var inner in entries)
            {
                // 起点・射程はここで刺した directionProvider 経由で ApplyBeamOrigin に伝わる
                inner.directionProvider = new BeamLineDirectionConfig(line.Clone());
                InitializeEntryProviders(inner);

                if (inner.seType == SeType.None && parentSeType != SeType.None)
                {
                    inner.seType = parentSeType;
                }
            }

            var fireStartTime = baseTime + signal.WarningDuration;
            for (var shot = 0; shot < signal.ShotCount; shot++)
            {
                foreach (var inner in entries)
                {
                    ScheduleEntry(inner, fireStartTime + shot * signal.ShotInterval + inner.time, depth);
                }
            }
        }

        /// <summary>
        /// ビーム1本を「予告線 → 本体を1体生成」に展開する(一枚絵式)。
        /// 予告線と同じ SpawnAtWorld 経路で本体を線分の起点に生成し、range=線分長 / duration=表示時間 を本体へ渡す。
        /// 本体側(SpriteBeamView/Presenter)が1枚のスプライトの表示/非表示でビームを表現する。
        /// </summary>
        void ExpandSpriteBeam(SpriteBeamAttackSignal signal, float baseTime)
        {
            // 予告線を出す瞬間(baseTime到達時)に line を確定する。
            // isRelative の敵位置基準もこのタイミングで取るので、敵が画面外から入場してくる場合でも
            // 「予告が始まる時点の敵位置」からビームが出る。確定した line を本体にも渡して起点を一致させる。
            if (signal.ShowWarning)
            {
                Observable.Timer(TimeSpan.FromSeconds(baseTime))
                    .Subscribe(_ =>
                    {
                        var line = ResolveBeamLine(signal);
                        onAttackTiming.OnNext(AttackEvent.SpawnAtWorld(
                            line.Start, line.Direction, line.Rotation, signal.WarningSourceIndex, line.Length, signal.WarningDuration));
                        ScheduleSpriteBeamBody(signal, line, signal.WarningDuration);
                    })
                    .AddTo(phaseDisposables);
                return;
            }

            // 予告なしの場合は baseTime 到達時に line を確定して即本体を出す。
            Observable.Timer(TimeSpan.FromSeconds(baseTime))
                .Subscribe(_ => ScheduleSpriteBeamBody(signal, ResolveBeamLine(signal), 0f))
                .AddTo(phaseDisposables);
        }

        /// <summary>予告開始時に確定した line を使い、delay 秒後にビーム本体を1体生成する。</summary>
        void ScheduleSpriteBeamBody(SpriteBeamAttackSignal signal, BeamLine line, float delay)
        {
            Observable.Timer(TimeSpan.FromSeconds(delay))
                .Subscribe(_ => onAttackTiming.OnNext(AttackEvent.SpawnAtWorld(
                    line.Start, line.Direction, line.Rotation, signal.BodySourceIndex, line.Length, signal.BodyDuration)))
                .AddTo(phaseDisposables);
        }

        /// <summary>
        /// signal の line を絶対ワールド座標の BeamLine に解決する。
        /// isRelative なら呼び出し時点の敵位置を基準に、Start/End をワールド軸のオフセットとして加算する。
        /// </summary>
        BeamLine ResolveBeamLine(SpriteBeamAttackSignal signal)
        {
            var line = signal.Line;
            if (!signal.IsRelative) return line;

            var origin = (Vector2)(getEnemyPosition?.Invoke() ?? Vector3.zero);
            return new BeamLine(line.Start + origin, line.End + origin);
        }

        void ExpandPreset(PresetAttackSignal signal, SeType parentSeType, float baseTime, int depth)
        {
            var timeline = signal.Preset.CreateTimeline();
            if (timeline == null) return;

            // ネストしたプリセットは常に先頭Phase(通常状態)のみを展開する。
            // プリセット部品自体にHP閾値のフェーズ切替を持たせる使い方は想定していない。
            var entries = timeline.phases.Count > 0 ? timeline.phases[0].entries : null;
            if (entries == null || entries.Count == 0) return;

            foreach (var inner in entries)
            {
                InitializeEntryProviders(inner);
                // 内側がNoneなら外側のseTypeを引き継ぐ
                if (inner.seType == SeType.None && parentSeType != SeType.None)
                {
                    inner.seType = parentSeType;
                }
            }

            var totalCycles = signal.Loop && signal.LoopCount > 0 ? signal.LoopCount : 1;
            for (var cycle = 0; cycle < totalCycles; cycle++)
            {
                foreach (var inner in entries)
                {
                    var innerTime = baseTime + cycle * signal.CycleDuration + inner.time;
                    ScheduleEntry(inner, innerTime, depth);
                }
            }
        }

        public AttackTimeline DeepCopy()
        {
            var copy = new AttackTimeline
            {
                phases = new List<AttackPhase>(phases.Count)
            };
            foreach (var phase in phases)
            {
                copy.phases.Add(phase.DeepCopy());
            }
            return copy;
        }

        public void Update(float deltaTime) { }

        public void Dispose()
        {
            phaseDisposables?.Dispose();
            disposables?.Dispose();
            onAttackTiming?.Dispose();
        }
    }
}
