using OdinNative.Wrapper.Media;
using System;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom RMS dBFS component for <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>
    /// <para>
    /// This class is an effect in the odin audio pipeline to calculate Root Mean Square (RMS) averaging with the decibels relative to full scale (dBFS).
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="PipelineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/></remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinRmsComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/RMS dBFS")]
    public class OdinRmsComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Tooltip("Root Mean Square (RMS) averaging with the decibels relative to full scale (dBFS)")]
        public float RmsDbfs = 0f;

        public override IntPtr GetEffectUserData() => IntPtr.Zero;
        public override void CustomEffectCallback(OdinTArray<float> audio, ref bool isSilent, IntPtr _)
        {
            base.CustomEffectCallback(audio, ref isSilent, _);

            if (_corrupt || !base.IsEnabled) return;

            Span<float> frame = audio.GetBuffer();
            int numSamples = frame.Length;
            if (numSamples == 0) return;

            //20.0 * (f32::sqrt(squared_sum / Block::LENGTH as f32)).max(f32::MIN_POSITIVE).log10();
            float squaredSum = 0f;
            for (int i = 0; i < numSamples; i++)
                squaredSum += frame[i] * frame[i];

            float rms = Mathf.Sqrt(squaredSum / numSamples);

            RmsDbfs = rms > float.Epsilon
                ? 20f * Mathf.Log10(rms)
                : -144f; // floored to avoid >0 at silence
        }
    }
}