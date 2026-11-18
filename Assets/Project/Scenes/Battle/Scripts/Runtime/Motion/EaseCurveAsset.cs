using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    [CreateAssetMenu(fileName = "EaseCurve", menuName = "Battle/Ease Curve")]
    public class EaseCurveAsset : ScriptableObject
    {
        [SerializeField] AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public AnimationCurve Curve => curve;
    }
}
