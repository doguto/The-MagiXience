using System;
using DG.Tweening;
using Project.Scenes.Battle.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [Serializable]
    public class ReflectMovementConfig : IMovementStep
    {
        [SerializeField] float speed = 5f;
        [SerializeField] int maxReflections = 1;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            var minX = ScreenBoundsCache.MinX;
            var maxX = ScreenBoundsCache.MaxX;
            var minY = ScreenBoundsCache.MinY;
            var maxY = ScreenBoundsCache.MaxY;

            var vel = direction != Vector2.zero
                ? direction.normalized * speed
                : Vector2.left * speed;
            var reflections = 0;
            var enteredScreen = false;

            return PullMovementHelper.Create(target, 0f, (t, dt) =>
            {
                var pos = (Vector2)t.position + vel * dt;
                var isInScreen = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;

                if (!enteredScreen)
                {
                    enteredScreen = isInScreen;
                }
                else if (reflections < maxReflections)
                {
                    if (pos.x <= minX || pos.x >= maxX)
                    {
                        vel.x = -vel.x;
                        pos.x = Mathf.Clamp(pos.x, minX, maxX);
                        reflections++;
                    }

                    if (pos.y <= minY || pos.y >= maxY)
                    {
                        vel.y = -vel.y;
                        pos.y = Mathf.Clamp(pos.y, minY, maxY);
                        reflections++;
                    }
                }

                t.position = new Vector3(pos.x, pos.y, t.position.z);
            });
        }
    }
}
