using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using System.Runtime.InteropServices;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Imports.NativeLibraryMethods;

namespace OdinNative.Wrapper.Media
{
    /// <summary>
    /// Custom effect for <see cref="MediaPipeline"/> callbacks
    /// </summary>
    public class CustomEffect<T> : PiplineEffect where T : unmanaged
    {
        /// <summary>
        /// Internal effect constructor, use <c>CustomEffect.Create{T}(MediaPipeline, Action{OdinCallbackAudioData, T}, T)"</c>
        /// </summary>
        /// <param name="parentHandle">pipeline handle</param>
        /// <param name="effectId">effect id</param>
        protected internal CustomEffect(OdinPipelineHandle parentHandle, uint effectId) : base(parentHandle, effectId)
        {
        }

        /// <summary>
        /// Add a <see cref="CustomEffect{T}"/> to <see cref="PiplineEffect.Parent"/> pipline.
        /// </summary>
        /// <param name="index">effect index in pipeline</param>
        /// <param name="callback">effect event</param>
        /// <param name="userData">Will be passed to <paramref name="callback"/> with <see cref="DeserializeUserdata(byte[])"/></param>
        /// <remarks>Untracked effect for the parent and manually manage with <c>NativeMethods</c></remarks>
        /// <returns>effect id</returns>
        protected virtual uint Insert(uint index, PipelineCallback<T> callback, T userData)
        {
            Utility.Assert(this.Parent.IsAlive, $"{nameof(CustomEffect<T>.Insert)} handle is released");

            OdinCustomEffectCallbackDelegate effectCallbackThunk = (IntPtr bufferPtr, uint samplesCount, ref bool isSilent, IntPtr userdata) =>
            {
                using (OdinArrayf buffer = new OdinArrayf(bufferPtr, 0, (int)samplesCount))
                    callback?.Invoke(buffer, ref isSilent, this.DeserializeUserdata(userdata));
            };

            IntPtr user_data = this.SerializeUserdata(userData, false);

            var result = Odin.Library.Methods.PipelineInsertCustomEffect(this.Parent, index, effectCallbackThunk, user_data, out uint effectId);
            Utility.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertCustomEffect)} in {nameof(CustomEffect<T>.Insert)} failed: {Utility.OdinLastErrorString()} (code {result}) index {index}");

            this.Callback = effectCallbackThunk;
            this.Index = index;
            return this.Id = effectId;
        }

        /// <summary>
        /// Serialize arbitary userdata
        /// </summary>
        /// <param name="value">byte array data of value <see cref="T"/></param>
        /// <returns>byte array representation of userdata value</returns>
        public virtual byte[] SerializeUserdata(T value) => CustomEffect<T>.Serialize(value);
        /// <summary>
        /// Serialize arbitary userdata
        /// </summary>
        /// <param name="value">object data of value <see cref="T"/></param>
        /// <param name="destroy">true to call the DestroyStructure(IntPtr, Type) method.</param>
        /// <remarks>When the memory block already contains data and <paramref name="destroy"/> is <c>false</c> can lead to a memory leak</remarks>
        /// <returns></returns>
        public virtual IntPtr SerializeUserdata(T value, bool destroy = true) => CustomEffect<T>.Serialize(value, destroy);

        /// <summary>
        /// Deserializes userdata from a byte array
        /// </summary>
        /// <param name="data">custom userdata</param>
        /// <returns>Instance of <see cref="CustomEffect{T}"/></returns>
        public virtual T DeserializeUserdata(byte[] data) => CustomEffect<T>.Deserialize(data);
        /// <summary>
        /// Deserializes userdata from a pointer
        /// </summary>
        /// <param name="ptr">pointer</param>
        /// <returns>Instance of <see cref="CustomEffect{T}"/></returns>
        public virtual T DeserializeUserdata(IntPtr ptr) => CustomEffect<T>.Deserialize(ptr);


        /// <summary>
        /// Insert a custom effect in the specified pipline
        /// </summary>
        /// <param name="pipeline">where to create a vad effect</param>
        /// <param name="callback">delegate reference for effect event</param>
        /// <param name="userData">custom userdata Marshal.StructureToPtr</param>
        /// <returns>Instance of <see cref="CustomEffect{T}"/></returns>
        public static CustomEffect<T> Create(MediaPipeline pipeline, PipelineCallback<T> callback, T userData)
        {
            if (pipeline == null) return null;

            OdinCustomEffectCallbackDelegate callbackProxy = (IntPtr samplesPtr, uint samplesCount, ref bool isSilent, IntPtr userdata) =>
            {
                using (OdinArrayf buffer = new OdinArrayf(samplesPtr, 0, (int)samplesCount))
                    callback?.Invoke(buffer, ref isSilent, Deserialize(userdata));
            };

            uint newIndex = pipeline.GetNextIndex();
            IntPtr user_data = Serialize(userData, false);
            return pipeline.InsertCustomEffect<T>(newIndex, callbackProxy, user_data);
        }

        /// <summary>
        /// Serialize structure to pointer for arbitary data <see cref="T"/>
        /// </summary>
        /// <param name="value">data <see cref="T"/> structure</param>
        /// <remarks>copy data for Marshal.StructureToPtr</remarks>
        /// <returns>byte array representation of data structure</returns>
        public static byte[] Serialize(T value)
        {
            int size = Marshal.SizeOf<T>(value);
            byte[] buffer = new byte[size];
            IntPtr valuePtr = IntPtr.Zero;

            try
            {
                valuePtr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr<T>(value, valuePtr, false);
                Marshal.Copy(valuePtr, buffer, 0, size);

                return buffer;
            }
            finally
            {
                if (valuePtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(valuePtr);
            }
        }

        /// <summary>
        /// Serialize structure to pointer for arbitary data <see cref="T"/>
        /// </summary>
        /// <param name="value">data <see cref="T"/> structure</param>
        /// <param name="destroy">free value</param>
        /// <returns>pointer of data structure</returns>
        public static IntPtr Serialize(T value, bool destroy = true)
        {

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>(value));
            try
            {
                Marshal.StructureToPtr<T>(value, ptr, destroy);
                return ptr;
            }
            finally
            {
                if (ptr != IntPtr.Zero && destroy)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Deserializes a structure from a byte array
        /// </summary>
        /// <param name="data">binary data to deserialize</param>
        /// <remarks>copy to Marshal.PtrToStructure</remarks>
        /// <returns>structure <see cref="T"/></returns>
        public static T Deserialize(byte[] data)
        {
            T result = default(T);
            if (data == null || data.Length <= 0) return result;

            var pinnedObject = new GCHandle();
            try
            {
                pinnedObject = GCHandle.Alloc(data, GCHandleType.Pinned);
                result = Marshal.PtrToStructure<T>(pinnedObject.AddrOfPinnedObject());
            }
            finally
            {
                if (pinnedObject.IsAllocated)
                    pinnedObject.Free();
            }

            return result;
        }
        /// <summary>
        /// Deserializes a structure from a pointer
        /// </summary>
        /// <param name="ptr">pointer to deserialize as structure</param>
        /// <returns>structure <see cref="T"/></returns>
        public static T Deserialize(IntPtr ptr) => Marshal.PtrToStructure<T>(ptr);
    }
}