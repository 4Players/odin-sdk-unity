#if !UNITY_WEBGL

using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using System;
using System.Linq;
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
    /// Handles microphone input data and sends input to ODIN
    /// <para>
    /// This convenient class gathers input audio data from Unity managed Microphone to pass the data with <see cref="OnAudioData"/> event to other components.
    /// </para>
    /// </summary>
    /// <remarks>A room/media will register to the callback event to redirect the data to Odin.</remarks>
    [AddComponentMenu("Odin/Audio/OdinMicrophoneReader")]
    [DisallowMultipleComponent]
    public class OdinMicrophoneReader : MonoBehaviour
    {
#if PLATFORM_ANDROID || UNITY_ANDROID
        /// <summary>
        /// Check if the user has authorized use of the microphone
        /// </summary>
        /// <remarks>Andriod 6+ with <see cref="UnityEngine.Android.Permission.Microphone"/> <see href="https://docs.unity3d.com/ScriptReference/Android.Permission.Microphone.html">(Permission)</see></remarks>
        public bool HasPermission => Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
        /// <summary>
        /// Check if the user has authorized use of the microphone
        /// </summary>
        /// <remarks>In other build targets except for Web Player, this function will always return true.</remarks>
        public bool HasPermission => Application.HasUserAuthorization(UserAuthorization.Microphone);
#endif
        private bool InitialPermission;

        /// <summary>
        /// Skips registered PushAudio for the OnMicrophoneData event
        /// </summary>
        /// <remarks>This will stop progress of time in the audio pipeline and can result in missing event updates like OnMediaActiveStateChanged. Should only be disabled if handled.</remarks>
        [Tooltip("Redirect the captured audio to all rooms.")]
        [SerializeField]
        public bool RedirectCapturedAudio = true;
        /// <summary>
        /// Zero out the event audio buffer for PushAudio.
        /// </summary>
        [Tooltip("Silence the captured audio to all rooms.")]
        [SerializeField]
        public bool SilenceCapturedAudio = false;

        [Tooltip("Indicates whether the recording should continue recording if AudioClipLength is reached, and wrap around and record from the beginning of the AudioClip.")]
        [SerializeField]
        public bool ContinueRecording = true;
        [Header("AudioClip Settings")]
        public int Samplerate = 48000;

        private bool IsInputDeviceConnected;
        private int InputMinFreq;
        private int InputMaxFreq;
        /// <summary>
        /// Enable/Disable the use of <see cref="OdinNative.Unity.Audio.OdinMicrophoneReader.InputDevice"/> as a new/fixed device name
        /// </summary>
        public bool CustomInputDevice;
        /// <summary>
        /// The device name to use as microphone in Unity. (i.e <see href="https://docs.unity3d.com/ScriptReference/Microphone.Start.html">Microphone.Start</see>)
        /// </summary>
        /// <remarks>If you pass a null or empty string for the device name then the default microphone will be used. You can get a list of available microphone devices from the devices property. (see <see href="https://docs.unity3d.com/ScriptReference/Microphone-devices.html">Microphone.devices</see>)</remarks>
        public string InputDevice;
        private AudioClip InputClip;
        private bool IsFirstStartGlobal;
        internal bool IsStreaming;

        /// <summary>
        /// Use UnityEngine.Microphone.Start <see href="https://docs.unity3d.com/ScriptReference/Microphone.Start.html">(Microphone.Start)</see> in <see cref="Start"/>
        /// </summary>
        [SerializeField]
        [Tooltip("Automatical microphone start on Start()")]
        public bool AutostartListen = true;

        public bool CustomMicVolumeScale = false;
        [SerializeField]
        [Tooltip("Automatical microphone volume boost")]
        public float MicVolumeScale = 1f;

        [SerializeField]
        public UnityAudioData OnAudioData;

        public int MicrophoneSamplerate => InputClip == null ? Samplerate : InputClip.frequency;
        public int MicrophoneChannels => InputClip == null ? 1 : InputClip.channels;

        void Awake()
        {
            if (OnAudioData == null) OnAudioData = new UnityAudioData();
        }

        void OnEnable()
        {
            AudioSettings.OnAudioConfigurationChanged += AudioSettings_OnAudioConfigurationChanged;
            if (InputClip != null && Microphone.IsRecording(InputDevice)) IsStreaming = true;

            OnMicrophoneData += PushAudio;
        }

        void Reset()
        {
            AutostartListen = true;
            RedirectCapturedAudio = true;
            SilenceCapturedAudio = false;
            ContinueRecording = true;

            CustomMicVolumeScale = false;
            MicVolumeScale = 1f;
        }

        void Start()
        {
            SetupMicrophoneReader();
        }

        private void SetupMicrophoneReader()
        {
            IsStreaming = false;
            IsFirstStartGlobal = true;
            InputClip = null;
            IsInputDeviceConnected = false;
            SetupBuffers();
            SetupMicrophone(InputDevice);
            if (Microphone.IsRecording(InputDevice)) IsFirstStartGlobal = false;
            if (HasPermission)
            {
                InitialPermission = HasPermission; // Override because we should not need the check this lifetime anymore. 
                if (AutostartListen) StartListen();
            }
        }

        private string SetupMicrophone(string customDevice = "")
        {
#if PLATFORM_ANDROID || UNITY_ANDROID || PLATFORM_IOS || UNITY_IOS || UNITY_WEBGL
            Debug.Log($"User has authorization of Microphone: {HasPermission}");
#endif
            InputDevice = CustomInputDevice ? customDevice : Microphone.devices.FirstOrDefault();

            if (string.IsNullOrEmpty(InputDevice) && CustomInputDevice == false || Microphone.devices.Length <= 0)
            {
                IsInputDeviceConnected = false;
                Debug.LogWarning($"{nameof(OdinMicrophoneReader)} no Microphone.devices found.");
            }
            else
            {
                IsInputDeviceConnected = true;
                if (string.IsNullOrEmpty(InputDevice))
                    if (OdinDefaults.Verbose || OdinDefaults.Debug) Debug.LogWarning($"{nameof(OdinMicrophoneReader)} setup unknown system default device.");
                else
                    if (OdinDefaults.Verbose || OdinDefaults.Debug) Debug.Log($"{nameof(OdinMicrophoneReader)} setup device \"{InputDevice}\".");
            }

            if (IsInputDeviceConnected == false) return string.Empty;

            Microphone.GetDeviceCaps(InputDevice, out InputMinFreq, out InputMaxFreq);
            if(InputMaxFreq > 0)
                Samplerate = Samplerate > InputMinFreq && Samplerate < InputMaxFreq ? Samplerate : InputMaxFreq;

            return InputDevice;
        }

        /// <summary>
        /// Start Unity microphone capture
        /// </summary>
        /// <remarks>if "Autostart Listen" in Editor component is true, the capture will be called in Unity-Start(void).</remarks>
        public bool StartListen()
        {
            if (IsInputDeviceConnected == false) return false;

            InputClip = Microphone.Start(InputDevice, ContinueRecording, 3, ((int)Samplerate));
            if (OdinDefaults.Debug) Debug.Log($"Microphone start \"{InputDevice}\" {Samplerate}Hz, {InputClip.name}: {(ContinueRecording ? "looping" : "once")}, {InputClip.length}s {InputClip.channels} channels {InputClip.frequency}Hz {InputClip.samples} samples");
            RBuffer.MicPosition = Microphone.GetPosition(InputDevice);
            return IsStreaming = InputClip != null;
        }

        private void PushAudio(float[] buffer, int position)
        {
            if (RedirectCapturedAudio == false) return;

            if (CustomMicVolumeScale)
            {
                float bufferScale = GetVolumeScale(MicVolumeScale);
                SetVolume(ref buffer, bufferScale);
            }

            if(SilenceCapturedAudio)
                Array.Clear(buffer, 0, buffer.Length);

            OnAudioData?.Invoke(buffer, position, SilenceCapturedAudio);
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
            if (IsInputDeviceConnected == false) return;

            if (InitialPermission == false)
            {
                /* If the app targets Android 11 or higher and isn't used for a few months,
                 * the system protects user data by automatically resetting the sensitive runtime permissions
                 * that the user had granted.*/
                if (HasPermission)
                {
                    InitialPermission = HasPermission; // Override because we should not need the check this lifetime anymore. 
                    ResetDevice(InputDevice);
                    return;
                }
                /* if the user taps Deny for a specific permission more than once during the app's lifetime on a device,
                 * the user doesn't see the system permissions dialog even if the app requests that permission again.
                 * The user's action implies "don't ask again."*/
                else return;
            }

            PullClipData();
        }

        private void AudioSettings_OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            if (deviceWasChanged && isActiveAndEnabled)
                ResetDevice(InputDevice);
        }

        internal void ResetDevice(string deviceName)
        {
            if (IsStreaming && Microphone.IsRecording(deviceName))
                Microphone.End(deviceName);
            
            SetupMicrophoneReader();
        }
        

        /// <summary>
        /// Stop Unity Microphone capture if this AudioSender created the recording
        /// </summary>
        public void StopListen()
        {
            // Stops the device only if this Sender started the recording
            if (IsFirstStartGlobal && Microphone.IsRecording(InputDevice))
                Microphone.End(InputDevice);

            IsStreaming = false;
        }

        void OnDisable()
        {
            IsStreaming = false;
            OnMicrophoneData -= PushAudio;
            AudioSettings.OnAudioConfigurationChanged -= AudioSettings_OnAudioConfigurationChanged;
        }

        void OnDestroy()
        {
            OnAudioData?.RemoveAllListeners();

            StopListen();
        }

        #region Buffer
        public delegate void MicrophoneCallbackDelegate(float[] buffer, int position);
        /// <summary>
        /// Event is fired if raw microphone data is available
        /// </summary>
        public MicrophoneCallbackDelegate OnMicrophoneData;

        private class RBuffer
        {
            public static int MicPosition = 0;

            public const int sizesMin = 10;
            public const int sizesMax = 11;

            const int redundancy = 8; // times 8 ea buffer size to cycle
            int index = 0;

            float[][] internalBuffers = new float[redundancy][];

            public float[] buffer
            {
                get
                {
                    return internalBuffers[index];
                }
            }

            public void Cycle()
            {
                index = (index + 1) % redundancy;
            }

            public RBuffer(int size)
            {
                for (int i = 0; i < redundancy; i++)
                {
                    internalBuffers[i] = new float[1 << size];
                }
            }
        }

        RBuffer[] MicBuffers = new RBuffer[RBuffer.sizesMax + 1];

        void SetupBuffers()
        {
            RBuffer.MicPosition = 0;
            MicBuffers = new RBuffer[RBuffer.sizesMax + 1];
            for (int i = RBuffer.sizesMin; i <= RBuffer.sizesMax; i++)
                MicBuffers[i] = new RBuffer(i);
        }

        void PullClipData()
        {
            // initialization failure
            if (MicBuffers == null || MicBuffers.All(b => b == null))
            {
                Debug.LogError("Odin MicBuffer corrupted. Try restart!");
                SetupMicrophoneReader();
                return;
            }
            // no running devices or manually disabled
            if (IsStreaming == false || isActiveAndEnabled == false) return;
            // on failure, Microphone.Start should return null not an AudioClip without samples
            if (InputClip == null || InputClip.samples == 0) return;

            int newPosition = Microphone.GetPosition(InputDevice);
            // device is not recording or buffer got collected
            if (RBuffer.MicPosition == newPosition || MicBuffers == null) return;

            // give a sample on start ( S + 1 - 0 ) % S = 1 and give a sample at the end ( S + 0 - 99 ) % S = 1
            int dataToRead = (InputClip.samples + newPosition - RBuffer.MicPosition) % InputClip.samples;
            for (int i = RBuffer.sizesMax; i >= RBuffer.sizesMin; i--)
            {
                RBuffer mic = MicBuffers[i];
                int n = mic.buffer.Length; // 1 << i;

                while (dataToRead >= n)
                {
                    // If the read length from the offset is longer than the clip length,
                    // the read will wrap around and read the remaining samples from the start of the clip.
                    InputClip.GetData(mic.buffer, RBuffer.MicPosition);
                    RBuffer.MicPosition = (RBuffer.MicPosition + n) % InputClip.samples;
                    OnMicrophoneData?.Invoke(mic.buffer, RBuffer.MicPosition);

                    mic.Cycle();
                    dataToRead -= n;
                }
            }
        }
        #endregion Buffer
    }
}
#else

using OdinNative.Unity.Events;
using UnityEngine;

namespace OdinNative.Unity.Audio
{
    /// <summary>
    /// Handles microphone input data and sends input to ODIN
    /// <para>
    /// This convenient class gathers input audio data from Unity managed Microphone to pass the data with <see cref="OnAudioData"/> event to other components.
    /// </para>
    /// </summary>
    /// <remarks>A room/media will register to the callback event to redirect the data to Odin.</remarks>
    [AddComponentMenu("Odin/Audio/OdinMicrophoneReader")]
    [DisallowMultipleComponent]
    public class OdinMicrophoneReader : MonoBehaviour
    {
        public int MicrophoneSamplerate => 48000;
        public int MicrophoneChannels => 1;

        [SerializeField]
        public UnityAudioData OnAudioData = new UnityAudioData();
    }
}
#endif