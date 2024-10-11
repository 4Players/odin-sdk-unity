using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OdinNative.Unity.Samples
{
    /// <summary>
    /// Minimal room join sample
    /// </summary>
    public class SimpleRoomJoin : MonoBehaviour
    {
        public string RoomName;

        private void Reset()
        {
            RoomName = "default";
        }

        // Start is called before the first frame update
        void Start()
        {
            OdinHandler.Instance.JoinRoom(RoomName);
        }
    }
}