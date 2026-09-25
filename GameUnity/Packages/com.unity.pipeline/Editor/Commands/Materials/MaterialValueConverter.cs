using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Unity.Pipeline.Editor.Commands.Materials
{
    /// <summary>
    /// Bridges agent-supplied JSON values and a <see cref="Material"/>'s shader properties for the
    /// get_material_properties / set_material_properties commands (CLI-213).
    ///
    /// Value encoding by shader property type:
    /// - Float / Range / Int : a JSON number
    /// - Color               : <c>[r, g, b, a]</c> (floats 0–1) or a <c>"#RRGGBB"</c>/<c>"#RRGGBBAA"</c> hex string
    /// - Vector              : <c>[x, y, z, w]</c>
    /// - Texture (TexEnv)    : an <see cref="ObjectRef"/> object (resolved + confined to the authoring
    ///                         root), or <c>null</c> to clear
    ///
    /// Reads return the same shapes (Color as <c>[r,g,b,a]</c>, Vector as <c>[x,y,z,w]</c>, Texture as
    /// <c>{ texture: ObjectRef }</c> or null) so a read round-trips back through a write.
    /// </summary>
    internal static class MaterialValueConverter
    {
        /// <summary>
        /// Read a single shader property's current value off the material into a plain object suitable
        /// for JSON serialization. The shape matches the <see cref="ShaderPropertyType"/>.
        /// </summary>
        public static object ReadValue(Material material, string propertyName, ShaderPropertyType type)
        {
            switch (type)
            {
                case ShaderPropertyType.Color:
                {
                    var c = material.GetColor(propertyName);
                    return new[] { c.r, c.g, c.b, c.a };
                }
                case ShaderPropertyType.Vector:
                {
                    var v = material.GetVector(propertyName);
                    return new[] { v.x, v.y, v.z, v.w };
                }
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    return material.GetFloat(propertyName);
                case ShaderPropertyType.Int:
                    return material.GetInteger(propertyName);
                case ShaderPropertyType.Texture:
                {
                    var tex = material.GetTexture(propertyName);
                    // A re-usable handle for the assigned texture, or null when no texture is bound.
                    var handle = ObjectResolver.Describe(tex);
                    return handle == null ? null : new { texture = handle };
                }
                default:
                    return null;
            }
        }

        /// <summary>
        /// Apply a JSON value to a single shader property on the material, converting by the shader's
        /// declared property type. Throws <see cref="MaterialPropertyTypeMismatch"/> when the supplied
        /// JSON shape does not match the property's type (so the caller can record it in
        /// <c>unknown[]</c> with a reason). Texture refs are resolved via <see cref="ObjectResolver"/>
        /// and confined to the authoring root; an out-of-root or unresolved ref throws
        /// <see cref="ArgumentException"/> (a hard failure — never silently dropped).
        /// </summary>
        public static void ApplyValue(Material material, string propertyName, ShaderPropertyType type, JToken value)
        {
            switch (type)
            {
                case ShaderPropertyType.Float:
                    material.SetFloat(propertyName, ToFloat(propertyName, "Float", value));
                    break;

                case ShaderPropertyType.Range:
                    material.SetFloat(propertyName, ToFloat(propertyName, "Range", value));
                    break;

                case ShaderPropertyType.Int:
                    // Unity 6: Material.SetInteger is the typed setter for an Int shader property.
                    material.SetInteger(propertyName, ToInt(propertyName, value));
                    break;

                case ShaderPropertyType.Color:
                    material.SetColor(propertyName, ToColor(propertyName, value));
                    break;

                case ShaderPropertyType.Vector:
                    material.SetVector(propertyName, ToVector4(propertyName, value));
                    break;

                case ShaderPropertyType.Texture:
                    material.SetTexture(propertyName, ToTexture(propertyName, value));
                    break;

                default:
                    throw new MaterialPropertyTypeMismatch(
                        $"{propertyName}: unsupported shader property type '{type}'.");
            }
        }

        private static float ToFloat(string property, string typeLabel, JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
                throw new MaterialPropertyTypeMismatch($"{property}: expected {typeLabel}, got null.");
            if (value.Type != JTokenType.Integer && value.Type != JTokenType.Float)
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected {typeLabel} (a number), got {Describe(value)}.");
            return value.ToObject<float>();
        }

        private static int ToInt(string property, JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
                throw new MaterialPropertyTypeMismatch($"{property}: expected Int, got null.");
            if (value.Type != JTokenType.Integer && value.Type != JTokenType.Float)
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected Int (a number), got {Describe(value)}.");
            // Reject fractional JSON numbers (e.g. 1.5) rather than silently rounding/truncating.
            // An Int shader property must receive a whole number.
            var asDouble = value.ToObject<double>();
            if (asDouble != Math.Truncate(asDouble))
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected Int (a whole number), got the non-integer value {value}.");
            return value.ToObject<int>();
        }

        /// <summary>
        /// Parse a Color from either <c>[r,g,b,a]</c> (3 or 4 floats) or a hex string
        /// (<c>"#RRGGBB"</c> / <c>"#RRGGBBAA"</c>, leading '#' optional). Default alpha is 1.0.
        /// </summary>
        private static Color ToColor(string property, JToken value)
        {
            if (value is JArray arr)
            {
                if (arr.Count != 3 && arr.Count != 4)
                    throw new MaterialPropertyTypeMismatch(
                        $"{property}: expected Color as [r,g,b,a] (or [r,g,b]), got an array of length {arr.Count}.");
                // Route each component through ToFloat so non-numeric/null elements surface as a clean
                // MaterialPropertyTypeMismatch rather than throwing and failing the whole command.
                float r = ToFloat(property, "Color", arr[0]);
                float g = ToFloat(property, "Color", arr[1]);
                float b = ToFloat(property, "Color", arr[2]);
                float a = arr.Count == 4 ? ToFloat(property, "Color", arr[3]) : 1f;
                return new Color(r, g, b, a);
            }

            if (value != null && value.Type == JTokenType.String)
            {
                if (TryParseHexColor(value.ToObject<string>(), out var color))
                    return color;
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected Color as [r,g,b,a] or a '#RRGGBB'/'#RRGGBBAA' hex string, got '{value}'.");
            }

            throw new MaterialPropertyTypeMismatch(
                $"{property}: expected Color as [r,g,b,a] or a hex string, got {Describe(value)}.");
        }

        private static Vector4 ToVector4(string property, JToken value)
        {
            if (!(value is JArray arr))
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected Vector as [x,y,z,w], got {Describe(value)}.");
            if (arr.Count == 0 || arr.Count > 4)
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected Vector as [x,y,z,w] (1–4 numbers), got an array of length {arr.Count}.");

            // Missing trailing components default to 0 (matches Unity's Vector4 component default),
            // so [x,y,z] and [x,y] are accepted as shorthand for a 4-component vector.
            // Route each present component through ToFloat so non-numeric/null elements surface as a
            // clean MaterialPropertyTypeMismatch rather than throwing and failing the whole command.
            float x = arr.Count > 0 ? ToFloat(property, "Vector", arr[0]) : 0f;
            float y = arr.Count > 1 ? ToFloat(property, "Vector", arr[1]) : 0f;
            float z = arr.Count > 2 ? ToFloat(property, "Vector", arr[2]) : 0f;
            float w = arr.Count > 3 ? ToFloat(property, "Vector", arr[3]) : 0f;
            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Resolve a texture property value: an explicit <c>null</c> clears it, otherwise the value must
        /// be an <see cref="ObjectRef"/> that resolves to a <see cref="Texture"/> confined to the
        /// authoring root. A non-texture object, an out-of-root asset, or an unresolved handle is a hard
        /// failure (never silently dropped).
        /// </summary>
        private static Texture ToTexture(string property, JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
                return null;

            // ReadValue returns textures as { "texture": <ObjectRef> } so a get->set round-trip works.
            // Accept both that wrapper form and a bare ObjectRef supplied directly by the caller.
            JToken refToken = value;
            if (value is JObject wrapper && wrapper.ContainsKey("texture"))
                refToken = wrapper["texture"];

            ObjectRef handle;
            try
            {
                handle = refToken.ToObject<ObjectRef>();
            }
            catch (Exception)
            {
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected Texture as an object reference {{guid/path/...}} or null, got {Describe(value)}.");
            }

            if (handle == null || handle.IsEmpty)
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected Texture as an object reference {{guid/path/...}} or null, got {Describe(value)}.");

            if (!ObjectResolver.TryResolve(handle, out var obj, out var error))
                throw new ArgumentException($"{property}: could not resolve texture reference: {error}");

            if (!(obj is Texture texture))
                throw new MaterialPropertyTypeMismatch(
                    $"{property}: expected Texture, got a {obj.GetType().Name}.");

            // Confine to the authoring root: a GUID/globalId/path handle must bind a texture that lives
            // on disk under the sandbox. An in-memory/scene Texture has an empty asset path; allowing it
            // would bypass the root confinement, so reject any ref that doesn't resolve to a persisted
            // asset. Mirrors AssetCommands.ResolveAssetPath's confinement.
            var assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentException(
                    $"{property}: texture reference does not resolve to an on-disk asset under the authoring root '{ProjectPaths.AuthoringRoot}'; in-memory or scene textures are not allowed.");

            var confined = ProjectPaths.Resolve(assetPath, out var confineError);
            if (confined == null)
                throw new ArgumentException(
                    $"{property}: texture '{assetPath}' is outside the authoring root '{ProjectPaths.AuthoringRoot}': {confineError}");

            return texture;
        }

        /// <summary>
        /// Parse a hex color string ("#RRGGBB", "#RRGGBBAA", or without the leading '#'). Returns false
        /// for any malformed input. Channels are 0–255 mapped to 0–1; default alpha is 1.0.
        /// </summary>
        public static bool TryParseHexColor(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(hex))
                return false;

            var s = hex.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal))
                s = s.Substring(1);

            if (s.Length != 6 && s.Length != 8)
                return false;

            if (!byte.TryParse(s.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
                !byte.TryParse(s.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
                !byte.TryParse(s.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                return false;

            byte a = 255;
            if (s.Length == 8 &&
                !byte.TryParse(s.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out a))
                return false;

            color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
        }

        /// <summary>Map a <see cref="UnityEngine.Rendering.ShaderPropertyType"/> to the public type label used in results.</summary>
        public static string TypeLabel(ShaderPropertyType type)
        {
            switch (type)
            {
                case ShaderPropertyType.Color: return "Color";
                case ShaderPropertyType.Vector: return "Vector";
                case ShaderPropertyType.Float: return "Float";
                case ShaderPropertyType.Range: return "Range";
                case ShaderPropertyType.Texture: return "TexEnv";
                case ShaderPropertyType.Int: return "Int";
                default: return type.ToString();
            }
        }

        private static string Describe(JToken value)
        {
            if (value == null)
                return "null";
            switch (value.Type)
            {
                case JTokenType.Array: return "array";
                case JTokenType.Object: return "object";
                case JTokenType.Null: return "null";
                default: return value.Type.ToString().ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// Thrown when a supplied value's JSON shape does not match the shader property's declared type.
    /// The command catches this and records the property in <c>unknown[]</c> with the message as the
    /// reason, rather than failing the whole call.
    /// </summary>
    internal sealed class MaterialPropertyTypeMismatch : Exception
    {
        public MaterialPropertyTypeMismatch(string message) : base(message) { }
    }
}
