using System.Collections;
using System.Collections.Generic;
using System;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core
{
    /// <summary>
    /// Audio processing configuration of an ODIN room
    /// </summary>
    public class OdinRoomConfig : IOdinApmConfig
    {
        /// <summary>
        /// Enables or disables voice activity detection (VAD)
        /// </summary>
        public bool VoiceActivityDetection
        {
            get { return ApmConfig.voice_activity_detection; }
            set { ApmConfig.voice_activity_detection = value; }
        }

        /// <summary>
        /// Voice probability value when the VAD should engage.
        /// </summary>
        public float VoiceActivityDetectionAttackProbability
        {
            get { return ApmConfig.voice_activity_detection_attack_probability; }
            set { ApmConfig.voice_activity_detection_attack_probability = value; }
        }

        /// <summary>
        /// Voice probability value when the VAD should disengage after previously being engaged.
        /// </summary>
        public float VoiceActivityDetectionReleaseProbability
        {
            get { return ApmConfig.voice_activity_detection_release_probability; }
            set { ApmConfig.voice_activity_detection_release_probability = value; }
        }

        /// <summary>
        /// Enables or disables volume gate
        /// </summary>
        public bool VolumeGate
        {
            get { return ApmConfig.volume_gate; }
            set { ApmConfig.volume_gate = value; }
        }

        /// <summary>
        /// Root mean square power (dBFS) when the volume gate should engage.
        /// </summary>
        public float VolumeGateAttackLoudness
        {
            get { return ApmConfig.volume_gate_attack_loudness; }
            set { ApmConfig.volume_gate_attack_loudness = value; }
        }

        /// <summary>
        /// Root mean square power (dBFS) when the volume gate should disengage after previously being engaged.
        /// </summary>
        public float VolumeGateReleaseLoudness
        {
            get { return ApmConfig.volume_gate_release_loudness; }
            set { ApmConfig.volume_gate_release_loudness = value; }
        }

        /// <summary>
        /// Enable or disable echo cancellation
        /// </summary>
        public bool EchoCanceller
        {
            get { return ApmConfig.echo_canceller; }
            set { ApmConfig.echo_canceller = value; }
        }

        /// <summary>
        /// Enable or disable high pass filtering
        /// </summary>
        public bool HighPassFilter
        {
            get { return ApmConfig.high_pass_filter; }
            set { ApmConfig.high_pass_filter = value; }
        }

        /// <summary>
        /// Enable or disable the pre amplifier
        /// </summary>
        /// <remarks>
        /// The native ODIN APM has no pre amplifier since 1.7.0. The value is kept for source
        /// compatibility and no longer reaches the native library.
        /// </remarks>
        [Obsolete("The native APM has no pre amplifier. Setting this has no effect.")]
        public bool PreAmplifier { get; set; }

        /// <summary>
        /// Set the aggressiveness of the suppression
        /// </summary>
        public OdinNoiseSuppressionLevel NoiseSuppressionLevel
        {
            get { return ApmConfig.noise_suppression_level; }
            set { ApmConfig.noise_suppression_level = value; }
        }
        /// <summary>
        /// Enable or disable the transient suppressor
        /// </summary>
        public bool TransientSuppressor
        {
            get { return ApmConfig.transient_suppressor; }
            set { ApmConfig.transient_suppressor = value; }
        }
        /// <summary>
        /// Enable or disable the gain controller
        /// </summary>
        /// <remarks>
        /// Convenience wrapper around <see cref="GainControllerVersion"/>. Enabling selects
        /// <see cref="OdinGainControllerVersion.V2"/>, the recommended version.
        /// </remarks>
        public bool GainController
        {
            get { return ApmConfig.gain_controller_version != OdinGainControllerVersion.None; }
            set { ApmConfig.gain_controller_version = value ? OdinGainControllerVersion.V2 : OdinGainControllerVersion.None; }
        }

        /// <summary>
        /// Set the version of the gain controller (AGC) to use
        /// </summary>
        public OdinGainControllerVersion GainControllerVersion
        {
            get { return ApmConfig.gain_controller_version; }
            set { ApmConfig.gain_controller_version = value; }
        }

        internal bool RemoteConfig { get; private set; }

        internal OdinApmConfig GetOdinApmConfig() => ApmConfig;

        private OdinApmConfig ApmConfig = new OdinApmConfig();

        private OdinRoomConfig(OdinApmConfig config) : this(config.voice_activity_detection, config.voice_activity_detection_attack_probability, config.voice_activity_detection_release_probability, config.volume_gate, config.volume_gate_attack_loudness, config.volume_gate_release_loudness, config.echo_canceller, config.high_pass_filter, false, config.noise_suppression_level, config.transient_suppressor, config.gain_controller_version, true) { }
        /// <summary>
        /// Audio processing configuration of an ODIN room
        /// </summary>
        /// <param name="voiceActivityDetection">Enables or disables voice activity detection (VAD)</param>
        /// <param name="voiceActivityDetectionAttackProbability">Voice probability value when the VAD should engage.</param>
        /// <param name="voiceActivityDetectionReleaseProbability">Voice probability value when the VAD should disengage after previously being engaged.</param>
        /// <param name="volumeGate">Enables or disables volume gate</param>
        /// <param name="volumeGateAttackLoudness">Root mean square power (dBFS) when the volume gate should engage.</param>
        /// <param name="volumeGateReleaseLoudness">Root mean square power (dBFS) when the volume gate should disengage after previously being engaged.</param>
        /// <param name="echoCanceller">Enable or disable echo cancellation</param>
        /// <param name="highPassFilter">Enable or disable high pass filtering</param>
        /// <param name="preAmplifier">Enable or disable the pre amplifier</param>
        /// <param name="noiseSuppressionLevel">Set the aggressiveness of the suppression</param>
        /// <param name="transientSuppressor">Enable or disable the transient suppressor</param>
        /// <param name="gainController">Enable or disable the gain controller</param>
        public OdinRoomConfig(
            bool voiceActivityDetection = false,
            float voiceActivityDetectionAttackProbability = 0f,
            float voiceActivityDetectionReleaseProbability = 0f,
            bool volumeGate = false,
            float volumeGateAttackLoudness = 0,
            float volumeGateReleaseLoudness = 0,
            bool echoCanceller = false,
            bool highPassFilter = false,
            bool preAmplifier = false,
            OdinNoiseSuppressionLevel noiseSuppressionLevel = OdinNoiseSuppressionLevel.None,
            bool transientSuppressor = false,
            bool gainController = false)
            : this(voiceActivityDetection, voiceActivityDetectionAttackProbability, voiceActivityDetectionReleaseProbability, volumeGate, volumeGateAttackLoudness, volumeGateReleaseLoudness, echoCanceller, highPassFilter, preAmplifier, noiseSuppressionLevel, transientSuppressor, gainController ? OdinGainControllerVersion.V2 : OdinGainControllerVersion.None, false)
        {
        }
        /// <summary>
        /// Audio processing configuration of an ODIN room
        /// </summary>
        /// <param name="odinApm">Interface for Audio processing configuration of an ODIN room</param>
        public OdinRoomConfig(IOdinApmConfig odinApm) : this(odinApm.VoiceActivityDetection, odinApm.VoiceActivityDetectionAttackProbability, odinApm.VoiceActivityDetectionReleaseProbability, odinApm.VolumeGate, odinApm.VolumeGateAttackLoudness, odinApm.VolumeGateReleaseLoudness, odinApm.EchoCanceller, odinApm.HighPassFilter, odinApm.PreAmplifier, odinApm.NoiseSuppressionLevel, odinApm.TransientSuppressor, odinApm.GainController ? OdinGainControllerVersion.V2 : OdinGainControllerVersion.None) { }
        internal OdinRoomConfig(bool voiceActivityDetection, float voiceActivityDetectionAttackProbability, float voiceActivityDetectionReleaseProbability, bool volumeGate, float volumeGateAttackLoudness,
            float volumeGateReleaseLoudness, bool echoCanceller, bool highPassFilter, bool preAmplifier, OdinNoiseSuppressionLevel noiseSuppressionLevel, bool transientSuppressor, OdinGainControllerVersion gainControllerVersion, bool remote = false)
        {
            VoiceActivityDetection = voiceActivityDetection;
            VoiceActivityDetectionAttackProbability = voiceActivityDetectionAttackProbability;
            VoiceActivityDetectionReleaseProbability = voiceActivityDetectionReleaseProbability;
            VolumeGate = volumeGate;
            VolumeGateAttackLoudness = volumeGateAttackLoudness;
            VolumeGateReleaseLoudness = volumeGateReleaseLoudness;
            EchoCanceller = echoCanceller;
            HighPassFilter = highPassFilter;
#pragma warning disable CS0618 // kept for source compatibility
            PreAmplifier = preAmplifier;
#pragma warning restore CS0618
            NoiseSuppressionLevel = noiseSuppressionLevel;
            TransientSuppressor = transientSuppressor;
            GainControllerVersion = gainControllerVersion;
            RemoteConfig = remote;
        }

        /// <summary>
        /// Creates Apm OdinRoomConfig based on <see cref="OdinNative.Odin.OdinDefaults"/>
        /// </summary>
        /// <returns>default Odin Apm Room Config</returns>
        public static OdinRoomConfig GetOdinDefault()
        {
            return new OdinRoomConfig(
                Odin.OdinDefaults.VoiceActivityDetection,
                Odin.OdinDefaults.VoiceActivityDetectionAttackProbability,
                Odin.OdinDefaults.VoiceActivityDetectionReleaseProbability,
                Odin.OdinDefaults.VolumeGate,
                Odin.OdinDefaults.VolumeGateAttackLoudness,
                Odin.OdinDefaults.VolumeGateReleaseLoudness,
                Odin.OdinDefaults.EchoCanceller,
                Odin.OdinDefaults.HighPassFilter,
                Odin.OdinDefaults.PreAmplifier,
                Odin.OdinDefaults.NoiseSuppressionLevel,
                Odin.OdinDefaults.TransientSuppressor,
                Odin.OdinDefaults.GainControllerVersion);
        }

        /// <summary>
        /// Debug
        /// </summary>
        /// <returns>info</returns>
        public override string ToString()
        {
            return $"{nameof(OdinRoomConfig)}:" +
                $" {nameof(VoiceActivityDetection)} {VoiceActivityDetection}" +
                $", {nameof(VoiceActivityDetectionAttackProbability)} {VoiceActivityDetectionAttackProbability}" +
                $", {nameof(VoiceActivityDetectionReleaseProbability)} {VoiceActivityDetectionReleaseProbability}" +
                $", {nameof(VolumeGate)} {VolumeGate}" +
                $", {nameof(VolumeGateAttackLoudness)} {VolumeGateAttackLoudness}" +
                $", {nameof(VolumeGateReleaseLoudness)} {VolumeGateReleaseLoudness}" +
                $", {nameof(EchoCanceller)} {EchoCanceller}" +
                $", {nameof(HighPassFilter)} {HighPassFilter}" +
#pragma warning disable CS0618 // kept for source compatibility
                $", {nameof(PreAmplifier)} {PreAmplifier}" +
#pragma warning restore CS0618
                $", {nameof(NoiseSuppressionLevel)} {Enum.GetName(typeof(OdinNoiseSuppressionLevel), NoiseSuppressionLevel)}" +
                $", {nameof(TransientSuppressor)} {TransientSuppressor}" +
                $", {nameof(GainControllerVersion)} {GainControllerVersion}" +
                $", {nameof(RemoteConfig)} {RemoteConfig}";
        }
    }
}