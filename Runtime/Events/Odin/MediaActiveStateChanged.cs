using System;
using UnityEngine.Events;

namespace OdinNative.Unity.Events
{
    /// <summary>
    /// Arguments for activity state change events on a decoder media stream.
    /// </summary>
    public class MediaActiveStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// true when the audio stream is active (not silent)
        /// </summary>
        public bool Active { get; internal set; }
        /// <summary>
        /// Id of the media stream that changed state
        /// </summary>
        public ulong MediaId { get; internal set; }
        /// <summary>
        /// Id of the peer that owns the media stream
        /// </summary>
        public uint PeerId { get; internal set; }
    }

    /// <summary>
    /// Unity Inspector event wrapper for media activity state changes.
    /// A persistent callback that can be saved with the Scene.
    /// <see href="https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html">(UnityEvent)</see>
    /// </summary>
    [Serializable]
    public class MediaActiveStateChangedProxy : UnityEvent<object, MediaActiveStateChangedEventArgs>
    {
    }
}