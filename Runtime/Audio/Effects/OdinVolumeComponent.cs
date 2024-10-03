using OdinNative.Core;
using System;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom volume component for <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/>. Recommend the use of AudioSource.volume or AudioMixer/AudioMixerGroup instead
    /// <para>
    /// This class is a effect in the odin audio pipline to change audio buffers by amplify the volume level.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/></remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinvolumecomponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Volume")]
    public class OdinVolumeComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Range(0f, 1.0f)]
        [Tooltip("Set sample volume by amplitude.")]
        public float Volume = 1.0f;

        [Tooltip("Toggle between db calculation and direct")]
        public bool Ldb = false;

        public override void CustomEffectCallback(OdinArrayf audio, ref bool isSilent, IntPtr _)
        {
            if (_corrupt || !base.IsEnabled) return;

            Utility.Assert(audio != null, $"{nameof(CustomEffectCallback)} audio is null");
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

            float bufferScale = Ldb ? Mathf.Log10(Volume) * 20f : Volume;

            Span<float> frame = audio.GetBuffer();
            for (int i = 0; i < frame.Length; i++)
                frame[i] *= bufferScale;

            audio.FlushBuffer();
        }
    }
}