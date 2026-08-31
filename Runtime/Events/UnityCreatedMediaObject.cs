using System;
using UnityEngine.Events;

namespace OdinNative.Unity.Events
{
    [Obsolete("Not raised by any component since the 2.x media rework; kept for compatibility and to be removed in a future release")]
    [Serializable]
    public class UnityCreatedMediaObject : UnityEvent<ulong, ulong, ushort>
    {
    }
}

