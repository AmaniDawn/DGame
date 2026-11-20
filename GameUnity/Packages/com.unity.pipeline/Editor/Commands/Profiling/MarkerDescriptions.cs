using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;

namespace Unity.Pipeline.Editor.Commands.Profiling
{
    /// <summary>
    /// The Profiler's own description of a marker: what it means and what to check next.
    /// <c>MarkersInformationProvider</c> is internal to the Editor assembly, so it is read by reflection.
    /// </summary>
    static class MarkerDescriptions
    {
        static readonly Func<string, string> s_Lookup;
        static readonly Dictionary<string, string> s_Cache = new Dictionary<string, string>(StringComparer.Ordinal);

        static MarkerDescriptions()
        {
            try
            {
                // Not a sweep of AppDomain.GetAssemblies(), which trips the UAC0005 analyzer.
                var type = typeof(ProfilerDriver).Assembly.GetType("UnityEditor.Profiling.MarkersInformationProvider");
                var method = type?.GetMethod("GetMarkerInfo", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (method != null)
                    s_Lookup = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), method);
            }
            catch (Exception)
            {
                s_Lookup = null;
            }
        }

        /// <summary>Whether this Editor has a marker description table at all.</summary>
        internal static bool Available => s_Lookup != null;

        /// <summary><paramref name="entry"/> as JSON with the marker's <c>description</c> added; omitted when the Profiler has none.</summary>
        internal static JObject Annotate(object entry, string markerName)
        {
            var json = JObject.FromObject(entry);
            var description = Get(markerName);
            if (description != null)
                json["description"] = description;
            return json;
        }

        /// <summary>The description of <paramref name="markerName"/>, or null when the Profiler has none.</summary>
        internal static string Get(string markerName)
        {
            if (s_Lookup == null || string.IsNullOrEmpty(markerName))
                return null;

            if (s_Cache.TryGetValue(markerName, out var cached))
                return cached;

            string description;
            try
            {
                description = s_Lookup(markerName);
                if (string.IsNullOrEmpty(description))
                    description = null;
            }
            catch (Exception)
            {
                description = null;
            }

            s_Cache[markerName] = description;
            return description;
        }
    }
}
