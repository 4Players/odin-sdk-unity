using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OdinNative.Core.Platform
{
    /// <summary>
    /// Platforms supported by the native ODIN runtime
    /// </summary>
    [Flags]
    public enum SupportedPlatform
    {
        None = 1 << 0,
        Android = 1 << 1,
        iOS = 1 << 2,
        MacOSX = 1 << 3,
        Linux = 1 << 4,
        Windows = 1 << 5,
        UnixLinux = Android | Linux,
        UnixApple = iOS | MacOSX,
        Any = ~None
    }

    public struct PlatformBinaryLocations
    {
        public string[] Paths { get; set; }
        public bool Is64Bit { get; set; }
        public SupportedPlatform SupportedPlatform { get; set; }
    }

    /// <summary>
    /// This class file helps covering the platform specific requirements of the ODIN package as install locations 
    /// will vary based on how it is installed.
    /// <list type="bullet">
    /// <item>
    /// <term>Installing from git</term>
    /// <description>$PROJECT_PATH/Library/PackageCache/io.fourplayers.odin@$COMMIT_HASH</description>
    /// </item>
    /// <item>
    /// <term>Installing from Unity asset store</term>
    /// <description>$PROJECT_PATH/Assets/4Players/ODIN</description>
    /// </item>
    /// <item>
    /// <term>Installing from tarball</term>
    /// <description>$PROJECT_PATH/Assets/io.fourplayers.odin</description>
    /// </item>
    /// <item>
    /// <term>Installing from Unity package bundle</term>
    /// <description>$PROJECT_PATH/Packages/io.fourplayers.odin</description>
    /// </item>
    /// </list>
    /// </summary>
    public static class PlatformLocations
    {
        public const string PackageName = "io.fourplayers.odin";
        public const string PackageVendor = "4Players";
        public const string PackageShortName = "ODIN";

        public const string AssetPath = "Assets/" + PackageName + "/Plugins";
        public const string AssetStorePath = "Assets/" + PackageVendor + "/" + PackageShortName + "/Plugins";
        public const string TarballPath = "Assets/" + PackageName + "/Plugins";
        public const string PackagePath = "Packages/" + PackageName + "/Plugins";
        
        private static string GetFullPackagePath => Path.GetFullPath(PackagePath);

        private static readonly Func<string, bool> TryDirectoryExists = (string path) => 
        { 
            bool result = false;
#pragma warning disable CS0168 // Variable is declared but never used
            try { result = System.IO.Directory.Exists(path); }
            catch (Exception _) { result = false; }
#pragma warning restore CS0168 // Variable is declared but never used
            return result; 
        };
        public static bool IsGit() => TryDirectoryExists(LibraryCache + "/Plugins");
        public static bool IsUnityStore() => TryDirectoryExists(AssetStorePath);
        public static bool IsTarball() => TryDirectoryExists(TarballPath);
        public static bool IsPackageBundle() => TryDirectoryExists(PackagePath);
        public static bool IsCustom() => !IsGit() && !IsUnityStore() && !IsTarball() && !IsPackageBundle();

        public static string LibraryCache
        {
            get
            {
                string libraryCache = "Library/PackageCache";
                try
                {
                    libraryCache = System.IO.Directory
                        .GetDirectories(libraryCache)
                        .Where(dir => dir.Contains(PackageName))
                        .FirstOrDefault();
                }
                catch (System.IO.DirectoryNotFoundException) 
                {
                    libraryCache = libraryCache + "/" + PackageName; // missing @$COMMIT_HASH and set invalid git path
                } 
                return libraryCache;
            }
        }

        public static readonly IReadOnlyCollection<PlatformBinaryLocations> BinaryLocations = new List<PlatformBinaryLocations>
        {
            #region Apple
            new PlatformBinaryLocations()
            {
                SupportedPlatform = SupportedPlatform.iOS,
                Paths = new string[] {
                    string.Format("{0}/../{1}", UnityEngine.Application.dataPath, "Frameworks"), // Frameworks
                },
                Is64Bit = true,
            },
            new PlatformBinaryLocations()
            {
                SupportedPlatform = SupportedPlatform.MacOSX,
                Paths = new string[] {
                    string.Format("{0}/{1}", PackagePath, "macos/universal"), // PkgManager
                    string.Format("{0}/{1}", GetFullPackagePath, "macos/universal"), 
                    string.Format("{0}/{1}", AssetPath, "macos/universal"), // Editor
                    string.Format("{0}/{1}", AssetStorePath, "macos/universal"), // Asset Store
                    string.Format("{0}/{1}", LibraryCache, "Plugins/macos/universal"), // PackageCache
#if UNITY_64
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone appbundle
#endif
                },
                Is64Bit = true,
            },
            #endregion Apple

            #region Unix
            new PlatformBinaryLocations()
            {
                SupportedPlatform = SupportedPlatform.Linux,
                Paths = new string[] {
                    string.Format("{0}/{1}", PackagePath, "linux/x86_64"), // PkgManager
#if !(UNITY_ANDROID && ENABLE_MONO)
                    string.Format("{0}/{1}", GetFullPackagePath, "linux/x86_64"), 
#endif
                    string.Format("{0}/{1}", AssetPath, "linux/x86_64"), // Editor
                    string.Format("{0}/{1}", AssetStorePath, "linux/x86_64"), // Asset Store
                    string.Format("{0}/{1}", LibraryCache, "Plugins/linux/x86_64"), // PackageCache
                    string.Format("{0}/{1}", "Plugins", "x86_64"), // Standalone
#if UNITY_64
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone
                    string.Format("{0}/{1}/{2}", UnityEngine.Application.dataPath, "Plugins", "x86_64"), // Standalone
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone
#endif
                },
                Is64Bit = true,
            },
            new PlatformBinaryLocations()
            {
                SupportedPlatform = SupportedPlatform.Linux,
                Paths = new string[] {
                    string.Format("{0}/{1}", PackagePath, "linux/x86"), // PkgManager
#if !(UNITY_ANDROID && ENABLE_MONO)
                    string.Format("{0}/{1}", GetFullPackagePath, "linux/x86"), 
#endif
                    string.Format("{0}/{1}", AssetPath, "linux/x86"), // Editor
                    string.Format("{0}/{1}", AssetStorePath, "linux/x86"), // Asset Store
                    string.Format("{0}/{1}", LibraryCache, "Plugins/linux/x86"), // PackageCache
                    string.Format("{0}/{1}", "Plugins", "x86"), // Standalone
#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone
                    string.Format("{0}/{1}/{2}", UnityEngine.Application.dataPath, "Plugins", "x86"), // Standalone
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone
#endif
                },
                Is64Bit = false,
            },
            #endregion Unix

            #region Windows
            new PlatformBinaryLocations()
            {
                SupportedPlatform = SupportedPlatform.Windows,
                Paths = new string[] {
                    string.Format("{0}/{1}", PackagePath, "windows/x86_64"), // PkgManager
                    string.Format("{0}/{1}", GetFullPackagePath, "windows/x86_64"),
                    string.Format("{0}/{1}", AssetPath, "windows/x86_64"), // Editor
                    string.Format("{0}/{1}", AssetStorePath, "windows/x86_64"), // Asset Store
                    string.Format("{0}/{1}", LibraryCache, "Plugins/windows/x86_64"), // PackageCache
                    string.Format("{0}/{1}", "Plugins", "x86_64"), // Standalone
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone
                    string.Format("{0}/{1}/{2}", UnityEngine.Application.dataPath, "Plugins", "x86_64"), // Standalone
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone
#endif
                },
                Is64Bit = true,
            },
            new PlatformBinaryLocations()
            {
                SupportedPlatform = SupportedPlatform.Windows,
                Paths = new string[] {
                    string.Format("{0}/{1}", PackagePath, "windows/x86"), // PkgManager
                    string.Format("{0}/{1}", GetFullPackagePath, "windows/x86"),
                    string.Format("{0}/{1}", AssetPath, "windows/x86"), // Editor
                    string.Format("{0}/{1}", AssetStorePath, "windows/x86"), // Asset Store
                    string.Format("{0}/{1}", LibraryCache, "Plugins/windows/x86"), // PackageCache
                    string.Format("{0}/{1}", "Plugins", "x86"), // Standalone
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone
                    string.Format("{0}/{1}/{2}", UnityEngine.Application.dataPath, "Plugins", "x86"), // Standalone
                    string.Format("{0}/{1}", UnityEngine.Application.dataPath, "Plugins"), // Standalone
#endif
                },
                Is64Bit = false,
            },
            #endregion Windows
        };

        public static IEnumerable<string> GetPaths(SupportedPlatform platform, bool is64Bit)
        {
            return BinaryLocations
                .Where(bin => bin.SupportedPlatform.HasFlag(platform) && bin.Is64Bit == is64Bit)
                .SelectMany(el => el.Paths);
        }
    }
}
