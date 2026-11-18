using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Entity
{
    /// <summary>
    /// BGMの再生位置に対応するスペクトラムバーの高さを保持する。
    /// 音量に左右されないよう、事前に記録した値を再生位置から引く。
    /// </summary>
    public class SpectrumBarHeightData : ScriptableObject
    {
        [SerializeField] int barCount;
        [SerializeField] int startSample;
        [SerializeField] int hopSamples;
        [SerializeField] byte[] heightRates;

        public int BarCount => barCount;

        /// <summary>
        /// 再生位置に対応するフレームの先頭位置を返す。記録範囲外ならfalseを返す。
        /// </summary>
        public bool TryGetFrameOffset(int timeSample, out int frameOffset)
        {
            frameOffset = 0;
            if (timeSample < startSample) return false;

            var offset = (timeSample - startSample) / hopSamples * barCount;
            if (offset + barCount > heightRates.Length) return false;

            frameOffset = offset;
            return true;
        }

        /// <summary>
        /// バーの高さを最大高さに対する割合(0-1)で返す。
        /// </summary>
        public float GetHeightRate(int frameOffset, int barIndex)
        {
            return heightRates[frameOffset + barIndex] / (float)byte.MaxValue;
        }

#if UNITY_EDITOR
        public void SetRecordedData(int newBarCount, int newStartSample, int newHopSamples, byte[] newHeightRates)
        {
            barCount = newBarCount;
            startSample = newStartSample;
            hopSamples = newHopSamples;
            heightRates = newHeightRates;
        }
#endif
    }
}
