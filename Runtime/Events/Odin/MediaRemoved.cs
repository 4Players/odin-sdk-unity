using System;
using UnityEngine.Events;

namespace OdinNative.Unity.Events
{
    /// <summary>
    /// Arguments for decoder removed events dispatched from <see cref="OdinNative.Unity.OdinRoom"/>
    /// </summary>
    public class DecoderRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// peer id
        /// </summary>
        public uint PeerId { get; internal set; }
        /// <summary>
        /// media id
        /// </summary>
        public ulong MediaId { get; internal set; }
    }

    /// <summary>
    /// This class provides the base functionality for UnityEvents based on <see cref="DecoderRemovedEventArgs"/>.
    /// A persistent callback that can be saved with the Scene.
    /// Unity Inspector event wrapper <see href="https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html">(UnityEvent)</see>
    /// </summary>
    [Serializable]
    public class DecoderRemovedProxy : UnityEvent<object, DecoderRemovedEventArgs>
    {
    }
}
