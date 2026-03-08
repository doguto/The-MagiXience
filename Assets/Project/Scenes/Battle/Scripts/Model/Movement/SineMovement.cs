using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 基準方向に直線移動しながら、垂直方向にサイン波で蛇行する。
    /// </summary>
    public class SineMovement : IMovementStrategy
    {
        readonly Vector3 baseVelocity;
        readonly Vector3 sineAxis;   // 蛇行する軸（baseVelocity の垂直方向）
        readonly float amplitude;
        readonly float frequency;    // Hz

        float elapsed;
        float prevSineValue;

        public bool IsCompleted => false;

        /// <param name="baseVelocity">直線移動の速度ベクトル</param>
        /// <param name="amplitude">蛇行の振幅（ワールド単位）</param>
        /// <param name="frequency">蛇行の周波数（Hz）</param>
        public SineMovement(Vector3 baseVelocity, float amplitude, float frequency)
        {
            this.baseVelocity = baseVelocity;
            this.amplitude = amplitude;
            this.frequency = frequency;

            // 蛇行軸は baseVelocity の2D垂直（Z軸周り90度回転）
            sineAxis = new Vector3(-baseVelocity.y, baseVelocity.x, 0f).normalized;
        }

        public void Initialize()
        {
            elapsed = 0f;
            prevSineValue = 0f;
        }

        public Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime)
        {
            elapsed += deltaTime;
            float sineValue = Mathf.Sin(elapsed * frequency * 2f * Mathf.PI) * amplitude;
            float sineDelta = sineValue - prevSineValue;
            prevSineValue = sineValue;

            return currentPosition + baseVelocity * deltaTime + sineAxis * sineDelta;
        }
    }
}
