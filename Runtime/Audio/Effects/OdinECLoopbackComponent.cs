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
    /// <remarks>This <see cref="PipelineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/> and requires a <see cref="OdinNative.Unity.Audio.OdinApmComponent"/> in the pipeline.</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinECLoopbackComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/EC Loopback")]
    [RequireComponent(typeof(OdinApmComponent))]
    public class OdinECLoopbackComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        public OdinApmComponent ApmEffect;

        protected override void Start()
        {
            base.Start();

            if (ApmEffect == null)
                ApmEffect = this.gameObject.GetComponent<OdinApmComponent>();

            if (ApmEffect == null && _warn)
            {
                OdinLog.LogInfo($"{gameObject.name} does not have a {nameof(OdinApmComponent)} to call update playback for this {this.GetType()}");
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
                if (audioBuffer.TryCopyTo(loopbackBuffer))
                    UnityQueue.Enqueue(() => ApmEffect.UpdateApmPlayback(loopbackBuffer));
            }
        }
    }
}
