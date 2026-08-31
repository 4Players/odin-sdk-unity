using OdinNative.Wrapper.Room;
using System;
using UnityEngine.Events;

namespace OdinNative.Unity.Events
{
    /// <summary>
    /// This class provides the base functionality for ODIN SDK UnityEvents.
    /// A persistent callback that can be saved with the Scene.
    /// </summary>
    [Obsolete("Not raised by any component since the 2.x media rework; kept for compatibility and to be removed in a future release")]
    [Serializable]
    public class RoomLeaveProxy : UnityEvent<RoomLeaveEventArgs>
    {
    }
}

