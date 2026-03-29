using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace GameBattleAssemblyAnalyzer;

/// <summary>
/// GameBattle程序集分析器常量定义
/// </summary>
public sealed class Definition
{
    #region 诊断ID

    /// <summary>
    /// Unity引擎类型使用检测的诊断ID
    /// </summary>
    public const string DiagnosticId_UnityTypeUsage = "BATTLE001";

    #endregion

    #region Unity引擎类型使用检测诊断信息

    /// <summary>
    /// Unity引擎类型使用错误标题
    /// </summary>
    public static readonly LocalizableString TitleUnityTypeUsage = "GameBattle程序集不允许使用Unity引擎相关代码";

    /// <summary>
    /// Unity引擎类型使用错误消息格式
    /// {0}: 使用的Unity类型
    /// </summary>
    public static readonly LocalizableString MessageFormatUnityTypeUsage = "GameBattle程序集是纯逻辑层，不允许使用Unity引擎相关的表现层代码。检测到使用: '{0}'";

    /// <summary>
    /// Unity引擎类型使用错误描述
    /// </summary>
    public static readonly LocalizableString DescriptionUnityTypeUsage = "GameBattle程序集应该只包含纯游戏逻辑代码，不依赖Unity引擎。所有Unity相关的表现层代码应该放在其他程序集中。";

    #endregion

    /// <summary>
    /// 诊断类别
    /// </summary>
    public const string Category = "GameBattleAssembly";

    /// <summary>
    /// 需要检测的程序集名称
    /// </summary>
    public const string TargetAssemblyName = "GameBattle";

    /// <summary>
    /// Unity引擎命名空间前缀列表
    /// <remarks>用于检测是否使用了Unity引擎相关的代码</remarks>
    /// </summary>
    public static readonly List<string> UnityNamespacePrefixes = new List<string>
    {
        "UnityEngine",
        "UnityEditor",
        "Unity.Collections",
        "Unity.Mathematics"
    };

    /// <summary>
    /// 常见的Unity引擎类型名称（用于更精确的检测）
    /// </summary>
    public static readonly HashSet<string> UnityTypeNames = new HashSet<string>
    {
        // GameObject相关
        "GameObject",
        "Component",
        "Behaviour",
        "MonoBehaviour",
        "Transform",
        "RectTransform",

        // 资源相关
        "ResourceRequest",
        "AssetBundle",
        "AssetBundleRequest",

        // 渲染相关
        "Renderer",
        "MeshRenderer",
        "SkinnedMeshRenderer",
        "SpriteRenderer",
        "ParticleRenderer",
        "TrailRenderer",
        "LineRenderer",
        "Mesh",
        "Material",
        "Shader",
        "Texture",
        "Texture2D",
        "Texture3D",
        "Cubemap",
        "RenderTexture",
        "Sprite",

        // 动画相关
        "Animation",
        "Animator",
        "AnimationClip",
        "AnimationState",

        // 物理相关
        "Rigidbody",
        "Rigidbody2D",
        "Collider",
        "Collider2D",
        "BoxCollider",
        "SphereCollider",
        "CapsuleCollider",
        "MeshCollider",
        "WheelCollider",
        "Joint",

        // 音频相关
        "AudioSource",
        "AudioClip",
        "AudioListener",

        // UI相关
        "Canvas",
        "CanvasGroup",
        "CanvasRenderer",
        "Graphic",
        "Image",
        "RawImage",
        "Text",
        "TextMeshPro",
        "Button",
        "Toggle",
        "Slider",
        "ScrollRect",

        // 相机相关
        "Camera",

        // 光照相关
        "Light",

        // 输入相关
        "Input",

        // 时间相关
        "Time",

        // 场景相关
        "Scene",
        "SceneManager",

        // 协程相关
        "Coroutine",
        "YieldInstruction",

        // 事件系统
        "EventSystem",

        // 向量和数学相关
        "Vector2",
        "Vector3",
        "Vector4",
        "Quaternion",
        "Matrix4x4",
        "Color",
        "Rect",

        // 其他常见Unity类型
        "ScriptableObject",
        "LayerMask",
        "Tag"
    };
}
