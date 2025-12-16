//using System.Collections.Generic;
//using System.Text;

//namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

///// <summary>
///// 코드 생성을 위한 StringBuilder 래퍼
///// 가독성 향상 및 일관된 들여쓰기를 제공합니다.
///// </summary>
//internal class CodeBuilder
//{
//    private readonly StringBuilder _sb;
//    private int _indentLevel;
//    private const string IndentString = "    "; // 4 spaces

//    public CodeBuilder()
//    {
//        _sb = new StringBuilder();
//        _indentLevel = 0;
//    }

//    /// <summary>
//    /// 현재 들여쓰기 레벨을 증가시킵니다.
//    /// </summary>
//    public CodeBuilder Indent()
//    {
//        _indentLevel++;
//        return this;
//    }

//    /// <summary>
//    /// 현재 들여쓰기 레벨을 감소시킵니다.
//    /// </summary>
//    public CodeBuilder Unindent()
//    {
//        if (_indentLevel > 0)
//        {
//            _indentLevel--;
//        }
//        return this;
//    }

//    /// <summary>
//    /// 들여쓰기를 적용하여 라인을 추가합니다.
//    /// </summary>
//    public CodeBuilder AppendLine(string line = "")
//    {
//        if (string.IsNullOrEmpty(line))
//        {
//            _sb.AppendLine();
//        }
//        else
//        {
//            _sb.Append(GetIndent());
//            _sb.AppendLine(line);
//        }
//        return this;
//    }

//    ///// <summary>
//    ///// 들여쓰기 없이 라인을 추가합니다.
//    ///// </summary>
//    //public CodeBuilder AppendRaw(string text)
//    //{
//    //    _sb.Append(text);
//    //    return this;
//    //}

//    ///// <summary>
//    ///// 여러 라인을 추가합니다.
//    ///// </summary>
//    //public CodeBuilder AppendLines(params string[] lines)
//    //{
//    //    foreach (var line in lines)
//    //    {
//    //        AppendLine(line);
//    //    }
//    //    return this;
//    //}

//    ///// <summary>
//    ///// 중괄호 블록을 시작합니다.
//    ///// </summary>
//    //public CodeBuilder OpenBrace()
//    //{
//    //    AppendLine("{");
//    //    Indent();
//    //    return this;
//    //}

//    ///// <summary>
//    ///// 중괄호 블록을 종료합니다.
//    ///// </summary>
//    //public CodeBuilder CloseBrace()
//    //{
//    //    Unindent();
//    //    AppendLine("}");
//    //    return this;
//    //}

//    ///// <summary>
//    ///// 파라미터 목록을 생성합니다.
//    ///// </summary>
//    //public CodeBuilder AppendParameters(
//    //    List<string> parameters,
//    //    bool includeTrailingComma = false)
//    //{
//    //    for (int i = 0; i < parameters.Count; i++)
//    //    {
//    //        var isLast = i == parameters.Count - 1;
//    //        var comma = (!isLast || includeTrailingComma) ? "," : "";
//    //        AppendLine($"{parameters[i]}{comma}");
//    //    }
//    //    return this;
//    //}

//    /// <summary>
//    /// 현재 들여쓰기 문자열을 반환합니다.
//    /// </summary>
//    private string GetIndent()
//    {
//        return new string(' ', _indentLevel * IndentString.Length);
//    }

//    /// <summary>
//    /// 생성된 코드를 문자열로 반환합니다.
//    /// </summary>
//    public override string ToString()
//    {
//        return _sb.ToString();
//    }

//    ///// <summary>
//    ///// 빈 라인을 추가합니다.
//    ///// </summary>
//    //public CodeBuilder EmptyLine()
//    //{
//    //    return AppendLine();
//    //}
//}
