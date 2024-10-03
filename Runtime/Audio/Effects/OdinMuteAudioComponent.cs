using System;
using UnityEngine;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom mute audio component for <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/>
    /// <para>
    /// This class is a effect in the odin audio pipline to mute based on the <see cref="SilenceToggle"/> flag.
    /// The intention is to provide a convenient way with Unity Editor UI of Marshal a 1-byte signed integer bool for a specific audio packet in the current pipline.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/> and will toggle <see cref="OdinNative.Core.Imports.NativeBindings.OdinCallbackAudioData.is_silent"/></remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odinmuteaudiocomponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Mute Audio")]
    public class OdinMuteAudioComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Tooltip("Set \"is_silent\" flag to mute on native level. On false nothing will be set and the component does a simple passthrough.")]
        public bool SilenceToggle = false;

        public override IntPtr GetEffectUserData() => IntPtr.Zero;
        public override void CustomEffectCallback(OdinArrayf audio, ref bool isSilent, IntPtr _)
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