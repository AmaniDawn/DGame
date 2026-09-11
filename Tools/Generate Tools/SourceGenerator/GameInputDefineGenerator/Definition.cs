using System.Collections.Generic;

namespace GameInputDefineGenerator;

public static class Definition
{
    public const string TargetNamespace = "GameLogic";
    public const string TargetClassName = "GameInputActions";
    public static readonly List<string> TargetMapNames = ["GamePlay"];
    public const string TargetEnumName = "InputEventType";
    public const string TargetInputDefineClassName = "InputDefine";
    public const string InputDefineFileName = "InputDefine_Gen.g.cs";
    public const string InputEnableDefineName = "ENABLE_INPUT_SYSTEM";
}