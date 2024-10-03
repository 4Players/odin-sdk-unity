using System;
using UnityEngine;
using UnityEngine.Events;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Custom filter by collider component for <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/>
    /// <para>
    /// This class is a effect in the odin audio pipline to mute audio based on GameObject collisions in Unity space.
    /// The effect can help to trigger specific audio packets quickly without the adjustment of Server-side positions.
    /// </para>
    /// </summary>
    /// <remarks>This <see cref="OdinNative.Wrapper.Media.PiplineEffect"/> is a <see cref="OdinNative.Wrapper.Media.CustomEffect{}"/>; Odin supports a virtual position for Server-side culling (see <see cref="OdinNative.Wrapper.Room.Room"/>) outside of these pipline effects.</remarks>
    [HelpURL("https://www.4players.io/odin/sdk/unity/2.0.0/classes/odincolliderfiltercomponent/")]
    [AddComponentMenu("Odin/Audio/Effect/Collider Filter")]
    public class OdinColliderFilterComponent : OdinCustomEffectUnityComponentBase<IntPtr>
    {
        [Tooltip("Set the tag to compare match for collider object")]
        public string ColliderTag;
        public StringComparison Comparison = StringComparison.InvariantCultureIgnoreCase;
        protected virtual bool _Colliding => _ColliderCount > 0;
        private int _ColliderCount;
        public UnityAction<OdinArrayf> Callback;

#if !UNITY_WEBGL
        void OnTriggerEnter(Collider obj) => SetTriggerCount(obj, true);
        void OnTriggerExit(Collider obj) => SetTriggerCount(obj, false);
#endif
        public virtual void SetTriggerCount(Collider obj, bool isEnter)
        {
            if (string.Equals(obj.tag, ColliderTag, Comparison))
                _ColliderCount = isEnter ? _ColliderCount++ : _ColliderCount--;
        }

        public override void CustomEffectCallback(OdinArrayf audio, ref bool isSilent, IntPtr _)
        {
            base.CustomEffectCallback(audio, ref isSilent, _);

            if (!base._corrupt && base.IsEnabled && _Colliding)
                Callback?.Invoke(audio);
        }
    }
}