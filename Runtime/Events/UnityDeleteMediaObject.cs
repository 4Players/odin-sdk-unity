using System;
using UnityEngine.Events;

namespace OdinNative.Unity.Events
{
    /// <summary>
    /// Unity Inspector event wrapper <see href="https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html">(UnityEvent)</see>
    /// </summary>
    [Obsolete("Not raised by any component since the 2.x media rework; kept for compatibility and to be removed in a future release")]
    [Serializable]
    public class UnityDeleteMediaObject : UnityEvent<ushort>
    {
    }
}

