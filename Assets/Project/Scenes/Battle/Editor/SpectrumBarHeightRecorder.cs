using Project.Scenes.Battle.Scripts.Model.Entity;
using UnityEditor;
using UnityEngine;

namespace Project.Scenes.Battle.Editor
{
    /// <summary>
    /// 1面ボスの音量バー区間を再生し、バーの高さをSpectrumBarHeightDataへ記録する。
    /// Play中に実行する。曲を差し替えたときは記録し直す。
    /// </summary>
    public static class SpectrumBarHeightRecorder
    {
        const string ClipPath = "Assets/Project/Textures/Sounds/Bgm/Music Terminology.wav";
        const string DataPath = "Assets/Project/DataStore/Stage1/Boss/MusicTerminologySpectrum.asset";

        const int StartSample = 2976121;
        const int EndSample = 3897391;
        const int HopSamples = 735; // 44100Hzで60fps相当
        const int LeadSamples = 44100; // FFTのバッファが埋まるまでの助走

        const int BarCount = 32;
        const int FftSize = 1024;
        const float MaxBarHeight = 2f;
        const float Sensitivity = 25f;
        const int SpectrumRangeStart = 8;
        const int SpectrumRangeEnd = 21;
        const float RecordingVolume = 0.5f;

        static AudioSource source;
        static float[] spectrumData;
        static byte[] heightRates;
        static int lastFrame;
        static int capturedFrames;
        static bool previousRunInBackground;

        [MenuItem("Tools/Record Spectrum Bar Heights")]
        static void StartRecording()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[SpectrumBarHeightRecorder] Play中に実行してください。");
                return;
            }

            if (source != null) return;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            if (clip == null)
            {
                Debug.LogError($"[SpectrumBarHeightRecorder] 曲が見つかりません: {ClipPath}");
                return;
            }

            var frameCount = (EndSample - StartSample) / HopSamples;
            spectrumData = new float[FftSize];
            heightRates = new byte[frameCount * BarCount];
            lastFrame = -1;
            capturedFrames = 0;

            // フォーカスが外れると音声が止まり、古いスペクトラムを読み続けてしまう
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;

            var sourceObject = new GameObject("SpectrumBarHeightRecorder");
            source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = RecordingVolume;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.Play();
            source.timeSamples = StartSample - LeadSamples;

            EditorApplication.update += Tick;
            Debug.Log("[SpectrumBarHeightRecorder] 記録を開始しました。");
        }

        static void Tick()
        {
            if (!Application.isPlaying || source == null || !source.isPlaying)
            {
                Debug.LogError("[SpectrumBarHeightRecorder] 記録が中断されました。データは保存していません。");
                Cleanup();
                return;
            }

            var timeSample = source.timeSamples;
            if (timeSample < StartSample) return;

            var frameCount = heightRates.Length / BarCount;
            var frame = (timeSample - StartSample) / HopSamples;
            if (frame > lastFrame)
            {
                source.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
                var lastWritableFrame = Mathf.Min(frame, frameCount - 1);

                // 音声バッファ単位で再生位置が飛ぶため、飛ばしたフレームは同じ値で埋める
                for (var f = lastFrame + 1; f <= lastWritableFrame; f++)
                {
                    WriteFrame(f);
                }

                capturedFrames++;
                lastFrame = lastWritableFrame;
            }

            if (timeSample >= EndSample || lastFrame >= frameCount - 1)
            {
                Save(frameCount);
                Cleanup();
            }
        }

        static void WriteFrame(int frame)
        {
            // 元の32分割対数マッピングからSpectrumRangeStart~SpectrumRangeEndの帯域を切り出す
            var rangeBinStart = LogarithmicBinIndex(SpectrumRangeStart);
            var rangeBinEnd = LogarithmicBinIndex(SpectrumRangeEnd + 1);
            var rangeBinCount = rangeBinEnd - rangeBinStart;

            for (var i = 0; i < BarCount; i++)
            {
                // 切り出した帯域を均等に割り振る（反転: 左が高周波、右が低周波）
                var reversedIndex = BarCount - 1 - i;
                var binStart = rangeBinStart + rangeBinCount * reversedIndex / BarCount;
                var binEnd = rangeBinStart + rangeBinCount * (reversedIndex + 1) / BarCount;
                if (binEnd <= binStart) binEnd = binStart + 1;

                var sum = 0f;
                for (var b = binStart; b < binEnd && b < FftSize; b++)
                {
                    sum += spectrumData[b];
                }

                var normalizedAverage = sum / (binEnd - binStart) / RecordingVolume;
                var heightRate = Mathf.Clamp01(normalizedAverage * Sensitivity / MaxBarHeight);
                heightRates[frame * BarCount + i] = (byte)Mathf.RoundToInt(heightRate * byte.MaxValue);
            }
        }

        static int LogarithmicBinIndex(int barIndex)
        {
            // 対数スケールで0〜FftSize/2をBarCount分割にマッピング
            const int halfFft = FftSize / 2;
            var logValue = Mathf.Log10(halfFft) * barIndex / BarCount;
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Pow(10f, logValue)), 0, halfFft - 1);
        }

        static void Save(int frameCount)
        {
            // 既存アセットを更新し、prefabからの参照(GUID)を維持する
            var data = AssetDatabase.LoadAssetAtPath<SpectrumBarHeightData>(DataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SpectrumBarHeightData>();
                AssetDatabase.CreateAsset(data, DataPath);
            }

            data.SetRecordedData(BarCount, StartSample, HopSamples, heightRates);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SpectrumBarHeightRecorder] 保存しました: {DataPath} (全{frameCount}フレーム, 実測{capturedFrames}回)");
        }

        static void Cleanup()
        {
            EditorApplication.update -= Tick;
            Application.runInBackground = previousRunInBackground;

            if (source != null) Object.Destroy(source.gameObject);
            source = null;
        }
    }
}
