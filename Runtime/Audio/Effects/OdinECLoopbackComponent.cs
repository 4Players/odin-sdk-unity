using System;
using OdinNative.Wrapper.Media;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom echo canceller loopback component for <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>
    /// <para>
    /// This class is an effect in the odin audio pipeline to loopback pushed audio to apm.
    /// </para>
    /// </summary>
    /// <remarks>Attach to a playback/decoder pipeline and assign the APM on the capture/encoder pipeline.
    /// This tap precedes Unity mixing, volume and spatialization; a mixer loopback is preferable when available.
    /// Do not feed several decoder taps independently into one APM; mix their playback reference first.</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinECLoopbackComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/EC Loopback")]
    public class OdinECLoopbackComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        public OdinApmComponent ApmEffect;

        protected override void Start()
        {
            base.Start();

            if (ApmEffect != null)
                ApmEffect.ConfigurePlayback(48000, true);

            if (ApmEffect == null && _warn)
            {
                OdinLog.LogError($"{gameObject.name}: assign the capture pipeline's {nameof(OdinApmComponent)} as the playback reference target.");
                this.enabled = false;
            }
        }

        public override IntPtr GetEffectUserData() => IntPtr.Zero;
        public override void CustomEffectCallback(OdinTArray<float> audio, ref bool isSilent, IntPtr _)
        {
            base.CustomEffectCallback(audio, ref isSilent, _);

            if (!base._corrupt && base.IsEnabled)
            {
                Span<float> audioBuffer = audio.GetBuffer();
                var loopbackBuffer = new float[audioBuffer.Length];
                // Pipeline callbacks always contain 20 ms at 48 kHz stereo, before decoder egress.
                // The target APM belongs to the capture pipeline, not this playback pipeline.
                if (!isSilent) audioBuffer.CopyTo(loopbackBuffer);
                var target = ApmEffect;
                UnityQueue.Enqueue(() =>
                {
                    if (target != null && target.isActiveAndEnabled)
                        target.UpdateApmPlayback(loopbackBuffer, 48000, true);
                });
            }
        }
    }
}
