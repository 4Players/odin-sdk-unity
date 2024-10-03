using OdinNative.Core.Imports;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OdinNative.Core.Platform
{
    /// <summary>
    /// This class file helps covering the platform specific requirements of the ODIN package as install locations 
    /// will vary based on how it is installed.
    /// </summary>
    internal static class PlatformSpecific
    {
        private static class NativeWindowsMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
            public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procedureName);
            [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            public static extern bool FreeLibrary(IntPtr hModule);
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
            public static extern int GetModuleFileName(IntPtr hModule, [Out] byte[] lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);
        }

        private static class NativeUnixMehods
        {
#if (UNITY_STANDALONE_WIN && ENABLE_IL2CPP) || UNITY_WSA || UNITY_UWP
            public static IntPtr dlopen(string filename, int flags) => IntPtr.Zero;
            public static IntPtr dlerror() => IntPtr.Zero;
            public static IntPtr dlsym(IntPtr id, string symbol) => IntPtr.Zero;
            public static int dlclose(IntPtr id) => 0;
            public static int uname(IntPtr buf) => 0;
#else
            [DllImport("__Internal")]
            public static extern IntPtr dlopen(string filename, int flags);
            [DllImport("__Internal")]
            public static extern IntPtr dlerror();
            [DllImport("__Internal")]
            public static extern IntPtr dlsym(IntPtr id, string symbol);
            [DllImport("__Internal")]
            public static extern int dlclose(IntPtr id);
            [DllImport("__Internal")]
            public static extern int uname(IntPtr buf);
#endif
        }

        public static void LoadDynamicLibrary(SupportedPlatform platform, string[] possibleNames, out IntPtr handle, out string location)
        {
            foreach (string possibleName in possibleNames)
            {
                if (LoadDynamicLibrary(platform, possibleName, out handle, out location))
                    return;
            }
            string message = string.Join(", ", possibleNames);
            switch (platform)
            {
                case SupportedPlatform.Windows:
                    throw new DllNotFoundException(message, GetLastWindowsError());
                case SupportedPlatform.Android:
                case SupportedPlatform.iOS:
                case SupportedPlatform.Linux:
                case SupportedPlatform.MacOSX:
                    throw new DllNotFoundException(message, GetLastErrorUnix());
                default: throw new NotSupportedException();
            }
        }
        private static bool LoadDynamicLibrary(SupportedPlatform platform, string name, out IntPtr handle, out string location)
        {

            switch (platform)
            {
                case SupportedPlatform.Windows:
                    handle = NativeWindowsMethods.LoadLibrary(name);
                    if (handle == IntPtr.Zero)
                        goto default;
                    location = GetLocationWindows(handle) ?? name;
                    return true;
                case SupportedPlatform.Android:
                case SupportedPlatform.iOS:
                case SupportedPlatform.Linux:
                case SupportedPlatform.MacOSX:
                    handle = NativeUnixMehods.dlopen(name, 2 /* RTLD_NOW */);
                    if (handle == IntPtr.Zero)
                        goto default;
                    location = name;
                    return true;
                default:
                    handle = IntPtr.Zero;
                    location = null;
                    return false;
            }
        }

        private static string GetLocationWindows(IntPtr handle)
        {
            byte[] bytes = new byte[260];
            int length = NativeWindowsMethods.GetModuleFileName(handle, bytes, bytes.Length);
            if (length <= 0 || length == bytes.Length)
                return null;
            return Encoding.Default.GetString(bytes, 0, length);
        }

        public static void GetLibraryMethod<T>(SupportedPlatform platform, IntPtr handle, string name, out T t)
        {
            IntPtr result;
            switch (platform)
            {
                case SupportedPlatform.Windows:
                    result = NativeWindowsMethods.GetProcAddress(handle, name);
                    if (result == IntPtr.Zero)
                        throw new EntryPointNotFoundException(name, GetLastWindowsError());
                    break;
                case SupportedPlatform.Android:
                case SupportedPlatform.iOS:
                case SupportedPlatform.Linux:
                case SupportedPlatform.MacOSX:
                    result = NativeUnixMehods.dlsym(handle, name);
                    if (result == IntPtr.Zero)
                        throw new EntryPointNotFoundException(name, GetLastErrorUnix());
                    break;
                default: throw new NotSupportedException();
            }
            t = Marshal.GetDelegateForFunctionPointer<T>(result);
        }

        public static void UnloadDynamicLibrary(SupportedPlatform platform, IntPtr handle)
        {
            switch (platform)
            {
                case SupportedPlatform.Windows:
                    if (NativeWindowsMethods.FreeLibrary(handle) == false)
                        throw GetLastWindowsError();
                    break;
                case SupportedPlatform.Android:
                case SupportedPlatform.iOS:
                case SupportedPlatform.Linux:
                case SupportedPlatform.MacOSX:
                    if (NativeUnixMehods.dlclose(handle) != 0)
                        throw GetLastErrorUnix() ?? new InvalidOperationException();
                    break;
                default: throw new NotSupportedException();
            }
        }

        static SupportedPlatform GetUnixPlatform()
        {
#if PLATFORM_IOS || UNITY_IOS
            return SupportedPlatform.iOS;
#else
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                if (NativeUnixMehods.uname(buf) == 0)
                {
                    string os = Marshal.PtrToStringAnsi(buf);
                    if (os == "Darwin")
                        return SupportedPlatform.MacOSX;
                }
            }
            catch
            {
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
            return SupportedPlatform.Linux;
#endif
        }

        private static Exception GetLastWindowsError()
        {
            return new Win32Exception(Marshal.GetLastWin32Error());
        }

        private static Exception GetLastErrorUnix()
        {
            IntPtr error = NativeUnixMehods.dlerror();
            string message = Marshal.PtrToStringAnsi(error);
            return message != null ? new InvalidOperationException(message) : null;
        }

        /// <summary>
        /// Returns the name of the native binary that fits the current environment
        /// </summary>
        /// <param name="names">possible names of the native binary</param>
        /// <param name="platform">detected platform</param>
        /// <returns>true if a matching binary exists</returns>
        public static bool TryGetNativeBinaryName(out string[] names, out SupportedPlatform platform)
        {
            // check if OS is 64-, 32-, or something else bit
            bool is64Bit;
            switch (Native.SizeOfPointer)
            {
                case 8: is64Bit = true; break;
                case 4: is64Bit = false; break;
                default: names = null; platform = 0; return false;
            }

            // check if operating system is supported
            OperatingSystem operatingSystem = Environment.OSVersion;
            switch (operatingSystem.Platform)
            {
                case PlatformID.MacOSX: 
                    platform = SupportedPlatform.MacOSX; 
                    break;
                case PlatformID.Unix: 
                    platform = GetUnixPlatform(); 
                    break;
                case PlatformID.Win32NT:
                    if (operatingSystem.Version >= new Version(5, 1)) // if at least windows xp or newer
                    {
                        platform = SupportedPlatform.Windows;
                        break;
                    }
                    else goto default;
                default: platform = 0; names = null; return false;
            }

            names = PlatformLocations
                .GetPaths(platform, is64Bit)
                .ToArray();

            return true;
        }
    }
}
