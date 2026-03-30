using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GameLogic
{
    /// <summary>
    /// 高性能富文本解析器
    /// 支持: 颜色标签、图标、表情、链接
    /// </summary>
    public static class RichTextParser
    {
        private const string ColorStartTag = "<color=";
        private const string ColorEndTag = "</color>";
        private const int ColorStartTagLength = 7;
        private const int ColorEndTagLength = 8;

        // 链接格式: [link:id|text] 或 [link:id|text|color] 或 [link:id|text|color|style]
        private static readonly Regex s_linkRegex = new Regex(
            @"^\[link:(\d+)\|([^|\]]+)(?:\|([^|\]]+))?(?:\|([^|\]]+))?\]$",
            RegexOptions.Compiled);

        private static readonly List<RichTextElement> s_resultCache = new List<RichTextElement>(32);
        private static readonly Stack<string> s_colorStack = new Stack<string>(8);

        // 静态缓冲区，用于减少字符串分配
        private static readonly char[] s_charBuffer = new char[256];

        /// <summary>
        /// 解析富文本内容为元素列表
        /// </summary>
        public static List<RichTextElement> Parse(string text, string iconPrefix)
        {
            s_resultCache.Clear();
            s_colorStack.Clear();

            if (string.IsNullOrEmpty(text))
            {
                return s_resultCache;
            }

            // 仅在包含 Tab 时才进行替换，避免不必要的字符串分配
            if (text.Contains('\t'))
            {
                text = text.Replace("\t", "    ");
            }

            var currentElement = RichTextElement.Create(RichTextElementType.Text);
            int length = text.Length;
            int index = 0;

            while (index < length)
            {
                char c = text[index];

                // 检查颜色开始标签
                if (c == '<' && TryMatchColorStart(text, index, out int colorEndIndex, out string colorValue))
                {
                    FlushElement(currentElement);
                    s_colorStack.Push(colorValue);
                    currentElement = CreateTextElement(colorValue);
                    index = colorEndIndex + 1;
                    continue;
                }

                // 检查颜色结束标签
                if (c == '<' && TryMatchColorEnd(text, index))
                {
                    FlushElement(currentElement);

                    if (s_colorStack.Count > 0)
                    {
                        s_colorStack.Pop();
                    }

                    currentElement = CreateTextElementFromStack();
                    index += ColorEndTagLength;
                    continue;
                }

                // 检查特殊标签 [...]
                if (c == '[')
                {
                    int bracketEnd = FindClosingBracket(text, index);
                    if (bracketEnd != -1)
                    {
                        // 使用 Span 进行快速初步检查，避免不必要的字符串分配
                        ReadOnlySpan<char> tagSpan = text.AsSpan(index, bracketEnd - index + 1);

                        // 检查是否为表情 (使用 Span 快速检查)
                        if (RichTextConfig.IsEmojiSpan(tagSpan))
                        {
                            FlushElement(currentElement);
                            string tagContent = tagSpan.ToString();
                            s_resultCache.Add(RichTextElement.Create(RichTextElementType.Emoji, tagContent));
                            currentElement = CreateTextElementFromStack();
                            index = bracketEnd + 1;
                            continue;
                        }

                        // 检查是否为图标 (使用 Span 快速检查)
                        if (!string.IsNullOrEmpty(iconPrefix) && SpanContains(tagSpan, iconPrefix.AsSpan()))
                        {
                            FlushElement(currentElement);
                            string tagContent = tagSpan.ToString();
                            string spriteName = ExtractSpriteName(tagContent, iconPrefix);
                            s_resultCache.Add(RichTextElement.Create(RichTextElementType.Icon, spriteName));
                            currentElement = CreateTextElementFromStack();
                            index = bracketEnd + 1;
                            continue;
                        }

                        // 检查是否为链接 (使用 Span 快速初步检查)
                        if (IsLinkFast(tagSpan))
                        {
                            string tagContent = tagSpan.ToString();
                            if (s_linkRegex.IsMatch(tagContent))
                            {
                                FlushElement(currentElement);
                                s_resultCache.Add(RichTextElement.Create(RichTextElementType.Link, tagContent));
                                currentElement = CreateTextElementFromStack();
                                index = bracketEnd + 1;
                                continue;
                            }
                        }

                        // 不是有效标签，作为普通文本处理
                        // 使用 StringBuilder.Append(char[], int, int) 避免字符串分配
                        for (int i = index; i <= bracketEnd; i++)
                        {
                            currentElement.TextBuilder.Append(text[i]);
                        }
                        index = bracketEnd + 1;
                        continue;
                    }
                }

                // 普通字符
                currentElement.TextBuilder.Append(c);
                index++;
            }

            FlushElement(currentElement);
            RemoveEmptyElements();

            return s_resultCache;
        }

        private static bool TryMatchColorStart(string text, int startIndex, out int endIndex, out string colorValue)
        {
            endIndex = -1;
            colorValue = null;

            if (startIndex + ColorStartTagLength >= text.Length)
                return false;

            // 使用 Span 快速检查 "<color="
            ReadOnlySpan<char> textSpan = text.AsSpan(startIndex);
            if (!textSpan.StartsWith(ColorStartTag.AsSpan()))
                return false;

            // 查找结束符 '>'
            int closeIndex = -1;
            for (int i = startIndex + ColorStartTagLength; i < text.Length; i++)
            {
                if (text[i] == '>')
                {
                    closeIndex = i;
                    break;
                }
            }

            if (closeIndex == -1)
                return false;

            // 使用 Span 提取颜色值，仅在最后创建字符串
            int colorLength = closeIndex - startIndex - ColorStartTagLength;
            colorValue = text.AsSpan(startIndex + ColorStartTagLength, colorLength).ToString();
            endIndex = closeIndex;
            return true;
        }

        private static bool TryMatchColorEnd(string text, int startIndex)
        {
            if (startIndex + ColorEndTagLength > text.Length)
                return false;

            return text.AsSpan(startIndex).StartsWith(ColorEndTag.AsSpan());
        }

        private static int FindClosingBracket(string text, int startIndex)
        {
            for (int i = startIndex + 1; i < text.Length; i++)
            {
                if (text[i] == ']')
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 使用 Span 快速检查是否包含指定前缀 (避免创建子字符串)
        /// </summary>
        private static bool SpanContains(ReadOnlySpan<char> content, ReadOnlySpan<char> value)
        {
            return content.IndexOf(value) >= 0;
        }

        /// <summary>
        /// 使用 Span 快速检查是否可能是链接标签
        /// </summary>
        private static bool IsLinkFast(ReadOnlySpan<char> tagContent)
        {
            return tagContent.StartsWith("[link:".AsSpan());
        }

        private static string ExtractSpriteName(string tagContent, string iconPrefix)
        {
            // 使用 Span 跳过前导 '[' 和尾部 ']'
            ReadOnlySpan<char> content = tagContent.AsSpan();

            int start = 0;
            int end = content.Length;

            if (content.Length > 0 && content[0] == '[')
                start = 1;
            if (content.Length > 0 && content[content.Length - 1] == ']')
                end = content.Length - 1;

            content = content.Slice(start, end - start);

            // 查找并跳过 iconPrefix
            ReadOnlySpan<char> prefixSpan = iconPrefix.AsSpan();
            int prefixIndex = content.IndexOf(prefixSpan);
            if (prefixIndex >= 0)
            {
                // 移除 prefix
                if (prefixIndex == 0)
                {
                    content = content.Slice(prefixSpan.Length);
                }
                else
                {
                    // prefix 在中间，需要拼接
                    int bufferLength = content.Length - prefixSpan.Length;
                    if (bufferLength > 0 && bufferLength <= s_charBuffer.Length)
                    {
                        int writeIndex = 0;
                        content.Slice(0, prefixIndex).CopyTo(s_charBuffer.AsSpan(writeIndex));
                        writeIndex += prefixIndex;
                        content.Slice(prefixIndex + prefixSpan.Length).CopyTo(s_charBuffer.AsSpan(writeIndex));
                        return new string(s_charBuffer, 0, bufferLength);
                    }
                }
            }

            return content.ToString();
        }

        private static RichTextElement CreateTextElement(string colorValue)
        {
            return RichTextElement.Create(RichTextElementType.Text, colorValue);
        }

        private static RichTextElement CreateTextElementFromStack()
        {
            if (s_colorStack.Count > 0)
            {
                return RichTextElement.Create(RichTextElementType.Text, s_colorStack.Peek());
            }
            return RichTextElement.Create(RichTextElementType.Text);
        }

        private static void FlushElement(RichTextElement element)
        {
            if (element != null && element.TextBuilder.Length > 0)
            {
                s_resultCache.Add(element);
            }
            else
            {
                element?.Dispose();
            }
        }

        private static void RemoveEmptyElements()
        {
            for (int i = s_resultCache.Count - 1; i >= 0; i--)
            {
                var element = s_resultCache[i];
                if (element.Type == RichTextElementType.Text && element.TextBuilder.Length == 0)
                {
                    element.Dispose();
                    s_resultCache.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 解析链接标签，返回 LinkData 对象（使用对象池）
        /// </summary>
        public static LinkData ParseLink(string tagContent)
        {
            var match = s_linkRegex.Match(tagContent);
            if (!match.Success) return null;

            string color = match.Groups[3].Success && !string.IsNullOrEmpty(match.Groups[3].Value)
                ? match.Groups[3].Value
                : null;

            var style = RichTextLinkStyle.Normal;
            if (match.Groups[4].Success && !string.IsNullOrEmpty(match.Groups[4].Value))
            {
                string styleStr = match.Groups[4].Value;
                // 使用 OrdinalIgnoreCase 避免 ToLower 的字符串分配
                if (string.Equals(styleStr, "underline", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(styleStr, "u", StringComparison.OrdinalIgnoreCase))
                {
                    style = RichTextLinkStyle.Underline;
                }
            }

            return LinkData.Create(
                int.Parse(match.Groups[1].Value),
                match.Groups[2].Value,
                color,
                style
            );
        }
    }
}