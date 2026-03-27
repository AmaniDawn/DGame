using System.Collections.Generic;

namespace SourceGenerator;

public class Definition
{
    /// <summary>
    /// 指定检测的命名空间
    /// </summary>
    public static readonly List<string> TargetNameSpaces = ["GameLogic"]; //"DGame", "GameProto", "GameBattle"
    public static readonly string[] UsingNameSpace = ["UnityEngine", "UnityEngine.UI", "DGame"];//"UnityEngine", "UnityEngine.UI", "DGame"
    public const string DefaultAssemblyName = "GameLogic";
    public const string DefaultFrameworkNameSpace = "DGame";
    public const string DefaultDebugName = "DGame.DLogger";

    /// <summary>
    /// 文件生成的命名空间
    /// </summary>
    public const string NameSpace = "GameLogic";//"DGame";
    public const string AttributeName = "EventInterface";
    public const string StringToHash = "StringId.StringToHash";

    /// <summary>
    /// RuntimeInitialize文件生成的命名空间
    /// </summary>
    public static readonly List<string> RuntimeInitializeTargetNameSpaces = ["GameLogic"]; //"DGame", "GameProto", "GameBattle"
    public static readonly string[] RuntimeInitializeUsingNameSpace = ["UnityEngine"];
    public const string RuntimeInitializeNameSpace = "AOT";//"DGame";
    public const string RuntimeInitializeAttributeName = "RuntimeInitializeOnLoadMethod";

    /// <summary>
    /// RequireComponent文件生成的命名空间
    /// </summary>
    public static readonly List<string> RequireComponentTargetNameSpaces = ["GameLogic"]; //"DGame", "GameProto", "GameBattle"
    public static readonly string[] RequireComponentUsingNameSpace = ["UnityEngine"];
    public const string RequireComponentNameSpace = "AOT";//"DGame";
    public const string RequireComponentAttributeName = "RequireComponent";
}