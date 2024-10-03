using System;
using UnityEngine.Events;

namespace OdinNative.Unity.Events
{
    [Serializable]
    public class UnityCreatedMediaObject : UnityEvent<ulong, ulong, ushort>
    {
    }
}

