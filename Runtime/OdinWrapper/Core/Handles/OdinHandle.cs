using Microsoft.Win32.SafeHandles;
using OdinNative.Core.Platform;
using System;
using System.Threading;

namespace OdinNative.Core.Handles
{
    public abstract class OdinHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public bool IsAlive { get { return this.handle != IntPtr.Zero && this.IsClosed == false && this.IsInvalid == false && IsInitialized == true; } }
        public bool IsInitialized { get; protected set; }
        public string Location { get; }
        public SupportedPlatform Platform { get; }
        private readonly AutoResetEvent DllUnloaded = new AutoResetEvent(true);

        public OdinHandle(SupportedPlatform platform, string[] possibleFullpaths)
            : base(true)
        {
            try
            {
                Platform = platform;
                DllUnloaded.WaitOne();
                PlatformSpecific.LoadDynamicLibrary(platform, possibleFullpaths, out IntPtr handle, out string location);
                Location = location;
                SetHandle(handle);
            } 
            catch (Exception ex)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                Utility.Throw(ex, assertion: true);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        public void GetLibraryMethod<T>(string name, out T t)
        {
            PlatformSpecific.GetLibraryMethod(Platform, handle, name, out t);
        }

        protected override bool ReleaseHandle()
        {
            bool result = true;
            try
            {
                PlatformSpecific.UnloadDynamicLibrary(Platform, handle);
            }
            catch
            {
                result = false;
            }
            try
            {
                DllUnloaded.Set();
            }
            catch (ObjectDisposedException) { /* nop */ }
            return result;
        }
    }
}