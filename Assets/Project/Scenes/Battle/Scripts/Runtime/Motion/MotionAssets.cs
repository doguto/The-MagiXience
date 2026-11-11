using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    [CreateAssetMenu(fileName = "Motion", menuName = "Battle/Motion Preset")]
    public class MotionPreset : ScriptableObject
    {
        [SerializeReference, SubclassSelector] List<MotionStep> steps = new();

        public List<MotionStep> Steps => steps;
    }

    [CreateAssetMenu(fileName = "EaseCurve", menuName = "Battle/Ease Curve")]
    public class EaseCurveAsset : ScriptableObject
    {
        [SerializeField] AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public AnimationCurve Curve => curve;
    }

    public static class BattleTargets
    {
        // 自機の Transform。PlayerEntityPresenter が登録する
        public static Transform Player => Model.Movement.PlayerPositionReference.Transform;

        public static bool TryGetPlayerPosition(out Vector3 position)
        {
            var player = Player;
            position = player != null ? player.position : default;
            return player != null;
        }
    }
}
