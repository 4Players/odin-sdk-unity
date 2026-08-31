using System;
using OdinNative.Wrapper.Media;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom mute audio component for <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>
    /// <para>
    /// This class is an effect in the odin audio pipeline to mute based on the <see cref="SilenceToggle"/> flag.
    /// The intention is to provide a convenient way with Unity Editor UI of Marshal a 1-byte signed integer bool for a specific audio packet in the current pipeline.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="PipelineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/> and will toggle the silence flag of the effect callback (see <see cref="OdinNative.Core.Imports.NativeLibraryMethods.OdinCustomEffectCallbackDelegate"/>)</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinMuteAudioComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Mute Audio")]
    public class OdinMuteAudioComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Tooltip("Set \"is_silent\" flag to mute on native level. On false nothing will be set and the component does a simple passthrough.")]
        public bool SilenceToggle = false;

        public override IntPtr GetEffectUserData() => IntPtr.Zero;
        public override void CustomEffectCallback(OdinTArray<float> audio, ref bool isSilent, IntPtr _)
        {
            base.CustomEffectCallback(audio, ref isSilent, _);

            // Silence only on active valid toggle or use original native value to not override other flags on silence like VAD
            if (!base._corrupt && base.IsEnabled && SilenceToggle)
            {
                isSilent = SilenceToggle;
            }
        }
    }
}