using System;
using System.Collections;
using System.IO;
using System.Threading;
using Unity.Collections;
using Unity.Content;
using Unity.IO.Archive;
using Unity.Loading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.ScenePreview
{
    /// <summary>
    /// Loads one Scene Preview content archive (.ca): mounts it, reads its manifest, and loads its
    /// content file. Dispose it to unload.
    /// </summary>
    /// <remarks>
    /// Destroy every object instantiated from <see cref="Objects"/> (with <c>DestroyImmediate</c>)
    /// before disposing, so the content file can unload within <see cref="Dispose"/>.
    /// </remarks>
    public sealed class ContentArchiveLoader : IDisposable
    {
        // A mount prefix is never reused, even after a domain reload: a remounted prefix yields a content file that never finishes loading.
        static readonly string s_MountToken = Guid.NewGuid().ToString("N");
        static int s_MountSequence;

        const int k_UnloadTimeoutMs = 5000;

        ArchiveHandle m_Archive;
        bool m_HasArchive;
        ContentFile m_ContentFile;
        bool m_HasContentFile;
        string m_Error;
        string m_ArchivePath;
        bool m_Disposed;

        ContentArchiveLoader() { }

        /// <summary>
        /// Mount the archive and start loading its content file.
        /// </summary>
        /// <param name="archivePath">Path of the .ca file on disk.</param>
        /// <returns>The loader. Poll <see cref="IsDone"/> or yield <see cref="WaitForLoad"/>.</returns>
        public static ContentArchiveLoader Load(string archivePath)
        {
            var loader = new ContentArchiveLoader();
            loader.Begin(archivePath);
            return loader;
        }

        internal ContentArchiveManifest Manifest { get; private set; }

        /// <summary>True once loading has finished, successfully or not.</summary>
        public bool IsDone => m_Disposed || m_Error != null || !m_HasContentFile || m_ContentFile.LoadingStatus != LoadingStatus.InProgress;

        /// <summary>True when the content file loaded.</summary>
        public bool Succeeded => !m_Disposed && m_Error == null && m_HasContentFile && m_ContentFile.LoadingStatus == LoadingStatus.Completed;

        /// <summary>Why loading failed, or null while loading or after success.</summary>
        public string Error
        {
            get
            {
                if (m_Error != null)
                    return m_Error;
                if (m_Disposed)
                    return "the loader was disposed";
                if (!IsDone || Succeeded)
                    return null;
                return $"the content file failed to load (status {m_ContentFile.LoadingStatus})";
            }
        }

        /// <summary>The objects in the loaded content file, or an empty array until it has loaded.</summary>
        public Object[] Objects => Succeeded ? m_ContentFile.GetObjects() : Array.Empty<Object>();

        /// <summary>Coroutine that waits until <see cref="IsDone"/>.</summary>
        /// <returns>An enumerator to yield from a coroutine.</returns>
        public IEnumerator WaitForLoad()
        {
            while (!IsDone)
                yield return null;
        }

        void Begin(string archivePath)
        {
            if (string.IsNullOrEmpty(archivePath) || !File.Exists(archivePath))
            {
                m_Error = $"archive not found: '{archivePath}'";
                return;
            }

            m_ArchivePath = archivePath;
            var mountPrefix = $"scenepreview{s_MountToken}{Interlocked.Increment(ref s_MountSequence)}:";
            m_Archive = ArchiveFileInterface.MountAsync(ContentNamespace.Default, archivePath, mountPrefix);
            m_HasArchive = true;
            m_Archive.JobHandle.Complete();
            if (m_Archive.Status != ArchiveStatus.Complete)
            {
                m_Error = $"failed to mount '{archivePath}' (status {m_Archive.Status})";
                return;
            }

            var mountPath = m_Archive.GetMountPath();
            Manifest = ContentArchiveManifest.Read(mountPath);
            if (Manifest != null && Manifest.Version != ContentArchiveManifest.CurrentVersion)
            {
                m_Error = $"'{archivePath}' uses Scene Preview archive format {Manifest.Version}, and this player reads format " +
                    $"{ContentArchiveManifest.CurrentVersion}: build the player and the archive with the same com.unity.pipeline version";
                return;
            }
            if (string.IsNullOrEmpty(Manifest?.ContentFileName))
            {
                m_Error = $"'{archivePath}' has no {ContentArchiveManifest.FileName} naming its content file";
                return;
            }

            // Exactly one global-table dependency when the content references default resources, else none:
            // too many fails the load as surely as too few.
            var dependencies = new NativeArray<ContentFile>(Manifest.RequiresDefaultResources ? 1 : 0, Allocator.Temp);
            if (Manifest.RequiresDefaultResources)
                dependencies[0] = ContentFile.GlobalTableDependency;
            m_ContentFile = ContentLoadInterface.LoadContentFileAsync(ContentNamespace.Default, Path.Combine(mountPath, Manifest.ContentFileName), dependencies);
            m_HasContentFile = true;
            dependencies.Dispose();
        }

        /// <summary>
        /// Unload the content file, then unmount the archive. When the content file does not unload in
        /// time, the archive stays mounted and an error is logged; call again once its objects are destroyed.
        /// </summary>
        public void Dispose()
        {
            if (m_HasContentFile)
            {
                if (!m_ContentFile.UnloadAsync().WaitForCompletion(k_UnloadTimeoutMs))
                {
                    // Unmounting under a content file that is still loaded or loading is unsafe; leaking the mount is not.
                    Debug.LogError($"Scene Preview: '{m_ArchivePath}' did not unload within {k_UnloadTimeoutMs} ms, so it stays mounted. " +
                        "Destroy every object instantiated from it, and let its load finish, before disposing the loader.");
                    return;
                }
                m_HasContentFile = false;
            }

            if (m_HasArchive)
            {
                m_Archive.Unmount().Complete();
                m_HasArchive = false;
            }
            m_Disposed = true;
        }
    }
}
