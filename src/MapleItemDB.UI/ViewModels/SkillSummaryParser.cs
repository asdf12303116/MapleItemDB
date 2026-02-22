using System.Text;
using System.Text.RegularExpressions;

namespace MapleItemDB.UI.ViewModels;

/// <summary>
/// 技能公式计算器 — 解析并求值 WZ 中的 common 属性公式
/// 参考 WzComparerR2 的 Calculator 实现
/// 支持: +, -, *, /, u() (ceil), d() (floor), 变量 x (等级)
/// </summary>
internal static class SkillCalculator
{
    /// <summary>
    /// 解析公式字符串，以等级为参数 x 求值
    /// </summary>
    public static decimal Parse(string expression, decimal level)
    {
        try
        {
            var tokens = Tokenize(expression);
            var rpn = ToRPN(tokens);
            return Evaluate(rpn, level);
        }
        catch
        {
            return 0;
        }
    }

    private enum TokenType { Number, Id, Op, LParen, RParen, CallStart, CallEnd }
    private enum Tag { None, Unary, Call }

    private record Token(TokenType Type, string Value)
    {
        public Tag Tag { get; set; }
    }

    private static List<Token> Tokenize(string expr)
    {
        var tokens = new List<Token>();
        for (int i = 0; i < expr.Length; i++)
        {
            char c = expr[i];
            if (c is ' ' or '%') continue;
            if (c is '+' or '-' or '*' or '/') { tokens.Add(new(TokenType.Op, c.ToString())); continue; }
            if (c == '(') { tokens.Add(new(TokenType.LParen, "(")); continue; }
            if (c == ')') { tokens.Add(new(TokenType.RParen, ")")); continue; }
            if (c == ',') continue;

            if (char.IsDigit(c) || c == '.')
            {
                int start = i;
                while (i + 1 < expr.Length && (char.IsDigit(expr[i + 1]) || expr[i + 1] == '.')) i++;
                tokens.Add(new(TokenType.Number, expr[start..(i + 1)]));
            }
            else if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i + 1 < expr.Length && (char.IsLetterOrDigit(expr[i + 1]) || expr[i + 1] == '_')) i++;
                tokens.Add(new(TokenType.Id, expr[start..(i + 1)]));
            }
        }
        return tokens;
    }

    private static int Priority(Token t)
    {
        if (t.Tag == Tag.Unary) return 4;
        return t.Value switch { "+" or "-" => 1, "*" or "/" => 2, _ => 0 };
    }

    private static List<Token> ToRPN(List<Token> tokens)
    {
        var output = new List<Token>();
        var stack = new Stack<Token>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            switch (t.Type)
            {
                case TokenType.Number:
                    output.Add(t);
                    break;
                case TokenType.Id:
                    output.Add(t);
                    // 如果后面是左括号，标记为函数调用
                    if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.LParen)
                        tokens[i + 1] = tokens[i + 1] with { Tag = Tag.Call };
                    break;
                case TokenType.LParen:
                    stack.Push(t);
                    if (t.Tag == Tag.Call)
                        output.Add(new(TokenType.CallStart, ""));
                    break;
                case TokenType.RParen:
                    while (stack.Count > 0 && stack.Peek().Type != TokenType.LParen)
                        output.Add(stack.Pop());
                    if (stack.Count > 0)
                    {
                        var lp = stack.Pop();
                        if (lp.Tag == Tag.Call)
                            output.Add(new(TokenType.CallEnd, ""));
                    }
                    break;
                case TokenType.Op:
                    if (i == 0 || tokens[i - 1].Type is TokenType.LParen or TokenType.Op)
                        t = t with { Tag = Tag.Unary };
                    while (stack.Count > 0 && stack.Peek().Type == TokenType.Op && Priority(t) <= Priority(stack.Peek())
                           && !(t.Tag == Tag.Unary && stack.Peek().Tag == Tag.Unary))
                        output.Add(stack.Pop());
                    stack.Push(t);
                    break;
            }
        }
        while (stack.Count > 0)
            output.Add(stack.Pop());
        return output;
    }

    private static decimal Evaluate(List<Token> rpn, decimal x)
    {
        var stack = new Stack<object>();

        foreach (var t in rpn)
        {
            switch (t.Type)
            {
                case TokenType.Number:
                    stack.Push(decimal.Parse(t.Value));
                    break;
                case TokenType.Id:
                    if (t.Value == "x") stack.Push(x);
                    else if (t.Value == "u") stack.Push((Func<decimal, decimal>)Math.Ceiling);
                    else if (t.Value == "d") stack.Push((Func<decimal, decimal>)Math.Floor);
                    else stack.Push(0m); // 未知变量默认 0
                    break;
                case TokenType.Op:
                    if (t.Tag == Tag.Unary)
                    {
                        var a = Convert.ToDecimal(stack.Pop());
                        stack.Push(t.Value == "-" ? -a : a);
                    }
                    else
                    {
                        var b = Convert.ToDecimal(stack.Pop());
                        var a = Convert.ToDecimal(stack.Pop());
                        stack.Push(t.Value switch
                        {
                            "+" => a + b,
                            "-" => a - b,
                            "*" => a * b,
                            "/" => b != 0 ? a / b : 0m,
                            _ => 0m,
                        });
                    }
                    break;
                case TokenType.CallStart:
                    stack.Push(TokenType.CallStart);
                    break;
                case TokenType.CallEnd:
                    // 收集参数直到 CallStart
                    var args = new Stack<object>();
                    while (stack.Count > 0 && stack.Peek() is not TokenType)
                        args.Push(stack.Pop());
                    if (stack.Count > 0) stack.Pop(); // 移除 CallStart 标记
                    // 获取函数
                    if (stack.Count > 0 && stack.Peek() is Func<decimal, decimal> func && args.Count > 0)
                    {
                        stack.Pop();
                        stack.Push(func(Convert.ToDecimal(args.Pop())));
                    }
                    break;
            }
        }

        return stack.Count > 0 ? Convert.ToDecimal(stack.Pop()) : 0;
    }
}

