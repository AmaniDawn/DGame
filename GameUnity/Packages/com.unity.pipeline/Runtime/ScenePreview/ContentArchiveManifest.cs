using System;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Pipeline.ScenePreview
{
    /// <summary>
    /// The small JSON file packed into every Scene Preview content archive (.ca), describing what a
    /// loader needs to load it: the content file's name inside the archive, and whether that content
    /// file references the engine's default resources. Written by the Scene Preview importer, read by
    /// <see cref="ContentArchiveLoader"/>.
    /// </summary>
    [Serializable]
    class ContentArchiveManifest
    {
        /// <summary>Name of the manifest file inside the archive.</summary>
        internal const string FileName = "archive_manifest.json";

        /// <summary>The archive format this package writes and reads.</summary>
        internal const int CurrentVersion = 1;

        [SerializeField] int m_Version;
        [SerializeField] string m_ContentFileName;
        [SerializeField] bool m_RequiresDefaultResources;

        internal ContentArchiveManifest(string contentFileName, bool requiresDefaultResources, int version = CurrentVersion)
        {
            m_Version = version;
            m_ContentFileName = contentFileName;
            m_RequiresDefaultResources = requiresDefaultResources;
        }

        /// <summary>The archive format, or 0 for an archive written before formats were numbered.</summary>
        internal int Version => m_Version;

        /// <summary>Name of the content file inside the archive.</summary>
        internal string ContentFileName => m_ContentFileName;

        /// <summary>
        /// Whether the content file references the engine's default resources, so loading it needs
        /// exactly one <c>ContentFile.GlobalTableDependency</c>.
        /// </summary>
        internal bool RequiresDefaultResources => m_RequiresDefaultResources;

        /// <summary>Read the manifest of a mounted archive.</summary>
        /// <param name="mountPath">The archive's mount path, as returned by <c>ArchiveHandle.GetMountPath()</c>.</param>
        /// <returns>The manifest, or null when the archive has none.</returns>
        internal static ContentArchiveManifest Read(string mountPath)
        {
            var bytes = ReadAllBytesFromVfs(mountPath + FileName);
            if (bytes == null)
                return null;

            var manifest = new ContentArchiveManifest(null, false, 0);
            JsonUtility.FromJsonOverwrite(Encoding.UTF8.GetString(bytes), manifest);
            return manifest;
        }

        // Files inside a mounted archive live on Unity's virtual file system, which System.IO cannot read.
        internal static unsafe byte[] ReadAllBytesFromVfs(string vfsPath)
        {
            FileInfoResult info;
            var infoHandle = AsyncReadManager.GetFileInfo(vfsPath, &info);
            infoHandle.JobHandle.Complete();
            infoHandle.Dispose();

            if (info.FileState != FileState.Exists || info.FileSize <= 0)
                return null;

            var size = (int)info.FileSize;
            var buffer = new NativeArray<byte>(size, Allocator.Temp);
            try
            {
                var command = new ReadCommand
                {
                    Buffer = NativeArrayUnsafeUtility.GetUnsafePtr(buffer),
                    Offset = 0,
                    Size = size
                };

                var readHandle = AsyncReadManager.Read(vfsPath, &command, 1);
                readHandle.JobHandle.Complete();
                var status = readHandle.Status;
                readHandle.Dispose();

                if (status != ReadStatus.Complete)
                {
                    Debug.LogError($"Scene Preview: failed to read the manifest at '{vfsPath}': {status}");
                    return null;
                }

                return buffer.ToArray();
            }
            finally
            {
                buffer.Dispose();
            }
        }
    }
}
