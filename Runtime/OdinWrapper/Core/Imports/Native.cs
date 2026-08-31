using System;
using System.Runtime.InteropServices;
using System.Text;

namespace OdinNative.Core.Imports
{
    internal class Native
    {
        public const CallingConvention OdinCallingConvention = CallingConvention.Cdecl;
        public static readonly Encoding Encoding = Encoding.UTF8;
        public static readonly int SizeOfPointer = Marshal.SizeOf(typeof(IntPtr));

        public const int TrueI1 = 0x01;
        public const int FalseI1 = 0x00;

        public static class Offsets
        {
            public static class Audio
            {
                public static readonly int SamplesIntPtrOffset = 0x00; // Field1 0
                public static readonly int SamplesCountInt32Offset = 0x08; // Field2 8; SamplesIntPtrOffset + Marshal.SizeOf<IntPtr>();
                public static readonly int IsSilentByteOffset = 0x0C; // Field3 12; SamplesCountInt32Offset + sizeof(Int32) +1;
            }
        }

        public static string TryReadCString(IntPtr pointer)
        {
            try
            {
                return ReadByteString(pointer);
            }
            catch { return string.Empty; }
        }

        public static string TryReadCString(IntPtr pointer, int index, int length)
        {
            try
            {
                return ReadByteString(pointer)
                    .Substring(index, length);
            }
            catch { return string.Empty; }
        }

        public static string ReadByteString(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) return string.Empty;
            int length = 0;
            while (Marshal.ReadByte(pointer, length) != 0) length += 1;
            byte[] bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, length);
            return Encoding.GetString(bytes);
        }

        public static string ReadByteString(IntPtr pointer, int size)
        {
            byte[] buffer = new byte[size];
            Marshal.Copy(pointer, buffer, 0, buffer.Length);
            return Encoding.GetString(buffer);
        }

        public static string TryReadCString(IntPtr pointer, int size)
        {
            try
            {
                if (pointer == IntPtr.Zero)
                    return string.Empty;

                byte[] buffer = new byte[size];
                Marshal.Copy(pointer, buffer, 0, buffer.Length);
                string result = Native.Encoding.GetString(buffer);
                result = result.Substring(0, result.IndexOf('\0'));
                return result;
            }
            catch { return string.Empty; }
        }
    }
}
