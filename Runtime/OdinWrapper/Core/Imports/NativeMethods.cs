using OdinNative.Core.Handles;
using OdinNative.Wrapper;
using System;
using System.Diagnostics;
using static OdinNative.Core.Imports.NativeBindings;

namespace OdinNative.Core.Imports
{
    public class NativeMethods<T> where T : OdinHandle
    {
        readonly T Handle;

        public NativeMethods(T handle)
        {
            Handle = handle;
        }

        internal void LoadMethod<U>(string name, out U function) => this.Handle.GetLibraryMethod(name, out function);

        protected struct LockObject : IDisposable
        {
            private T Handle;

            public LockObject(T handle)
            {
                Handle = handle;
                bool success = false;
                Handle.DangerousAddRef(ref success);
                if (success == false)
                    throw new ObjectDisposedException(typeof(T).FullName);
            }
            void IDisposable.Dispose()
            {
                Handle.DangerousRelease();
            }
        }

        protected LockObject Lock
        {
            get { return new LockObject(Handle); }
        }

        public bool IsError(OdinError error)
        {
            return (error > OdinError.ODIN_ERROR_SUCCESS);
        }

#pragma warning disable IDE1006
#if ODIN_DEBUG
        internal static bool _NativeDebug = true;
#else
        internal static bool _NativeDebug = false; /* OdinDefaults.Debug; */
#endif
        [Conditional("ODIN_DEBUG")]
        protected static void _DbgTrace()
        {
            if (_NativeDebug)
                Debug.WriteLine(Environment.StackTrace);
        }
#pragma warning restore IDE1006
    }
}
