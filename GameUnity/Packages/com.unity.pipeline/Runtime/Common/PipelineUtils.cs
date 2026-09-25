using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
#if UNITY_6000_3_OR_NEWER
using UnityEngine.Assemblies;
#endif

namespace Unity.Pipeline
{
    /// <summary>
    /// Version-stable handle to a Unity object id. In 6000.4 Unity replaced the int instance id with
    /// <see cref="EntityId"/> (a ulong-backed handle), made <c>Object.GetInstanceID()</c> /
    /// <c>EditorUtility.InstanceIDToObject(int)</c> obsolete-as-error, and is removing the
    /// <c>EntityId</c>&lt;-&gt;<c>int</c> conversions. This struct stores the concrete id type for the
    /// running editor — <see cref="EntityId"/> on 6000.4+, <c>int</c> below — and converts implicitly to
    /// it so it can be passed straight to Unity APIs. All version branching for object ids is confined
    /// here. It (de)serializes as a single JSON integer (the raw ulong on 6000.4+, the int below).
    /// </summary>
    [JsonConverter(typeof(ObjectIdConverter))]
    public readonly struct ObjectId : System.IEquatable<ObjectId>
    {
#if UNITY_6000_4_OR_NEWER
        readonly EntityId m_Value;
        public ObjectId(EntityId value) { m_Value = value; }
        public static implicit operator EntityId(ObjectId id) => id.m_Value;
        public static implicit operator ObjectId(EntityId value) => new ObjectId(value);
        /// <summary>Raw 64-bit id — the canonical wire / serialization form.</summary>
        public ulong RawValue => EntityId.ToULong(m_Value);
        public static ObjectId FromRaw(ulong raw) => new ObjectId(EntityId.FromULong(raw));
        public bool Equals(ObjectId other) => m_Value.Equals(other.m_Value);
        public override int GetHashCode() => m_Value.GetHashCode();
#else
        readonly int m_Value;
        public ObjectId(int value) { m_Value = value; }
        public static implicit operator int(ObjectId id) => id.m_Value;
        public static implicit operator ObjectId(int value) => new ObjectId(value);
        /// <summary>Raw id — the canonical wire / serialization form.</summary>
        public long RawValue => m_Value;
        public static ObjectId FromRaw(long raw) => new ObjectId((int)raw);
        public bool Equals(ObjectId other) => m_Value == other.m_Value;
        public override int GetHashCode() => m_Value;
#endif
        public override bool Equals(object obj) => obj is ObjectId other && Equals(other);
        public override string ToString() => RawValue.ToString();

        /// <summary>Parse the canonical numeric form produced by <see cref="ToString"/>.</summary>
        public static ObjectId Parse(string s)
        {
#if UNITY_6000_4_OR_NEWER
            return FromRaw(ulong.Parse(s));
#else
            return FromRaw(long.Parse(s));
#endif
        }

        /// <summary>Try to parse the canonical numeric form produced by <see cref="ToString"/>.</summary>
        public static bool TryParse(string s, out ObjectId id)
        {
#if UNITY_6000_4_OR_NEWER
            if (ulong.TryParse(s, out var raw)) { id = FromRaw(raw); return true; }
#else
            if (long.TryParse(s, out var raw)) { id = FromRaw(raw); return true; }
#endif
            id = default;
            return false;
        }
    }

    /// <summary>JSON converter: an <see cref="ObjectId"/> is a single integer on the wire.</summary>
    public sealed class ObjectIdConverter : JsonConverter<ObjectId>
    {
        public override void WriteJson(JsonWriter writer, ObjectId value, JsonSerializer serializer)
        {
            writer.WriteValue(value.RawValue);
        }

        public override ObjectId ReadJson(JsonReader reader, System.Type objectType, ObjectId existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            switch (token.Type)
            {
                case JTokenType.Integer:
#if UNITY_6000_4_OR_NEWER
                    return ObjectId.FromRaw(token.Value<ulong>());
#else
                    return ObjectId.FromRaw(token.Value<long>());
#endif
                case JTokenType.String:
                    return ObjectId.Parse((string)token);
                default:
                    return default;
            }
        }
    }

    public static class PipelineUtils
    {
        public static string GetLoadedAssemblyPath(System.Reflection.Assembly a)
        {
#if UNITY_6000_5_OR_NEWER
            return a.GetLoadedAssemblyPath();
#else
            return a.Location;
#endif
        }

        public static System.Reflection.Assembly LoadFromBytes(byte[] bytes, byte[] pdb = null)
        {
#if UNITY_6000_5_OR_NEWER
            return UnityEngine.Assemblies.CurrentAssemblies.LoadFromBytes(bytes, pdb);
#else
            return System.Reflection.Assembly.Load(bytes, pdb);
#endif
        }

        public static T[] FindObjectsByType<T>() where T : Object
        {
#if UNITY_6000_4_OR_NEWER
            return GameObject.FindObjectsByType<T>();
#else
            return GameObject.FindObjectsByType<T>(FindObjectsSortMode.None) ;
#endif
        }

        public static IReadOnlyList<System.Reflection.Assembly> GetLoadedAssemblies()
        {
#if UNITY_6000_5_OR_NEWER
            return CurrentAssemblies.GetLoadedAssemblies();
#else
            return System.AppDomain.CurrentDomain.GetAssemblies();
#endif
        }

        /// <summary>The object's id as a version-stable <see cref="ObjectId"/> (EntityId on 6000.4+, int below).</summary>
        public static ObjectId GetObjectId(Object obj)
        {
#if UNITY_6000_4_OR_NEWER
            return new ObjectId(obj.GetEntityId());
#else
            return new ObjectId(obj.GetInstanceID());
#endif
        }

#if UNITY_EDITOR
        /// <summary>Resolve a loaded/scene object from its <see cref="ObjectId"/>, or null if not found.</summary>
        public static Object IdToObject(ObjectId id)
        {
#if UNITY_6000_4_OR_NEWER
            return UnityEditor.EditorUtility.EntityIdToObject(id);
#elif UNITY_6000_3_OR_NEWER
            // EntityIdToObject is the non-obsolete resolver from 6000.3, but the ulong-backed ObjectId
            // (GetRawData/FromULong) doesn't exist until 6000.4 — so ObjectId is still int-backed here and
            // we route the int through the (non-obsolete on 6.3) int->EntityId conversion.
            return UnityEditor.EditorUtility.EntityIdToObject((int)id);
#else
            return UnityEditor.EditorUtility.InstanceIDToObject((int)id);
#endif
        }
#endif
    }
}
