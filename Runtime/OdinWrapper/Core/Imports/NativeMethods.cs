using OdinNative.Core.Handles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core.Imports
{
    internal class NativeMethods
    {
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinStartupDelegate();
        readonly OdinStartupDelegate _OdinStartup;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinShutdownDelegate();
        readonly OdinShutdownDelegate _OdinShutdown;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinGenerateAccessKeyDelegate([In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinGenerateAccessKeyDelegate _OdinAccessKeyGenerate;

        #region Token Generator
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinTokenGeneratorCreateDelegate(string accessKey);
        readonly OdinTokenGeneratorCreateDelegate _OdinTokenGeneratorCreate;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinTokenGeneratorDestroyDelegate(IntPtr tokenGenerator);
        readonly OdinTokenGeneratorDestroyDelegate _OdinTokenGeneratorDestroy;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinTokenGeneratorCreateTokenDelegate(IntPtr tokenGenerator, string roomId, string userId, OdinTokenOptions options, [In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinTokenGeneratorCreateTokenDelegate _OdinTokenGeneratorCreateToken;
        #endregion

        #region Room
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinRoomConfigureApmDelegate(IntPtr room, NativeBindings.OdinApmConfig apmConfig);
        readonly OdinRoomConfigureApmDelegate _OdinRoomConfigureApm;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinRoomCreateDelegate();
        readonly OdinRoomCreateDelegate _OdinRoomCreate;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate void OdinRoomDestroyDelegate(IntPtr room);
        readonly OdinRoomDestroyDelegate _OdinRoomDestroy;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinRoomJoinDelegate(IntPtr room, string gatewayUrl, string roomToken, byte[] userData, ulong userDataLength);
        readonly OdinRoomJoinDelegate _OdinRoomJoin;
        internal delegate uint OdinRoomJoinDirectDelegate(IntPtr room, string url, string roomId, byte[] userData, ulong userDataLength);
        readonly OdinRoomJoinDirectDelegate _OdinRoomJoinDirect;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinRoomAddMediaDelegate(IntPtr room, IntPtr mediaStream);
        readonly OdinRoomAddMediaDelegate _OdinRoomAddMedia;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinRoomUpdateUserDataDelegate(IntPtr room, byte[] userData, ulong userDataLength);
        readonly OdinRoomUpdateUserDataDelegate _OdinRoomUpdateUserData;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinRoomConfigureAudioOutputDelegate(IntPtr room, IntPtr config);
        readonly OdinRoomConfigureAudioOutputDelegate _OdinRoomConfigureAudioOutput;

        public delegate void OdinEventCallback(IntPtr room, NativeBindings.OdinEvent odinEvent, IntPtr userDataPtr);
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinRoomSetEventCallbackDelegate(IntPtr room, OdinEventCallback callback);
        readonly OdinRoomSetEventCallbackDelegate _OdinRoomSetEventCallback;
        #endregion Room

        #region Media
        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinVideoStreamCreateDelegate();
        readonly OdinVideoStreamCreateDelegate _OdinVideoStreamCreate;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinAudioStreamCreateDelegate(NativeBindings.OdinAudioStreamConfig config);
        readonly OdinAudioStreamCreateDelegate _OdinAudioStreamCreate;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinMediaStreamDestroyDelegate(IntPtr mediaStream);
        readonly OdinMediaStreamDestroyDelegate _OdinMediaStreamDestroy;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate IntPtr OdinMediaStreamTypeDelegate(ref IntPtr mediaStream);
        readonly OdinMediaStreamTypeDelegate _OdinMediaStreamType;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinAudioPushDataDelegate(IntPtr mediaStream, [In] float[] buffer, [In] int bufferLength);
        readonly OdinAudioPushDataDelegate _OdinAudioPushData;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinAudioDataLenDelegate(IntPtr mediaStream);
        readonly OdinAudioDataLenDelegate _OdinAudioDataLen;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinAudioReadDataDelegate(IntPtr mediaStream, [In, Out][MarshalAs(UnmanagedType.LPArray)] float[] buffer, [In] int bufferLength);
        readonly OdinAudioReadDataDelegate _OdinAudioReadData;
        #endregion Media

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate uint OdinErrorFormatDelegate(uint error, [In, Out][MarshalAs(UnmanagedType.SysUInt)] IntPtr buffer, [In] int bufferLength);
        readonly OdinErrorFormatDelegate _OdinErrorFormat;

        [UnmanagedFunctionPointer(Native.OdinCallingConvention)]
        internal delegate bool OdinIsErrorDelegate(uint error);
        readonly OdinIsErrorDelegate _OdinIsError;

        readonly OdinHandle Handle;

        public NativeMethods(OdinHandle handle)
        {
            Handle = handle;

            handle.GetLibraryMethod("odin_startup", out _OdinStartup);
            handle.GetLibraryMethod("odin_shutdown", out _OdinShutdown);
            handle.GetLibraryMethod("odin_room_create", out _OdinRoomCreate);
            handle.GetLibraryMethod("odin_room_configure_apm", out _OdinRoomConfigureApm);
            handle.GetLibraryMethod("odin_room_destroy", out _OdinRoomDestroy);
            handle.GetLibraryMethod("odin_room_join", out _OdinRoomJoin);
            handle.GetLibraryMethod("odin_room_join_direct", out _OdinRoomJoinDirect);
            handle.GetLibraryMethod("odin_room_add_media", out _OdinRoomAddMedia);
            handle.GetLibraryMethod("odin_room_update_user_data", out _OdinRoomUpdateUserData);
            handle.GetLibraryMethod("odin_room_set_event_callback", out _OdinRoomSetEventCallback);
            handle.GetLibraryMethod("odin_video_stream_create", out _OdinVideoStreamCreate);
            handle.GetLibraryMethod("odin_audio_stream_create", out _OdinAudioStreamCreate);
            handle.GetLibraryMethod("odin_media_stream_destroy", out _OdinMediaStreamDestroy);
            handle.GetLibraryMethod("odin_media_stream_type", out _OdinMediaStreamType);
            handle.GetLibraryMethod("odin_audio_push_data", out _OdinAudioPushData);
            handle.GetLibraryMethod("odin_audio_data_len", out _OdinAudioDataLen);
            handle.GetLibraryMethod("odin_audio_read_data", out _OdinAudioReadData);
            handle.GetLibraryMethod("odin_error_format", out _OdinErrorFormat);
            handle.GetLibraryMethod("odin_access_key_generate", out _OdinAccessKeyGenerate);
            handle.GetLibraryMethod("odin_token_generator_create", out _OdinTokenGeneratorCreate);
            handle.GetLibraryMethod("odin_token_generator_destroy", out _OdinTokenGeneratorDestroy);
            handle.GetLibraryMethod("odin_token_generator_create_token", out _OdinTokenGeneratorCreateToken);
            handle.GetLibraryMethod("odin_is_error", out _OdinIsError);
        }

        private struct LockObject : IDisposable
        {
            private OdinHandle Handle;

            public LockObject(OdinHandle handle)
            {
                Handle = handle;
                bool success = false;
                Handle.DangerousAddRef(ref success);
                if (success == false)
                    throw new ObjectDisposedException(typeof(OdinLibrary).FullName);
            }
            void IDisposable.Dispose()
            {
                Handle.DangerousRelease();
            }
        }

        private LockObject Lock
        {
            get { return new LockObject(Handle); }
        }

        private void CheckAndThrow(uint error, string message = null)
        {
            if (Check(error))
#if !UNITY_STANDALONE && !UNITY_EDITOR && !ENABLE_IL2CPP && !ENABLE_MONO
                throw OdinLibrary.CreateException(error, message);
#else
                UnityEngine.Debug.LogException(OdinLibrary.CreateException(error, message));
#endif
        }

        private bool Check(uint error)
        {
            return IsError(error);
        }

        /// <summary>
        /// Provides a readable representation for a test key
        /// </summary>
        /// <param name="bufferSize">max string buffer size</param>
        /// <returns>Test Key</returns>
        internal string GenerateKey(int bufferSize = 128)
        {
            _keyPointer = Marshal.AllocHGlobal(bufferSize);
            uint size = _OdinAccessKeyGenerate(_keyPointer, bufferSize);
            if (InternalIsError(size))
            {
                Marshal.FreeHGlobal(_keyPointer);
                return string.Empty;
            }
            byte[] buffer = new byte[size];
            Marshal.Copy(_keyPointer, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(_keyPointer);
            return Encoding.UTF8.GetString(buffer);
        }
        private static IntPtr _keyPointer;

        /// <summary>
        /// Provides a readable representation from the error code of ErrorFormat
        /// </summary>
        /// <param name="error">string buffer</param>
        /// <param name="bufferSize">max string buffer size</param>
        /// <returns>Error message</returns>
        internal string GetErrorMessage(uint error, int bufferSize = 1024)
        {
            _stringPointer = Marshal.AllocHGlobal(bufferSize);
            uint size = ErrorFormat(error, _stringPointer, bufferSize);
            byte[] buffer = new byte[size];
            Marshal.Copy(_stringPointer, buffer, 0, buffer.Length);
            Marshal.FreeHGlobal(_stringPointer);
            return Encoding.UTF8.GetString(buffer);
        }
        private static IntPtr _stringPointer;

        /// <summary>
        /// Starts native runtime threads.
        /// <list type="table">
        /// <listheader><term>OdinRoom</term><description>RoomHandle for medias and events</description></listheader>
        /// <item>Create <description><see cref="RoomCreate"/></description></item>
        /// <item>Destroy <description><see cref="RoomDestroy"/></description></item>
        /// <item></item>
        /// <listheader><term>OdinMediaStream</term><description>StreamHandle for audio and video</description></listheader>
        /// <item>Create <description><see cref="AudioStreamCreate"/></description></item>
        /// <item>Destroy <description><see cref="MediaStreamDestroy"/></description></item>
        /// </list>
        /// </summary>
        /// <remarks>Stop with <see cref="Shutdown"/></remarks>
        public void Startup()
        {
            using (Lock)
                _OdinStartup();
        }

        /// <summary>
        /// Stops native runtime threads that are started with <see cref="Startup"/>
        /// </summary>
        public void Shutdown()
        {
            using (Lock)
                _OdinShutdown();
        }

        /// <summary>
        /// Create room object representation
        /// </summary>
        /// <returns><see cref="RoomHandle"/> always owns the <see cref="IntPtr"/> handle</returns>
        public RoomHandle RoomCreate()
        {
            using (Lock)
            {
                IntPtr handle = _OdinRoomCreate();
                return new RoomHandle(handle, _OdinRoomDestroy);
            }
        }

        /// <summary>
        /// Set OdinRoomConfig <see cref="NativeBindings.OdinApmConfig"/> in the <see cref="RoomCreate"/> provided room
        /// </summary>
        /// <remarks>currently only returns 0</remarks>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="config"><see cref="OdinRoomConfig"/></param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public uint RoomConfigure(RoomHandle room, OdinRoomConfig config)
        {
            using (Lock)
            {
                uint error = _OdinRoomConfigureApm(room, config);
                CheckAndThrow(error);
                return error;
            }
        }

        /// <summary>
        /// Free the allocated room object
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        public void RoomDestroy(RoomHandle room)
        {
            using (Lock)
                _OdinRoomDestroy(room);
        }

        /// <summary>
        /// Connect and join room on the gateway returned provided server
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="gatewayUrl">*const c_char</param>
        /// <param name="roomToken">*const c_char</param>
        /// <param name="userData">*const u8</param>
        /// <param name="userDataLength">usize</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public uint RoomJoin(RoomHandle room, string gatewayUrl, string roomToken, byte[] userData, int userDataLength)
        {
            using (Lock)
            {
                uint error = _OdinRoomJoin(room, gatewayUrl, roomToken, userData, (ulong)userDataLength);
                CheckAndThrow(error);
                return error;
            }
        }

        /// <summary>
        /// Connect and join room on the url provided server
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="url">*const c_char</param>
        /// <param name="roomId">*const c_char</param>
        /// <param name="userData">*const u8</param>
        /// <param name="userDataLength">usize</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public uint RoomJoin(RoomHandle room, string url, string roomId, string customerId, byte[] userData, int userDataLength)
        {
            using (Lock)
            {
                uint error = _OdinRoomJoinDirect(room, url, roomId, userData, (ulong)userDataLength);
                CheckAndThrow(error);
                return error;
            }
        }

        /// <summary>
        /// Add a <see cref="MediaStream"/> in the <see cref="RoomCreate"/> provided room.
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="mediaStream">*mut <see cref="MediaStream"/></param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public uint RoomAddMedia(RoomHandle room, StreamHandle stream)
        {
            using (Lock)
            {
                uint error = _OdinRoomAddMedia(room, stream);
                CheckAndThrow(error);
                return error;
            }
        }

        /// <summary>
        /// Update own Userdata
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="userData">*const u8</param>
        /// <param name="userDataLength">usize</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public uint RoomUpdateUserData(RoomHandle room, byte[] userData, ulong userDataLength)
        {
            using (Lock)
            {
                uint error = _OdinRoomUpdateUserData(room, userData, userDataLength);
                CheckAndThrow(error);
                return error;
            }
        }

        /// <summary>
        /// Register a <see cref="OdinEventCallback"/> for all room events in the <see cref="RoomCreate"/> provided room.
        /// </summary>
        /// <param name="room">*mut OdinRoom</param>
        /// <param name="callback">extern "C" fn(event: *const <see cref="NativeBindings.AkiEvent"/>) -> ()</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public uint RoomSetEventCallback(RoomHandle room, OdinEventCallback callback)
        {
            using (Lock)
            {
                uint error = _OdinRoomSetEventCallback(room, callback);
                CheckAndThrow(error);
                return error;
            }
        }

        /// <summary>
        /// NotSupported 
        /// </summary>
        /// <returns><see cref="MediaStream"/> *</returns>
        internal IntPtr VideoStreamCreate()
        {
            using (Lock)
                return _OdinVideoStreamCreate();
        }

        /// <summary>
        /// Creates a native <see cref="StreamHandle"/>. Can only be destroyed with  
        /// <see cref="MediaStreamDestroy"/>
        /// </summary>
        /// <param name="config"><see cref="OdinMediaConfig"/></param>
        /// <returns><see cref="StreamHandle"/> * as <see cref="IntPtr"/> so <see cref="StreamHandle"/> can own the handle</returns>
        public StreamHandle AudioStreamCreate(OdinMediaConfig config)
        {
            using (Lock)
            {
                IntPtr handle = _OdinAudioStreamCreate(config);
                return new StreamHandle(handle, _OdinMediaStreamDestroy);
            }
        }

        /// <summary>
        /// Destroy a native <see cref="StreamHandle"/> that is created before with <see cref="AudioStreamCreate"/>.
        /// </summary>
        /// <remarks> Should not be called on remote streams from <see cref="NativeBindings.AkiEvent"/>.</remarks>
        /// <param name="mediaStream"><see cref="StreamHandle"/> *</param>
        public void MediaStreamDestroy(StreamHandle handle)
        {
            using (Lock)
                _OdinMediaStreamDestroy(handle);
        }

        /// <summary>
        /// Sends the buffer data to Odin.
        /// </summary>
        /// <param name="mediaStream">OdinMediaStream *</param>
        /// <param name="buffer">allocated buffer to read from</param>
        /// <param name="bufferLength">size of the buffer</param>
        /// <returns>0 or error code that is readable with <see cref="ErrorFormat"/></returns>
        public uint AudioPushData(StreamHandle mediaStream, float[] buffer, int bufferLength)
        {
            using (Lock)
            {
                uint error = _OdinAudioPushData(mediaStream, buffer, bufferLength);
                //CheckAndThrow(error);
                return error;
            }
        }

        /// <summary>
        /// Get available audio data size.
        /// </summary>
        /// <param name="mediaStream">OdinMediaStream *</param>
        /// <returns>floats available to read with <see cref="AudioReadData"/></returns>
        public uint AudioDataLength(StreamHandle mediaStream)
        {
            using (Lock)
                return _OdinAudioDataLen(mediaStream);
        }

        /// <summary>
        /// Reads data into the buffer.
        /// </summary>
        /// <remarks>writes only audio data into the buffer even if the buffer size exceeded the available data</remarks>
        /// <param name="mediaStream">OdinMediaStream *</param>
        /// <param name="buffer">allocated buffer to write to</param>
        /// <param name="bufferLength">size of the buffer</param>
        /// <returns>count of written data</returns>
        public uint AudioReadData(StreamHandle mediaStream, [In, Out] float[] buffer, int bufferLength)
        {
            using (Lock)
                return _OdinAudioReadData(mediaStream, buffer, bufferLength);
        }

        /// <summary>
        /// Allocate TokenGenerator
        /// </summary>
        /// <param name="accessKey">*const c_char</param>
        /// <returns><see cref="TokenGeneratorHandle"/> always owns the <see cref="IntPtr"/> handle</returns>
        public TokenGeneratorHandle TokenGeneratorCreate(string accessKey)
        {
            using (Lock)
            {
                IntPtr handle = _OdinTokenGeneratorCreate(accessKey);
                return new TokenGeneratorHandle(handle, _OdinTokenGeneratorDestroy);
            }
        }

        /// <summary>
        /// Free the allocated TokenGenerator
        /// </summary>
        /// <param name="room">*mut OdinTokenGenerator</param>
        public void TokenGeneratorDestroy(TokenGeneratorHandle tokenGenerator)
        {
            using (Lock)
                _OdinTokenGeneratorDestroy(tokenGenerator);
        }

        /// <summary>
        /// Creat room token
        /// </summary>
        /// <param name="tokenGenerator">allocated TokenGenerator</param>
        /// <param name="roomId">*const c_char</param>
        /// <param name="userId">*const c_char</param>
        /// <param name="options"></param>
        /// <param name="buffer">*mut c_char</param>
        /// <param name="bufferLength">size *mut</param>
        /// <returns>Token or empty string</returns>
        public string TokenGeneratorCreateToken(TokenGeneratorHandle tokenGenerator, string roomId, string userId, OdinTokenOptions options, int bufferLength = 512)
        {
            using (Lock)
            {
                _tokenPointer = Marshal.AllocHGlobal(bufferLength);
                uint size = _OdinTokenGeneratorCreateToken(tokenGenerator, roomId, userId, options, _tokenPointer, bufferLength);
                if (InternalIsError(size))
                {
                    Marshal.FreeHGlobal(_tokenPointer);
                    return string.Empty;
                }
                byte[] token = new byte[size];
                Marshal.Copy(_tokenPointer, token, 0, token.Length);
                Marshal.FreeHGlobal(_tokenPointer);
                return Encoding.UTF8.GetString(token);
            }
        }
        private static IntPtr _tokenPointer;

        /// <summary>
        /// Writes a readable string representation of the error in a buffer.
        /// </summary>
        /// <param name="error">error code</param>
        /// <param name="buffer">String buffer pointer (e.g read with <see cref="Marshal.PtrToStringAnsi"/>)</param>
        /// <param name="bufferLength">String buffer length</param>
        /// <returns>0 or error code that is readable with <see cref="GetErrorMessage"/></returns>
        internal uint ErrorFormat(uint error, IntPtr buffer, int bufferLength)
        {
            using (Lock)
                return _OdinErrorFormat(error, buffer, bufferLength);
        }

        /// <summary>
        /// Check if the error code is in range of errors.
        /// </summary>
        /// <remarks>Code <see cref="Utility.OK"/> is never a error and will not be checked</remarks>
        /// <param name="error">error code</param>
        /// <returns>true if error</returns>
        internal bool IsError(uint error)
        {
            if (error == Utility.OK) return false;

            using (Lock)
                return _OdinIsError(error);
        }

        /// <summary>
        /// Local check if the error code is in range of errors.
        /// </summary>
        /// <param name="error">error code</param>
        /// <returns>true if error</returns>
        internal bool InternalIsError(uint error)
        {
            return (error >> 29) > 0;
        }
    }
}
