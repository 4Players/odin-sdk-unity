using System;
using OdinNative.Wrapper.Media;
using UnityEngine;
using UnityEngine.Events;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom filter by collider component for <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>
    /// <para>
    /// This class is an effect in the odin audio pipeline to mute audio based on GameObject collisions in Unity space.
    /// The effect can help to trigger specific audio packets quickly without the adjustment of Server-side positions.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="PipelineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{T}"/>; Odin supports a virtual position for Server-side culling (see <see cref="OdinNative.Wrapper.Room.Room"/>) outside of these pipeline effects.</remarks>
    [HelpURL("https://docs.4players.io/voice/unity/next/api/OdinNative.Unity.Audio/OdinColliderFilterComponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Collider Filter")]
    public class OdinColliderFilterComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Tooltip("Set the tag to compare match for collider object")]
        public string ColliderTag;
        public StringComparison Comparison = StringComparison.InvariantCultureIgnoreCase;
        protected virtual bool _Colliding => _ColliderCount > 0;
        private int _ColliderCount;
        public UnityAction<OdinTArray<float>> Callback;

#if !UNITY_WEBGL
        void OnTriggerEnter(Collider obj) => SetTriggerCount(obj, true);
        void OnTriggerExit(Collider obj) => SetTriggerCount(obj, false);
#endif
        public virtual void SetTriggerCount(Collider obj, bool isEnter)
        {
            if (string.Equals(obj.tag, ColliderTag, Comparison))
                _ColliderCount = Mathf.Max(0, _ColliderCount + (isEnter ? 1 : -1));
        }

        public override void CustomEffectCallback(OdinTArray<float> audio, ref bool isSilent, IntPtr _)
        {
            base.CustomEffectCallback(audio, ref isSilent, _);

            if (!base._corrupt && base.IsEnabled && _Colliding)
                Callback?.Invoke(audio);
        }
    }
}