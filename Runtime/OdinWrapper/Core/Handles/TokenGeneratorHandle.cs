using OdinNative.Core.Imports;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core.Handles
{
    class TokenGeneratorHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static implicit operator IntPtr(TokenGeneratorHandle handle) => handle.DangerousGetHandle();
        internal NativeMethods.OdinTokenGeneratorDestroyDelegate Free;

        /// <summary>
        /// Create TokenGenerator handle
        /// </summary>
        /// <remarks>On <see cref="ReleaseHandle"/> the handle calls <see cref="NativeMethods.OdinTokenGeneratorDestroyDelegate"/></remarks>
        /// <param name="handle">Token Generator handle pointer from <see cref="NativeMethods.OdinTokenGeneratorCreateDelegate"/></param>
        /// <param name="tokenGeneratorDestroyDelegate">Will be called on <see cref="ReleaseHandle"/></param>
        internal TokenGeneratorHandle(IntPtr handle, NativeMethods.OdinTokenGeneratorDestroyDelegate tokenGeneratorDestroyDelegate)
            : base(true)
        {
            Free = tokenGeneratorDestroyDelegate;
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            bool result = true;
            try
            {
                Free(handle);
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
