using System;
using UnityEngine.Events;
using OdinNative.Odin.Room;

namespace OdinNative.Unity.Events
{
    [Serializable]
    public class RoomLeaveProxy : UnityEvent<RoomLeaveEventArgs>
    {
    }
}

