namespace Functorium.Adapters.Observabilities.Naming;

public static partial class ObservabilityNaming
{
    /// <summary>
    /// Span 이름 생성 유틸리티
    /// </summary>
    public static class Spans
    {
        /// <summary>
        /// Span 작업 이름을 생성합니다.
        /// </summary>
        /// <param name="layer">레이어 (application/adapter)</param>
        /// <param name="category">카테고리 (usecase/repository)</param>
        /// <param name="handler">핸들러 이름</param>
        /// <param name="method">메서드 이름</param>
        /// <returns>Span 이름 (예: application usecase CreateProductCommand.Handle)</returns>
        public static string OperationName(string layer, string category, string handler, string method) =>
            $"{layer} {category} {handler}.{method}";
    }
}
