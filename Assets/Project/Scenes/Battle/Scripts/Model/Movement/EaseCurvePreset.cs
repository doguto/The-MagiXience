using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    [CreateAssetMenu(
        fileName = "NewEaseCurvePreset",
        menuName = "Battle/Ease Curve Preset")]
    public class EaseCurvePreset : ScriptableObject
    {
        [SerializeField] AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public AnimationCurve Curve => curve;
    }
}
