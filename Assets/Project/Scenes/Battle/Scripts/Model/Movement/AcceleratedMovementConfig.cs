using System;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class AcceleratedMovementConfig : IMovementStep
    {
        [SerializeField] Vector2 direction = Vector2.left;
        [SerializeField, Tooltip("ONでこの設定の Direction を優先し、攻撃側から渡された発射方向を使用しない。")]
        bool useConfiguredDirection = false;
        [SerializeField, Min(0f)] float initialSpeed = 2f;
        [SerializeField, Tooltip("負の値で減速")] float acceleration = 1f;
        [SerializeField, Min(0f)] float maxSpeed = 10f;
        [SerializeField, Min(0f)] float minSpeed = 0f;
        [SerializeField, Min(0f), Tooltip("継続時間（秒）。0で無限。")] float duration = 0f;
        [SerializeField, Tooltip("固定方向の加速度で放物運動する。ONの間は Acceleration / Max Speed / Min Speed を使用しない。")]
        bool useWorldAcceleration = false;
        [SerializeField, Tooltip("ワールド座標での加速度。Yを負にすると、発射方向に関係なく下に落ちる。")]
        Vector2 worldAcceleration = Vector2.down;

        [Header("Aimed parabola")]
        [SerializeField, Tooltip("発射時の自機を狙い、画面内の頂点を通る初速度と加速度を計算する。")]
        bool aimAtPlayer = false;
        [SerializeField, Tooltip("発射時の上下方向を優先。None・左向きでは上下を自動選択。Alternate VerticalやRight Shot Intervalを設定した場合は、その混成パターンを優先する。")]
        bool useBattleWind = true;
        [SerializeField, Min(0.1f), Tooltip("発射時の自機位置に到達するまでの秒数。")]
        float targetFlightTime = 4f;
        [SerializeField, Min(0.001f), Tooltip("発射位置と自機より風上側に折り返す距離。画面端では自動で縮める。")]
        float vertexDepth = 1.1f;
        [SerializeField, Min(0f)] float vertexScreenMargin = 0.35f;
        [SerializeField, Min(0), Tooltip("0で上下のみ。5なら約5発に1発、右加速度で折り返す放物弾を混ぜる。画面内に頂点を作れない場合は上下弾。")]
        int rightShotInterval;
        [SerializeField, Tooltip("上下の加速度を発射ごとに交互にする。")]
        bool alternateVertical;
        [SerializeField, Range(0f, 0.4f), Tooltip("到達時間の変化率。同時発射した弾の重なりを避ける。")]
        float flightTimeVariation;
        [SerializeField, Range(0f, 0.8f)] float vertexDepthVariation;

        public Tween Play(Transform target, Vector2 overrideDirection, Animator animator)
        {
            if (aimAtPlayer && PlayerPositionReference.Transform != null)
            {
                Vector2 start = target.position;
                Vector2 aim = PlayerPositionReference.Transform.position;
                Vector2 axis = useBattleWind ? BattleWind.ProjectileVector : worldAcceleration.normalized;
                int shot = AimedParabola.NextShotIndex();
                if (alternateVertical) axis = shot % 2 == 0 ? Vector2.up : Vector2.down;
                float flightTime = Mathf.Max(0.1f, targetFlightTime * (1f + flightTimeVariation * ((shot % 11) / 5f - 1f)));
                float depth = Mathf.Max(0.001f, vertexDepth * (1f + vertexDepthVariation * ((shot % 7) / 3f - 1f)));
                var screen = Rect.MinMaxRect(ScreenBoundsCache.MinX, ScreenBoundsCache.MinY,
                    ScreenBoundsCache.MaxX, ScreenBoundsCache.MaxY);
                AimedParabola.SolveMixed(start, aim, axis, screen, flightTime,
                    depth, vertexScreenMargin, rightShotInterval > 0 && shot % rightShotInterval == 0,
                    out var launchVelocity, out var windAcceleration, out _);
#if UNITY_EDITOR
                AimedParabola.RecordLaunch(start, aim, launchVelocity, windAcceleration, flightTime, false, true);
#endif
                float elapsed = 0f;
                float z = target.position.z;
                return PullMovementHelper.Create(target, duration, (t, dt) =>
                {
                    elapsed += dt;
                    Vector2 p = AimedParabola.Position(start, launchVelocity, windAcceleration, elapsed);
                    t.position = new Vector3(p.x, p.y, z);
                }, Ease.Linear);
            }

            Vector3 dir = ((Vector3)(!useConfiguredDirection && overrideDirection != Vector2.zero
                ? overrideDirection : direction)).normalized;
            Vector3 velocity = dir * initialSpeed;

            if (useWorldAcceleration)
            {
                Vector3 gravity = worldAcceleration;
                return PullMovementHelper.Create(target, duration, (t, dt) =>
                {
                    t.position += velocity * dt + 0.5f * gravity * dt * dt;
                    velocity += gravity * dt;
                }, Ease.Linear);
            }

            float accel = acceleration;
            float max = maxSpeed;

            float min = minSpeed;

            return PullMovementHelper.Create(target, duration, (t, dt) =>
            {
                velocity += dir * accel * dt;
                float speed = velocity.magnitude;
                if (max > 0f && speed > max) velocity = velocity.normalized * max;
                if (speed < min) velocity = speed > 0f ? velocity.normalized * min : dir * min;
                t.position += velocity * dt;
            });
        }
    }
}
