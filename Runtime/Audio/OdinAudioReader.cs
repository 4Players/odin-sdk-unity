using OdinNative.Unity.Events;
using System;
using UnityEngine;
#if PLATFORM_ANDROID || UNITY_ANDROID
using UnityEngine.Android;
#endif
#if PLATFORM_IOS || UNITY_IOS
using UnityEngine.iOS;
#endif

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Handles audioclip input data and sends input to ODIN
    /// <para>
    /// This convenient class gathers input audio data from Unity to pass the data with <see cref="OnAudioData"/> event to other components.
    /// </para>
    /// </summary>
    /// <remarks>A room/media will register to the callback event to redirect the data to Odin.</remarks>
    [AddComponentMenu("Odin/Audio/OdinAudioReader")]
    public class OdinAudioReader : MonoBehaviour
    {
        /// <summary>
        /// Skips registered PushAudio
        /// </summary>
        /// <remarks>This will stop progress of time in the audio pipeline and can result in missing event updates. Should only be disabled if handled.</remarks>
        [Tooltip("Redirect the input audio.")]
        [SerializeField]
        public bool RedirectInputAudio = true;
        /// <summary>
        /// Zero out the event audio buffer.
        /// </summary>
        [Tooltip("Silence the input audio.")]
        [SerializeField]
        public bool SilenceInputAudio = false;

        [Tooltip("Indicates whether the input should continue if AudioClipLength is reached, and wrap around and record from the beginning of the AudioClip.")]
        [SerializeField]
        public bool ContinueRecording = true;
        public AudioClip InputClip;
        internal bool IsStreaming;
        private float[] buffer;

        public bool CustomInputVolumeScale = false;
        [SerializeField]
        [Tooltip("Automatical input volume boost")]
        [Range(0.1f, 2.0f)]
        public float InputVolumeScale = 1f;

        public UnityAudioData OnAudioData;

        void Awake()
        {
            OnAudioData = new UnityAudioData();
        }

        void OnEnable()
        {
            if (InputClip == null)
                InputClip = GetComponent<AudioClip>();

            if (InputClip == null)
                this.enabled = false;

            buffer = new float[InputClip?.samples ?? 0]; // using Unity "MissingComponentException" msg as error

#if UNITY_WEBGL
            var room = GetComponent<OdinWebRoom>();
#else
            var room = GetComponent<OdinRoom>();
            if (room != null)
                OnAudioData.AddListener(room.ProxyAudio);
#endif
        }

        void Reset()
        {
            RedirectInputAudio = true;
            SilenceInputAudio = false;

            ContinueRecording = true;

            CustomInputVolumeScale = false;
            InputVolumeScale = 1f;
        }

        private void PushAudio(float[] buffer, int position)
        {
            if (RedirectInputAudio == false) return;

            if (CustomInputVolumeScale)
            {
                float bufferScale = GetVolumeScale(InputVolumeScale);
                SetVolume(ref buffer, bufferScale);
            }

            if(SilenceInputAudio)
                for(int i = 0; i < buffer.Length; i++)
                    buffer[i] = 0f;

            OnAudioData?.Invoke(buffer, position, SilenceInputAudio);
        }

        float GetVolumeScale(float value)
        {
            return Mathf.Pow(value, 3);
        }

        void SetVolume(ref float[] buffer, float scale)
        {
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] *= scale;
        }

        float GetAveragedVolume(float[] buffer)
        {
            float avg = 0;
            foreach (float s in buffer)
            {
                avg += Mathf.Abs(s);
            }
            return avg / buffer.Length * 100f;
        }

        void Update()
        {
            if(InputClip != null && InputClip.GetData(buffer, 0))
                PushAudio(buffer, 0);
        }

        void OnDisable()
        {
            OnAudioData.RemoveAllListeners();
            IsStreaming = false;
        }
    }
}
