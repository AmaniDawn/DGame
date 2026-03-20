using Fantasy.Entitas;
using ToolGood.Words;

#pragma warning disable CS0618 // 类型或成员已过时

namespace Fantasy;

public class SensitiveWordComponent : Entity
{
    public IllegalWordsSearch IllegalWordsSearch { get; set; }
    public WordsSearch WordsSearch { get; set; }
    private bool m_useAdvancedFilter = true;
    private bool m_useDBCcaseConverter = true;
    private bool m_useIgnoreCase = true;
    private bool m_useDuplicateWordFilter = true;
    private bool m_useSkipWordFilter = true;

    /// <summary>
    /// 是否使用高级过滤
    /// </summary>
    public bool UseAdvancedFilter
    {
        get => m_useAdvancedFilter;
        set
        {
            if (m_useAdvancedFilter != value)
            {
                m_useAdvancedFilter = value;

                if (m_useAdvancedFilter)
                {
                    IllegalWordsSearch = new IllegalWordsSearch();
                    WordsSearch = null;
                }
                else
                {
                    IllegalWordsSearch = null;
                    WordsSearch = new WordsSearch();
                }
            }
        }
    }

    /// <summary>
    /// 设置是否全角/半角自动转换
    /// </summary>
    public bool UseDBCcaseConverter
    {
        get => m_useDBCcaseConverter;
        set
        {
            if (m_useDBCcaseConverter != value)
            {
                m_useDBCcaseConverter = value;

                if (IllegalWordsSearch != null)
                {
                    IllegalWordsSearch.UseDBCcaseConverter = m_useDBCcaseConverter;
                }
            }
        }
    }

    /// <summary>
    /// 设置是否忽略大小写
    /// </summary>
    public bool UseIgnoreCase
    {
        get => m_useIgnoreCase;
        set
        {
            if (m_useIgnoreCase != value)
            {
                m_useIgnoreCase = value;

                if (IllegalWordsSearch != null)
                {
                    IllegalWordsSearch.UseIgnoreCase = m_useIgnoreCase;
                }
            }
        }
    }

    /// <summary>
    /// 设置是否自动识别重复字符
    /// </summary>
    public bool UseDuplicateWordFilter
    {
        get => m_useDuplicateWordFilter;
        set
        {
            if (m_useDuplicateWordFilter != value)
            {
                m_useDuplicateWordFilter = value;

                if (IllegalWordsSearch != null)
                {
                    IllegalWordsSearch.UseDuplicateWordFilter = m_useDuplicateWordFilter;
                }
            }
        }
    }

    /// <summary>
    /// 设置是否开启跳词检测
    /// </summary>
    public bool UseSkipWordFilter
    {
        get => m_useSkipWordFilter;
        set
        {
            if (m_useSkipWordFilter != value)
            {
                m_useSkipWordFilter = value;

                if (IllegalWordsSearch != null)
                {
                    IllegalWordsSearch.UseSkipWordFilter = m_useSkipWordFilter;
                }
            }
        }
    }

    /// <summary>
    /// 敏感词替换字符
    /// </summary>
    public char ReplaceChar { get; set; } = '*';
}