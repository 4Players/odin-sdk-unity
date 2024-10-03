using OdinNative.Core;
using System;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom volume scale component for <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/>
    /// <para>
    /// This class is a effect in the odin audio pipline to scale each sample individually in the buffer.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/> and will multiply each sample by a scale set with a exponent</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinvolumeboostcomponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Volume boost")]
    public class OdinVolumeBoostComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Tooltip("Base exponent is 1.0f and will be distorted if the exponent is to high. (Default: 2.0f ^ 1.0f)")]
        public float ExponentOfScale = 1.0f;

        [Tooltip("Base scale is 2.0f and will be distorted if the value is to high. (Default: 2.0f ^ 1.0f)")]
        public float Scale = 2.0f;

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

            float bufferScale = Mathf.Pow(Scale, ExponentOfScale);

            Span<float> frame = audio.GetBuffer();
            for (int i = 0; i < frame.Length; i++)
                frame[i] *= bufferScale;

            audio.FlushBuffer();
        }
    }
}