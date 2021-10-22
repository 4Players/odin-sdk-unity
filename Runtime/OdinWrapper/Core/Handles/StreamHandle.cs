using OdinNative.Core.Imports;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core.Handles
{
    class StreamHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(StreamHandle handle) => handle?.DangerousGetHandle() ?? IntPtr.Zero;
        internal NativeMethods.OdinMediaStreamDestroyDelegate Free;
        private bool OwnsHandle;

        internal StreamHandle(IntPtr handle, NativeMethods.OdinMediaStreamDestroyDelegate mediaDestroyDelegate)
            : base(true)
        {
            Free = mediaDestroyDelegate;
            SetHandle(handle);
            OwnsHandle = true;
        }

        internal StreamHandle(IntPtr handle, bool ownsHandle = false)
            : base(ownsHandle)
        {
            OwnsHandle = ownsHandle;
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            bool result = true;
            try
            {
                if (OwnsHandle)
                    Free(handle);
                else
                    SetHandleAsInvalid();
            }
            catch
            {
                result = false;
            }

            return result;
        }
    }
}
