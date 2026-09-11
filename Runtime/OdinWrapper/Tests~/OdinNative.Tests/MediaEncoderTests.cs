using System;
using System.Runtime.InteropServices;
using OdinNative.Core;
using OdinNative.Wrapper;
using Xunit;
using static OdinNative.Core.Imports.NativeBindings;
using ChannelMask = OdinNative.Core.Utility.ChannelMask;

namespace OdinNative.Tests
{
    [Collection("Native")]
    public class MediaEncoderTests
    {
        private const uint SampleRate = 48000;
        private const ChannelMask DefaultChannels = ChannelMask.Channel1 | ChannelMask.Channel2;

        private readonly NativeLibraryFixture _fixture;

        public MediaEncoderTests(NativeLibraryFixture fixture) => _fixture = fixture;

        [SkippableFact]
        public void Create_TransmitsOnDefaultChannels()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = MediaEncoder.Create(1, SampleRate, false);

            Assert.Equal(DefaultChannels, encoder.ChannelMask);
            Assert.Equal(DefaultChannels, TransmittedChannels(encoder));
        }

        [SkippableFact]
        public void ClearPosition_StopsTransmittingOnChannel()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = MediaEncoder.Create(1, SampleRate, false);

            Assert.True(encoder.ClearPosition(ChannelMask.Channel2));

            Assert.Equal(ChannelMask.Channel1, encoder.ChannelMask);
            Assert.Equal(ChannelMask.Channel1, TransmittedChannels(encoder));
        }

        [SkippableFact]
        public void SetPosition_AddsChannel()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = MediaEncoder.Create(1, SampleRate, false);

            encoder.SetPosition(ChannelMask.Channel3, new OdinPosition { X = 0.5f });

            Assert.Equal(DefaultChannels | ChannelMask.Channel3, encoder.ChannelMask);
            Assert.Equal(DefaultChannels | ChannelMask.Channel3, TransmittedChannels(encoder));
        }

        [SkippableFact]
        public void SetChannels_ReplacesChannels()
        {
            _fixture.SkipUnlessLoaded();
            using var encoder = MediaEncoder.Create(1, SampleRate, false);

            encoder.SetChannels(ChannelMask.Channel3);

            Assert.Equal(ChannelMask.Channel3, encoder.ChannelMask);
            Assert.Equal(ChannelMask.Channel3, TransmittedChannels(encoder));
        }

        /// <summary>
        ///     Encodes a tone and decodes the resulting datagrams to read the channels the encoder transmitted on.
        /// </summary>
        private static ChannelMask TransmittedChannels(MediaEncoder encoder)
        {
            using var decoder = MediaDecoder.Create(SampleRate, false);
            float[] frame = new float[SampleRate / 50];
            int pushed = 0;
            for (int f = 0; f < 50; f++)
            {
                for (int i = 0; i < frame.Length; i++, pushed++)
                    frame[i] = 0.5f * (float)Math.Sin(2 * Math.PI * 440 * pushed / SampleRate);
                Assert.Equal(OdinError.ODIN_ERROR_SUCCESS, encoder.Push(frame));

                byte[] buffer = new byte[2048];
                while (encoder.Pop(buffer, out uint count) == OdinError.ODIN_ERROR_SUCCESS && count > 0)
                {
                    GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        Assert.True(decoder.Push(pinned.AddrOfPinnedObject(), (int)count));
                    }
                    finally
                    {
                        pinned.Free();
                    }
                }
            }

            return OdinNative.Odin.Library.Methods.DecoderGetActiveChannels(decoder.Handle);
        }
    }
}
