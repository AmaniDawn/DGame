using Fantasy;
using Fantasy.Entitas.Interface;
using ToolGood.Words;
#pragma warning disable CS0618 // 类型或成员已过时

namespace Hotfix;

public sealed class SensitiveWordComponentAwakeSystem : AwakeSystem<SensitiveWordComponent>
{
    protected override void Awake(SensitiveWordComponent self)
    {
        if (self.UseAdvancedFilter)
        {
            self.IllegalWordsSearch = new IllegalWordsSearch
            {
                UseIgnoreCase = self.UseIgnoreCase,
                UseDBCcaseConverter = self.UseDBCcaseConverter,
                UseDuplicateWordFilter = self.UseDuplicateWordFilter
            };
        }
        else
        {
            self.WordsSearch = new WordsSearch();
        }
    }
}

public sealed class SensitiveWordComponentDestroySystem : DestroySystem<SensitiveWordComponent>
{
    protected override void Destroy(SensitiveWordComponent self)
    {
        self.IllegalWordsSearch = null;
        self.WordsSearch = null;
    }
}

public static class SensitiveWordComponentSystem
{
    /// <summary>
    /// 设置敏感词库
    /// </summary>
    /// <param name="keywords">敏感词库</param>
    public static void SetKeywords(this SensitiveWordComponent self, List<string> keywords)
    {
        if (self.UseAdvancedFilter)
        {
            if (self.IllegalWordsSearch == null)
            {
                self.IllegalWordsSearch = new IllegalWordsSearch();
            }

            self.IllegalWordsSearch.SetKeywords(keywords);
        }
        else
        {
            if (self.WordsSearch == null)
            {
                self.WordsSearch = new WordsSearch();
            }

            self.WordsSearch.SetKeywords(keywords);
        }
    }
    
    /// <summary>
    /// 设置敏感词库和黑名单分级
    /// </summary>
    /// <param name="keywords">敏感词库</param>
    /// <param name="blacklistTypes">黑名单分级</param>
    public static void SetKeywords(this SensitiveWordComponent self, List<string> keywords, List<int> blacklistTypes)
    {
        if (self.UseAdvancedFilter)
        {
            if (self.IllegalWordsSearch == null)
            {
                self.IllegalWordsSearch = new IllegalWordsSearch();
            }

            self.IllegalWordsSearch.SetKeywords(keywords);
            self.IllegalWordsSearch.SetBlacklist(blacklistTypes.ToArray());
        }
    }
    
    /// <summary>
    /// 检查文本是否包含敏感词
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <returns></returns>
    public static bool ContainsSensitiveWord(this SensitiveWordComponent self, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        if (self.UseAdvancedFilter)
        {
            return self.IllegalWordsSearch != null && self.IllegalWordsSearch.ContainsAny(content);
        }
        else
        {
            return self.WordsSearch != null && self.WordsSearch.ContainsAny(content);
        }
    }
    
    /// <summary>
    /// 替换敏感词为指定字符
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <param name="replaceChar">替换的字符</param>
    /// <returns></returns>
    public static string ReplaceSensitiveWords(this SensitiveWordComponent self, string content, char replaceChar = '\0')
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        if (replaceChar == '\0')
        {
            replaceChar = self.ReplaceChar;
        }

        if (self.UseAdvancedFilter)
        {
            return self.IllegalWordsSearch == null ? content : self.IllegalWordsSearch.Replace(content, replaceChar);
        }
        else
        {
            return self.WordsSearch == null ? content : self.WordsSearch.Replace(content, replaceChar);
        }
    }
    
    /// <summary>
    /// 设置跳词字符
    /// </summary>
    /// <param name="skipWords">跳词字符串</param>
    /// <returns></returns>
    public static void SetSkipWords(this SensitiveWordComponent self, string skipWords)
    {
        if (string.IsNullOrEmpty(skipWords))
        {
            return;
        }
        if (self.UseAdvancedFilter)
        {
            if (self.IllegalWordsSearch == null)
            {
                self.IllegalWordsSearch = new IllegalWordsSearch();
            }

            self.IllegalWordsSearch.SetSkipWords(skipWords);
        }
    }
    
    /// <summary>
    /// 黑名单分级查询第一个
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <returns></returns>
    public static IllegalWordsSearchResult FindFirst(this SensitiveWordComponent self, string content)
        => self.UseAdvancedFilter && self.IllegalWordsSearch != null ? self.IllegalWordsSearch.FindFirst(content) : null;
    
    /// <summary>
    /// 黑名单分级查询所有
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <returns></returns>
    public static List<IllegalWordsSearchResult> FindAll(this SensitiveWordComponent self, string content)
        => self.UseAdvancedFilter && self.IllegalWordsSearch != null ? self.IllegalWordsSearch.FindAll(content) : null;
    
    /// <summary>
    /// 获取文本中所有的敏感词
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <returns></returns>
    public static List<string> FindAllSensitiveWords(this SensitiveWordComponent self, string content)
    {
        List<string> result = new List<string>();

        if (self.UseAdvancedFilter)
        {
            if (self.IllegalWordsSearch == null)
            {
                self.IllegalWordsSearch = new IllegalWordsSearch();
            }

            var matches = self.IllegalWordsSearch.FindAll(content);

            foreach (var match in matches)
            {
                result.Add(match.Keyword);
            }
        }
        else
        {
            if (self.WordsSearch == null)
            {
                self.WordsSearch = new WordsSearch();
            }

            var matches = self.WordsSearch.FindAll(content);

            foreach (var match in matches)
            {
                result.Add(match.Keyword);
            }
        }

        return result;
    }
    
    /// <summary>
    /// 获取文本中所有的敏感词
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <param name="result">敏感词结果</param>
    /// <returns></returns>
    public static void FindAllSensitiveWords(this SensitiveWordComponent self, string content, ref List<string> result)
    {
        if (result == null)
        {
            result = new List<string>();
        }

        if (self.UseAdvancedFilter)
        {
            if (self.IllegalWordsSearch == null)
            {
                self.IllegalWordsSearch = new IllegalWordsSearch();
            }

            var matches = self.IllegalWordsSearch.FindAll(content);

            foreach (var match in matches)
            {
                result.Add(match.Keyword);
            }
        }
        else
        {
            if (self.WordsSearch == null)
            {
                self.WordsSearch = new WordsSearch();
            }

            var matches = self.WordsSearch.FindAll(content);

            foreach (var match in matches)
            {
                result.Add(match.Keyword);
            }
        }
    }
}