/// <summary>
/// 技能描述解析器 — 解析 WZ 技能描述中的占位符和格式标记
/// 参考 WzComparerR2 的 SummaryParser 实现
/// </summary>
internal static class SkillSummaryParser
{
    /// <summary>
    /// 解析技能描述/h 模板，将 #property 占位符替换为实际值
    /// </summary>
    /// <param name="template">描述模板 (含 #property 占位符和 #c...# 颜色标签)</param>
    /// <param name="level">当前等级 (传入 Calculator 作为 x 参数)</param>
    /// <param name="commonProps">common 属性公式字典</param>
    public static string Resolve(string template, int level, Dictionary<string, string> commonProps)
    {
        if (string.IsNullOrEmpty(template)) return "";

        var sb = new StringBuilder();
        int idx = 0;
        bool inColor = false;

        while (idx < template.Length)
        {
            if (template[idx] == '#')
            {
                // 尝试匹配属性标识符: #[_A-Za-z][_A-Za-z0-9]*
                int end = idx + 1;
                int len = 0;
                while (end < template.Length)
                {
                    char ch = template[end];
                    if (ch == '_' || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z')
                        || (len > 0 && ch >= '0' && ch <= '9'))
                    {
                        len++;
                        end++;
                    }
                    else break;
                }

                // 优先匹配 common 属性 (最长匹配优先)
                string? matchedProp = null;
                string? matchedKey = null;
                int matchedLen = 0;
                if (commonProps.Count > 0)
                {
                    for (int i = len; i > 0; i--)
                    {
                        var key = template.Substring(idx + 1, i);
                        if (TryGetIgnoreCase(commonProps, key, out var prop))
                        {
                            matchedProp = prop;
                            matchedKey = key;
                            matchedLen = i;
                            break;
                        }
                    }
                }

                if (matchedProp != null)
                {
                    // 计算公式
                    try
                    {
                        var val = SkillCalculator.Parse(matchedProp, level);
                        // 特殊处理: cooltimeMS → 秒
                        if (matchedKey == "cooltimeMS")
                            sb.AppendFormat("{0:F2}", val / 1000);
                        else if (matchedKey!.EndsWith("PerM", StringComparison.Ordinal))
                            sb.AppendFormat("{0:F1}", val / 100);
                        else
                            sb.Append((int)val);
                    }
                    catch
                    {
                        sb.Append("?");
                    }
                    idx += matchedLen + 1;
                    continue;
                }

                // 匹配 #c...# 颜色标签
                if (inColor)
                {
                    // 关闭颜色: 单独的 #
                    inColor = false;
                    sb.Append('#'); // 保留 # 结束标记给 MapleTextHelper 解析
                    idx++;
                }
                else if (idx + 1 < template.Length && template[idx + 1] == 'c')
                {
                    // 开始颜色: #c
                    inColor = true;
                    sb.Append("#c");
                    idx += 2;
                }
                else if (len == 0 && idx + 1 < template.Length)
                {
                    // 省略 c 的颜色段
                    inColor = true;
                    sb.Append("#c");
                    idx++;
                }
                else
                {
                    // 无法匹配的属性: 尝试显示为数字或 0
                    var key = template.Substring(idx + 1, len);
                    if (Regex.IsMatch(key, @"^\d+$"))
                        sb.Append(key);
                    else
                        sb.Append('0');
                    idx += len + 1;
                }
            }
            else if (template[idx] == '\\')
            {
                // 转义字符
                if (idx + 1 < template.Length)
                {
                    switch (template[idx + 1])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': break; // 忽略 \r
                        case '\\': sb.Append('\\'); break;
                        case 'c': break; // \c 忽略
                        default: sb.Append(template[idx + 1]); break;
                    }
                    idx += 2;
                }
                else
                {
                    idx++;
                }
            }
            else
            {
                sb.Append(template[idx++]);
            }
        }

        return sb.ToString();
    }

    private static bool TryGetIgnoreCase(Dictionary<string, string> dict, string key, out string? value)
    {
        foreach (var kv in dict)
        {
            if (kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }
        value = null;
        return false;
    }
}
