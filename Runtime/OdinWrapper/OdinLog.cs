using OdinNative.Core.Imports;
using OdinNative.Wrapper;
using System;
using System.Diagnostics;
using System.Text;

namespace OdinNative
{
    public static class OdinLog
    {
        public enum VerbosityLevel
        {
            Off = 0,
            Error,
            Warning,
            Info,
            Verbose,
            Native,
        }

        public enum LogType
        {
            Error = 0,
            Exception,
            Warning,
            Info,
            Debug,
            Native,
        }

        private static VerbosityLevel _LogVerbosity = OdinDefaults.Verbosity;
        /**
         * - LogVerbosity == LogVerbosity
         * - LogVerbosity == Native+1 => InitNative + CurrentLogLevel
         */
        public static VerbosityLevel LogVerbosity
        {
            get { return _LogVerbosity; }
            set
            {
                if (OdinDefaults.DEBUG && Hook != null && !InitHook)
                    InitHook = InitNative(_LogVerbosity);

                _LogVerbosity = value;
            }
        }

        public static Action<string, LogType, object[]> Logger { get; private set; } = DefaultLogger;

        public static void DefaultLogger(string message, LogType type, params object[] args)
        {
            if (OdinDefaults.DEBUG && Hook != null && !InitHook)
                LogVerbosity = _LogVerbosity;

            if (type <= (LogType)_LogVerbosity)
                GlobalWriter(type, $"[ODIN] {Enum.GetName(typeof(LogType), type) ?? "Unknown"}: {message}", assertion: true, args);
        }

        public static void SetLogger(Action<string, LogType, object[]> logger)
        {
            Logger = logger;
        }

        public static void Log(string message, LogType type, params object[] args) => Logger(message, type, args);
        public static void LogError(string message) => Log(message, LogType.Error);
        public static void LogAssert(bool condition, string message) { if (!condition) LogDebug(message); }
        public static void LogDebug(string message) => Log(message, LogType.Debug);
        public static void LogWarning(string message) => Log(message, LogType.Warning);
        public static void LogInfo(string message) => Log(message, LogType.Info);
        public static void LogException(Exception ex) => Log(ex.ToString(), LogType.Exception);
        public static void LogSilent(string message) => System.Diagnostics.Debug.WriteLine(message, "ODIN");
#if UNITY_64 || __MonoCS__
        [AOT.MonoPInvokeCallback(typeof(OdinLog))]
#endif
        public static void LogNative(string message)
        {
            try
            {
                NativeLog line = OdinNative.Utils.Json.JSONParser.FromJson<NativeLog>(message);
                DateTimeOffset dt = DateTimeOffset.FromUnixTimeMilliseconds((long)(line.timestamp * 1000));
                string msg = $"NATIVE ({line.level}@{line.timestamp}) {dt.ToUniversalTime()} \"{line.message}\"";
                switch (line.level)
                {
                    case "WARN":
                        LogWarning(msg);
                        break;
                    case "INFO":
                    case "DEBUG":
                        LogInfo(msg);
                        break;
                    case "TRACE":
                        LogSilent(msg);
                        break;
                    default:
                        LogDebug(msg);
                        break;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"{nameof(LogNative)} Unknown: {e} Callback: {message}", "ODIN-NATIVE");
            }
        }
        internal static NativeLibraryMethods.odin_debug_dump_state DumpState;
        internal static NativeLibraryMethods.odin_debug_set_logging_hook Hook;
        // native keeps the hook pointer for the lifetime of the library; the delegate
        // instance must stay referenced or its thunk is garbage collected
        private static readonly NativeLibraryMethods.logging_callback LogNativeCallback = LogNative;
        private static bool InitHook = false;
        public static bool InitNative(VerbosityLevel level)
        {
            if(Hook == null)
                return false;

            switch (level)
            {
                case VerbosityLevel.Error:
                case VerbosityLevel.Warning:
                    Hook(0, LogNativeCallback); // warn
                    return true;
                case VerbosityLevel.Info:
                    Hook(1, LogNativeCallback); // info
                    return true;
                case VerbosityLevel.Verbose:
                    Hook(2, LogNativeCallback); // debug
                    return true;
                case VerbosityLevel.Native:
                    Hook(3, LogNativeCallback); // trace
                    return true;
                case VerbosityLevel.Off:
                default:
                    return false;
            }
        }
        public static string GetState()
        {
            if (DumpState == null)
                return string.Empty;

            var sb = new StringBuilder(10240);
            uint length = (uint)sb.Capacity;
            DumpState(sb, ref length);
            return sb.ToString();
        }

#if UNITY_STANDALONE || UNITY_EDITOR || ENABLE_IL2CPP || ENABLE_MONO
        [Obsolete("Future versions of Unity are expected to always throw exceptions and not have Assertions.Assert._raiseExceptions https://docs.unity3d.com/ScriptReference/Assertions.Assert.html")]
#endif
        internal static void Throw(Exception e, bool assertion = false)
        {
#if !UNITY_STANDALONE && !UNITY_EDITOR && !ENABLE_IL2CPP && !ENABLE_MONO
            if (assertion)
                OdinLog.LogException(e);
            else
                throw e;
#else
            if (assertion)
                UnityEngine.Debug.LogAssertion(e);
            else
                UnityEngine.Debug.LogException(e);
#endif
        }

        [Conditional("DEBUG"), Conditional("UNITY_ASSERTIONS"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        internal static void Assert(bool condition = false) => Assert(condition, Wrapper.OdinWrapperException.GetLastError() ?? "");
        // ReSharper disable Unity.PerformanceAnalysis
        [Conditional("DEBUG"), Conditional("UNITY_ASSERTIONS"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        internal static void Assert(bool condition = false, string message = "", bool silent = false)
        {
            string msg = $"Assert: {condition} \"{message}\" {(condition ? "OK" : "\n" + Environment.StackTrace)}";
            if (silent)
                OdinLog.LogSilent(msg);
            else 
                OdinLog.LogDebug(msg);

            if (condition) return;
#pragma warning disable CS0618 // Type or member is obsolete
            OdinLog.Throw(new Wrapper.OdinWrapperException(msg), true);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static void GlobalWriter(LogType type, string message, bool assertion = true, params object[] args)
        {
#if !UNITY_STANDALONE && !UNITY_EDITOR && !ENABLE_IL2CPP && !ENABLE_MONO
            if (assertion) 
            {
                if(args?.Length == 0)
                    Debug.WriteLine(message, "ODIN");
                else
                    Debug.WriteLine(message, args);
            }
            else
                throw new Wrapper.OdinWrapperException(message);
#else
            switch (type)
            {
                case LogType.Error:
                    UnityEngine.Debug.LogError(message);
                    break;
                case LogType.Exception:
                    if(assertion)
                        UnityEngine.Debug.LogAssertion(new OdinUnityException(message));
                    else
                        UnityEngine.Debug.LogException(new OdinUnityException(message));
                    break;
                case LogType.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case LogType.Debug:
                    UnityEngine.Debug.Log(message); // instead of LogAssertion for better filter
                    break;
                case LogType.Native:
                    UnityEngine.Debug.unityLogger.Log(UnityEngine.LogType.Assert, message);
                    break;
                case LogType.Info:
                default:
                    UnityEngine.Debug.Log(message);
                    break;
            }
#endif
        }
    }

    internal struct NativeLog
    {
        public string level;
        public string message;
        public double timestamp;
    }
}