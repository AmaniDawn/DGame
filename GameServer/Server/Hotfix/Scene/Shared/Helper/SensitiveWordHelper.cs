using Fantasy;
using ToolGood.Words;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Hotfix;

public static class SensitiveWordHelper
{
    /// <summary>
    /// 设置敏感词库
    /// </summary>
    /// <param name="keywords">敏感词库</param>
    public static void SetKeywords(this Scene scene, List<string> keywords)
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        sensitiveWordComponent.SetKeywords(keywords);
    }
    
    /// <summary>
    /// 设置敏感词库和黑名单分级
    /// </summary>
    /// <param name="keywords">敏感词库</param>
    /// <param name="blacklistTypes">黑名单分级</param>
    public static void SetKeywords(this Scene scene, List<string> keywords, List<int> blacklistTypes)
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        sensitiveWordComponent.SetKeywords(keywords, blacklistTypes);
    }
    
    /// <summary>
    /// 检查文本是否包含敏感词
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <returns></returns>
    public static bool ContainsSensitiveWord(this Scene scene, string content)
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        return sensitiveWordComponent.ContainsSensitiveWord(content);
    }
    
    /// <summary>
    /// 替换敏感词为指定字符
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <param name="replaceChar">替换的字符</param>
    /// <returns></returns>
    public static string ReplaceSensitiveWords(this Scene scene, string content, char replaceChar = '\0')
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        return sensitiveWordComponent.ReplaceSensitiveWords(content, replaceChar);
    }
    
    /// <summary>
    /// 设置跳词字符
    /// </summary>
    /// <param name="skipWords">跳词字符串</param>
    /// <returns></returns>
    public static void SetSkipWords(this Scene scene, string skipWords)
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        sensitiveWordComponent.SetSkipWords(skipWords);
    }
    
    /// <summary>
    /// 黑名单分级查询第一个
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <returns></returns>
    public static IllegalWordsSearchResult FindFirst(this Scene scene, string content)
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        return sensitiveWordComponent.FindFirst(content);
    }
    
    /// <summary>
    /// 黑名单分级查询所有
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <returns></returns>
    public static List<IllegalWordsSearchResult> FindAll(this Scene scene, string content)
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        return sensitiveWordComponent.FindAll(content);
    }
    
    /// <summary>
    /// 获取文本中所有的敏感词
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <returns></returns>
    public static List<string> FindAllSensitiveWords(this Scene scene, string content)
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        return sensitiveWordComponent.FindAllSensitiveWords(content);
    }
    
    /// <summary>
    /// 获取文本中所有的敏感词
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <param name="result">敏感词结果</param>
    /// <returns></returns>
    public static void FindAllSensitiveWords(this Scene scene, string content, ref List<string> result)
    {
        var sensitiveWordComponent = scene.GetComponent<SensitiveWordComponent>();

        if (sensitiveWordComponent == null)
        {
            sensitiveWordComponent = scene.AddComponent<SensitiveWordComponent>();
        }
        sensitiveWordComponent.FindAllSensitiveWords(content, ref result);
    }
}