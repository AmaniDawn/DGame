using System.Collections.Generic;

namespace GameInputDefineGenerator;

public static class Definition
{
    public const string TargetNamespace = "GameLogic";
    public const string TargetClassName = "GameInputActions";
    public static readonly List<string> TargetMapNames = ["GamePlay"];
    public const string TargetEnumName = "InputActionType";
    public const string TargetFileName = "InputActionType_Gen.g.cs";
}
