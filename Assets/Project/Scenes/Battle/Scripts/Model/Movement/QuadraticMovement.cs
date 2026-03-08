using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 初速度 + 加速度ベクトルで二次関数的な曲線移動をする。
    /// AcceleratedMovement と違い、加速度は一定で maxSpeed による制限がない。
    /// 放物線・弧を描く動きを想定。
    /// </summary>
    public class QuadraticMovement : IMovementStrategy
    {
        readonly Vector3 initialVelocity;
        readonly Vector3 acceleration;
        Vector3 currentVelocity;

        public bool IsCompleted => false;

        /// <param name="initialVelocity">初速度ベクトル</param>
        /// <param name="acceleration">加速度ベクトル（毎秒の速度変化）</param>
        public QuadraticMovement(Vector3 initialVelocity, Vector3 acceleration)
        {
            this.initialVelocity = initialVelocity;
            this.acceleration = acceleration;
        }

        public void Initialize()
        {
            currentVelocity = initialVelocity;
        }

        public Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime)
        {
            currentVelocity += acceleration * deltaTime;
            return currentPosition + currentVelocity * deltaTime;
        }
    }
}
