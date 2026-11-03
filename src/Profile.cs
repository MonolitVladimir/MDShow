// SPDX-License-Identifier: MIT
using System;
using System.IO;

namespace MdShow
{
    internal static class Profile
    {
#if INSTALLED
        internal const string Scope = "Installed";
#elif ENGLISH_ONLY
        internal const string Scope = "PortableEnglish";
#else
        internal const string Scope = "PortableFull";
#endif
        internal static string DirectoryPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MDShow", Scope); }
        }

        internal static FileStream Acquire(string directory)
        {
            RequirePlainPath(directory);
            Directory.CreateDirectory(directory);
            RequirePlainPath(Path.Combine(directory, "profile.lock"));
            // File sharing is enforced across Windows sessions, unlike a Local mutex.
            return new FileStream(Path.Combine(directory, "profile.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        internal static void RequirePlainPath(string path)
        {
            for (string current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
            {
                try
                {
                    if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Profile paths must not contain reparse points.");
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
            }
        }

        // Called only for the consented Installed scope, by a non-elevated process.
        // Unknown files (including saved documents) and the legacy parent are retained.
        internal static void DeleteInstalledData(string installedDirectory)
        {
            string directory = Path.GetFullPath(installedDirectory);
            if (!String.Equals(Path.GetFileName(directory), "Installed", StringComparison.Ordinal))
                throw new ArgumentException("Expected the Installed profile scope.");
            RequirePlainPath(directory);
            if (!Directory.Exists(directory)) return;
            using (Acquire(directory))
            {
                DeleteCache(Path.Combine(directory, "WebView2"));
                foreach (string path in Directory.GetFiles(directory))
                {
                    string name = Path.GetFileName(path);
                    if (name != "settings.json" && name != "recovery.json" &&
                        !System.Text.RegularExpressions.Regex.IsMatch(name, @"\Arecovery\.json\.unreadable-[a-f0-9]{32}\.json\z")) continue;
                    RequirePlainPath(path);
                    File.Delete(path);
                }
            }
            File.Delete(Path.Combine(directory, "profile.lock"));
            if (Directory.GetFileSystemEntries(directory).Length == 0) Directory.Delete(directory);
        }

        private static void DeleteCache(string directory)
        {
            RequirePlainPath(directory);
            if (!Directory.Exists(directory)) return;
            foreach (string entry in Directory.GetFileSystemEntries(directory))
            {
                RequirePlainPath(entry);
                if ((File.GetAttributes(entry) & FileAttributes.Directory) != 0) DeleteCache(entry);
                else File.Delete(entry);
            }
            Directory.Delete(directory);
        }
    }
}
