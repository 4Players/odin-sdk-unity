using System;
using UnityEngine.Events;

namespace OdinNative.Unity.Events
{
    [Serializable]
    public class UnityAudioData : UnityEvent<float[], int, bool>
    {
    }
}

