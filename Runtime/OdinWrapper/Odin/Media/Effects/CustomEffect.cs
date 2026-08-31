using OdinNative.Core;
using OdinNative.Core.Imports;
using System;
using System.Runtime.InteropServices;
using static OdinNative.Core.Imports.NativeBindings;
using static OdinNative.Core.Imports.NativeLibraryMethods;

namespace OdinNative.Wrapper.Media
{
    internal interface IEffectInvoker
    {
        void Invoke(IntPtr samples, uint count, ref bool isSilent);
    }

    /// <summary>
    /// static class: IL2CPP can AOT-compile
    /// </summary>
    internal static class CustomEffectTrampoline 
    {
        internal static readonly OdinCustomEffectCallbackDelegate Delegate = Callback;

#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(OdinCustomEffectCallbackDelegate))]
#endif
        internal static void Callback(IntPtr samples, uint count, [In, Out][MarshalAs(UnmanagedType.I1)] ref bool isSilent, IntPtr userData)
        {
            if (userData == IntPtr.Zero) return;
            (GCHandle.FromIntPtr(userData).Target as IEffectInvoker)?.Invoke(samples, count, ref isSilent);
        }
    }

    /// <summary>
    /// Custom effect for <see cref="MediaPipeline"/> callbacks
    /// </summary>
    public class CustomEffect<T> : PipelineEffect where T : unmanaged
    {
        private GCHandle _contextHandle;

        /// <summary>
        /// PipelineCallback{T} and userData value like UE TOdinCustomEffectUserData so Callback can dispatch into
        /// </summary>
        private sealed class Context : IEffectInvoker
        {
            private readonly PipelineCallback<T> _callback;
            private readonly T _userData;

            public Context(PipelineCallback<T> callback, T userData)
            {
                _callback = callback;
                _userData = userData;
            }

            public void Invoke(IntPtr samples, uint count, ref bool isSilent)
            {
                using (OdinTArray<float> buffer = new OdinTArray<float>(samples, 0, (int)count))
                    _callback?.Invoke(buffer, ref isSilent, _userData);
            }
        }

        /// <summary>
        /// Internal effect constructor, use <c>CustomEffect.Create{T}(MediaPipeline, Action{OdinCallbackAudioData, T}, T)"</c>
        /// </summary>
        /// <param name="parentHandle">pipeline handle</param>
        /// <param name="effectId">effect id</param>
        protected internal CustomEffect(OdinPipelineHandle parentHandle, uint effectId) : base(parentHandle, effectId)
        {
        }

        ~CustomEffect()
        {
            if (_contextHandle.IsAllocated)
                _contextHandle.Free();
        }

        /// <summary>
        /// Add a <see cref="CustomEffect{T}"/> to <see cref="PipelineEffect.Parent"/> pipeline.
        /// </summary>
        /// <param name="index">effect index in pipeline</param>
        /// <param name="callback">effect event</param>
        /// <param name="userData">Will be passed to <paramref name="callback"/> on each invocation</param>
        /// <remarks>Untracked effect for the parent and manually manage with <c>NativeMethods</c></remarks>
        /// <returns>effect id</returns>
        public virtual uint Insert(uint index, PipelineCallback<T> callback, T userData)
        {
            OdinLog.Assert(this.Parent.IsAlive, $"{nameof(CustomEffect<T>.Insert)} handle is released");

            if (_contextHandle.IsAllocated)
                _contextHandle.Free();

            var context = new Context(callback, userData);
            _contextHandle = GCHandle.Alloc(context);

            var result = Odin.Library.Methods.PipelineInsertCustomEffect(this.Parent, index, CustomEffectTrampoline.Delegate, GCHandle.ToIntPtr(_contextHandle), out uint effectId);
            OdinLog.Assert(Utility.IsOk(result), $"{nameof(Odin.Library.Methods.PipelineInsertCustomEffect)} in {nameof(CustomEffect<T>.Insert)}: {Utility.OdinLastErrorString()} (code {result}) index {index}");

            this.Callback = CustomEffectTrampoline.Delegate;
            this.Index = index;
            return this.Id = effectId;
        }

        /// <summary>
        /// Serialize arbitrary userdata
        /// </summary>
        /// <param name="value">byte array data of value <typeparamref name="T"/></param>
        /// <returns>byte array representation of userdata value</returns>
        public virtual byte[] SerializeUserdata(T value) => CustomEffect<T>.Serialize(value);
        /// <summary>
        /// Serialize arbitrary userdata
        /// </summary>
        /// <param name="value">object data of value <typeparamref name="T"/></param>
        /// <param name="destroy">true to call the DestroyStructure(IntPtr, Type) method.</param>
        /// <remarks>When the memory block already contains data and <paramref name="destroy"/> is <c>false</c> can lead to a memory leak</remarks>
        /// <returns></returns>
        public virtual IntPtr SerializeUserdata(T value, bool destroy = true) => CustomEffect<T>.Serialize(value, destroy);

        /// <summary>
        /// Deserializes userdata from a byte array
        /// </summary>
        /// <param name="data">custom userdata</param>
        /// <returns>deserialized userdata value of type <typeparamref name="T"/></returns>
        public virtual T DeserializeUserdata(byte[] data) => CustomEffect<T>.Deserialize(data);
        /// <summary>
        /// Deserializes userdata from a pointer
        /// </summary>
        /// <param name="ptr">pointer</param>
        /// <returns>deserialized userdata value of type <typeparamref name="T"/></returns>
        public virtual T DeserializeUserdata(IntPtr ptr) => CustomEffect<T>.Deserialize(ptr);

        /// <summary>
        /// Insert a custom effect in the specified pipeline
        /// </summary>
        /// <param name="pipeline">where to create a custom effect</param>
        /// <param name="callback">delegate reference for effect event</param>
        /// <param name="userData">custom userdata passed to <paramref name="callback"/> on each invocation</param>
        /// <returns>Instance of <see cref="CustomEffect{T}"/></returns>
        public static CustomEffect<T> Create(MediaPipeline pipeline, PipelineCallback<T> callback, T userData)
        {
            if (pipeline == null) return null;

            var context = new Context(callback, userData);
            GCHandle handle = GCHandle.Alloc(context);

            uint newIndex = pipeline.GetNextIndex();
            var effect = pipeline.InsertCustomEffect<T>(newIndex, CustomEffectTrampoline.Delegate, GCHandle.ToIntPtr(handle));
            if (effect != null)
                effect._contextHandle = handle;
            else
                handle.Free();
            return effect;
        }

        /// <summary>
        /// Serialize structure to pointer for arbitrary data <typeparamref name="T"/>
        /// </summary>
        /// <param name="value">data <typeparamref name="T"/> structure</param>
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
        /// Serialize structure to pointer for arbitrary data <typeparamref name="T"/>
        /// </summary>
        /// <param name="value">data <typeparamref name="T"/> structure</param>
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
        /// <returns>structure <typeparamref name="T"/></returns>
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
        /// <returns>structure <typeparamref name="T"/></returns>
        public static T Deserialize(IntPtr ptr) => Marshal.PtrToStructure<T>(ptr);
    }
}
