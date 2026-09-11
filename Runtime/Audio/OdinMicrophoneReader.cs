#if !UNITY_WEBGL

using OdinNative.Unity.Events;
using OdinNative.Wrapper;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        /// <remarks>Android 6+ with <see cref="UnityEngine.Android.Permission.Microphone"/> <see href="https://docs.unity3d.com/ScriptReference/Android.Permission.Microphone.html">(Permission)</see></remarks>
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
        /// Redirect the captured audio to the <see cref="OnAudioData"/> event
        /// </summary>
        /// <remarks>Disabling skips the registered PushAudio, stops progress of time in the audio pipeline and can result in missing event updates like OnMediaActiveStateChanged. Should only be disabled if handled.</remarks>
        [Tooltip("Redirect the captured audio to all rooms.")]
        [SerializeField]
        public bool RedirectCapturedAudio = true;
        /// <summary>
        /// Zero out the event audio buffer for PushAudio.
        /// </summary>
        [Tooltip("Silence the captured audio to all rooms.")]
        [SerializeField]
        public bool SilenceCapturedAudio = false;

        [Tooltip("Indicates whether the recording should continue when the 3 second AudioClip buffer is full, wrapping around and recording from the beginning of the AudioClip.")]
        [SerializeField]
        public bool ContinueRecording = true;
        [Header("AudioClip Settings")]
        public int Samplerate = 48000;

        /// <summary>
        /// True while a capture device was found and set up.
        /// </summary>
        public bool IsInputDeviceConnected { get; private set; }
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
        [Tooltip("Automatic microphone start on Start()")]
        public bool AutostartListen = true;

        public bool CustomMicVolumeScale = false;
        [SerializeField]
        [Tooltip("Automatic microphone volume boost")]
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
            SceneManager.sceneLoaded += SceneManager_OnSceneLoaded;
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
            InitialPermission = HasPermission;
            if (HasPermission)
            {
                if (AutostartListen) StartListen();
            }
            else
            {
                OdinLog.LogWarning($"{nameof(OdinMicrophoneReader)} has no microphone permission yet, capture is waiting for the user to grant it.");
#if PLATFORM_ANDROID || UNITY_ANDROID
                Permission.RequestUserPermission(Permission.Microphone);
#else
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
#endif
                // result is handled in Update()
            }
        }

        private string SetupMicrophone(string customDevice = "")
        {
#if PLATFORM_ANDROID || UNITY_ANDROID || PLATFORM_IOS || UNITY_IOS || UNITY_WEBGL
            OdinLog.LogInfo($"User has authorization of Microphone: {HasPermission}");
#endif
            string[] devices = Microphone.devices;
            InputDevice = CustomInputDevice ? customDevice : devices.FirstOrDefault();

            if (string.IsNullOrEmpty(InputDevice) && CustomInputDevice == false || devices.Length <= 0)
            {
                IsInputDeviceConnected = false;
                OdinLog.LogWarning($"{nameof(OdinMicrophoneReader)} no Microphone.devices found.");
            }
            else
            {
                IsInputDeviceConnected = true;
                if (string.IsNullOrEmpty(InputDevice))
                    OdinLog.LogWarning($"{nameof(OdinMicrophoneReader)} setup unknown system default device.");
                else
                    OdinLog.LogInfo($"{nameof(OdinMicrophoneReader)} setup device \"{InputDevice}\".");
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
            if (InputClip == null)
            {
                OdinLog.LogWarning($"Microphone start \"{InputDevice}\" {Samplerate}Hz failed (no permission or device gone)");
                return IsStreaming = false;
            }
            OdinLog.LogInfo($"Microphone start \"{InputDevice}\" {Samplerate}Hz, {InputClip.name}: {(ContinueRecording ? "looping" : "once")}, {InputClip.length}s {InputClip.channels} channels {InputClip.frequency}Hz {InputClip.samples} samples");
            _MicPosition = Microphone.GetPosition(InputDevice);
            return IsStreaming = true;
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
            // permission check first: some platforms only enumerate devices after the grant
            if (InitialPermission == false)
            {
                /* If the app targets Android 11 or higher and isn't used for a few months,
                 * the system protects user data by automatically resetting the sensitive runtime permissions
                 * that the user had granted.*/
                if (HasPermission)
                {
                    InitialPermission = true; // Override because we should not need the check this lifetime anymore.
                    ResetDevice(InputDevice); // full re-setup, the device list may have changed with the grant
                }
                /* if the user taps Deny for a specific permission more than once during the app's lifetime on a device,
                 * the user doesn't see the system permissions dialog even if the app requests that permission again.
                 * The user's action implies "don't ask again."*/
                return;
            }

            if (IsInputDeviceConnected == false) return;

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
            SceneManager.sceneLoaded -= SceneManager_OnSceneLoaded;
        }

        // loading a scene blocks the main thread while the device keeps capturing
        private void SceneManager_OnSceneLoaded(Scene scene, LoadSceneMode mode) => _SceneLoadedSinceLastPull = true;

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

        // per-reader clip read cursor in frames; must not be shared between instances
        private int _MicPosition;

        private class RBuffer
        {
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

        /// <summary>
        /// Upper bound on how much captured audio a single <see cref="PullClipData"/> forwards.
        /// </summary>
        private const float MaxDrainSeconds = 0.2f;
        private float _LastDrainWarningTime = float.NegativeInfinity;
        private bool _SceneLoadedSinceLastPull;
        private static readonly global::Unity.Profiling.ProfilerMarker GetDataMarker = new global::Unity.Profiling.ProfilerMarker("ODIN.Microphone.GetData");
        private static readonly global::Unity.Profiling.ProfilerMarker DispatchAudioMarker = new global::Unity.Profiling.ProfilerMarker("ODIN.Microphone.DispatchAudio");

        /// <summary>
        /// Minimum time between two capture device resets.
        /// </summary>
        private const float DeviceResetCooldown = 1.0f;
        private float _LastDeviceResetTime = float.NegativeInfinity;

        RBuffer[] MicBuffers = new RBuffer[RBuffer.sizesMax + 1];

        private bool AllBuffersNull()
        {
            for (int i = 0; i < MicBuffers.Length; i++)
                if (MicBuffers[i] != null) return false;

            return true;
        }

        void SetupBuffers()
        {
            _MicPosition = 0;
            MicBuffers = new RBuffer[RBuffer.sizesMax + 1];
            for (int i = RBuffer.sizesMin; i <= RBuffer.sizesMax; i++)
                MicBuffers[i] = new RBuffer(i);
        }

        void PullClipData()
        {
            // initialization failure
            if (MicBuffers == null || AllBuffersNull())
            {
                OdinLog.LogError("Odin MicBuffer corrupted. Try restart!");
                SetupMicrophoneReader();
                return;
            }
            // no running devices or manually disabled
            if (IsStreaming == false || isActiveAndEnabled == false) return;
            // on failure, Microphone.Start should return null not an AudioClip without samples
            if (InputClip == null || InputClip.samples == 0)
            {
                // self-guard a clip left empty/stale by a device change, in case the audio
                // configuration changed event did not on the current platform.
                // Skip if no microphone available
                if (IsInputDeviceConnected && Time.unscaledTime - _LastDeviceResetTime >= DeviceResetCooldown)
                {
                    // A reset reopens the capture device and reallocates every ring buffer. If the clip
                    // stays invalid - the audio engine being disabled underneath us will do that - an
                    // unthrottled retry burns a full core doing this once per frame.
                    _LastDeviceResetTime = Time.unscaledTime;
                    ResetDevice(InputDevice);
                }
                return;
            }

            // a stall caused by a scene load is expected, any other stall is worth a warning
            bool sceneLoaded = _SceneLoadedSinceLastPull;
            _SceneLoadedSinceLastPull = false;

            int newPosition = Microphone.GetPosition(InputDevice);
            // device is not recording or buffer got collected
            if (_MicPosition == newPosition || MicBuffers == null) return;

            // positions from Microphone.GetPosition are frames; a buffer holds
            // buffer.Length / channels frames of interleaved data
            int channels = Mathf.Max(1, InputClip.channels);

            // give a sample on start ( S + 1 - 0 ) % S = 1 and give a sample at the end ( S + 0 - 99 ) % S = 1
            int dataToRead = (InputClip.samples + newPosition - _MicPosition) % InputClip.samples;

            // Any stall - a scene load, an audio engine init, a paused editor - leaves a backlog in the
            // clip. Draining all of it in one call means a GetData plus a full encode per buffer, which
            // can cost hundreds of milliseconds and produces the next stall itself. Voice older than
            // MaxDrainSeconds is worthless anyway, so skip past it instead of working through it.
            // The cap must still admit the smallest ring buffer, otherwise the loop below never runs
            // while the cursor keeps skipping ahead, and capture goes silent for good. That happens
            // at low samplerates, where MaxDrainSeconds is worth fewer frames than one buffer holds.
            int minFramesPerPull = (1 << RBuffer.sizesMin) / channels;
            int maxFramesPerPull = Mathf.Max(minFramesPerPull, Mathf.FloorToInt(MaxDrainSeconds * InputClip.frequency));
            if (dataToRead > maxFramesPerPull)
            {
                int skip = dataToRead - maxFramesPerPull;
                string dropMessage = $"{nameof(OdinMicrophoneReader)} dropping {skip} captured frames ({(float)skip / InputClip.frequency:0.00}s) to catch up after a stall";
                if (sceneLoaded)
                    OdinLog.LogInfo($"{dropMessage} caused by a scene load");
                else if (Time.unscaledTime - _LastDrainWarningTime >= 1f)
                {
                    _LastDrainWarningTime = Time.unscaledTime;
                    OdinLog.LogWarning(dropMessage);
                }
                _MicPosition = (_MicPosition + skip) % InputClip.samples;
                dataToRead = maxFramesPerPull;
            }

            for (int i = RBuffer.sizesMax; i >= RBuffer.sizesMin; i--)
            {
                RBuffer mic = MicBuffers[i];
                int framesPerBuffer = mic.buffer.Length / channels; // 1 << i for mono
                if (framesPerBuffer <= 0) continue;

                while (dataToRead >= framesPerBuffer)
                {
                    // If the read length from the offset is longer than the clip length,
                    // the read will wrap around and read the remaining samples from the start of the clip.
                    using (GetDataMarker.Auto())
                    {
                        if (InputClip.GetData(mic.buffer, _MicPosition) == false)
                            return; // do not push a stale buffer on a failed read
                    }
                    _MicPosition = (_MicPosition + framesPerBuffer) % InputClip.samples;
                    using (DispatchAudioMarker.Auto())
                        OnMicrophoneData?.Invoke(mic.buffer, _MicPosition);

                    mic.Cycle();
                    dataToRead -= framesPerBuffer;
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