namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 제네릭 타입에서 내부 타입을 추출하는 유틸리티 클래스
/// </summary>
internal static class TypeExtractor
{
    /// <summary>
    /// FinT&lt;A, B&gt; 형태에서 두 번째 타입 파라미터 B를 추출합니다.
    /// B는 제네릭 타입(예: List&lt;T&gt;)일 수 있으므로 중첩된 &lt;&gt; 처리를 지원합니다.
    /// </summary>
    /// <param name="returnType">추출할 타입 문자열 (예: "global::LanguageExt.FinT&lt;global::LanguageExt.IO, global::System.Collections.Generic.List&lt;DataResult&gt;&gt;")</param>
    /// <returns>추출된 타입 (예: "global::System.Collections.Generic.List&lt;DataResult&gt;")</returns>
    public static string ExtractSecondTypeParameter(string returnType)
    {
        // ===== 입출력 예제 =====
        //
        // 1. 단순 타입:
        //    Input:  "FinT<IO, string>"
        //    Output: "string"
        //
        // 2. 단순 제네릭 타입:
        //    Input:  "FinT<IO, List<int>>"
        //    Output: "List<int>"
        //
        // 3. 중첩된 제네릭 타입:
        //    Input:  "FinT<IO, Dictionary<string, List<int>>>"
        //    Output: "Dictionary<string, List<int>>"
        //
        // 4. Fully Qualified Name (실제 코드에서 생성되는 형태):
        //    Input:  "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.List<DataResult>>"
        //    Output: "global::System.Collections.Generic.List<DataResult>"
        //
        // 5. 다층 중첩 타입:
        //    Input:  "FinT<IO, Result<Data<User<string>>>>"
        //    Output: "Result<Data<User<string>>>"
        //
        // 6. 배열 타입:
        //    Input:  "FinT<IO, string[]>"
        //    Output: "string[]"
        //
        // 7. Nullable 타입:
        //    Input:  "FinT<IO, int?>"
        //    Output: "int?"
        //
        // 8. 튜플 타입:
        //    Input:  "FinT<IO, (string Name, int Age)>"
        //    Output: "(string Name, int Age)"
        //
        // 9. FinT가 없는 경우 (원본 반환):
        //    Input:  "string"
        //    Output: "string"
        //
        // 10. 공백 포함:
        //     Input:  "FinT<IO,  string  >"
        //     Output: "string"

        if (string.IsNullOrEmpty(returnType))
        {
            return returnType;
        }

        int finTStart = returnType.IndexOf("FinT<", StringComparison.Ordinal);
        if (finTStart == -1)
        {
            return returnType;
        }

        // FinT< 다음부터 시작
        int start = finTStart + 5;

        // 첫 번째 타입 파라미터(A)를 건너뛰기 위해 콤마 찾기
        int? commaIndex = FindFirstTypeParameterSeparator(returnType, start);

        if (!commaIndex.HasValue)
        {
            return returnType;
        }

        // 콤마 다음부터 시작 (공백 제거)
        start = SkipWhitespace(returnType, commaIndex.Value + 1);

        // 두 번째 타입 파라미터(B)의 끝 찾기
        int? end = FindTypeParameterEnd(returnType, start);

        if (!end.HasValue)
        {
            return returnType;
        }

        return returnType.Substring(start, end.Value - start).Trim();
    }

    /// <summary>
    /// 첫 번째 타입 파라미터와 두 번째 타입 파라미터를 구분하는 콤마의 위치를 찾습니다.
    /// 중첩된 제네릭 타입 내부의 콤마는 무시합니다.
    /// </summary>
    private static int? FindFirstTypeParameterSeparator(string text, int startIndex)
    {
        int bracketCount = 1; // FinT< 의 < 때문에 1로 시작

        for (int i = startIndex; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '<')
            {
                bracketCount++;
            }
            else if (c == '>')
            {
                bracketCount--;

                if (bracketCount == 0)
                {
                    // FinT의 끝에 도달했지만 콤마를 찾지 못함
                    return null;
                }
            }
            else if (c == ',' && bracketCount == 1)
            {
                // 첫 번째 레벨의 콤마를 찾음
                return i;
            }
        }

        return null;
    }

    /// <summary>
    /// 타입 파라미터의 끝 위치를 찾습니다.
    /// </summary>
    private static int? FindTypeParameterEnd(string text, int startIndex)
    {
        int bracketCount = 1; // 상위 FinT< 때문에 1로 시작

        for (int i = startIndex; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '<')
            {
                bracketCount++;
            }
            else if (c == '>')
            {
                bracketCount--;

                if (bracketCount == 0)
                {
                    return i;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 공백 문자를 건너뜁니다.
    /// </summary>
    private static int SkipWhitespace(string text, int startIndex)
    {
        while (startIndex < text.Length && char.IsWhiteSpace(text[startIndex]))
        {
            startIndex++;
        }

        return startIndex;
    }
}
