static class JsonText
{
    private enum Kind
    {
        String,
        Punctuation,
        Literal
    }

    private readonly record struct Token(Kind Kind, int Start, int End, char Mark);

    public static string? RemoveProperty(string text, string key)
    {
        string? result = null;

        while (RemoveFirst(result ?? text, key) is string next)
            result = next;

        return result;
    }

    public static string? SetTopLevelProperty(string text, string key, string value)
    {
        var tokens = Tokenize(text);

        if (tokens.Count < 2 || tokens[0].Mark != '{' || tokens[^1].Mark != '}')
            return null;

        int keyIndex = FindTopLevelKey(text, tokens, key);

        if (keyIndex >= 0)
        {
            int last = ValueEnd(tokens, keyIndex + 2);
            return text[..tokens[keyIndex + 2].Start] + value + text[tokens[last].End..];
        }

        var before = tokens[^2];
        string comma = before.Mark is '{' or ',' ? "" : ",";
        string insertion = $"{comma}\n  {Quote(key)}: {value}";

        return text.Insert(before.End, insertion);
    }

    public static string IndentOfTopLevelKey(string text, string key)
    {
        var tokens = Tokenize(text);
        int keyIndex = FindTopLevelKey(text, tokens, key);

        if (keyIndex < 0)
            return "  ";

        int start = tokens[keyIndex].Start;
        int lineStart = LineStart(text, start);

        return IsBlank(text, lineStart, start) ? text[lineStart..start] : "  ";
    }

    private static string Quote(string value) => $"\"{value}\"";

    private static string? RemoveFirst(string text, string key)
    {
        var tokens = Tokenize(text);

        for (int i = 1; i < tokens.Count - 2; i++)
        {
            if (!IsKey(text, tokens, i, key))
                continue;

            int last = ValueEnd(tokens, i + 2);
            var (from, to) = RemovalRange(text, tokens, i, last);

            return text.Remove(from, to - from);
        }

        return null;
    }

    private static (int From, int To) RemovalRange(string text, List<Token> tokens, int keyIndex, int lastIndex)
    {
        int keyStart = tokens[keyIndex].Start;
        int valueEnd = tokens[lastIndex].End;
        var previous = tokens[keyIndex - 1];

        if (lastIndex + 1 < tokens.Count && tokens[lastIndex + 1].Mark == ',')
        {
            int afterComma = tokens[lastIndex + 1].End;
            int lineStart = LineStart(text, keyStart);
            int lineEnd = LineEnd(text, afterComma);

            if (IsBlank(text, lineStart, keyStart) && IsBlank(text, afterComma, lineEnd))
                return (lineStart, Math.Min(lineEnd + 1, text.Length));

            while (afterComma < text.Length && text[afterComma] is ' ' or '\t')
                afterComma++;

            return (keyStart, afterComma);
        }

        if (previous.Mark is ',' or '{' && IsBlank(text, previous.End, keyStart))
            return (previous.Mark == ',' ? previous.Start : previous.End, valueEnd);

        return (keyStart, valueEnd);
    }

    private static int FindTopLevelKey(string text, List<Token> tokens, string key)
    {
        int depth = 0;

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (token.Kind == Kind.Punctuation && token.Mark is '{' or '[')
                depth++;
            else if (token.Kind == Kind.Punctuation && token.Mark is '}' or ']')
                depth--;
            else if (depth == 1 && IsKey(text, tokens, i, key))
                return i;
        }

        return -1;
    }

    private static bool IsKey(string text, List<Token> tokens, int index, string key)
    {
        if (index < 1 || index + 2 >= tokens.Count || tokens[index].Kind != Kind.String)
            return false;

        var token = tokens[index];

        return text.AsSpan(token.Start + 1, token.End - token.Start - 2).SequenceEqual(key)
            && tokens[index - 1].Mark is '{' or ','
            && tokens[index + 1].Mark == ':';
    }

    private static int ValueEnd(List<Token> tokens, int start)
    {
        if (tokens[start].Kind != Kind.Punctuation || tokens[start].Mark is not ('{' or '['))
            return start;

        int depth = 0;

        for (int i = start; i < tokens.Count; i++)
        {
            if (tokens[i].Kind != Kind.Punctuation)
                continue;

            if (tokens[i].Mark is '{' or '[')
                depth++;
            else if (tokens[i].Mark is '}' or ']' && --depth == 0)
                return i;
        }

        return tokens.Count - 1;
    }

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                i = LineEnd(text, i);
            }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                int close = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close < 0 ? text.Length : close + 2;
            }
            else if (c == '"')
            {
                int end = EndOfString(text, i);
                tokens.Add(new Token(Kind.String, i, end, '"'));
                i = end;
            }
            else if ("{}[]:,".Contains(c))
            {
                tokens.Add(new Token(Kind.Punctuation, i, i + 1, c));
                i++;
            }
            else
            {
                int start = i;

                while (i < text.Length && !char.IsWhiteSpace(text[i]) && !"{}[]:,\"/".Contains(text[i]))
                    i++;

                if (i == start)
                    i++;

                tokens.Add(new Token(Kind.Literal, start, i, 'x'));
            }
        }

        return tokens;
    }

    private static int EndOfString(string text, int start)
    {
        int i = start + 1;

        while (i < text.Length)
        {
            if (text[i] == '\\')
                i += 2;
            else if (text[i] == '"')
                return i + 1;
            else
                i++;
        }

        return text.Length;
    }

    private static int LineStart(string text, int index) =>
        index == 0 ? 0 : text.LastIndexOf('\n', index - 1) + 1;

    private static int LineEnd(string text, int index)
    {
        int end = text.IndexOf('\n', index);

        return end < 0 ? text.Length : end;
    }

    private static bool IsBlank(string text, int from, int to)
    {
        for (int i = from; i < to; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
                return false;
        }

        return true;
    }
}
