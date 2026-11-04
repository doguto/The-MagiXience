using System.Collections.Generic;
using UnityEngine;

namespace Project.Commons.UI.Scripts.View
{
    // 音量(0-100)を四角のマスで表示するゲージ。
    // 1マスが音量10に対応し、端数は該当マスを左上から斜めに部分表示する(例: 96 → 9マス + 6/10マス)。
    public class VolumeGaugeView : MonoBehaviour
    {
        // 左から順に並べたマス。
        [SerializeField] List<DiagonalFillImage> cells;
        [SerializeField] int maxValue = 100;

        public void SetValue(int value)
        {
            if (cells == null || cells.Count == 0) return;

            var valuePerCell = (float)maxValue / cells.Count;
            var clamped = Mathf.Clamp(value, 0, maxValue);
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i] == null) continue;
                cells[i].FillAmount = (clamped - i * valuePerCell) / valuePerCell;
            }
        }
    }
}
