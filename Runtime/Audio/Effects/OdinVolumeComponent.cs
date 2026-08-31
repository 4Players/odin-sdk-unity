using OdinNative.Core;
using System;
using OdinNative.Wrapper.Media;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom volume component for <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>. Recommend the use of AudioSource.volume or AudioMixer/AudioMixerGroup instead
    /// <para>
    /// This class is an effect in the odin audio pipeline to change audio buffers by amplify the volume level.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="PipelineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/></remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinVolumeComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Volume")]
    public class OdinVolumeComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Range(0f, 1.0f)]
        [Tooltip("Set sample volume: direct amplitude scale, or with Ldb the fader position on a dB-linear scale.")]
        public float Volume = 1.0f;

        [Tooltip("Interpret Volume as a dB-linear fader (0..1 maps to -60dB..0dB) instead of a direct amplitude scale.")]
        public bool Ldb = false;

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
            if (Volume == 0f || Volume < float.Epsilon)
            {
                isSilent = true;
                return;
            }

            // dB-linear fader taper: Volume 0..1 maps to -60dB..0dB gain
            float bufferScale = Ldb ? Mathf.Pow(10f, Mathf.Lerp(-60f, 0f, Volume) / 20f) : Volume;

            Span<float> frame = audio.GetBuffer();
            for (int i = 0; i < frame.Length; i++)
                frame[i] *= bufferScale;

            audio.FlushBuffer();
        }
    }
}