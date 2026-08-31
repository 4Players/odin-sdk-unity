using OdinNative.Core;
using System;
using OdinNative.Wrapper.Media;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom volume scale component for <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>
    /// <para>
    /// This class is an effect in the odin audio pipeline to scale each sample individually in the buffer.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="PipelineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/> and will multiply each sample by a scale set with a exponent</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinVolumeBoostComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Volume boost")]
    public class OdinVolumeBoostComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Tooltip("Base exponent is 1.0f and will be distorted if the exponent is too high. (Default: 2.0f ^ 1.0f)")]
        public float ExponentOfScale = 1.0f;

        [Tooltip("Base scale is 2.0f and will be distorted if the value is too high. (Default: 2.0f ^ 1.0f)")]
        public float Scale = 2.0f;

        public override void CustomEffectCallback(OdinTArray<float> audio, ref bool isSilent, IntPtr _)
        {
            if (_corrupt || !base.IsEnabled) return;

            OdinLog.Assert(audio != null, $"{nameof(CustomEffectCallback)} audio is null");
            if (audio == null)
            {
                _corrupt = true;
                return;
            }

            if (isSilent) return;

            float bufferScale = Mathf.Pow(Scale, ExponentOfScale);

            Span<float> frame = audio.GetBuffer();
            for (int i = 0; i < frame.Length; i++)
                frame[i] *= bufferScale;

            audio.FlushBuffer();
        }
    }
}