namespace OdinNative.Wrapper
{
    /// <summary>
    /// ODIN default configuration
    /// </summary>
    public static class OdinDefaults
    {
        public static bool DEBUG = false;
        /// <summary>
        /// PackageName
        /// </summary>
        public const string SDKID = "io.fourplayers.odin";
        /// <summary>
        /// Default Gateway
        /// </summary>
        public const string GATEWAY = "gateway.odin.4players.io";
        /// <summary>
        /// Default Samplerate
        /// </summary>
        public const int SampleRate = 48000;
        /// <summary>
        /// Default Stereo flag
        /// </summary>
        public const bool Stereo = false;
        /// <summary>
        /// Enable additional logs
        /// </summary>
        public static OdinLog.VerbosityLevel Verbosity = OdinLog.VerbosityLevel.Warning;
        /// <summary>
        /// Default access key
        /// </summary>
        public static string AccessKey { get; set; } = "";
        /// <summary>
        /// Default server url
        /// </summary>
        public static string Server { get; set; } = "https://" + GATEWAY;
        /// <summary>
        /// Default text representation of UserData
        /// </summary>
        public static string UserDataText { get; set; } = "";

        /// <summary>
        /// JWT room token lifetime
        /// </summary>
        public static ulong TokenLifetime { get; set; } = 300;

        #region Apm
        /// <summary>
        /// Indicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool VoiceActivityDetection = true;
        /// <summary>
        /// Indicates the vad attack probability ApmConfig setting by default
        /// </summary>
        public static float VoiceActivityDetectionAttackProbability = 0.9f;
        /// <summary>
        /// Indicates the vad release probability ApmConfig setting by default
        /// </summary>
        public static float VoiceActivityDetectionReleaseProbability = 0.8f;
        /// <summary>
        /// Indicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool VolumeGate = false;
        /// <summary>
        /// Indicates the gate attack loudness ApmConfig setting by default
        /// </summary>
        public static float VolumeGateAttackLoudness = -30.0f;
        /// <summary>
        /// Indicates the gate release loudness ApmConfig setting by default
        /// </summary>
        public static float VolumeGateReleaseLoudness = -40.0f;
        /// <summary>
        /// When enabled the echo canceller will try to subtract echoes, reverberation and unwanted
        /// added sounds from the audio input signal. Note that you need to process the reverse audio
        /// stream, also known as the loopback data to be used in the ODIN echo canceller.
        /// </summary>
        public static bool EchoCanceller = false;
        /// <summary>
        /// When enabled, the high-pass filter will remove low-frequency content from the input audio
        /// signal, thus making it sound cleaner and more focused.
        /// </summary>
        public static bool HighPassFilter = false;
        /// <summary>
        /// When enabled, the transient suppressor will try to detect and attenuate keyboard clicks.
        /// </summary>
        public static bool TransientSuppressor = false;
        /// <summary>
        /// When enabled, the noise suppressor will remove distracting background noise from the input
        /// audio signal. You can control the aggressiveness of the suppression. Increasing the level
        /// will reduce the noise level at the expense of a higher speech distortion.
        /// </summary>
        public static Core.Imports.NativeBindings.OdinNoiseSuppressionLevel NoiseSuppressionLevel = Core.Imports.NativeBindings.OdinNoiseSuppressionLevel.ODIN_NOISE_SUPPRESSION_LEVEL_NONE;
        /// <summary>
        /// When enabled, the gain controller will bring the input audio signal to an appropriate range
        /// when it's either too loud or too quiet.
        /// </summary>
        public static Core.Imports.NativeBindings.OdinGainControllerVersion GainControllerVersion = Core.Imports.NativeBindings.OdinGainControllerVersion.ODIN_GAIN_CONTROLLER_VERSION_DISABLED;
        #endregion Apm

        #region Vi
        /// <summary>
        /// Indicates whether the ViConfig setting is enabled by default
        /// </summary>
        public static bool VoiceIsolation = false;
        /// <summary>
        /// Indicates the default maximum attenuation applied to non-speech in dB by the ViConfig setting
        /// </summary>
        public static float VoiceIsolationAttenuationLimitDb = 100.0f;
        #endregion Vi
    }
}
