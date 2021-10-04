using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OdinNative.Core.Imports
{
    internal class Native
    {
        public const CallingConvention OdinCallingConvention = CallingConvention.Cdecl;
        public static readonly Encoding Encoding = Encoding.UTF8;
        public static readonly int SizeOfPointer = Marshal.SizeOf(typeof(IntPtr));

        public static void FreeUnmanaged(IntPtr pointer)
        {
            Marshal.FreeHGlobal(pointer);
        }

        public static string ReadString(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) return null;
            int length = 0;
            while (Marshal.ReadByte(pointer, length) != 0) length += 1;
            byte[] bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.GetString(bytes);
        }
    }

    internal class UnmanagedPointer : SafeHandleZeroOrMinusOneIsInvalid
    {
        public UnmanagedPointer(IntPtr pointer)
            : base(true)
        {
            SetHandle(pointer);
        }

        protected override bool ReleaseHandle()
        {
            Native.FreeUnmanaged(handle);
            return true;
        }
    }
}
