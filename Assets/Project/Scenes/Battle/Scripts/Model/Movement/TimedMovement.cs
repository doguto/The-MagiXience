using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 指定した時間だけ別のMovementStrategyを実行し、時間が経過したら IsCompleted = true になる。
    /// SequentialMovement のステップとして使うことを想定。
    /// </summary>
    public class TimedMovement : IMovementStrategy
    {
        readonly IMovementStrategy inner;
        readonly float duration;
        float elapsed;

        public TimedMovement(IMovementStrategy inner, float duration)
        {
            this.inner = inner;
            this.duration = duration;
        }

        public bool IsCompleted => elapsed >= duration;

        public void Initialize()
        {
            elapsed = 0f;
            inner.Initialize();
        }

        public Vector3 UpdateMovement(Vector3 currentPosition, float deltaTime)
        {
            elapsed += deltaTime;
            return inner.UpdateMovement(currentPosition, deltaTime);
        }
    }
}
