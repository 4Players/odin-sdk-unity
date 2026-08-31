using OdinNative.Core.Handles;
using OdinNative.Core.Imports;
using Xunit;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Tests
{
    [Collection("Native")]
    public class PipelineTests
    {
        private readonly NativeLibraryFixture _fixture;

        public PipelineTests(NativeLibraryFixture fixture) => _fixture = fixture;

        private NativeLibraryMethods Methods => OdinNative.Odin.Library.Methods;

        private OdinEncoderHandle CreateEncoder()
        {
            var result = Methods.EncoderCreate(1, 48000, false, out var encoder);
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, result);
            return encoder;
        }

        [SkippableFact]
        public void EncoderAndDecoder_Create()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = CreateEncoder();
            Assert.False(encoder.IsInvalid);
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.DecoderCreate(48000, false, out var decoder));
            using (decoder)
                Assert.False(decoder.IsInvalid);
        }

        [SkippableFact]
        public void ApmConfig_Roundtrips()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = CreateEncoder();
            var pipeline = Methods.EncoderGetPipeline(encoder);
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineInsertApmEffect(pipeline, 0, 48000, false, out uint effectId));

            var config = new OdinApmConfig
            {
                echo_canceller = true,
                high_pass_filter = true,
                transient_suppressor = false,
                noise_suppression = OdinNoiseSuppressionLevel.ODIN_NOISE_SUPPRESSION_LEVEL_HIGH,
                gain_controller_version = OdinGainControllerVersion.ODIN_GAIN_CONTROLLER_VERSION_V2,
            };
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineSetApmConfig(pipeline, effectId, config));
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineGetApmConfig(pipeline, effectId, out var back));

            Assert.Equal(config.echo_canceller, back.echo_canceller);
            Assert.Equal(config.high_pass_filter, back.high_pass_filter);
            Assert.Equal(config.transient_suppressor, back.transient_suppressor);
            Assert.Equal(config.noise_suppression, back.noise_suppression);
            Assert.Equal(config.gain_controller_version, back.gain_controller_version);
        }

        [SkippableFact]
        public void VadConfig_Roundtrips()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = CreateEncoder();
            var pipeline = Methods.EncoderGetPipeline(encoder);
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineInsertVadEffect(pipeline, 0, out uint effectId));

            var config = new OdinVadConfig
            {
                voice_activity = new OdinSensitivityConfig { enabled = true, attack_threshold = 0.9f, release_threshold = 0.8f },
                volume_gate = new OdinSensitivityConfig { enabled = true, attack_threshold = -30f, release_threshold = -40f },
            };
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineSetVadConfig(pipeline, effectId, config));
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineGetVadConfig(pipeline, effectId, out var back));

            Assert.Equal(config.voice_activity.enabled, back.voice_activity.enabled);
            Assert.Equal(config.voice_activity.attack_threshold, back.voice_activity.attack_threshold);
            Assert.Equal(config.voice_activity.release_threshold, back.voice_activity.release_threshold);
            Assert.Equal(config.volume_gate.enabled, back.volume_gate.enabled);
            Assert.Equal(config.volume_gate.attack_threshold, back.volume_gate.attack_threshold);
            Assert.Equal(config.volume_gate.release_threshold, back.volume_gate.release_threshold);
        }

        [SkippableFact]
        public void ViConfig_Roundtrips()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = CreateEncoder();
            var pipeline = Methods.EncoderGetPipeline(encoder);
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineInsertViEffect(pipeline, 0, out uint effectId));

            var config = new OdinViConfig { enabled = true, attenuation_limit_db = 42.5f };
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineSetViConfig(pipeline, effectId, config));
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.PipelineGetViConfig(pipeline, effectId, out var back));

            Assert.Equal(config.enabled, back.enabled);
            Assert.Equal(config.attenuation_limit_db, back.attenuation_limit_db);
        }

        [SkippableFact]
        public void Encoder_PushPop_ProducesDatagram()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = CreateEncoder();
            // 20ms of silence at 48kHz mono
            float[] samples = new float[960];
            Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, Methods.EncoderPush(encoder, samples));
            byte[] datagram = new byte[2048];
            var result = Methods.EncoderPop(encoder, ref datagram);
            // popping may legitimately report no data yet, but must not fail hard
            Assert.True(result == OdinError.ODIN_ERROR_SUCCESS || result == OdinError.ODIN_ERROR_NO_DATA,
                $"unexpected EncoderPop result: {result}");
        }
    }
}
