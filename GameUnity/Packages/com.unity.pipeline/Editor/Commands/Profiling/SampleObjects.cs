using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor.Profiling;
using UnityEngine;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>
    /// The Unity objects a Profiler sample ran for (for example the MonoBehaviour behind <c>Update() [Invoke]</c>).
    /// The recorded ids belong to the recording session, so a loaded capture's ids do not match the open Editor and
    /// are never resolved to live objects: name and type are read from the capture through the Profiler API.
    /// </summary>
    internal static class SampleObjects
    {
        internal const int MaxObjects = 5;

        /// <summary>
        /// The distinct objects of the item's merged samples, in first-seen order, at most <see cref="MaxObjects"/>.
        /// <paramref name="count"/> is the distinct total. Empty when the item has none of its own.
        /// </summary>
        internal static JArray Collect(HierarchyFrameDataView view, int itemId, out int count)
        {
            var ids = new List<ObjectId>();
            var seen = new HashSet<ObjectId>();
#if UNITY_6000_4_OR_NEWER
            var recorded = new List<EntityId>();
            view.GetItemMergedSamplesEntityId(itemId, recorded);
            foreach (var recordedId in recorded)
            {
                var id = new ObjectId(recordedId);
                if (id.RawValue != 0 && seen.Add(id))
                    ids.Add(id);
            }
#else
#pragma warning disable CS0618
            var recorded = new List<int>();
            view.GetItemMergedSamplesInstanceID(itemId, recorded);
#pragma warning restore CS0618
            foreach (var recordedId in recorded)
            {
                var id = new ObjectId(recordedId);
                if (id.RawValue != 0 && seen.Add(id))
                    ids.Add(id);
            }
#endif

            count = ids.Count;
            var objects = new JArray();
            for (var i = 0; i < ids.Count && i < MaxObjects; i++)
                objects.Add(Describe(view, ids[i]));
            return objects;
        }

        static JObject Describe(HierarchyFrameDataView view, ObjectId id)
        {
            var entry = new JObject
            {
                ["entityId"] = new JValue(id.RawValue),
                ["name"] = JValue.CreateNull(),
                ["type"] = JValue.CreateNull(),
                ["gameObject"] = JValue.CreateNull()
            };

            if (!TryGetInfo(view, id, out var name, out var nativeTypeIndex, out var related))
                return entry;

            entry["name"] = name;
            entry["type"] = view.GetUnityObjectNativeTypeInfo(nativeTypeIndex, out var type) ? type.name : null;
            if (related.RawValue != 0 && !related.Equals(id))
            {
                entry["gameObject"] = new JObject
                {
                    ["entityId"] = new JValue(related.RawValue),
                    ["name"] = TryGetInfo(view, related, out var gameObjectName, out _, out _) ? gameObjectName : null
                };
            }

            return entry;
        }

        static bool TryGetInfo(HierarchyFrameDataView view, ObjectId id, out string name, out int nativeTypeIndex, out ObjectId related)
        {
#if UNITY_6000_4_OR_NEWER
            if (view.GetUnityObjectInfo(id, out var info))
            {
                name = info.name;
                nativeTypeIndex = info.nativeTypeIndex;
                related = new ObjectId(info.relatedGameObjectEntityId);
                return true;
            }
#else
#pragma warning disable CS0618
            if (view.GetUnityObjectInfo(id, out var info))
            {
                name = info.name;
                nativeTypeIndex = info.nativeTypeIndex;
                related = new ObjectId(info.relatedGameObjectInstanceId);
                return true;
            }
#pragma warning restore CS0618
#endif
            name = null;
            nativeTypeIndex = 0;
            related = default;
            return false;
        }
    }
}
