using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.Pipeline
{
    /// <summary>
    /// Writes files that carry secrets, readable only by the current user.
    /// </summary>
    /// <remarks>
    /// Every write goes to its own uniquely named sibling temporary file that is restricted before the
    /// contents reach it, then replaces the target. Readers never see a partially written file, a write
    /// whose permissions can't be applied leaves the target untouched, and concurrent writers to the
    /// same path (two processes sharing a folder) never touch each other's temporary file.
    /// </remarks>
    static class SecureFile
    {
        /// <summary>
        /// Writes <paramref name="contents"/> to <paramref name="path"/>, creating the parent directory
        /// if needed. Concurrent writes to the same path are safe; the last replace wins.
        /// </summary>
        /// <param name="path">The file to create or overwrite.</param>
        /// <param name="contents">The text to write.</param>
        /// <exception cref="IOException">The file couldn't be written, or its permissions couldn't be restricted.</exception>
        public static void WriteAllText(string path, string contents) =>
            WriteAllText(path, contents, FilePermissions.RestrictToCurrentUser);

        internal static void WriteAllText(string path, string contents, Action<string> restrict)
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            var tempPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";

            try
            {
                using (new FileStream(tempPath, FileMode.CreateNew)) { }
                restrict(tempPath);
                File.WriteAllText(tempPath, contents);

                ReplaceWith(tempPath, fullPath);
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                throw;
            }
        }

        // A rename keeps the source's own permissions, unlike File.Replace, which on Windows merges
        // in the replaced file's ACL.
        static void ReplaceWith(string source, string destination)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    if (!MoveFileEx(source, destination, MoveFileReplaceExisting | MoveFileWriteThrough))
                        throw new IOException($"Could not replace '{destination}' (error {Marshal.GetLastWin32Error()}).");
                    break;
                default:
                    if (rename(source, destination) != 0)
                        throw new IOException($"Could not replace '{destination}' (errno {Marshal.GetLastWin32Error()}).");
                    break;
            }
        }

        const uint MoveFileReplaceExisting = 0x1;
        const uint MoveFileWriteThrough = 0x8;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool MoveFileEx(string existingFileName, string newFileName, uint flags);

        [DllImport("libc", SetLastError = true)]
        static extern int rename(string oldPath, string newPath);

        /// <summary>Deletes <paramref name="path"/>, if it exists. Failures are logged, not thrown.</summary>
        /// <param name="path">The file to delete.</param>
        public static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Pipeline: Failed to delete '{path}': {ex.Message}");
            }
        }
    }
}
