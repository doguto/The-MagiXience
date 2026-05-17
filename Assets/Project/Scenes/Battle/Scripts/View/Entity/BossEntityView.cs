using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Battle.Scripts.View.Entity
{
    public class BossEntityView : EnemyEntityView
    {
        [Header("Hp Bar")]
        [SerializeField] Image normalHpBarFillImage;
        [SerializeField] Image strongHpBarFillImage;

        public void SetNormalHpRatio(float ratio)
        {
            if (normalHpBarFillImage == null) return;
            normalHpBarFillImage.fillAmount = Mathf.Clamp01(ratio);
        }

        public void SetStrongHpRatio(float ratio)
        {
            if (strongHpBarFillImage == null) return;
            strongHpBarFillImage.fillAmount = Mathf.Clamp01(ratio);
        }
    }
}
