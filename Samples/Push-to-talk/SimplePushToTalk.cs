using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OdinNative.Unity.Audio;
using OdinNative.Odin.Room;

namespace OdinNative.Unity.Samples
{
    /// <summary>
    /// Showcases simple push-to-talk functionality using the Odin SDK.
    /// </summary>
    public class SimplePushToTalk : MonoBehaviour
    {
        /// <summary>
        /// Name of the room to join.
        /// </summary>
        [Tooltip("Name of the room to join.")]
        public string RoomName;
        /// <summary>
        /// Hotkey used to activate push-to-talk.
        /// </summary>
        [SerializeField, Tooltip("Hotkey used to activate push-to-talk.")]
        public KeyCode PushToTalkHotkey = KeyCode.C;
        /// <summary>
        /// Enable or disable push-to-talk functionality.
        /// </summary>
        [Tooltip("Enable or disable push-to-talk functionality.")]
        public bool UsePushToTalk = true;
        /// <summary>
        /// Reference to the MicrophoneReader component.
        /// </summary>
        [Tooltip("Reference to the MicrophoneReader component.")]
        public MicrophoneReader AudioSender;
        
        /// <summary>
        /// The Microphone stream instance.
        /// </summary>
        private OdinNative.Odin.Media.MicrophoneStream MicStream;

        /// <summary>
        /// Resets the component to default values.
        /// </summary>
        private void Reset()
        {
            RoomName = "default";
            PushToTalkHotkey = KeyCode.C;
            UsePushToTalk = true;
        }

        /// <summary>
        /// Get reference to microphone reader instance and join room.
        /// </summary>
        void Start()
        {
            if (AudioSender == null)
            {
                #if UNITY_6000_0_OR_NEWER
                AudioSender = FindFirstObjectByType<MicrophoneReader>();
                #else
                AudioSender = FindObjectOfType<MicrophoneReader>();
                #endif
            }
            // Join the specified room.
            OdinHandler.Instance.JoinRoom(RoomName);
        }

        /// <summary>
        /// Handles push-to-talk input and muting/unmuting of the microphone stream.
        /// </summary>
        void Update()
        {
            // Attempt to get the MicrophoneStream if not already assigned.
            if (MicStream == null)
            {
                if (OdinHandler.Instance.Rooms.Contains(RoomName))
                {
                    MicStream = OdinHandler.Instance.Rooms[RoomName]?.MicrophoneMedia;
                }
            }
            else
            {
                // Mute or unmute the microphone stream based on push-to-talk input.
                if (UsePushToTalk)
                {
                    bool isPttButtonDown = Input.GetKey(PushToTalkHotkey);
                    MicStream.MuteStream(!isPttButtonDown);
                }
            }
        }
    }
}