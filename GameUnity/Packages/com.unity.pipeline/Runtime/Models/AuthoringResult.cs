using System;
using Newtonsoft.Json;

namespace Unity.Pipeline.Models
{
    /// <summary>
    /// Canonical identity of an object an authoring command created or acted on, returned as the
    /// command result so an agent can reference it in a follow-up call (via <see cref="ObjectRef"/>).
    /// Asset objects carry assetPath/guid(/fileId); loaded/scene objects carry instanceId/hierarchyPath.
    /// </summary>
    [Serializable]
    public class AuthoringResult
    {
        /// <summary>Canonical GlobalObjectId string for the object.</summary>
        [JsonProperty("globalId")]
        public string GlobalId { get; set; }

        /// <summary>Project-relative asset path, if the object is an asset.</summary>
        [JsonProperty("assetPath")]
        public string AssetPath { get; set; }

        /// <summary>Asset GUID, if the object is an asset.</summary>
        [JsonProperty("guid")]
        public string Guid { get; set; }

        /// <summary>Local file id within the asset, for sub-assets.</summary>
        [JsonProperty("fileId")]
        public long? FileId { get; set; }

        /// <summary>Instance id, if the object is loaded/in a scene.</summary>
        [JsonProperty("instanceId")]
        public ObjectId? InstanceId { get; set; }

        /// <summary>Scene hierarchy path, for scene objects.</summary>
        [JsonProperty("hierarchyPath")]
        public string HierarchyPath { get; set; }

        /// <summary>Object type name (e.g. "GameObject", "Material", "DefaultAsset").</summary>
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
