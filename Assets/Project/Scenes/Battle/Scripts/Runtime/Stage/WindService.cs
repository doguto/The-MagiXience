using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 風の予定表 (向き × 秒数を順に繰り返す) を進め、開始時に自機・敵弾・矢印表示へ自分を登録する。
    // Enabled が OFF でも予定表は進み、自機狙い放物線の曲げる向きなどには使われる
    public sealed class WindService : IStageService, IMoveModifier, IBulletForce
    {
        readonly BattleWindSettings initial;
        readonly WindArrowsPresenter arrowsPrefab;
        BattleWindSettings settings;
        int index;
        float remaining;
        StageContext stage;
        WindArrowsPresenter arrows;

        public WindService(BattleWindSettings initial, WindArrowsPresenter arrowsPrefab = null)
        {
            this.initial = initial;
            this.arrowsPrefab = arrowsPrefab;
        }

        bool HasSchedule => settings != null && settings.intervals != null && settings.intervals.Count > 0;
        public BattleWindSettings Current => settings;
        public bool Enabled => HasSchedule && settings.enabled;
        public WindDirection ProjectileDirection => HasSchedule ? settings.intervals[index].direction : WindDirection.None;
        public Vector2 ProjectileVector => ToVector(ProjectileDirection);
        public WindDirection Direction => Enabled ? ProjectileDirection : WindDirection.None;
        public Vector2 Vector => ToVector(Direction);
        public float PlayerSpeedRatio => Enabled ? Mathf.Clamp01(settings.playerSpeedRatio) : 0f;
        public float BulletAcceleration => Enabled ? Mathf.Max(0f, settings.bulletAcceleration) : 0f;
        public float BulletDrag => Enabled ? Mathf.Max(0f, settings.bulletDrag) : 0f;

        public void Begin(StageContext context)
        {
            stage = context;
            var services = context.World.Services;
            services.Player?.AddMoveModifier(this);
            services.Bullets.AddForce(this);
            if (arrowsPrefab != null && services.Background != null)
            {
                arrows = UnityEngine.Object.Instantiate(arrowsPrefab, services.Background.transform, false);
                arrows.Bind(this, services.Background);
            }
            Apply(initial);
        }

        public void End()
        {
            if (stage != null)
            {
                var services = stage.World.Services;
                if (services.Player) services.Player.RemoveMoveModifier(this);
                services.Bullets.RemoveForce(this);
            }
            if (arrows) UnityEngine.Object.Destroy(arrows.gameObject);
            arrows = null;
            settings = null;
            stage = null;
        }

        // 予定表を差し替えて先頭から始める。無風から吹き始めたら効果音を鳴らす
        public void Apply(BattleWindSettings value)
        {
            var before = Direction;
            settings = value;
            index = 0;
            remaining = HasSchedule ? Mathf.Max(0.1f, settings.intervals[0].seconds) : 0f;
            PlayStartSeIfStarted(before);
        }

        public void Tick(float dt)
        {
            if (!HasSchedule || dt <= 0f) return;
            var before = Direction;
            remaining -= dt;
            while (remaining <= 0f)
            {
                index = (index + 1) % settings.intervals.Count;
                remaining += Mathf.Max(0.1f, settings.intervals[index].seconds);
            }
            PlayStartSeIfStarted(before);
        }

        Vector2 IMoveModifier.Modify(Vector2 velocity, Vector2 input, float speed) => velocity + Vector * (PlayerSpeedRatio * speed);

        bool IBulletForce.Active => Enabled;
        Vector2 IBulletForce.LaunchAxis => ProjectileVector;

        // 本来の軌道に対する風のずれを積分する。抵抗があるため流される速さは 加速度 / 抵抗 で頭打ちになる
        public void Integrate(Vector2 authoredVelocity, float dt, ref Vector2 velocityCorrection, ref Vector2 positionCorrection)
        {
            if (dt <= 0f) return;
            if (Direction == WindDirection.None)
            {
                velocityCorrection = Vector2.zero;
                return;
            }

            var acceleration = Vector * BulletAcceleration;
            var drag = BulletDrag;
            if (drag < 0.00001f)
            {
                positionCorrection += velocityCorrection * dt + acceleration * (0.5f * dt * dt);
                velocityCorrection += acceleration * dt;
                return;
            }

            var terminal = acceleration / drag - authoredVelocity;
            var decay = Mathf.Exp(-drag * dt);
            positionCorrection += terminal * dt + (velocityCorrection - terminal) * ((1f - decay) / drag);
            velocityCorrection = terminal + (velocityCorrection - terminal) * decay;
        }

        void PlayStartSeIfStarted(WindDirection before)
        {
            if (before != WindDirection.None || Direction == WindDirection.None) return;
            if (settings.startSe != SeType.None) stage?.World.Services.PlaySe?.Invoke(settings.startSe);
        }

        public static Vector2 ToVector(WindDirection direction) => direction switch
        {
            WindDirection.Up => Vector2.up,
            WindDirection.Down => Vector2.down,
            WindDirection.Left => Vector2.left,
            WindDirection.Right => Vector2.right,
            _ => Vector2.zero,
        };
    }
}
