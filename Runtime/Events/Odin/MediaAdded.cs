using System;
using UnityEngine.Events;

namespace OdinNative.Unity.Events
{
    /// <summary>
    /// Arguments for decoder added events dispatched from <see cref="OdinNative.Unity.OdinRoom"/>
    /// </summary>
    public class DecoderAddedEventArgs : EventArgs
    {
        /// <summary>
        /// room id
        /// </summary>
        public ulong RoomId { get; internal set; }
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
    /// This class provides the base functionality for UnityEvents based on <see cref="DecoderAddedEventArgs"/>.
    /// A persistent callback that can be saved with the Scene.
    /// Unity Inspector event wrapper <see href="https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html">(UnityEvent)</see>
    /// </summary>
    [Serializable]
    public class DecoderAddedProxy : UnityEvent<object, DecoderAddedEventArgs>
    {
    }
}
