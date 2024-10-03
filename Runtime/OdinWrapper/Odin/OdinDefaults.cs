namespace OdinNative.Wrapper
{
    /// <summary>
    /// ODIN default configuration
    /// </summary>
    public static class OdinDefaults
    {
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
        public static bool Verbose = true;
        /// <summary>
        /// Enable additional debug logs
        /// </summary>
        public static bool Debug = false;
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
        /// Idicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool VoiceActivityDetection = true;
        /// <summary>
        /// Idicates the vad attack probability ApmConfig setting by default
        /// </summary>
        public static float VoiceActivityDetectionAttackProbability = 0.9f;
        /// <summary>
        /// Idicates the vad release probability ApmConfig setting by default
        /// </summary>
        public static float VoiceActivityDetectionReleaseProbability = 0.8f;
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool VolumeGate = false;
        /// <summary>
        /// Idicates the gate attack loudness ApmConfig setting by default
        /// </summary>
        public static float VolumeGateAttackLoudness = -30.0f;
        /// <summary>
        /// Idicates the gate release loudness ApmConfig setting by default
        /// </summary>
        public static float VolumeGateReleaseLoudness = -40.0f;
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool EchoCanceller = false;
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool HighPassFilter = false;
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool PreAmplifier = false;
        /// <summary>
        /// Idicates the level of noise suppression ApmConfig setting by default
        /// </summary>
        public static Core.Imports.NativeBindings.OdinNoiseSuppression NoiseSuppressionLevel = Core.Imports.NativeBindings.OdinNoiseSuppression.ODIN_NOISE_SUPPRESSION_NONE;
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool TransientSuppressor = false;
        /// <summary>
        /// Idicates whether the ApmConfig setting is enabled by default
        /// </summary>
        public static bool GainController = false;
        #endregion Apm
    }
}
