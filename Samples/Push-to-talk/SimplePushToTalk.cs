using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OdinNative.Unity.Audio;
using OdinNative.Odin.Room;

namespace OdinNative.Unity.Samples
{
    public class SimplePushToTalk : MonoBehaviour
    {
        public string RoomName;
        [SerializeField]
        public KeyCode PushToTalkHotkey;
        public bool UsePushToTalk = true;
        public MicrophoneReader AudioSender;
        private OdinNative.Odin.Media.MicrophoneStream MicStream;

        private void Reset()
        {
            RoomName = "default";
            PushToTalkHotkey = KeyCode.C;
            UsePushToTalk = true;
        }

        // Start is called before the first frame update
        void Start()
        {
            if (AudioSender == null)
                AudioSender = FindObjectOfType<MicrophoneReader>();

            OdinHandler.Instance.JoinRoom(RoomName);
        }

        // Update is called once per frame
        void Update()
        {
            // All MicrophoneStreams
            //if (AudioSender)
            //    AudioSender.SilenceCapturedAudio = !(UsePushToTalk ? Input.GetKey(PushToTalkHotkey) : true);
            // Selected MicrophoneStream
            if (MicStream == null)
                MicStream = OdinHandler.Instance.Rooms[RoomName]?.MicrophoneMedia;
            else
                MicStream.MuteStream(!(UsePushToTalk ? Input.GetKey(PushToTalkHotkey) : true));
        }
    }
}